using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Nulah.TimeTracker.Domain.Models;

namespace Nulah.TimeTracker.Controls;

public partial class FlexingTimeSegment : UserControl
{
	public static readonly StyledProperty<TimeEntryDto> TimeEntryProperty
		= AvaloniaProperty.Register<FlexingTimeSegment, TimeEntryDto>(nameof(TimeEntry), new()
		{
			Colour = Color.FromRgb(50, 40, 30).ToUInt32()
		});

	public TimeEntryDto TimeEntry
	{
		get => GetValue(TimeEntryProperty);
		set => SetValue(TimeEntryProperty, value);
	}

	public static readonly StyledProperty<TimeSpan> TotalDurationProperty
		= AvaloniaProperty.Register<FlexingTimeSegment, TimeSpan>(nameof(TotalDuration), TimeSpan.FromHours(2));

	public TimeSpan TotalDuration
	{
		get => GetValue(TotalDurationProperty);
		set => SetValue(TotalDurationProperty, value);
	}

	public static readonly StyledProperty<Rect> TotalBoundaryProperty
		= AvaloniaProperty.Register<FlexingTimeSegment, Rect>(nameof(TotalBoundary));

	public Rect TotalBoundary
	{
		get => GetValue(TotalBoundaryProperty);
		set => SetValue(TotalBoundaryProperty, value);
	}

	public static readonly DirectProperty<FlexingTimeSegment, double> DisplayWidthProperty =
		AvaloniaProperty.RegisterDirect<FlexingTimeSegment, double>(nameof(DisplayWidth), o => o.DisplayWidth);

	private double _displayWidth;

	public double DisplayWidth
	{
		get => _displayWidth;
		private set => SetAndRaise(DisplayWidthProperty, ref _displayWidth, value);
	}

	public FlexingTimeSegment()
	{
		InitializeComponent();
	}


	// TODO: wrap this in another controller that passes in the boundary once it gets it maybe?
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property != TotalBoundaryProperty || TimeEntry.End == null)
		{
			return;
		}
		
		var percent = (TimeEntry.End.Value.Ticks - TimeEntry.Start.Ticks) / (double)TotalDuration.Ticks;
		DisplayWidth = percent * TotalBoundary.Right;
	}
}