using Avalonia.Input;
using Avalonia.Interactivity;
using Nulah.TimeTracker.Domain.Models;

namespace Nulah.TimeTracker.Models;

public class TimeEntryClickedEventArgs : RoutedEventArgs
{
	public readonly PointerReleasedEventArgs PointerReleasedEventArgs;
	public readonly TimeEntryDto? TimeEntry;

	public TimeEntryClickedEventArgs(
		RoutedEvent routedEvent,
		PointerReleasedEventArgs pointerReleasedEventArgs,
		TimeEntryDto? timeEntry
	) : base(routedEvent)
	{
		PointerReleasedEventArgs = pointerReleasedEventArgs;
		TimeEntry = timeEntry;
	}
}