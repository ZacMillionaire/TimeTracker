using Nulah.TimeTracker.Data;
using Nulah.TimeTracker.Data.Criteria;
using Nulah.TimeTracker.Domain.Models;

namespace Nulah.TimeTracker.Core;

public class TimeManager
{
	private readonly TimeTrackerRepository _timeTrackerRepository;
	private readonly TimeProvider _timeProvider;

	public TimeManager(TimeTrackerRepository timeTrackerRepository)
	{
		_timeTrackerRepository = timeTrackerRepository;
		_timeProvider = TimeProvider.System;
	}

	public async Task<TimeEntryCreateResponse> CreateAsync(TimeEntryDto newTimeEntry)
	{
		var created = await _timeTrackerRepository.CreateAsync(newTimeEntry);

		return new TimeEntryCreateResponse
		{
			IsError = false,
			TimeEntry = created
		};
	}

	public async Task<TimeEntryUpdateResponse> UpdateAsync(TimeEntryDto newEntry)
	{
		var updated = await _timeTrackerRepository.UpdateAsync(newEntry);
		return new TimeEntryUpdateResponse
		{
			IsError = updated == null,
			TimeEntry = updated
		};
	}

	public async Task<List<TimeEntryDto>> GetEntries(TimeEntryQueryCriteria? timeEntryQueryCriteria = null)
	{
		return await _timeTrackerRepository.GetEntries(timeEntryQueryCriteria);
	}

	public async Task<TimeEntryDto> GetTimeEntry(int timeEntryId)
	{
		return await _timeTrackerRepository.GetEntry(timeEntryId);
	}
}

public class TimeEntryCreateResponse
{
	public bool IsError { get; set; }
	public TimeEntryDto? TimeEntry { get; set; }
}

public class TimeEntryUpdateResponse
{
	public bool IsError { get; set; }
	public TimeEntryDto? TimeEntry { get; set; }
}