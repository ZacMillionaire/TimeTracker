using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Nulah.TimeTracker.Converters;

public class LightenColourConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is uint packedColour)
		{
			return new SolidColorBrush(packedColour)
			{
				Opacity = 0.05
			};
		}

		return null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}