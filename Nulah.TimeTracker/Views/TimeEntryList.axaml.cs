using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Nulah.TimeTracker.Domain.Models;
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

	private void OpenSelectedTimeEntry(object? sender, PointerReleasedEventArgs e)
	{
		if (sender is StyledElement { DataContext: TimeEntryDto timeEntryDto })
		{
			ViewModel?.TimeEntrySelected.Invoke(timeEntryDto.Id);
		}
	}
}