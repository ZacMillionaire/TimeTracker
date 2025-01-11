using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Nulah.TimeTracker.Core;
using Nulah.TimeTracker.Domain.Models;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.States;
using Splat;

namespace Nulah.TimeTracker.ViewModels;

public class TimeEntryCreateEditViewModelDesignTime : TimeEntryCreateEditViewModel
{
	public TimeEntryCreateEditViewModelDesignTime()
	{
		SelectedDate = DateTimeOffset.Now;
		StartTime = SelectedDate?.TimeOfDay;
		EndTime = StartTime?.Add(TimeSpan.FromMinutes(15));
	}
}

public class TimeEntryCreateEditViewModel : ValidatingViewModelBase
{
	private readonly TimeManager? _timeManager;

	public Dictionary<string, TimeSpan> Increments => new()
	{
		{ "15m", TimeSpan.FromMinutes(15) },
		{ "30m", TimeSpan.FromMinutes(30) },
		{ "45m", TimeSpan.FromMinutes(45) },
		{ "1h", TimeSpan.FromMinutes(60) },
		{ "1h30m", TimeSpan.FromMinutes(90) },
		{ "2h", TimeSpan.FromMinutes(120) },
	};

	public ICommand SaveTimeEntryCommand { get; protected set; }
	public ICommand ClearTimeEntryCommand { get; protected set; }
	public ICommand IncrementEndTime { get; protected set; }
	public ReactiveCommand<Unit, Unit> ClearEndTime { get; protected set; }

	public ReactiveCommand<string, Unit> SetToNow { get; protected set; }

	private int? _timeEntryId;

	private string? _taskName;
	private string? _description;
	private DateTimeOffset? _selectedDate;
	private TimeSpan? _startTime;
	private TimeSpan? _endTime;
	private bool _isSaving;
	private Color? _colour;
	private bool _excludeFromDurationTotal;

	public string? TaskName
	{
		get => _taskName;
		set => this.RaiseAndSetIfChanged(ref _taskName, value);
	}

	public string? Description
	{
		get => _description;
		set => this.RaiseAndSetIfChanged(ref _description, value);
	}

	public DateTimeOffset? SelectedDate
	{
		get => _selectedDate;
		set => this.RaiseAndSetIfChanged(ref _selectedDate, value);
	}

	public TimeSpan? StartTime
	{
		get => _startTime;
		set => this.RaiseAndSetIfChanged(ref _startTime, value);
	}

	public TimeSpan? EndTime
	{
		get => _endTime;
		set => this.RaiseAndSetIfChanged(ref _endTime, value);
	}

	public bool IsSaving
	{
		get => _isSaving;
		set => this.RaiseAndSetIfChanged(ref _isSaving, value);
	}

	public Color? Colour
	{
		get => _colour ?? Colors.Transparent;
		set => this.RaiseAndSetIfChanged(ref _colour, value);
	}

	public bool ExcludeFromDurationTotal
	{
		get => _excludeFromDurationTotal;
		set => this.RaiseAndSetIfChanged(ref _excludeFromDurationTotal, value);
	}

	private readonly IObservable<IValidationState> _taskNameNotEmpty;
	private readonly IObservable<IValidationState> _dateRequired;
	private readonly IObservable<IValidationState> _startTimeRequired;
	public readonly IObservable<IValidationState> _startEndValid;

	public Action<TimeEntryDto> TimeEntryCreated = actionedTimeEntry =>
	{
	};

	public Action<TimeEntryDto> TimeEntryActioned = actionedTimeEntry =>
	{
	};

	readonly ObservableAsPropertyHelper<TimeSpan?> _duration;
	public TimeSpan? Duration => _duration.Value;
	public Func<string?, CancellationToken, Task<IEnumerable<object>>>? SearchForEntries { get; init; }


	public TimeEntryCreateEditViewModel(TimeManager? timeManager = null)
	{
		_timeManager = timeManager ?? Locator.Current.GetService<TimeManager>();

		SearchForEntries = GetAggregateSearchSuggestions;

		SaveTimeEntryCommand = ReactiveCommand.Create(
			SaveTimeEntryAsync,
			this.WhenAnyValue(
				x => x.StartTime,
				x => x.EndTime,
				x => x.SelectedDate,
				x => x.TaskName,
				(start, end, selected, name) => start.HasValue && selected.HasValue && !string.IsNullOrWhiteSpace(name)
			)
		);

		ClearTimeEntryCommand = ReactiveCommand.Create(Reset);

		IncrementEndTime = ReactiveCommand.Create(
			(TimeSpan increaseBy) =>
			{
				EndTime = _startTime?.Add(increaseBy);
			},
			this.WhenAnyValue(
				x => x.StartTime,
				startTime => startTime.HasValue
			)
		);

		SetToNow = ReactiveCommand.Create((string propertyName) =>
		{
			switch (propertyName)
			{
				case "Start":
					StartTime = new TimeSpan(DateTimeOffset.Now.TimeOfDay.Hours, DateTimeOffset.Now.TimeOfDay.Minutes, 0);
					break;
				case "End":
					EndTime = new TimeSpan(DateTimeOffset.Now.TimeOfDay.Hours, DateTimeOffset.Now.TimeOfDay.Minutes, 0);
					break;
			}
		});

		ClearEndTime = ReactiveCommand.Create(
			() =>
			{
				EndTime = null;
			},
			this.WhenAnyValue(
				x => x.EndTime,
				endTime => endTime.HasValue
			)
		);

		_duration = this.WhenAnyValue(
				vm => vm.StartTime,
				vm => vm.EndTime,
				(start, end) =>
				{
					if (end > start)
					{
						return end - start;
					}

					return null;
				})
			.ToProperty(this, vm => vm.Duration);

		// Validations
		_taskNameNotEmpty = this.WhenAnyValue(x => x.TaskName)
			.Select(TaskNameValid);
		_dateRequired = this.WhenAnyValue(x => x.SelectedDate)
			.Select(DateRequiredValid);
		_startTimeRequired = this.WhenAnyValue(x => x.StartTime)
			.Select(x => !x.HasValue
				? new ValidationState(false, "Start time is required")
				: ValidationState.Valid);
		_startEndValid = this.WhenAnyValue(
				x => x.StartTime,
				x => x.EndTime
			)
			.Select(((TimeSpan? start, TimeSpan? end) x) => x.start == x.end
				? new ValidationState(false, "Start and end cannot be the same")
				: x.start > x.end
					? new ValidationState(false, "End cannot be before start")
					: ValidationState.Valid);

		this.ValidationRule(viewmodel => viewmodel.TaskName, _taskNameNotEmpty);
		this.ValidationRule(viewmodel => viewmodel.SelectedDate, _dateRequired);
		this.ValidationRule(viewmodel => viewmodel.StartTime, _startTimeRequired);
		this.ValidationRule(viewmodel => viewmodel.Duration, _startEndValid);
	}

	private bool _timeEntryLoading;

	public void LoadTimeEntry(int timeEntryId)
	{
		if (_timeManager != null)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				if (_timeManager.GetTimeEntry(timeEntryId) is { } timeEntry)
				{
					_timeEntryLoading = true;
					// TODO: this will trigger the auto combo box to trigger a search when TaskName is set
					// so find a way to not make that happen
					_timeEntryId = timeEntry.Id;
					TaskName = timeEntry.Name;
					Description = timeEntry.Description;
					SelectedDate = timeEntry.Start.Date;
					StartTime = timeEntry.Start.TimeOfDay;
					EndTime = timeEntry.End?.TimeOfDay;
					Colour = timeEntry.Colour != null
						? Color.FromUInt32(timeEntry.Colour.Value)
						: null;
					ExcludeFromDurationTotal = timeEntry.ExcludeFromDurationTotal;
					_timeEntryLoading = false;
				}
			});
		}
	}

	private IValidationState TaskNameValid(string? value)
	{
		return !string.IsNullOrWhiteSpace(value)
			? ValidationState.Valid
			: new ValidationState(false, "Task name is required");
	}

	private IValidationState DateRequiredValid(DateTimeOffset? value)
	{
		return !value.HasValue
			? new ValidationState(false, "Date is required")
			: ValidationState.Valid;
	}

	private void SaveTimeEntryAsync()
	{
		if (ValidationContext.IsValid && _timeManager != null)
		{
			IsSaving = true;

			var newEntryDto = new TimeEntryDto()
			{
				Name = _taskName!,
				Description = _description,
				// Use .Date to ensure we never add time onto any time other than midnight
				Start = _selectedDate!.Value.Date.Add(_startTime!.Value),
				End = _endTime.HasValue ? _selectedDate!.Value.Date.Add(_endTime!.Value) : null,
				Colour = Colour?.ToUInt32(),
				ExcludeFromDurationTotal = _excludeFromDurationTotal
			};

			if (_timeEntryId == null)
			{
				CreateNewEntry(newEntryDto, _timeManager);
			}
			else
			{
				newEntryDto.Id = _timeEntryId.Value;
				UpdateExistingEntry(newEntryDto, _timeManager);
			}

			IsSaving = false;
		}
	}

	private void CreateNewEntry(TimeEntryDto newEntry, TimeManager timeManager)
	{
		var createResult = timeManager.CreateAsync(newEntry);

		if (createResult is { IsError: false, TimeEntry: { } timeEntry })
		{
			TimeEntryCreated.Invoke(timeEntry);
			Reset();
		}
		else
		{
			throw new NotImplementedException("create result failed not implemented");
		}
	}

	private void UpdateExistingEntry(TimeEntryDto newEntry, TimeManager timeManager)
	{
		var createResult = timeManager.UpdateAsync(newEntry);

		if (createResult is { IsError: false, TimeEntry: { } timeEntry })
		{
			TimeEntryActioned.Invoke(timeEntry);
			Reset();
		}
		else
		{
			throw new NotImplementedException("update result failed not implemented");
		}
	}

	private void Reset()
	{
		_timeEntryId = null;
		StartTime = null;
		EndTime = null;
		SelectedDate = null;
		TaskName = null;
		Description = null;
		Colour = null;
		ExcludeFromDurationTotal = false;
	}

	private Task<IEnumerable<object>> GetAggregateSearchSuggestions(string? searchString, CancellationToken cancellationToken)
	{
		if (_timeManager != null)
		{
			if (_timeEntryLoading)
			{
				Console.WriteLine("autocomplete ignored due to time entry being loaded");

				return Task.FromResult<IEnumerable<object>>(Array.Empty<object>());
			}

			Console.WriteLine("Loading autocomplete");

			return Task.FromResult<IEnumerable<object>>(_timeManager.GetAggregatedSearchSuggestions(searchString));
		}

		return Task.FromResult<IEnumerable<object>>(Array.Empty<object>());
	}

	public void SetNameAndColourFromSelectedSuggestion(TimeEntrySearchAggregatedSuggestion selectedSuggestion)
	{
		TaskName = selectedSuggestion.Name;
		Colour = selectedSuggestion.Colour != null
			? Color.FromUInt32(selectedSuggestion.Colour.Value)
			: Colors.Transparent;
	}
}

/// <summary>
/// Container class for attached properties. Must inherit from <see cref="AvaloniaObject"/>.
/// </summary>
public class DitryTrack : AvaloniaObject
{
	static DitryTrack()
	{
		CommandProperty.Changed.AddClassHandler<Interactive>(HandleCommandChanged);
	}

	/// <summary>
	/// Identifies the <seealso cref="CommandProperty"/> avalonia attached property.
	/// </summary>
	/// <value>Provide an <see cref="ICommand"/> derived object or binding.</value>
	public static readonly AttachedProperty<ICommand?> CommandProperty = AvaloniaProperty.RegisterAttached<DitryTrack, Interactive, ICommand?>(
		"Command", default, false, BindingMode.OneWay);

	/// <summary>
	/// Identifies the <seealso cref="DirtyTrackerProperty"/> avalonia attached property.
	/// Use this as the parameter for the <see cref="CommandProperty"/>.
	/// </summary>
	/// <value>Any value of type <see cref="object"/>.</value>
	private static readonly AttachedProperty<DirtyTracker> DirtyTrackerProperty = AvaloniaProperty.RegisterAttached<DitryTrack, Interactive, DirtyTracker>(
		"CommandParameter", default(DirtyTracker), false, BindingMode.OneWay, null);

	private class DirtyTracker()
	{
		public Guid Id = Guid.NewGuid();
	}

	/// <summary>
	/// <see cref="CommandProperty"/> changed event handler.
	/// </summary>
	private static void HandleCommandChanged(Interactive interactElem, AvaloniaPropertyChangedEventArgs args)
	{
		if (args.NewValue is ICommand commandValue)
		{
			interactElem.SetValue(DirtyTrackerProperty, new DirtyTracker());
			// Add non-null value
			interactElem.AddHandler(InputElement.KeyDownEvent, Handler);
		}
		else
		{
			interactElem.ClearValue(DirtyTrackerProperty);
			// remove prev value
			interactElem.RemoveHandler(InputElement.KeyDownEvent, Handler);
		}

		// local handler fcn
		static void Handler(object? s, RoutedEventArgs e)
		{
			if (s is Interactive interactElem)
			{
				// This is how we get the parameter off of the gui element.
				object commandParameter = interactElem.GetValue(DirtyTrackerProperty);
				ICommand commandValue = interactElem.GetValue(CommandProperty);
				if (commandValue?.CanExecute(commandParameter) == true)
				{
					commandValue.Execute(commandParameter);
				}
			}
		}
	}


	/// <summary>
	/// Accessor for Attached property <see cref="CommandProperty"/>.
	/// </summary>
	public static void SetCommand(AvaloniaObject element, ICommand commandValue)
	{
		element.SetValue(CommandProperty, commandValue);
	}

	/// <summary>
	/// Accessor for Attached property <see cref="CommandProperty"/>.
	/// </summary>
	public static ICommand GetCommand(AvaloniaObject element)
	{
		return element.GetValue(CommandProperty);
	}

	/// <summary>
	/// Accessor for Attached property <see cref="DirtyTrackerProperty"/>.
	/// </summary>
	public static void SetCommandParameter(AvaloniaObject element, object parameter)
	{
		element.SetValue(DirtyTrackerProperty, parameter);
	}

	/// <summary>
	/// Accessor for Attached property <see cref="DirtyTrackerProperty"/>.
	/// </summary>
	public static object GetCommandParameter(AvaloniaObject element)
	{
		return element.GetValue(DirtyTrackerProperty);
	}
}