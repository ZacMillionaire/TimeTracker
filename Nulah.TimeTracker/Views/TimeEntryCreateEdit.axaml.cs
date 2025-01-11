using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Nulah.TimeTracker.Domain.Models;
using Nulah.TimeTracker.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace Nulah.TimeTracker.Views;

public partial class TimeEntryCreateEdit : ReactiveUserControl<TimeEntryCreateEditViewModel>
{
	public TimeEntryCreateEdit()
	{
		InitializeComponent();

		this.WhenActivated(disposables =>
		{
			this.BindValidation(ViewModel,
					vm => vm.Duration,
					v => v.StartEndValidationError.Text)
				.DisposeWith(disposables);
		});
	}

	private void AutoCompleteBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (sender is not AutoCompleteBox { SelectedItem: TimeEntrySearchAggregatedSuggestion s })
		{
			return;
		}

		Console.WriteLine($"{DateTime.Now:o} Selection changed");
		ViewModel?.SetNameAndColourFromSelectedSuggestion(s);
		e.Handled = true;
	}
}