using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DynamicData.Binding;

namespace Nulah.TimeTracker.Controls;

// Finish this up if I ever decide I want a custom autocomplete box
public partial class TaskAutoCompleteBox : UserControl
{
	private CancellationTokenSource? _populationCancellationTokenSource;

	public Func<string?, CancellationToken, Task<IEnumerable<object>>>? AsyncPopulator
	{
		get => GetValue(AsyncPopulatorProperty);
		set => SetValue(AsyncPopulatorProperty, value);
	}
	public static readonly StyledProperty<Func<string?, CancellationToken, Task<IEnumerable<object>>>?> AsyncPopulatorProperty =
		AvaloniaProperty.Register<TaskAutoCompleteBox, Func<string?, CancellationToken, Task<IEnumerable<object>>>?>(
			nameof(AsyncPopulator));
	public TaskAutoCompleteBox()
	{
		InitializeComponent();
		SuggestionSearchTextInput.WhenValueChanged(x => x.Text)
			.Subscribe(SearchTextUpdated);
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		base.OnLostFocus(e);
	}

	private void SearchTextUpdated(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return;
		}

		if (AsyncPopulator != null)
		{
			Search(value);
		}
	}

	private bool Search(string searchText)
	{
		_populationCancellationTokenSource?.Cancel(false);
		_populationCancellationTokenSource?.Dispose();
		_populationCancellationTokenSource = null;

		if (AsyncPopulator == null)
		{
			return false;
		}

		_populationCancellationTokenSource = new CancellationTokenSource();
		var task = PopulateAsync(searchText, _populationCancellationTokenSource.Token);
		if (task.Status == TaskStatus.Created)
			task.Start();

		return true;
	}

	public IDataTemplate ItemTemplate
	{
		get => GetValue(ItemTemplateProperty);
		set => SetValue(ItemTemplateProperty, value);
	}
	public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
		AvaloniaProperty.Register<TaskAutoCompleteBox, IDataTemplate>(
			nameof(ItemTemplate));
	public IEnumerable? SearchResults
	{
		get => GetValue(SearchResultsProperty);
		set => SetValue(SearchResultsProperty, value);
	}
	public static readonly StyledProperty<IEnumerable?> SearchResultsProperty =
		AvaloniaProperty.Register<TaskAutoCompleteBox, IEnumerable?>(
			nameof(SearchResults));
	
	private async Task PopulateAsync(string? searchText, CancellationToken cancellationToken)
	{
		try
		{
			IEnumerable<object> result = await AsyncPopulator!.Invoke(searchText, cancellationToken);
			var resultList = result.ToList();

			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					SetCurrentValue(SearchResultsProperty, resultList);
					SearchResultsList.ItemsSource = resultList;
					PopulateComplete();
				}
			});
		}
		catch (TaskCanceledException)
		{
		}
		finally
		{
			_populationCancellationTokenSource?.Dispose();
			_populationCancellationTokenSource = null;
		}
	}

	private void PopulateComplete()
	{
		if (!SuggestionFlyout.IsOpen)
		{
			SuggestionFlyout.Open();
		}
	}
}