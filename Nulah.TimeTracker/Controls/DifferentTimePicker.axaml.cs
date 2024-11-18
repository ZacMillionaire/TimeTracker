using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Nulah.TimeTracker.Controls;

public class DifferentTimePicker : TemplatedControl
{
	private static readonly List<string> Hours = Enumerable.Range(00, 12)
		.Select(i => DateTime.MinValue.AddHours(i).ToString("HH"))
		.ToList();
	private static readonly List<string> Minutes = Enumerable.Range(00, 59)
		.Select(i => DateTime.MinValue.AddMinutes(i).ToString("mm"))
		.ToList();
	
	public static readonly StyledProperty<List<string>> HourPickerProperty
		= AvaloniaProperty.Register<DifferentTimePicker, List<string>>(nameof(HourPicker));

	public List<string> HourPicker
	{
		get => GetValue(HourPickerProperty);
		private set => SetValue(HourPickerProperty, value);
	}
	
	
	public static readonly StyledProperty<List<string>> MinutePickerProperty
		= AvaloniaProperty.Register<DifferentTimePicker, List<string>>(nameof(MinutePicker));

	public List<string> MinutePicker
	{
		get => GetValue(MinutePickerProperty);
		private set => SetValue(MinutePickerProperty, value);
	}

	public DifferentTimePicker()
	{
		HourPicker = Hours;
		MinutePicker = Minutes;
	}
}