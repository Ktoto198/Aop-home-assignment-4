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

namespace ResSim.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindow MainWindow { get; set; }
    public static RecipeData? recipeData;

    private readonly MealProcessing _mealProcessing = new MealProcessing();

    RecipeProgress recipeProgress = new RecipeProgress();

    [ObservableProperty]
    private ObservableCollection<RecipeProgress> recipesInProgress;
    private Window? mainWindow;


    public MainWindowViewModel(MainWindow mainWindow)
    {
        MainWindow = mainWindow;
        RecipesInProgress = new ObservableCollection<RecipeProgress>();
        _mealProcessing = new MealProcessing(RecipesInProgress);
        Console.WriteLine(recipesInProgress.Count);

    }

    public MainWindowViewModel(Window? mainWindow)
    {
        this.mainWindow = mainWindow;
    }

    public async Task StartMealProcessing(List<Recipe> recipes)
    {
        var tasks = new List<Task>();
        var recipeQueue = new ConcurrentQueue<Recipe>(recipes.OrderBy(_ => Guid.NewGuid())); // Shuffle recipes

        int stationCount = 2; // Or any number of stations you want

        for (int i = 0; i < stationCount; i++)
        {
            int stationId = i + 1;
            var stationName = $"Station {stationId}";

            tasks.Add(Task.Run(async () =>
            {
                while (recipeQueue.TryDequeue(out var recipe))
                {
                    Console.WriteLine($"Processing recipe: {recipe.Name} at {stationName}");

                    // Add the recipe to the UI immediately
                    var recipeProgress = new RecipeProgress
                    {
                        RecipeName = recipe.Name ?? "Unknown Recipe",
                        Status = "In Progress",
                        Progress = 0
                    };

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        RecipesInProgress.Add(recipeProgress);
                    });

                    // Process the recipe
                    var kitchenStation = new KitchenStation(stationName, recipe);
                    await kitchenStation.ProcessMealsAsync(recipe);

                    // Update the recipe progress after processing
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        recipeProgress.Status = "Completed";
                        recipeProgress.Progress = 100;
                    });

                    Console.WriteLine($"Finished processing recipe: {recipe.Name} at {stationName}");
                }
                Console.WriteLine($"{stationName} has completed all recipes.");
            }));
        }

        try
        {
            await Task.WhenAll(tasks);
            Console.WriteLine("All stations have completed meal processing!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred while processing meals: {ex.Message}");
        }
    }
}
