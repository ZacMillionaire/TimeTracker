using MemoryPack;
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

	public async Task<TimeEntryDto> CreateAsync(TimeEntryDto newTimeEntry)
	{
		var newEntry = new TimeEntry()
		{
			Name = newTimeEntry.Name,
			Description = newTimeEntry.Description,
			Start = newTimeEntry.Start,
			End = newTimeEntry.End,
		};

		var db = new SQLiteAsyncConnection(_databaseLocation);
		await db.InsertAsync(newEntry);

		return MapToDto(newEntry);
	}

	private TimeEntryDto MapToDto(TimeEntry newEntry)
	{
		return new TimeEntryDto()
		{
			Id = newEntry.Id,
			Name = newEntry.Name,
			Description = newEntry.Description,
			Start = newEntry.Start,
			End = newEntry.End,
		};
	}
}