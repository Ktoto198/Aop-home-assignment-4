using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResSim.Views;
using ResSim.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using Avalonia.Threading;
using Avalonia.Controls;
using System.Threading;

namespace ResSim.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public MainWindow MainWindow { get; set; }
        private Window? mainWindow;
        public static RecipeData? recipeData;
        private readonly MealProcessing _mealProcessing = new MealProcessing();
        RecipeProgress recipeProgress = new RecipeProgress();

        [ObservableProperty]
        private ObservableCollection<RecipeProgress> recipesInProgress;

        [ObservableProperty]
        private ObservableCollection<RecipeProgress>? filteredRecipesInProgress;

        [ObservableProperty]
        private double _progress = new RecipeProgress().Progress;

        [ObservableProperty]
        private bool allOrdersCompleted;

        [ObservableProperty]
        private int stationNumber = 1;

        [ObservableProperty]
        private int maxStationNumber = 6;

        private int _currentRunningStations = 0;
        private ConcurrentQueue<Recipe> _recipeQueue;
        private CancellationTokenSource? _cancellationTokenSource;
        private List<Task> _runningTasks = new List<Task>();
        private Dictionary<int, CancellationTokenSource> _stationCancellationTokenSources = new Dictionary<int, CancellationTokenSource>();

        public MainWindowViewModel(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
            RecipesInProgress = new ObservableCollection<RecipeProgress>();
            _mealProcessing = new MealProcessing(RecipesInProgress);
        }

        public MainWindowViewModel(Window? mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        // This method will be called whenever the StationNumber changes
        partial void OnStationNumberChanged(int value)
        {
            if (_recipeQueue == null || _cancellationTokenSource == null)
                return;

            if (value > _currentRunningStations)
            {
                // Add more stations if needed
                int additionalStations = value - _currentRunningStations;
                for (int i = 0; i < additionalStations; i++)
                {
                    StartStation(_currentRunningStations + 1, _cancellationTokenSource.Token);
                    _currentRunningStations++;
                }
            }
            // No need to remove stations, only add
        }

        [RelayCommand]
        public void ShowActiveOrders()
        {
            // Ensure FilteredRecipesInProgress is not null.
            FilteredRecipesInProgress ??= new ObservableCollection<RecipeProgress>();

            // Filter the active orders and directly assign to FilteredRecipesInProgress.
            var active = RecipesInProgress.Where(r => r.Status == "In Progress").ToList();

            FilteredRecipesInProgress.Clear();
            foreach (var recipe in active)
            {
                FilteredRecipesInProgress.Add(recipe);
            }

            // Update RecipesInProgress (if necessary).
            RecipesInProgress = FilteredRecipesInProgress;
        }

        private void CheckAllOrdersCompleted()
        {
            AllOrdersCompleted = RecipesInProgress.All(r => r.Status == "Completed");
        }

        // Method to start meal processing
        public async Task StartMealProcessing(List<Recipe> recipes)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            _runningTasks.Clear();
            _recipeQueue = new ConcurrentQueue<Recipe>(recipes.OrderBy(_ => Guid.NewGuid()));

            int stationCount = StationNumber;

            for (int i = 0; i < stationCount; i++)
            {
                StartStation(i + 1, cancellationToken);
            }

            _currentRunningStations = stationCount;

            try
            {
                await Task.WhenAll(_runningTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error or cancellation occurred: {ex.Message}");
            }
        }

        private void StartStation(int stationId, CancellationToken cancellationToken)
        {
            string stationName = $"Station {stationId}";
            var cancellationTokenSource = new CancellationTokenSource();
            _stationCancellationTokenSources[stationId] = cancellationTokenSource;

            var task = Task.Run(async () =>
            {
                List<RecipeProgress> recipesBeingProcessed = new List<RecipeProgress>();

                while (_recipeQueue.TryDequeue(out var recipe))
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    // Exit if the station is no longer needed due to the number of stations being reduced
                    if (stationId > StationNumber)
                    {
                        // Remove recipes being processed by this station
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            foreach (var progress in recipesBeingProcessed)
                            {
                                RecipesInProgress.Remove(progress);  // Remove the recipe from the collection
                            }
                        });

                        Console.WriteLine($"{stationName} stopped due to StationNumber decrease and removed its recipes.");
                        return;  // Stop processing if station number has been reduced
                    }

                    // Create a new progress object for this recipe
                    var recipeProgress = new RecipeProgress
                    {
                        RecipeName = recipe.Name ?? "Unknown Recipe",
                        Status = "In Progress",
                        Progress = 0
                    };

                    // Add recipe progress to the list of recipes in progress
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        RecipesInProgress.Add(recipeProgress);
                    });

                    recipesBeingProcessed.Add(recipeProgress);  // Track the recipe as being processed by this station

                    // Simulate processing the recipe
                    var kitchenStation = new KitchenStation(stationName, recipe);
                    await kitchenStation.ProcessMealsAsync(recipe, recipeProgress);

                    // Update the progress once completed
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        recipeProgress.Status = "Completed";
                        recipeProgress.Progress = 100;
                    });
                }

                Console.WriteLine($"{stationName} finished processing recipes.");
            }, cancellationToken);

            // Add the task to the list of running tasks
            _runningTasks.Add(task);
        }

        // Method to stop processing
        public async Task StopProcessing()
        {
            if (_cancellationTokenSource != null)
            {
                // Request cancellation
                _cancellationTokenSource.Cancel();

                // Wait for all running tasks to complete (cancelled or not)
                await Task.WhenAll(_runningTasks);

                // Optionally, reset any state after stopping
                Console.WriteLine("Processing stopped.");
            }
        }
    }
}