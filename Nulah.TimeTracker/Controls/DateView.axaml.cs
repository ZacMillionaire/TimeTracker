using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Nulah.TimeTracker.Models;
using Nulah.TimeTracker.ViewModels;

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

	private void WeekPicker_OnSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count > 0 && e.AddedItems[0] is DateTime selectedDate)
		{
			// TODO: redesign the UI for this because flyouts with controls feel super buggy.
			// Might mean recreating a popup for the calendar specifically
			ViewModel?.LoadWeekFromDate(selectedDate);
		}
	}

	private void WeekSelectorButton_OnClick(object? sender, RoutedEventArgs e)
	{
		(Resources["CalendarFlyout"] as Flyout)?.ShowAt(WeekSummaryContainer);
	}

	private void CloseCalendarFlyout_OnClick(object? sender, RoutedEventArgs e)
	{
		(Resources["CalendarFlyout"] as Flyout)?.Hide();
	}
}