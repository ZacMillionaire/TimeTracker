using Nulah.TimeTracker.Data;
using ReactiveUI;

namespace Nulah.TimeTracker.ViewModels;

public class SummarisedTimeEntryViewModel : ViewModelBase
{
	private bool _selected;

	public bool Selected
	{
		get => _selected;
		set => this.RaiseAndSetIfChanged(ref _selected, value);
	}

	public SummarisedTimeEntryDto SummarisedTimeEntryDto { get; init; }

	internal SummarisedTimeEntryViewModel(SummarisedTimeEntryDto summarisedTimeEntryDto)
	{
		SummarisedTimeEntryDto = summarisedTimeEntryDto;
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