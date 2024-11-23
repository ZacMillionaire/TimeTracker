using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Nulah.TimeTracker.Domain.Models;
using Nulah.TimeTracker.Models;

namespace Nulah.TimeTracker.Controls;

public class TimeEntrySmall : TemplatedControl
{
	#region Routed Events

	private static readonly RoutedEvent<TimeEntryClickedEventArgs> OpenSelectedTimeEntryEvent =
		RoutedEvent.Register<TimeEntrySmall, TimeEntryClickedEventArgs>(nameof(OpenSelectedTimeEntry), RoutingStrategies.Bubble);

	public event EventHandler<TimeEntryClickedEventArgs> OpenSelectedTimeEntry
	{
		add => AddHandler(OpenSelectedTimeEntryEvent, value);
		remove => RemoveHandler(OpenSelectedTimeEntryEvent, value);
	}

	#endregion

	#region Styled Properties

	public static readonly StyledProperty<IBrush?> BackgroundHoverProperty
		= AvaloniaProperty.Register<TimeEntrySmall, IBrush?>(nameof(BackgroundHover));

	public IBrush? BackgroundHover
	{
		get => GetValue(BackgroundHoverProperty);
		set => SetValue(BackgroundHoverProperty, value);
	}

	#endregion

	#region Controls

	private Border? _container;

	internal Border? Container
	{
		get => _container;
		set
		{
			if (_container != null)
			{
				_container.PointerReleased -= OpenSelectedTimeEntryEventInternal;
			}

			_container = value;

			if (_container != null)
			{
				_container.PointerReleased += OpenSelectedTimeEntryEventInternal;
			}
		}
	}

	#endregion

	private void OpenSelectedTimeEntryEventInternal(object? sender, PointerReleasedEventArgs e)
	{
		RaiseEvent(new TimeEntryClickedEventArgs(OpenSelectedTimeEntryEvent, e, DataContext as TimeEntryDto));
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		Container = e.NameScope.Find<Border>("TimeEntryBorder") ?? throw new Exception("Container border not found");
		base.OnApplyTemplate(e);
	}
}