using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Nulah.TimeTracker.Domain.Models;
using Nulah.TimeTracker.ViewModels;

namespace Nulah.TimeTracker.Converters;

public class BorderWidthConverter : IMultiValueConverter
{
	public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values[0] is TimeEntryDto { End: not null } timeEntryDto
		    && values[1] is TimeSpan { Ticks: not 0} duration
		    && values[2] is Rect { Right: not 0 } boundary)
		{
			var percent = (timeEntryDto.End.Value.Ticks - timeEntryDto.Start.Ticks) / (double)duration.Ticks;
			return percent * boundary.Right;
		}

		// throw new NotImplementedException();
		return (double)0;
	}
}