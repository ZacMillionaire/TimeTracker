using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using DynamicData;
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

// TODO: This class definitely feels like it does too much and I should look at brreaking this up a bit so it triggers
// updates within time lists rather than their respective and attached view models.
public class DateViewModel : ViewModelBase
{
	private SummarisedTimeEntryViewModel? _selectedTimeSummary;

	/// <summary>
	/// Maintains the source for time entries for the selected date
	/// </summary>
	protected readonly SourceCache<TimeEntryDto, int> SelectedDateTimeEntriesCache = new(x => x.Id);

	/// <summary>
	/// Maintains the source for the summarised week
	/// </summary>
	protected readonly SourceCache<SummarisedTimeEntryViewModel, DateTime> LoadedWeekSummaryCache = new(x => x.SummarisedTimeEntryDto.Date);

	/// <summary>
	/// Backing field for <see cref="SelectedDateTimeEntries"/> bound within the constructor from <see cref="SelectedDateTimeEntriesCache"/>
	/// </summary>
	private readonly ReadOnlyObservableCollection<TimeEntryDto> _selectedDateTimeEntriesBinding;

	/// <summary>
	/// Backing field for <see cref="TimeEntrySummaries"/> bound within the constructor from <see cref="LoadedWeekSummaryCache"/>
	/// </summary>
	private readonly ReadOnlyObservableCollection<SummarisedTimeEntryViewModel> _loadedWeekSummaryBinding;

	public ReadOnlyObservableCollection<TimeEntryDto> SelectedDateTimeEntries => _selectedDateTimeEntriesBinding;
	public ReadOnlyObservableCollection<SummarisedTimeEntryViewModel> TimeEntrySummaries => _loadedWeekSummaryBinding;

	// todo change this to a datetime as we don't use it in the xaml and probably private
	// TODO: set the active property on this when selected and add styling to it
	public SummarisedTimeEntryViewModel? SelectedTimeSummary
	{
		get => _selectedTimeSummary;
		set => this.RaiseAndSetIfChanged(ref _selectedTimeSummary, value);
	}

	public ReactiveCommand<SummarisedTimeEntryViewModel, Unit> SelectDateCommand { get; private set; }

	public Action<int> TimeEntrySelected { get; init; } = (int timeEntryId) =>
	{
	};

	private readonly TimeManager? _timeManager;

	public DateViewModel(TimeManager? timeManager = null)
	{
		SelectDateCommand = ReactiveCommand.Create<SummarisedTimeEntryViewModel>(DateSelected);

		_timeManager = timeManager ?? Locator.Current.GetService<TimeManager>();

		SelectedDateTimeEntriesCache
			.Connect()
			.DeferUntilLoaded()
			.Bind(out _selectedDateTimeEntriesBinding)
			.Subscribe();

		LoadedWeekSummaryCache
			.Connect()
			.DeferUntilLoaded()
			.Bind(out _loadedWeekSummaryBinding)
			.Subscribe();

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

				SelectWeekFromDate(DateTimeOffset.Now);

				// TODO: change this to null and handle the first time use having no time entries added
				SelectedTimeSummary = TimeEntrySummaries.LastOrDefault();
				SetSelectedTimeEntriesForDate(DateTimeOffset.Now.Date, _timeManager);
			});
		}
	}

	/// <summary>
	/// Loads the time entry summary for the week containing the given date
	/// </summary>
	/// <param name="selectedDate"></param>
	public void SelectWeekFromDate(DateTimeOffset selectedDate)
	{
		if (_timeManager != null)
		{
			SelectWeekFromDateInternal(selectedDate, _timeManager);
		}
	}

	/// <summary>
	/// Loads the time entry summary for the week containing the given date
	/// </summary>
	/// <param name="date"></param>
	/// <param name="timeManager"></param>
	private void SelectWeekFromDateInternal(DateTimeOffset date, TimeManager timeManager)
	{
		var startOfWeek = GetStartOfWeek(date);
		GetTimeEntrySummariesForRange(startOfWeek, startOfWeek.AddDays(7), timeManager);
	}

	private DateTimeOffset GetStartOfWeek(DateTimeOffset fromDate)
	{
		var diff = (7 + (fromDate.DayOfWeek - Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek)) % 7;
		return fromDate.AddDays(-1 * diff).Date;
	}

	/// <summary>
	/// Sets <see cref="SelectedTimeSummary"/> to the given value, and if not null, loads all time entries for the given date
	/// </summary>
	/// <param name="summarisedTimeEntryViewModel"></param>
	private void DateSelected(SummarisedTimeEntryViewModel? summarisedTimeEntryViewModel = null)
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				SelectedTimeSummary = summarisedTimeEntryViewModel;
				if (summarisedTimeEntryViewModel != null)
				{
					SetSelectedTimeEntriesForDate(new DateTimeOffset(summarisedTimeEntryViewModel.SummarisedTimeEntryDto.Date, DateTimeOffset.Now.Offset), _timeManager);
				}
			});
		}
	}

	public void TimeEntryCreated(TimeEntryDto createdTimeEntry)
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				// Is the current selected time date the same as time entry we just created
				if (SelectedTimeSummary != null && SelectedTimeSummary.SummarisedTimeEntryDto.Date != createdTimeEntry.Start.Date)
				{
					// Do we have a time entry summary for this date loaded?
					var matchingTimeSummary = TimeEntrySummaries
						.FirstOrDefault(x => x.SummarisedTimeEntryDto.Date == createdTimeEntry.Start.Date);

					// If we do, set the selected time summary to the existing one
					if (matchingTimeSummary != null)
					{
						SelectedTimeSummary = matchingTimeSummary;
						// Refresh the summaries as we're already in the loaded set
						SetSelectedTimeEntriesForDate(createdTimeEntry.Start, _timeManager);

						// currently this refreshes the entire week summary for the created entry date
						// TODO: move time entry summaries to the same as selected date time entries and use a source cache
						// TODO: have this simply update the relevant matchingTimeSummary and insert/remove/update the relevant time summary
						// TODO: update GetTimeEntrySummariesForRange to update indexes and replace this line with a refresh on a single summary by getting the summary for that single date
						// TODO: definitely change this to a source cache now that I'm also creating view models around the dtos
						// TODO: have this simply update the relevant matching time summary and insert/remove/update the relevant time summary
						// TODO: same as above, update this once backed by a source cache
						SelectWeekFromDateInternal(createdTimeEntry.Start, _timeManager);
					}
					else
					{
						// we don't have the current date loaded, either it's a new date entry outside of our range
						// or a date very far in the future.
						SelectWeekFromDateInternal(createdTimeEntry.Start, _timeManager);
						// Load the time entries for the date of the created time entry
						SetSelectedTimeEntriesForDate(createdTimeEntry.Start, _timeManager);

						// Set the selected time summary
						SelectedTimeSummary = TimeEntrySummaries
							.First(x => x.SummarisedTimeEntryDto.Date == createdTimeEntry.Start.Date);
					}
				}
				else
				{
					// We created a time entry for the current selected time summary but for now we reload the entire
					// day as we may have somehow added in additional dates from elsewhere (the database can be updated
					// freely currently as we don't hold an exclusive lock on it when the app is running)
					SetSelectedTimeEntriesForDate(createdTimeEntry.Start, _timeManager);
					// Refresh the summaries as we're already in the loaded set
					// TODO: move time entry summaries to the same as selected date time entries and use a source cache
					// TODO: have this simply update the relevant matchingTimeSummary and insert/remove/update the relevant time summary
					// TODO: update GetTimeEntrySummariesForRange to update indexes and replace this line with a refresh on a single summary by getting the summary for that single date
					// TODO: definitely change this to a source cache now that I'm also creating view models around the dtos
					// TODO: have this simply update the relevant matching time summary and insert/remove/update the relevant time summary
					// TODO: same as above, update this once backed by a source cache
					SelectWeekFromDateInternal(createdTimeEntry.Start, _timeManager);
				}
			});
		}
	}

	/// <summary>
	/// Updates <see cref="SelectedDateTimeEntriesCache"/> based on the incoming <paramref name="updatedTimeEntryDto"/>,
	/// and refreshes the date summary it belongs to.
	/// <para>
	///	This method assumes that the updating time entry belongs to the selected summary. If it does not in the future
	/// then this may cause unintended behaviours and cause the selected summary to drift out of sync
	/// </para>
	/// </summary>
	/// <param name="updatedTimeEntryDto"></param>
	public void UpdateTimeEntry(TimeEntryDto updatedTimeEntryDto)
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				// TODO: check that the currently selected summary belongs to the updating time entry. If it doesn't, do not
				// update the cache
				SelectedDateTimeEntriesCache.AddOrUpdate(updatedTimeEntryDto);
				// lazy refresh the date summaries
				SelectWeekFromDateInternal(updatedTimeEntryDto.Start, _timeManager);
			});
		}
	}

	/// <summary>
	/// Loads all <see cref="TimeEntryDto"/>'s for the given date into <see cref="SelectedDateTimeEntriesCache"/>.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="timeManager"></param>
	private void SetSelectedTimeEntriesForDate(DateTimeOffset start, TimeManager timeManager)
	{
		// Load the time entries for the given date into the cache
		SelectedDateTimeEntriesCache.Edit(cache =>
		{
			// Completely replace the contents of the cache in an edit to (hopefully) only raise a single
			// property notify.
			// Or maybe Load doesn't raise property notifies which is why its only available on ISourceUpdater<>?
			// I have no idea, the DynamicData library has _sweet fuck all_ documentation.
			cache.Load(GetTimeEntriesForDate(start, timeManager));
		});
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
				// do we have a time summary returned? If we do return it, otherwise return an empty one
				if (summaries.FirstOrDefault(x => x.Date == date) is { } matchingSummary)
				{
					return new SummarisedTimeEntryViewModel(matchingSummary);
				}

				return new SummarisedTimeEntryViewModel(
					new()
					{
						Date = date.Date,
						Summaries = []
					}
				);
			})
			.ToList();

		LoadedWeekSummaryCache.Edit(cache =>
		{
			cache.Load(populatedSummaries);
		});
	}

	/// <summary>
	/// Returns any time entries for the given date, ordered by their start date descending
	/// </summary>
	/// <param name="date"></param>
	/// <param name="timeManager"></param>
	/// <returns></returns>
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

		List<string> names =
		[
			"short name",
			"a",
			"somewhat longer name but not that long",
			"lmao, lol even Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco"
		];

		LoadedWeekSummaryCache.Edit(cache =>
		{
			var pretendSummarisedTimeEntryViewModels = Enumerable.Range(0, 7)
				.Select(x => new SummarisedTimeEntryViewModel(new SummarisedTimeEntryDto
				{
					Date = DateTimeOffset.Now.AddDays(-3 + x).Date,
					Summaries = Enumerable.Range(0, 6)
						.Select(y => new TimeEntrySummaryDto
						{
							Duration = new TimeSpan(0, r.Next(0, 180), 0),
							Colour = Color.FromRgb(
								(byte)r.Next(0, 255),
								(byte)r.Next(0, 255),
								(byte)r.Next(0, 255)
							).ToUInt32()
						})
						.ToList()
				}))
				.ToList();
			cache.Load(pretendSummarisedTimeEntryViewModels);
		});

		SelectedTimeSummary = TimeEntrySummaries.First();
		SelectedTimeSummary.Selected = true;

		var id = 1;
		SelectedDateTimeEntriesCache.Edit(cache =>
		{
			var pretendEntries = SelectedTimeSummary.SummarisedTimeEntryDto.Summaries
				.Select(x => new TimeEntryDto
				{
					Id = id++,
					Colour = x.Colour,
					Name = names[r.Next(0, names.Count)],
					Description = descriptions[r.Next(0, descriptions.Count)],
					Start = DateTimeOffset.Now,
					End = DateTimeOffset.Now.Add(x.Duration!.Value),
				});
			cache.Load(pretendEntries);
		});
	}
}