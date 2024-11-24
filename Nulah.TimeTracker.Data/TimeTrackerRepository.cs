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
			Colour = newTimeEntry.Colour
		};

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

		db.Update(updatingEntry);

		return MapToDto(updatingEntry);
	}

	public List<SummarisedTimeEntryDto> GetEntrySummaries(TimeEntryQueryCriteria? timeEntryQueryCriteria = null)
	{
		var db = GetConnection();

		var timeEntries = db.Table<TimeEntry>()
			.Where(BuildTransactionQuery(timeEntryQueryCriteria))
			.GroupBy(x => x.Start.Date)
			.Select(x => new SummarisedTimeEntryDto
			{
				Date = x.Key,
				Summaries = x.Select(y => new TimeEntrySummaryDto()
					{
						Colour = y.Colour,
						Duration = y.End - y.Start
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
			Colour = newEntry.Colour
		};
	}

	private SQLiteConnection GetConnection()
	{
		return new SQLiteConnection(_databaseLocation);
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

	public TimeSpan Duration => new(
		Summaries
			.Where(x => x.Duration.HasValue)
			.Sum(x => x.Duration!.Value.Ticks)
	);
}

public class TimeEntrySummaryDto
{
	public uint? Colour { get; set; }
	public TimeSpan? Duration { get; set; }
}