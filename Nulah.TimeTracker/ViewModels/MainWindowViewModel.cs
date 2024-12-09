using System;
using Nulah.TimeTracker.Controls;
using Nulah.TimeTracker.Domain.Models;
using ReactiveUI;

namespace Nulah.TimeTracker.ViewModels;

public class MainWindowViewModelDesignTime : MainWindowViewModel
{
	public MainWindowViewModelDesignTime()
	{
		WindowContent = new TimeEntryCreateEditViewModel();
		Memory = "123.4MB";
	}
}

public class MainWindowViewModel : ViewModelBase
{
	private ReactiveObject? _windowContent;
	private string? _memory;

	public ReactiveObject? WindowContent
	{
		get => _windowContent;
		set => this.RaiseAndSetIfChanged(ref _windowContent, value);
	}

	private DateViewModel? _dateViewModel;

	public DateViewModel? DateViewModel
	{
		get => _dateViewModel;
		set => this.RaiseAndSetIfChanged(ref _dateViewModel, value);
	}

	public string? Memory
	{
		get => _memory;
		set => this.RaiseAndSetIfChanged(ref _memory, value);
	}

	private string? _appVersion;

	public string? AppVersion
	{
		get => _appVersion;
		set => this.RaiseAndSetIfChanged(ref _appVersion, value);
	}

	public MainWindowViewModel()
	{
		WindowContent = new TimeEntryCreateEditViewModel()
		{
			TimeEntryCreated = TimeEntryCreated,
			TimeEntryActioned = TimeEntryUpdated,
		};

		DateViewModel = new()
		{
			TimeEntrySelected = LoadTimeEntryForEdit,
		};
	}

	private void TimeEntryCreated(TimeEntryDto createdTimeEntry)
	{
		DateViewModel?.TimeEntryCreated(createdTimeEntry);
	}

	private void TimeEntryUpdated(TimeEntryDto updatedTimeEntry)
	{
		DateViewModel?.UpdateTimeEntry(updatedTimeEntry);
	}

	private void LoadTimeEntryForEdit(int selectedTimeEntryId)
	{
		if (_windowContent is TimeEntryCreateEditViewModel editCreateViewModel)
		{
			editCreateViewModel.LoadTimeEntry(selectedTimeEntryId);
		}
	}
}