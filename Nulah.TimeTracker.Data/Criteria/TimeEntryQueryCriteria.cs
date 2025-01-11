namespace Nulah.TimeTracker.Data.Criteria;

public class TimeEntryQueryCriteria
{
	public string? TaskName { get; set; }
	public DateTimeOffset? From { get; set; }
	public DateTimeOffset? To { get; set; }
	public string? FullTextSearch { get; set; }
}