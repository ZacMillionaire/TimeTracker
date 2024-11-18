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
}