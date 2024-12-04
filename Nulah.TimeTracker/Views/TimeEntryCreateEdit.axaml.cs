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
		// TODO: reimplement this as not an auto complete box because it raises selection events once the asyncpopulator
		// returns.
		// It's dumb and I don't care to work around it
		if (sender is AutoCompleteBox && e.AddedItems is [TimeEntrySearchAggregatedSuggestion selectedSuggestion])
		{
			ViewModel?.SetNameAndColourFromSelectedSuggestion(selectedSuggestion);
			e.Handled = true;
		}
	}
}