using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Nulah.TimeTracker.Core;
using Nulah.TimeTracker.Data;
using Nulah.TimeTracker.Data.Criteria;
using Nulah.TimeTracker.Domain.Models;
using Nulah.TimeTracker.Models;
using Nulah.TimeTracker.ViewModels;
using ReactiveUI;
using Splat;

namespace Nulah.TimeTracker.Controls;

public partial class DateView : ReactiveUserControl<DateViewModel>
{
	public DateView()
	{
		InitializeComponent();
	}

	private void OpenSelectedTimeEntry(object? sender, TimeEntryClickedEventArgs e)
	{
		if (e.TimeEntry != null)
		{
			ViewModel?.TimeEntrySelected.Invoke(e.TimeEntry.Id);
		}
	}

	private void Calendar_OnSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count > 0 && e.AddedItems[0] is DateTime selectedDate)
		{
			// I would love to hide the flyout on click but it breaks the calandar in a way
			// that then causes any mouse over a date to raise SelectedDatesChanged.
			// I've no idea if it's a bug or what but whatever I'll deal with this later when/if I give a shit
			// No combination of e.handled, or dispatching to the UIThread, or sleep threading or whatever will do it.
			// WeekSelectorButton.Flyout?.Hide();
			ViewModel?.SelectWeekFromDate(selectedDate);
		}
	}
}

public class DateViewModel : ViewModelBase
{
	private List<SummarisedTimeEntryDto> _timeEntrySummaries;
	private List<TimeEntryDto> _selectedDateTimeEntries;
	private SummarisedTimeEntryDto _selectedTimeSummary;
	private Dictionary<int, TimeEntryDto> _selectedDateTimeEntriesIndex = new();

	public List<SummarisedTimeEntryDto> TimeEntrySummaries
	{
		get => _timeEntrySummaries;
		set => this.RaiseAndSetIfChanged(ref _timeEntrySummaries, value);
	}

	public List<TimeEntryDto> SelectedDateTimeEntries
	{
		get => _selectedDateTimeEntries;
		protected set => this.RaiseAndSetIfChanged(ref _selectedDateTimeEntries, value);
	}

	// todo change this to a datetime as we don't use it in the xaml
	public SummarisedTimeEntryDto SelectedTimeSummary
	{
		get => _selectedTimeSummary;
		set => this.RaiseAndSetIfChanged(ref _selectedTimeSummary, value);
	}

	public ReactiveCommand<SummarisedTimeEntryDto, Unit> SelectDateCommand { get; private set; }

	public Action<int> TimeEntrySelected { get; init; } = (int timeEntryId) =>
	{
	};

	private readonly TimeManager? _timeManager;

	public DateViewModel(TimeManager? timeManager = null)
	{
		SelectDateCommand = ReactiveCommand.Create<SummarisedTimeEntryDto>(DateSelected);

		_timeManager = timeManager ?? Locator.Current.GetService<TimeManager>();

		GetTimeSummaries();
	}

	public void GetTimeSummaries()
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				// TODO: maybe filter out summaries with duration under a certain time that would be too small to see
				// Under 15 min if we have more than 5 entries maybe? Hard to determine what should/shouldn't be culled

				GetTimeEntrySummariesForRange(DateTimeOffset.Now.Date.AddDays(-3), DateTimeOffset.Now.Date.AddDays(4), _timeManager);

				// TODO: change this to null and handle the first time use having no time entries added
				SelectedTimeSummary = TimeEntrySummaries.LastOrDefault() ?? new();
				SetSelectedTimeEntriesForDate(DateTimeOffset.Now.Date, _timeManager);
			});
		}
	}

	public void SelectWeekFromDate(DateTimeOffset selectedDate)
	{
		if (_timeManager != null)
		{
			var startOfWeek = GetStartOfWeek(selectedDate);
			GetTimeEntrySummariesForRange(startOfWeek, startOfWeek.AddDays(7), _timeManager);
		}
	}

	private DateTimeOffset GetStartOfWeek(DateTimeOffset fromDate)
	{
		var diff = (7 + (fromDate.DayOfWeek - Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek)) % 7;
		return fromDate.AddDays(-1 * diff).Date;
	}

	private void DateSelected(SummarisedTimeEntryDto summarisedTimeEntryDto)
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				SelectedTimeSummary = summarisedTimeEntryDto;
				SetSelectedTimeEntriesForDate(new DateTimeOffset(summarisedTimeEntryDto.Date, DateTimeOffset.Now.Offset), _timeManager);
			});
		}
	}

	public void TimeEntryCreated(TimeEntryDto createdTimeEntry)
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				if (SelectedTimeSummary.Date != createdTimeEntry.Start.Date)
				{
					var matchingTimeSummary = TimeEntrySummaries
						.FirstOrDefault(x => x.Date == createdTimeEntry.Start.Date);

					if (matchingTimeSummary != null)
					{
						SelectedTimeSummary = matchingTimeSummary;
						// Refresh the summaries as we're already in the loaded set
						SetSelectedTimeEntriesForDate(createdTimeEntry.Start, _timeManager);

						// TODO: have this simply update the relevant matchingTimeSummary and insert/remove/update the relevant time summary
						// TODO: update GetTimeEntrySummariesForRange to update indexes and replace this line with a refresh on a single summary by getting the summary for that single date
						TimeEntrySummaries = _timeManager.GetEntrySummary();
					}
					else
					{
						// we don't have the current date loaded, either it's a new date entry outside of our range
						// or a date very far in the future.
						// Either way at this stage we should have at least 1 entry for the given date, so we select a spread
						// around the incoming entry and effectively reload the summaries
						GetTimeEntrySummariesForRange(
							createdTimeEntry.Start.Date.AddDays(-3),
							createdTimeEntry.Start.Date.AddDays(4),
							_timeManager
						);

						SelectedTimeSummary = TimeEntrySummaries
							.First(x => x.Date == createdTimeEntry.Start.Date);
					}
				}
				else
				{
					SetSelectedTimeEntriesForDate(createdTimeEntry.Start, _timeManager);
					// Refresh the summaries as we're already in the loaded set
					// TODO: have this simply update the relevant matching time summary and insert/remove/update the relevant time summary
					TimeEntrySummaries = _timeManager.GetEntrySummary();
				}
			});
		}
	}

	/// <summary>
	/// Loads all <see cref="TimeEntryDto"/>'s for the given date into <see cref="SelectedDateTimeEntries"/>, and builds the index
	/// for each.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="timeManager"></param>
	private void SetSelectedTimeEntriesForDate(DateTimeOffset start, TimeManager timeManager)
	{
		SelectedDateTimeEntries = GetTimeEntriesForDate(start, timeManager);
		if (_selectedDateTimeEntries.Count > 0)
		{
			_selectedDateTimeEntriesIndex = _selectedDateTimeEntries.ToDictionary(x => x.Id, x => x);
			var a = _selectedDateTimeEntries[0].Equals(_selectedDateTimeEntriesIndex[_selectedDateTimeEntries[0].Id]);
		}
	}

	/// <summary>
	/// Loads a summary of time entries between a start and end date, and sets <see cref="TimeEntrySummaries"/>.
	/// <para>
	/// Dates with no time entries will be a summary with no <see cref="TimeEntrySummaryDto"/>
	/// </para>
	/// </summary>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <param name="timeManager"></param>
	private void GetTimeEntrySummariesForRange(DateTimeOffset start, DateTimeOffset end, TimeManager timeManager)
	{
		var summaries = timeManager.GetEntrySummary(new TimeEntryQueryCriteria()
		{
			From = start,
			To = end,
		});

		var populatedSummaries = Enumerable.Range(0, (end - start).Days)
			.Select(offset =>
			{
				var date = start.AddDays(offset);
				if (summaries.FirstOrDefault(x => x.Date == date) is { } matchingSummary)
				{
					return matchingSummary;
				}

				return new SummarisedTimeEntryDto()
				{
					Date = date.Date,
					Summaries = []
				};
			})
			.ToList();

		TimeEntrySummaries = populatedSummaries;
	}

	public void UpdateTimeEntry(TimeEntryDto createdTimeEntry)
	{
		Dispatcher.UIThread.Invoke(() =>
		{
			// TODO: update similar to how create does
		});
	}

	private List<TimeEntryDto> GetTimeEntriesForDate(DateTimeOffset date, TimeManager timeManager)
	{
		var timeEntries = timeManager.GetEntries(new TimeEntryQueryCriteria()
		{
			From = new DateTimeOffset(DateOnly.FromDateTime(date.Date), new TimeOnly(0, 0), date.Offset),
			To = new DateTimeOffset(DateOnly.FromDateTime(date.Date), new TimeOnly(23, 59), date.Offset),
		});

		// TODO: make sure these are ordering correctly by start, eg, PM times are at the bottom
		return timeEntries
			.OrderBy(x => x.Start)
			.ToList();
	}
}

public class DateViewDesignModel : DateViewModel
{
	public DateViewDesignModel()
	{
		// ReactiveCommand.Create<DateGroup>(selectedDateGroup =>
		// {
		// 	Selected = selectedDateGroup;
		// });

		var r = new Random();

		List<string?> descriptions =
		[
			"test text test text test text test text test text test text",
			"Short description",
			"Slightly longer description",
			null,
			"1",
			"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco"
		];

		var timeEntries = Enumerable.Range(0, 6)
			.Select(x => new TimeEntryDto()
			{
				Name = $"Name {x}",
				Description = "test text test text test text test text test text test text",
				Start = DateTimeOffset.Now.Add(TimeSpan.FromHours(-1)),
				End = DateTimeOffset.Now.Add(TimeSpan.FromHours(2)),
				Colour = Color.FromRgb((byte)(1 + x * 50), (byte)(1 + x * 30), (byte)(1 + x * 10)).ToUInt32()
			})
			.ToList();

		TimeEntrySummaries = Enumerable.Range(0, 7)
			.Select(x => new SummarisedTimeEntryDto
			{
				Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, x + 1),
				Summaries = Enumerable.Range(0, r.Next(1, 7))
					.Select(y => new TimeEntrySummaryDto()
					{
						Duration = DateTimeOffset.Now.Add(TimeSpan.FromHours(r.Next(1, 2))) - DateTimeOffset.Now.Add(TimeSpan.FromHours(-1 * r.Next(0, 3))),
						Colour = Color.FromRgb(
								(byte)r.Next(0, 255),
								(byte)r.Next(0, 255),
								(byte)r.Next(0, 255)
							)
							.ToUInt32()
					})
					.ToList()
			})
			.ToList();

		SelectedDateTimeEntries = TimeEntrySummaries.First().Summaries.Select((x, i) => new TimeEntryDto()
			{
				Name = $"Name {i}",
				Description = descriptions[r.Next(0, descriptions.Count)],
				Start = DateTimeOffset.Now.Add(TimeSpan.FromHours(-1)),
				End = DateTimeOffset.Now.Add(TimeSpan.FromHours(2)),
				Colour = x.Colour
			})
			.ToList();
	}
}