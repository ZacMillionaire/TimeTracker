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

	public TimeEntryCreateResponse CreateAsync(TimeEntryDto newTimeEntry)
	{
		var created = _timeTrackerRepository.CreateAsync(newTimeEntry);

		return new TimeEntryCreateResponse
		{
			IsError = false,
			TimeEntry = created
		};
	}

	public TimeEntryUpdateResponse UpdateAsync(TimeEntryDto newEntry)
	{
		var updated = _timeTrackerRepository.UpdateAsync(newEntry);
		return new TimeEntryUpdateResponse
		{
			IsError = updated == null,
			TimeEntry = updated
		};
	}

	public List<SummarisedTimeEntryDto> GetEntrySummary(TimeEntryQueryCriteria? timeEntryQueryCriteria = null)
	{
		return _timeTrackerRepository.GetEntrySummaries(timeEntryQueryCriteria);
	}

	public List<TimeEntryDto> GetEntries(TimeEntryQueryCriteria? timeEntryQueryCriteria = null)
	{
		return _timeTrackerRepository.GetEntries(timeEntryQueryCriteria);
	}

	public TimeEntryDto? GetTimeEntry(int timeEntryId)
	{
		return _timeTrackerRepository.GetEntry(timeEntryId);
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