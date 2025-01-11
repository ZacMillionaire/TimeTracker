using System;
using Nulah.TimeTracker.Data;
using ReactiveUI;

namespace Nulah.TimeTracker.ViewModels;

// TODO: create a selected time entry view model here for use with updating durations
public class SummarisedTimeEntryViewModel : ViewModelBase
{
	private bool _selected;

	public bool Selected
	{
		get => _selected;
		set => this.RaiseAndSetIfChanged(ref _selected, value);
	}

	private TimeSpan _totalDuration;

	public TimeSpan TotalDuration
	{
		get => _totalDuration;
		set => this.RaiseAndSetIfChanged(ref _totalDuration, value);
	}

	public SummarisedTimeEntryDto SummarisedTimeEntryDto { get; init; }

	internal SummarisedTimeEntryViewModel(SummarisedTimeEntryDto summarisedTimeEntryDto)
	{
		SummarisedTimeEntryDto = summarisedTimeEntryDto;
		// hacky quick solution to get duration updating when summaries change
		TotalDuration = summarisedTimeEntryDto.DurationWithExclusions;
	}
}

public static class SummarisedTimeEntryViewModelExtensions
{
	public static SummarisedTimeEntryViewModel? ToViewModel(this SummarisedTimeEntryDto? dto)
	{
		return dto == null
			? default
			: new SummarisedTimeEntryViewModel(dto);
	}
}