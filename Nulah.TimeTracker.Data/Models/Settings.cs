using SQLite;

namespace Nulah.TimeTracker.Data.Models;

public class Settings
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	public string Setting { get; set; } = null!;
	public string? Value { get; set; }
}