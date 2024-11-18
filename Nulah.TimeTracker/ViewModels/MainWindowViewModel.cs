using ReactiveUI;

namespace Nulah.TimeTracker.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	private ReactiveObject _windowContent;

	public ReactiveObject WindowContent
	{
		get => _windowContent;
		set => this.RaiseAndSetIfChanged(ref _windowContent, value);
	}

	public MainWindowViewModel()
	{
		WindowContent = new TimeEntryCreateEditViewModel();
	}
}