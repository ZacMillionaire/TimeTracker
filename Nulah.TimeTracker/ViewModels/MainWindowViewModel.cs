using System;
using ReactiveUI;

namespace Nulah.TimeTracker.ViewModels;

public class MainWindowViewModelDesignTime : MainWindowViewModel
{
	public MainWindowViewModelDesignTime()
	{
		TimeEntryListViewModel = new TimeEntryListViewModelDesignTime();
		WindowContent = new TimeEntryCreateEditViewModel();
		Memory = "123.4MB";
		Console.WriteLine("design view");
	}
}

public class MainWindowViewModel : ViewModelBase
{
	private ReactiveObject? _windowContent;
	private TimeEntryListViewModel? _timeEntryListViewModel;
	private string? _memory;

	public ReactiveObject? WindowContent
	{
		get => _windowContent;
		set => this.RaiseAndSetIfChanged(ref _windowContent, value);
	}

	public TimeEntryListViewModel? TimeEntryListViewModel
	{
		get => _timeEntryListViewModel;
		set => this.RaiseAndSetIfChanged(ref _timeEntryListViewModel, value);
	}

	public string? Memory
	{
		get => _memory;
		set => this.RaiseAndSetIfChanged(ref _memory, value);
	}

	public MainWindowViewModel()
	{
		WindowContent = new TimeEntryCreateEditViewModel()
		{
			TimeEntryCreated = ReloadTimeEntries
		};

		TimeEntryListViewModel = new TimeEntryListViewModel()
		{
			TimeEntrySelected = LoadTimeEntryForEdit
		};
	}

	private void ReloadTimeEntries(int createdTask)
	{
		TimeEntryListViewModel?.GetTimeEntries();
	}

	private void LoadTimeEntryForEdit(int selectedTimeEntryId)
	{
		if (_windowContent is TimeEntryCreateEditViewModel editCreateViewModel)
		{
			editCreateViewModel.LoadTimeEntry(selectedTimeEntryId);
		}
	}
}