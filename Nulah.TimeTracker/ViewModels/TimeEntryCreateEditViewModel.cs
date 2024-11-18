using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nulah.TimeTracker.Core;
using Nulah.TimeTracker.Domain.Models;
using ReactiveUI;
using ReactiveUI.Validation.Contexts;
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

	private readonly IObservable<IValidationState> _taskNameNotEmpty;
	private readonly IObservable<IValidationState> _dateRequired;
	private readonly IObservable<IValidationState> _startTimeRequired;
	public readonly IObservable<IValidationState> _startEndValid;

	public Action<int> TimeEntryCreated = i =>
	{
	};

	readonly ObservableAsPropertyHelper<TimeSpan?> _duration;
	public TimeSpan? Duration => _duration.Value;

	public TimeEntryCreateEditViewModel(TimeManager? timeManager = null)
	{
		_timeManager = timeManager ?? Locator.Current.GetService<TimeManager>();


		SaveTimeEntryCommand = ReactiveCommand.CreateFromTask(
			SaveTimeEntryAsync,
			this.WhenAnyValue(
				x => x.StartTime,
				x => x.EndTime,
				(start, end) => start.HasValue
			)
		);

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
					StartTime = new TimeSpan(DateTimeOffset.Now.TimeOfDay.Hours,DateTimeOffset.Now.TimeOfDay.Minutes,0);
					break;
				case "End":
					EndTime = new TimeSpan(DateTimeOffset.Now.TimeOfDay.Hours,DateTimeOffset.Now.TimeOfDay.Minutes,0);
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

	private IValidationState TaskNameValid(string? value)
	{
		return !string.IsNullOrWhiteSpace(value)
			? ValidationState.Valid
			: new ValidationState(false, "Task name is required");
	}

	private IValidationState DateRequiredValid(DateTimeOffset? value)
	{
		return value == null || !value.HasValue
			? new ValidationState(false, "Date is required")
			: ValidationState.Valid;
	}

	private async Task SaveTimeEntryAsync()
	{
		if (ValidationContext.IsValid && _timeManager != null)
		{
			IsSaving = true;

			var newEntryDto = new TimeEntryDto()
			{
				Name = _taskName!,
				Description = _description,
				Start = _selectedDate!.Value.Add(_startTime!.Value),
				End = _endTime.HasValue ? _selectedDate!.Value.Add(_endTime!.Value) : null
			};

			await CreateNewEntry(newEntryDto, _timeManager);

			IsSaving = false;
		}
	}

	private async Task CreateNewEntry(TimeEntryDto newEntry, TimeManager timeManager)
	{
		var createResult = await timeManager.CreateAsync(newEntry);

		if (createResult is { IsError: false, TimeEntry: { } timeEntry })
		{
			TimeEntryCreated.Invoke(timeEntry.Id);
			Reset();
			this.ClearValidationRules();
		}
		else
		{
			throw new NotImplementedException("create result failed not implemented");
		}
	}

	private void Reset()
	{
		StartTime = DateTimeOffset.Now.TimeOfDay;
		EndTime = null;
		SelectedDate = DateTimeOffset.Now.Date;
		TaskName = null;
		Description = null;
		_timeEntryId = null;
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