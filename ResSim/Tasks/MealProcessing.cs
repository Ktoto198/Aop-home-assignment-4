using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using ResSim.Models;
using ResSim.ViewModels;

namespace ResSim;

public class MealProcessing
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(2, 2); // Limit to 1 concurrent thread
    private static readonly object _recipesLock = new object(); // Lock for the recipes list
    public List<RecipeProgress>? recipesInProgress = new List<RecipeProgress>();
    private ObservableCollection<RecipeProgress> _recipesInProgress;

    public MealProcessing(ObservableCollection<RecipeProgress> recipesInProgress)
    {
        _recipesInProgress = recipesInProgress;
    }

    public MealProcessing()
    {
    }
    public async Task Run(Recipe recipe)
    {
        if (recipe == null) Console.WriteLine("Recipe is null.");

        Console.WriteLine($"Starting meal processing for {recipe.Name}...");
        await ProcessMealsAsync(recipe);
    }

    private async Task StartProcessing(Recipe recipe)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (recipe == null || recipe.IsActive) return;
            recipe.IsActive = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ProcessMealsAsync(Recipe recipe)
    {
        await StartProcessing(recipe);

        try
        {
            Console.WriteLine("Processing meals...");
            await ProcessStepsAsync(recipe);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during meal processing: {ex.Message}");
        }
    }

    private async Task ProcessStepsAsync(Recipe recipe)
    {
        if (recipe?.Steps == null || recipe.Steps.Count == 0)
        {
            Console.WriteLine("No steps to process.");
            return;
        }

        Console.WriteLine($"Processing {recipe.Name} with {recipe.Steps.Count} steps.");
        var recipeProgress = new RecipeProgress()
        {
            Status = "In Progress",
            CurrentStep = recipe.Steps[0].Description,
        };

         // Add the initial progress to the list of ongoing recipes
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            recipesInProgress.Add(recipeProgress);
        });
        
        for (int i = 0; i < recipe.Steps.Count; i++)
        {
            var step = recipe.Steps[i];

            Console.WriteLine($"Processing step {i + 1}: {step.Description} (Duration: {step.Duration}s)");
            await Task.Delay(step.Duration * 1000); // Non-blocking delay
            step.IsCompleted = true;
            Dispatcher.UIThread.Post(() =>
            {
                recipeProgress.Progress = recipeProgress.UpdateProgress(i, recipe.Steps.Count); // Update progress
                recipeProgress.CurrentStep = step.Description; 
            });
        }

        recipe.IsCompleted = true;
        Dispatcher.UIThread.Post(() =>
        {
            recipeProgress.Status = "Completed";
        });

        Console.WriteLine($"{recipe.Name} processing completed.");
    }

}