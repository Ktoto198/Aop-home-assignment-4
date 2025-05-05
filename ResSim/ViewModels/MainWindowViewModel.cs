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

        [ObservableProperty]
        public int simulationSpeed = 1;

        private int _currentRunningStations = 0;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Initialize with a single permit
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
            else if (value < _currentRunningStations)
            {
                // Stop the excess stations if the station number decreases
                int stationsToStop = _currentRunningStations - value;
                for (int i = 0; i < stationsToStop; i++)
                {
                    StopStation(_currentRunningStations - i);
                }
                _currentRunningStations = value;
            }
            // No need to remove stations, only add or stop them
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

        [RelayCommand]
        public void AllOrdersCompletedCommand()
        {
            // Check if all orders are completed
            CheckAllOrdersCompleted();

            // If all orders are completed, show a message
            if (AllOrdersCompleted)
            {
                Console.WriteLine("All orders have been completed.");
            }
        }

        private void CheckAllOrdersCompleted()
        {
            AllOrdersCompleted = RecipesInProgress.All(r => r.Status == "Completed");
        }

        // Method to start meal processing
        public async Task StartMealProcessing(List<Recipe> recipes)
        {
            if (_cancellationTokenSource != null)
            {
                Console.WriteLine("Meal processing is already running.");
                return;
            }

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

            var task = Task.Run(async () =>
            {
                List<RecipeProgress> recipesBeingProcessed = new List<RecipeProgress>();

                try
                {
                    while (_recipeQueue.TryDequeue(out var recipe))
                    {
                        // Check for cancellation on each recipe being processed
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine($"{stationName} was canceled before starting a new recipe.");
                            break;
                        }

                        // If station number is decreased, stop processing
                        if (stationId > StationNumber)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                foreach (var progress in recipesBeingProcessed)
                                {
                                    RecipesInProgress.Remove(progress);
                                }
                            });

                            Console.WriteLine($"{stationName} stopped due to StationNumber decrease.");
                            break;
                        }

                        // Create and update progress for this recipe
                        var recipeProgress = new RecipeProgress
                        {
                            RecipeName = recipe.Name ?? "Unknown Recipe",
                            Status = "In Progress",
                            Progress = 0
                        };

                        // Update UI
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            RecipesInProgress.Add(recipeProgress);
                        });

                        recipesBeingProcessed.Add(recipeProgress);

                        // Simulate processing
                        var kitchenStation = new KitchenStation(stationName, recipe);
                        await kitchenStation.ProcessMealsAsync(recipe, recipeProgress, simulationSpeed, cancellationToken);

                        // Update progress after processing is done
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            recipeProgress.Status = "Completed";
                            recipeProgress.Progress = 100;
                        });
                    }

                    Console.WriteLine($"{stationName} finished processing recipes.");
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation exception
                    Console.WriteLine($"{stationName} was canceled.");
                }
            }, cancellationToken);

            // Add task to the running tasks list
            _runningTasks.Add(task);
        }

        [RelayCommand]
        public async Task StopProcessingCommand()
        {
            await StopProcessing();
        }

        private void StopStation(int stationId)
        {
            if (_stationCancellationTokenSources.TryGetValue(stationId, out var cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
                _stationCancellationTokenSources.Remove(stationId);
                Console.WriteLine($"Station {stationId} stopped.");
            }
        }

        // Method to stop processing
        public async Task StopProcessing()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();

                try
                {
                    await Task.WhenAll(_runningTasks);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Tasks were cancelled.");
                }

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;  // 👈 resets for new start
            }

            _recipeQueue = null;
            _runningTasks.Clear();
            _currentRunningStations = 0;
            AllOrdersCompleted = false;

            Console.WriteLine("Processing stopped and cleaned up.");
        }
    }
}