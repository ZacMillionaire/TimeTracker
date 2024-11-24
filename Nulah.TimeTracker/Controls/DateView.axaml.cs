using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
				Date = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, x + 1),
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

		Selected = TimeEntrySummaries.First().Summaries.Select((x, i) => new TimeEntryDto()
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

public class DateViewModel : ViewModelBase
{
	private List<SummarisedTimeEntryDto> _timeEntrySummaries;
	private List<TimeEntryDto> _selected;

	public List<SummarisedTimeEntryDto> TimeEntrySummaries
	{
		get => _timeEntrySummaries;
		set => this.RaiseAndSetIfChanged(ref _timeEntrySummaries, value);
	}

	public List<TimeEntryDto> Selected
	{
		get => _selected;
		protected set => this.RaiseAndSetIfChanged(ref _selected, value);
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
				TimeEntrySummaries = _timeManager.GetEntrySummary();
				Selected = GetTimeEntriesForDate(DateTimeOffset.Now.Date, _timeManager);
			});
		}
	}

	private void DateSelected(SummarisedTimeEntryDto summarisedTimeEntryDto)
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				Selected = GetTimeEntriesForDate(new DateTimeOffset(summarisedTimeEntryDto.Date, TimeOnly.MinValue, DateTimeOffset.Now.Offset), _timeManager);
			});
		}
	}

	public void TimeEntryCreated(TimeEntryDto createdTimeEntry)
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
			});
		}
	}

	public void UpdateTimeEntry(TimeEntryDto createdTimeEntry)
	{
		Dispatcher.UIThread.Invoke(() =>
		{
		});
	}

	private List<TimeEntryDto> GetTimeEntriesForDate(DateTimeOffset date, TimeManager timeManager)
	{
		var timeEntries = timeManager.GetEntries(new TimeEntryQueryCriteria()
		{
			From = new DateTimeOffset(DateOnly.FromDateTime(date.Date), new TimeOnly(0, 0), date.Offset),
			To = new DateTimeOffset(DateOnly.FromDateTime(date.Date), new TimeOnly(23, 59), date.Offset),
		});

		return timeEntries
			.OrderByDescending(x => x.Start)
			.ToList();
	}
}