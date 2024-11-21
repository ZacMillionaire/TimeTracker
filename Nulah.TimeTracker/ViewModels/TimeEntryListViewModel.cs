using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using Nulah.TimeTracker.Core;
using Nulah.TimeTracker.Domain.Models;
using ReactiveUI;
using Splat;

namespace Nulah.TimeTracker.ViewModels;

public class TimeEntryListViewModelDesignTime : TimeEntryListViewModel
{
	public TimeEntryListViewModelDesignTime()
	{
		EntryGroups = Enumerable.Range(0, 15)
			.Select(y =>
			{
				var now = DateTimeOffset.Now.Add(TimeSpan.FromDays(y));
				return new DateGroup()
				{
					Date = new DateOnly(now.Year, now.Month, now.Day),
					Entries = Enumerable.Range(0, 6)
						.Select(x => new TimeEntryDto()
						{
							Name = $"Name {x}",
							Description = "test text test text test text test text test text test text",
							Start = DateTimeOffset.Now.Add(TimeSpan.FromHours(-1)),
							End = DateTimeOffset.Now.Add(TimeSpan.FromHours(2)),
							Colour = Color.FromRgb((byte)(1 + x * 50), (byte)(1 + x * 30), (byte)(1 + x * 10)).ToUInt32()
						})
						.ToList()
				};
			})
			.ToList();
	}
}

public class TimeEntryListViewModel : ViewModelBase
{
	private readonly TimeManager? _timeManager;

	private List<DateGroup> _entryGroups = new();

	public List<DateGroup> EntryGroups
	{
		get => _entryGroups;
		set => this.RaiseAndSetIfChanged(ref _entryGroups, value);
	}

	public Action<int> TimeEntrySelected { get; init; } = (int timeEntryId) =>
	{
	};
	
	public TimeEntryListViewModel(TimeManager? timeManager = null)
	{
		_timeManager = timeManager ?? Locator.Current.GetService<TimeManager>();

		GetTimeEntries();
	}

	public void GetTimeEntries()
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.InvokeAsync(async () =>
			{
				var groupedEntries = await GetTimeEntryDateGroups(_timeManager);

				if (EntryGroups.Count == 0)
				{
					groupedEntries.First().Expanded = true;
				}
				else
				{
					var currentlyExpanded = _entryGroups.Where(x => x.Expanded)
						.Select(x => x.Date);

					groupedEntries.ForEach(x =>
					{
						if (currentlyExpanded.Contains(x.Date))
						{
							x.Expanded = true;
						}
					});
				}

				EntryGroups = groupedEntries;
			});
		}
	}

	private async Task<List<DateGroup>> GetTimeEntryDateGroups(TimeManager timeManager)
	{
		var timeEntries = await timeManager.GetEntries();
		return timeEntries.GroupBy(x => new DateOnly(x.Start.Year, x.Start.Month, x.Start.Day))
			.Select(x => new DateGroup()
			{
				Date = x.Key,
				Entries = x.OrderBy(y => y.Start)
					.ToList()
			})
			.ToList();
	}
}

public class DateGroup : ReactiveObject
{
	public DateOnly Date { get; set; }
	public List<TimeEntryDto> Entries { get; set; }

	private bool _expanded;

	public bool Expanded
	{
		get => _expanded;
		set => this.RaiseAndSetIfChanged(ref _expanded, value);
	}
}