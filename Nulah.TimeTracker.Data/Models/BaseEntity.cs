using SQLite;

namespace Nulah.TimeTracker.Data.Models;

internal abstract class BaseEntity
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
}