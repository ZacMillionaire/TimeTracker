using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Nulah.TimeTracker.Data;
using Splat;

namespace Nulah.TimeTracker.Views;

public partial class SettingsWindow : Window
{
	private readonly TimeTrackerRepository _timeTrackerRepository;
	
	public SettingsWindow()
	{
		InitializeComponent();
		// TODO: being lazy and just pulling this in here, but also: it's totally fine this is a desktop app
		_timeTrackerRepository = Locator.Current.GetService<TimeTrackerRepository>();
		// TODO: bind this to a view model
		LastIndexDate.Text = _timeTrackerRepository.GetLastIndexRebuildDate()?.ToString("f");
	}
	
	private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		Close();
	}

	private void ReindexButton_OnClick(object? sender, RoutedEventArgs e)
	{
		LastIndexDate.Text = _timeTrackerRepository.RebuildIndex().ToString("f");
	}
}