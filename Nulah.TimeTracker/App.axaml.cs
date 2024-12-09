using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Nulah.TimeTracker.Core;
using Nulah.TimeTracker.Data;
using Nulah.TimeTracker.ViewModels;
using Nulah.TimeTracker.Views;
using Splat;

namespace Nulah.TimeTracker;

public partial class App : Application
{
	private DispatcherTimer _diagnosticTimer;

	public override void Initialize()
	{
		if (!Design.IsDesignMode)
		{
			AddCommonServices();
		}

		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainWindowViewModel()
				{
					AppVersion = GetVersion(),
				},
			};

			GetMemoryUsage(desktop.MainWindow.DataContext, EventArgs.Empty);

			_diagnosticTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(5)
			};
			_diagnosticTimer.Tick += (s, e) => GetMemoryUsage(desktop.MainWindow.DataContext, EventArgs.Empty);
			_diagnosticTimer.Start();
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void GetMemoryUsage(object? sender, EventArgs e)
	{
		if (sender is not MainWindowViewModel mainWindowViewModel)
		{
			return;
		}

		using var proc = Process.GetCurrentProcess();
		// var a = new
		// {
		// 	ws = proc.WorkingSet64/1024.0/1024.0,
		// 	vm = proc.VirtualMemorySize64/1024.0/1024.0,
		// 	pm = proc.PagedMemorySize64/1024.0/1024.0,
		// 	prvm = proc.PrivateMemorySize64/1024.0/1024.0,
		// 	sm = proc.PagedSystemMemorySize64/1024.0/1024.0
		// };
		mainWindowViewModel.Memory = $"Mem: {proc.PrivateMemorySize64 / 1024.0 / 1024.0:F2}MB";
	}

	private string? GetVersion()
	{
		return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
	}

	private static void AddCommonServices()
	{
		var dataLocation = Path.Join(AppContext.BaseDirectory, "data");
		Directory.CreateDirectory(dataLocation);

		Locator.CurrentMutable.RegisterConstant<TimeTrackerRepository>(new TimeTrackerRepository(Path.Join(dataLocation, "app.db")));
		Locator.CurrentMutable.RegisterConstant<TimeManager>(new TimeManager(GetOrThrowOnServiceNull<TimeTrackerRepository>()));
	}

	private static TService GetOrThrowOnServiceNull<TService>()
	{
		return Locator.Current.GetService<TService>() ?? throw new Exception($"{typeof(TService)} not registered");
	}
}