using System.Linq.Expressions;
using MemoryPack;
using Nulah.TimeTracker.Data.Criteria;
using Nulah.TimeTracker.Data.Models;
using Nulah.TimeTracker.Domain.Models;
using SQLite;

namespace Nulah.TimeTracker.Data;

public class TimeTrackerRepository
{
	private readonly string _databaseLocation;

	public TimeTrackerRepository(string databaseLocation)
	{
		using var db = new SQLiteConnection(databaseLocation);

		db.CreateTable<TimeEntry>();
		db.CreateTable<Settings>();

		_databaseLocation = databaseLocation;
	}

	private Dictionary<int, TimeEntry> LoadFromFile(string databaseLocation)
	{
		var dbContents = File.ReadAllBytes(databaseLocation);
		return dbContents.Length > 0
			? MemoryPackSerializer.Deserialize<Dictionary<int, TimeEntry>>(dbContents) ?? new Dictionary<int, TimeEntry>()
			: new Dictionary<int, TimeEntry>();
	}

	public TimeEntryDto CreateAsync(TimeEntryDto newTimeEntry)
	{
		var newEntry = new TimeEntry()
		{
			Name = newTimeEntry.Name,
			Description = newTimeEntry.Description,
			Start = newTimeEntry.Start,
			End = newTimeEntry.End,
			Colour = newTimeEntry.Colour,
			ExcludeFromDurationTotal = newTimeEntry.ExcludeFromDurationTotal,
		};

		newEntry.FullText = CreateFullText(newEntry);

		var db = GetConnection();
		db.Insert(newEntry);

		return MapToDto(newEntry);
	}

	public TimeEntryDto? UpdateAsync(TimeEntryDto newEntry)
	{
		var db = GetConnection();
		var updatingEntry = db.Table<TimeEntry>()
			.FirstOrDefault(x => x.Id == newEntry.Id);

		if (updatingEntry == null)
		{
			return null;
		}

		updatingEntry.Name = newEntry.Name;
		updatingEntry.Description = newEntry.Description;
		updatingEntry.Start = newEntry.Start;
		updatingEntry.End = newEntry.End;
		updatingEntry.Colour = newEntry.Colour;
		updatingEntry.ExcludeFromDurationTotal = newEntry.ExcludeFromDurationTotal;
		updatingEntry.FullText = CreateFullText(updatingEntry);

		db.Update(updatingEntry);

		return MapToDto(updatingEntry);
	}

	public List<SummarisedTimeEntryDto> GetEntrySummaries(TimeEntryQueryCriteria? timeEntryQueryCriteria = null)
	{
		var db = GetConnection();

		var timeEntries = db.Table<TimeEntry>()
			.Where(BuildTransactionQuery(timeEntryQueryCriteria))
			.GroupBy(x => x.Start.ToLocalTime().Date)
			.Select(x => new SummarisedTimeEntryDto
			{
				Date = x.Key,
				Summaries = x.OrderBy(y => y.Start)
					.Select(y => new TimeEntrySummaryDto()
					{
						Colour = y.Colour,
						Duration = y.End - y.Start,
						ExcludeFromDurationTotal = y.ExcludeFromDurationTotal
					})
					.ToList()
			})
			.OrderBy(x => x.Date)
			.ToList();

		return timeEntries;
	}

	public List<TimeEntryDto> GetEntries(TimeEntryQueryCriteria? timeEntryQueryCriteria = null)
	{
		var db = GetConnection();
		var timeEntries = db.Table<TimeEntry>()
			.Where(BuildTransactionQuery(timeEntryQueryCriteria))
			.ToList();

		return timeEntries
			.Select(MapToDto)
			.ToList();
	}

	public TimeEntryDto? GetEntry(int timeEntryId)
	{
		var db = GetConnection();
		var timeEntry = db.Table<TimeEntry>()
			.FirstOrDefault(x => x.Id == timeEntryId);

		return timeEntry == null
			? null
			: MapToDto(timeEntry);
	}

	public List<TimeEntrySearchAggregatedSuggestion> GetAggregatedSearchSuggestions(string? searchTerm)
	{
		var criteria = new TimeEntryQueryCriteria()
		{
			FullTextSearch = searchTerm?.ToUpper()
		};

		var connection = GetConnection();
		var matchingEntries = connection.Table<TimeEntry>()
			.Where(BuildTransactionQuery(criteria))
			.GroupBy(x => new { x.Colour, x.Name })
			.Select(x => new TimeEntrySearchAggregatedSuggestion()
			{
				Colour = x.Key.Colour,
				Name = x.Key.Name,
				Descriptions = x.Where(y => !string.IsNullOrWhiteSpace(y.Description))
					.Select(y => y.Description!)
					.Distinct()
					.Take(5)
					.ToList()
			});

		return matchingEntries.ToList();
	}

	public DateTimeOffset RebuildIndex()
	{
		// lazy first pass just to get a way to update indexes for my existing database
		// TODO: track the last index date somewhere
		// TODO: update the index periodically based on this date on start up
		// TODO: make this setting _opt in_ rather than opt out
		var connection = GetConnection();
		// TODO: should probably batch this in pages or similar
		var allEntries = connection.Table<TimeEntry>()
			.ToList();
		foreach (var timeEntry in allEntries)
		{
			timeEntry.FullText = CreateFullText(timeEntry);
		}

		connection.UpdateAll(allEntries);

		return SetLastIndexRebuild(DateTimeOffset.Now, connection);
	}

	/// <summary>
	/// Returns the date the index was last rebuilt, or null if it has never been built
	/// </summary>
	/// <returns></returns>
	public DateTimeOffset? GetLastIndexRebuildDate()
	{
		var connection = GetConnection();
		var indexSetting = connection.Table<Settings>()
			.FirstOrDefault(x => x.Setting == "LastIndexRebuild");

		if (DateTimeOffset.TryParse(indexSetting?.Value, out var lastRebuildDate))
		{
			return lastRebuildDate;
		}

		return null;
	}

	private DateTimeOffset SetLastIndexRebuild(DateTimeOffset updateTime, SQLiteConnection connection)
	{
		var settingsTable = connection.Table<Settings>();

		if (settingsTable.FirstOrDefault(x => x.Setting == "LastIndexRebuild") is {} rebuildIndexDateSetting)
		{
			rebuildIndexDateSetting.Value = updateTime.ToString();
			connection.Update(rebuildIndexDateSetting);
		}
		else
		{
			var newRebuildDate = new Settings()
			{
				Setting = "LastIndexRebuild",
				Value = updateTime.ToString()
			};
			connection.Insert(newRebuildDate);
		}
		
		return updateTime;
	}

	private TimeEntryDto MapToDto(TimeEntry newEntry)
	{
		return new TimeEntryDto()
		{
			Id = newEntry.Id,
			Name = newEntry.Name,
			Description = newEntry.Description,
			// Because sqlite can't store DateTimeOffset but instead stores in UTC,
			// we simply set the offset to whatever the users current timezone is based on whatever
			// the OS returns us.
			// TODO: make this configurable maybe?
			Start = newEntry.Start.ToOffset(TimeZoneInfo.Local.BaseUtcOffset),
			End = newEntry.End?.ToOffset(TimeZoneInfo.Local.BaseUtcOffset),
			Colour = newEntry.Colour,
			ExcludeFromDurationTotal = newEntry.ExcludeFromDurationTotal
		};
	}

	private SQLiteConnection GetConnection()
	{
		return new SQLiteConnection(_databaseLocation);
	}

	private string CreateFullText(TimeEntry entry)
	{
		return $"{entry.Name.ToUpper()}{entry.Description?.ToUpper()}";
	}

	/// <summary>
	/// Creates a predicate for linq to sql to filter transactions as appropriate.
	/// </summary>
	/// <param name="timeEntryQueryCriteria"></param>
	/// <returns></returns>
	private static Expression<Func<TimeEntry, bool>> BuildTransactionQuery(TimeEntryQueryCriteria? timeEntryQueryCriteria)
	{
		// TODO: move this to its own criteria class maybe?
		// Set criteria to a new instance if null is given
		timeEntryQueryCriteria ??= new TimeEntryQueryCriteria();

		Expression<Func<TimeEntry, bool>>? baseFunc = null;

		if (!string.IsNullOrWhiteSpace(timeEntryQueryCriteria.TaskName))
		{
			baseFunc = baseFunc.And(x => x.Name.Contains(timeEntryQueryCriteria.TaskName));
		}

		if (!string.IsNullOrWhiteSpace(timeEntryQueryCriteria.FullTextSearch))
		{
			baseFunc = baseFunc.And(x => x.FullText.Contains(timeEntryQueryCriteria.FullTextSearch));
		}

		if (timeEntryQueryCriteria.From.HasValue)
		{
			baseFunc = baseFunc.And(x => timeEntryQueryCriteria.From.Value <= x.Start);
		}

		if (timeEntryQueryCriteria.To.HasValue)
		{
			baseFunc = baseFunc.And(x => x.Start <= timeEntryQueryCriteria.To.Value);
		}

		// Return an "empty" expression if we have a criteria object, but no criteria to act on
		baseFunc ??= x => true;

		if (baseFunc.CanReduce)
		{
			baseFunc.Reduce();
		}

		return baseFunc;
	}
}

public class SummarisedTimeEntryDto
{
	public DateTime Date { get; set; }
	public List<TimeEntrySummaryDto> Summaries { get; set; } = [];

	/// <summary>
	/// Total duration of all summaries
	/// </summary>
	public TimeSpan Duration => new(
		Summaries
			.Where(x => x is { Duration: not null })
			.Sum(x => x.Duration!.Value.Ticks)
	);

	/// <summary>
	/// Total timespan not including excluded entries 
	/// </summary>
	public TimeSpan DurationWithExclusions => new(
		Summaries
			.Where(x => x is { Duration: not null, ExcludeFromDurationTotal: false })
			.Sum(x => x.Duration!.Value.Ticks)
	);
}

public class TimeEntrySummaryDto
{
	public uint? Colour { get; set; }
	public TimeSpan? Duration { get; set; }
	public bool ExcludeFromDurationTotal { get; set; }
}