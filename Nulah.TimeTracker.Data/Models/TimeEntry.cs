using MemoryPack;
using SQLite;

namespace Nulah.TimeTracker.Data.Models;

[MemoryPackable]
internal partial class TimeEntry : BaseEntity
{
	[MemoryPackOrder(0)]
	[Indexed]
	public string Name { get; set; } = null!;

	[MemoryPackOrder(1)]
	public string? Description { get; set; }

	[MemoryPackOrder(2)]
	[Indexed]
	public DateTimeOffset Start { get; set; }

	[MemoryPackOrder(3)]
	public DateTimeOffset? End { get; set; }

	[MemoryPackOrder(4)]
	public uint? Colour { get; set; }

	/// <summary>
	/// Hint that the time for this entry be excluded from any durations 
	/// </summary>
	[MemoryPackOrder(5)]
	public bool ExcludeFromDurationTotal { get; set; }
}