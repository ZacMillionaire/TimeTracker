namespace Nulah.TimeTracker.Domain.Models;

public class TimeEntrySearchSuggestion
{
}

public class TimeEntrySearchAggregatedSuggestion
{
	public string Name { get; set; } = null!;
	public uint? Colour { get; set; }
	public List<string> Descriptions { get; init; } = [];

	// lazy string join with 
	public string Description => string.Join(
		", ",
		[
			..Descriptions.Select(x => x.Length > 25
				? $"{x[..25]}..."
				: x)
		]
	);
}