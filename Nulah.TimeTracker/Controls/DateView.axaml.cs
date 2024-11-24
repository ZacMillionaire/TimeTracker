using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Nulah.TimeTracker.Domain.Models;
using Nulah.TimeTracker.ViewModels;

namespace Nulah.TimeTracker.Controls;

public partial class DateView : UserControl
{
	public DateView()
	{
		InitializeComponent();
	}
}

public class DateViewModel : ViewModelBase
{
	public List<DateGroup> Dates { get; set; }
	
	public DateGroup Selected { get; set; }

	public DateViewModel()
	{
		var r = new Random();
		Dates = Enumerable.Range(0, 7)
			.Select(x => new DateGroup
			{
				Date = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, x + 1),
				Entries = Enumerable.Range(0,r.Next(1,7))
					.Select(y => new TimeEntryDto
					{
						Name = $"Name {y}",
						Description = "test text test text test text test text test text test text",
						Start = DateTimeOffset.Now.Add(TimeSpan.FromHours(-1 * r.Next(0,3))),
						End = DateTimeOffset.Now.Add(TimeSpan.FromHours(r.Next(1,2))),
						Colour = Color.FromRgb(
								(byte)r.Next(0,255),
								(byte)r.Next(0,255),
								(byte)r.Next(0,255)
							)
							.ToUInt32()
					})
					.ToList()
			})
			.ToList();

		Selected = Dates.First();
	}
}