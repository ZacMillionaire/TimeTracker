namespace Nulah.TimeTracker.Domain.Models;

public class TimeEntryDto
{
	public int Id { get; set; }
	public string Name { get; set; } = null!;
	public string? Description { get; set; }
	public DateTimeOffset Start { get; set; }
	public DateTimeOffset? End { get; set; }
}