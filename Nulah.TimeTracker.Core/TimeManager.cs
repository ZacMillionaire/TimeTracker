using Nulah.TimeTracker.Data;
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
}

public class TimeEntryCreateResponse
{
	public bool IsError { get; set; }
	public TimeEntryDto? TimeEntry { get; set; }
}