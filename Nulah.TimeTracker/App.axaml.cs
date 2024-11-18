using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Nulah.TimeTracker.Core;
using Nulah.TimeTracker.Data;
using Nulah.TimeTracker.ViewModels;
using Nulah.TimeTracker.Views;
using Splat;

namespace Nulah.TimeTracker;

public partial class App : Application
{
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
				DataContext = new MainWindowViewModel(),
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
	
	private static void AddCommonServices()
	{
		var dataLocation = Path.Join(AppContext.BaseDirectory, "data");
		Directory.CreateDirectory(dataLocation);
		
		Locator.CurrentMutable.RegisterConstant<TimeTrackerRepository>(new TimeTrackerRepository(Path.Join(dataLocation,"app.db")));
		Locator.CurrentMutable.RegisterConstant<TimeManager>(new TimeManager(GetOrThrowOnServiceNull<TimeTrackerRepository>()));
	}

	private static TService GetOrThrowOnServiceNull<TService>()
	{
		return Locator.Current.GetService<TService>() ?? throw new Exception($"{typeof(TService)} not registered");
	}
}