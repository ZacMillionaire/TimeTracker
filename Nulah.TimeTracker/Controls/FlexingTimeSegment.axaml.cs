using System;
using Avalonia;
using Avalonia.Controls;
using Nulah.TimeTracker.Data;

namespace Nulah.TimeTracker.Controls;

public partial class FlexingTimeSegment : UserControl
{
	public static readonly StyledProperty<TimeEntrySummaryDto?> TimeEntrySummaryProperty
		= AvaloniaProperty.Register<FlexingTimeSegment, TimeEntrySummaryDto?>(nameof(TimeEntrySummary));

	public TimeEntrySummaryDto? TimeEntrySummary
	{
		get => GetValue(TimeEntrySummaryProperty);
		set => SetValue(TimeEntrySummaryProperty, value);
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

		// TODO: this method gets called for every property change so definitely look at how to
		// improve this so it only gets called once all properties we care about are available
		if (change.Property != TotalBoundaryProperty || TimeEntrySummary?.Duration == null)
		{
			return;
		}

		var percent = TimeEntrySummary.Duration.Value.Ticks / (double)TotalDuration.Ticks;
		DisplayWidth = percent * TotalBoundary.Right;
	}
}