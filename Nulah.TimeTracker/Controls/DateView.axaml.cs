using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Nulah.TimeTracker.Domain.Models;
using Nulah.TimeTracker.ViewModels;
using ReactiveUI;

namespace Nulah.TimeTracker.Controls;

public partial class DateView : ReactiveUserControl<DateViewModel>
{
	public DateView()
	{
		InitializeComponent();
	}
}

public class DateViewModel : ViewModelBase
{
	public List<DateGroup> Dates { get; set; }

	private DateGroup _selected;

	public DateGroup Selected
	{
		get => _selected;
		private set => this.RaiseAndSetIfChanged(ref _selected, value);
	}

	public ReactiveCommand<DateGroup, Unit> SelectDateCommand { get; private set; }

	public DateViewModel()
	{
		SelectDateCommand = ReactiveCommand.Create<DateGroup>((DateGroup selectedDateGroup) =>
		{
			Selected = selectedDateGroup;
		});


		var r = new Random();


		List<string?> descriptions =
		[
			"test text test text test text test text test text test text",
			"Short description",
			"Slightly longer description",
			null,
			"1",
			"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco"
		];

		Dates = Enumerable.Range(0, 7)
			.Select(x => new DateGroup
			{
				Date = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, x + 1),
				Entries = Enumerable.Range(0, r.Next(1, 7))
					.Select(y => new TimeEntryDto
					{
						Name = $"Name {y}",
						Description = descriptions[r.Next(0, descriptions.Count)],
						Start = DateTimeOffset.Now.Add(TimeSpan.FromHours(-1 * r.Next(0, 3))),
						End = DateTimeOffset.Now.Add(TimeSpan.FromHours(r.Next(1, 2))),
						Colour = Color.FromRgb(
								(byte)r.Next(0, 255),
								(byte)r.Next(0, 255),
								(byte)r.Next(0, 255)
							)
							.ToUInt32()
					})
					.ToList()
			})
			.ToList();

		Selected = Dates.First();
	}
}