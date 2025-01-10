using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using Nulah.TimeTracker.Core;
using Nulah.TimeTracker.Data;
using Nulah.TimeTracker.Data.Criteria;
using Nulah.TimeTracker.Domain.Models;
using Nulah.TimeTracker.Models.Enums;
using ReactiveUI;
using Splat;

namespace Nulah.TimeTracker.ViewModels;

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

	public SummarisedTimeEntryViewModel? SelectedTimeSummary
	{
		get => _selectedTimeSummary;
		set => this.RaiseAndSetIfChanged(ref _selectedTimeSummary, value);
	}

	private bool _isEnabled = true;

	public bool IsEnabled
	{
		get => _isEnabled;
		set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
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
			.SortAndBind(
				out _selectedDateTimeEntriesBinding,
				SortExpressionComparer<TimeEntryDto>.Ascending(x => x.Start)
			)
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
				IsEnabled = false;

				LoadWeekFromDate(DateTimeOffset.Now);

				LoadSelectedTimeEntriesForDate(DateTimeOffset.Now.Date, _timeManager);

				UpdateSelectedSummary(LoadedWeekSummaryCache.Lookup(DateTimeOffset.Now.Date).Value);

				IsEnabled = true;
			});
		}
	}

	/// <summary>
	/// Loads the time entry summary for the week containing the given date
	/// </summary>
	/// <param name="selectedDate"></param>
	public void LoadWeekFromDate(DateTimeOffset selectedDate)
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				IsEnabled = false;

				LoadWeekFromDateInternal(selectedDate, _timeManager);
				// Load the time entries for the day
				LoadSelectedTimeEntriesForDate(selectedDate, _timeManager);

				// Then set the selected time summary
				UpdateSelectedSummary(LoadedWeekSummaryCache.Lookup(selectedDate.Date).Value);

				IsEnabled = true;
			});
		}
	}

	/// <summary>
	/// Updates the week summary and loaded time entries based on the start date of the created time entry
	/// </summary>
	/// <param name="createdTimeEntry"></param>
	public void TimeEntryCreated(TimeEntryDto createdTimeEntry)
	{
		if (_timeManager != null)
		{
			TimeEntryModified(createdTimeEntry, TimeEntryModifyAction.Created);
		}
	}

	/// <summary>
	/// Updates <see cref="SelectedDateTimeEntriesCache"/> based on the incoming <paramref name="updatedTimeEntryDto"/>,
	/// and refreshes the date summary it belongs to.
	/// </summary>
	/// <param name="updatedTimeEntryDto"></param>
	public void TimeEntryUpdated(TimeEntryDto updatedTimeEntryDto)
	{
		if (_timeManager != null)
		{
			TimeEntryModified(updatedTimeEntryDto, TimeEntryModifyAction.Updated);
		}
	}

	private void TimeEntryModified(TimeEntryDto createdTimeEntry, TimeEntryModifyAction modifyAction)
	{
		Dispatcher.UIThread.Invoke(() =>
		{
			IsEnabled = false;

			// Do we have a weekday summary selected, and would it contain our new entry?
			if (_selectedTimeSummary != null && _selectedTimeSummary.SummarisedTimeEntryDto.Date != createdTimeEntry.Start.Date)
			{
				// Store the previously selected date in the event we're updating the date on a time entry
				var previouslySelectedTimeSummary = _selectedTimeSummary.SummarisedTimeEntryDto.Date;

				// The currently selected summary isn't for this time entry, find if any other loaded weekday would contain this time entry
				var matchingTimeSummary = LoadedWeekSummaryCache.Lookup(createdTimeEntry.Start.Date);

				// If it would, set it to the current selected, and add it to what we have displayed
				if (matchingTimeSummary.HasValue)
				{
					// Load the time entries for the date we're switching to
					LoadSelectedTimeEntriesForDate(createdTimeEntry.Start, _timeManager);

					// Update the target summary date to reflect the new entry
					UpdateTimeEntrySummaryForDate(createdTimeEntry.Start, _timeManager);

					// Update the previously selected time entry to ensure the previous time entry summary is updated with any changes
					UpdateTimeEntrySummaryForDate(previouslySelectedTimeSummary, _timeManager);

					// Set the selected time summary to the existing one if we're adding a new entry
					if (modifyAction == TimeEntryModifyAction.Created)
					{
						UpdateSelectedSummary(matchingTimeSummary.Value);
					}
				}
				else
				{
					// We don't have a week for the new time entry loaded, either it's a new date entry outside of our range
					// or a date very far in the future.
					// Load the time entries for the date of the created time entry
					LoadWeekFromDateInternal(createdTimeEntry.Start, _timeManager);

					// Load the time entries for the day
					LoadSelectedTimeEntriesForDate(createdTimeEntry.Start, _timeManager);

					// Then set the selected time summary
					UpdateSelectedSummary(LoadedWeekSummaryCache.Lookup(createdTimeEntry.Start.Date).Value);
				}
			}
			else
			{
				AddOrUpdateEntryInCurrentSelectedDate(createdTimeEntry);
				// We created a time entry for the current selected time summary but for now we reload the entire
				// day as we may have somehow added in additional dates from elsewhere (the database can be updated
				// freely currently as we don't hold an exclusive lock on it when the app is running)
				UpdateTimeEntrySummaryForDate(createdTimeEntry.Start, _timeManager);

				// TODO: create a selected date duration property because I'm currently too lazy to refactor a view model
				// for summaries just yet (I should probably do that anyway though)
			}

			IsEnabled = true;
		});
	}

	/// <summary>
	/// Loads the time entry summary for the week containing the given date
	/// </summary>
	/// <param name="date"></param>
	/// <param name="timeManager"></param>
	private void LoadWeekFromDateInternal(DateTimeOffset date, TimeManager timeManager)
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
				UpdateSelectedSummary(summarisedTimeEntryViewModel);
				if (summarisedTimeEntryViewModel != null)
				{
					LoadSelectedTimeEntriesForDate(new DateTimeOffset(summarisedTimeEntryViewModel.SummarisedTimeEntryDto.Date, DateTimeOffset.Now.Offset), _timeManager);
				}
			});
		}
	}

	/// <summary>
	/// Updates the selected weekday summary to the given value, if it is not null,
	/// also sets <see cref="SummarisedTimeEntryViewModel.Selected"/> to true
	/// </summary>
	/// <param name="summarisedTimeEntryViewModel"></param>
	private void UpdateSelectedSummary(SummarisedTimeEntryViewModel? summarisedTimeEntryViewModel)
	{
		// clear the selected state on the previous value
		if (SelectedTimeSummary != null)
		{
			SelectedTimeSummary.Selected = false;
		}

		SelectedTimeSummary = summarisedTimeEntryViewModel;

		// If we're setting the selected value a not-null value, set selected to true
		if (SelectedTimeSummary != null)
		{
			SelectedTimeSummary.Selected = true;
		}
	}

	/// <summary>
	/// Adds or updates a time entry in the current selected time entry. If <paramref name="timeEntryDto"/> is not for the
	/// selected date, nothing happens.
	/// </summary>
	/// <param name="timeEntryDto"></param>
	private void AddOrUpdateEntryInCurrentSelectedDate(TimeEntryDto timeEntryDto)
	{
		// Do nothing if the selected time summary would not contain the incoming time entry, or we don't have a selected
		// summary
		if (_selectedTimeSummary == null || _selectedTimeSummary.SummarisedTimeEntryDto.Date != timeEntryDto.Start.Date)
		{
			return;
		}

		// Load the time entries for the given date into the cache
		SelectedDateTimeEntriesCache.AddOrUpdate(timeEntryDto);
	}

	/// <summary>
	/// Loads all <see cref="TimeEntryDto"/>'s for the given date into <see cref="SelectedDateTimeEntriesCache"/>.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="timeManager"></param>
	private void LoadSelectedTimeEntriesForDate(DateTimeOffset start, TimeManager timeManager)
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
	/// Loads a summary of time entries between a start and end date, and updates <see cref="LoadedWeekSummaryCache"/>.
	/// <para>
	/// Dates with no time entries will be a summary with no <see cref="TimeEntrySummaryDto"/>.
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
	/// Updates a loaded weekday summary by the given <paramref name="date"/> by loading from the database.
	/// If no matching weekday summary is found no action is performed. 
	/// </summary>
	/// <param name="date"></param>
	/// <param name="timeManager"></param>
	private void UpdateTimeEntrySummaryForDate(DateTimeOffset date, TimeManager timeManager)
	{
		var loadedSummary = LoadedWeekSummaryCache.Lookup(date.Date);

		// Don't attempt to update a summary for a date that isn't loaded.
		if (!loadedSummary.HasValue)
		{
			return;
		}

		// set to start of day to only capture time entries for today
		// TODO: this can probably be improved
		var startOfDate = date.Add(-date.TimeOfDay);

		// Load the summaries for the given date
		var summaries = timeManager.GetEntrySummary(new TimeEntryQueryCriteria()
		{
			From = startOfDate,
			To = startOfDate.Add(new TimeSpan(23, 59, 59)),
		});

		// If we get a single value, update the value in the loaded week. Will throw otherwise.
		// TODO: do nothing if the date param does not exist in the loaded week summary
		if (summaries.SingleOrDefault() is { } foundSummary)
		{
			loadedSummary.Value.SummarisedTimeEntryDto.Summaries = foundSummary.Summaries;
		}
		else
		{
			// Clear the summaries for the summarised time entry as there are no longer any time entries associated to it
			loadedSummary.Value.SummarisedTimeEntryDto.Summaries = [];
		}

		loadedSummary.Value.Selected = true;
		LoadedWeekSummaryCache.AddOrUpdate(loadedSummary.Value);
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