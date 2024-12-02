using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
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
		// TODO: implement the checks that sender is an auto complete box, and that the added items of e
		// has any items, and the first one is an aggregate search suggestion
		// then send that off to the view model to populate colour and name based on selection
		// also update autocombo box to bind the the name property on the aggregate search type
	}
}