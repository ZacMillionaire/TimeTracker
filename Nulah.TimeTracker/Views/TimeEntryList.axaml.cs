using Avalonia;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Nulah.TimeTracker.Models;
using Nulah.TimeTracker.ViewModels;

namespace Nulah.TimeTracker.Views;

public partial class TimeEntryList : ReactiveUserControl<TimeEntryListViewModel>
{
	public TimeEntryList()
	{
		InitializeComponent();
	}

	private void ToggleExpandDateGroup(object? sender, PointerReleasedEventArgs e)
	{
		if (sender is StyledElement { DataContext: DateGroup dateGroup })
		{
			dateGroup.Expanded = !dateGroup.Expanded;
		}
	}

	private void OpenSelectedTimeEntry(object? sender, TimeEntryClickedEventArgs e)
	{
		if (e.TimeEntry != null)
		{
			ViewModel?.TimeEntrySelected.Invoke(e.TimeEntry.Id);
		}
	}
}