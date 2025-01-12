using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;

namespace Nulah.TimeTracker.Views;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
	}

	private void SettingsButton_OnPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		var settingsWindow = new SettingsWindow()
		{
			// DataContext = new MainWindowViewModel()
			// {
			// 	AppVersion = GetVersion(),
			// },
			ShowInTaskbar = false,
			CanResize = false,
			ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.SystemChrome,
			ExtendClientAreaToDecorationsHint = true,
			ExtendClientAreaTitleBarHeightHint = 0,
			SystemDecorations = SystemDecorations.BorderOnly,
		};
		settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		settingsWindow.ShowDialog(this);
	}
}