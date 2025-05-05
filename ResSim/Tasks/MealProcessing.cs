using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ResSim.Models;
using ResSim.ViewModels;

namespace ResSim;

public class MealProcessing
{
    public bool IsRunning { get; private set; }
    private static SemaphoreSlim _semaphore = new SemaphoreSlim(2, 2); // Limit to 2 concurrent threads
    private CancellationTokenSource? _cancellationTokenSource; // Declare the cancellation token source
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

    public async Task Run(Recipe recipe, RecipeProgress recipeProgress, int simulationSpeed, CancellationToken cancellationToken)
    {
        if (recipe == null)
        {
            Console.WriteLine("Recipe is null.");
            return;
        }

        Console.WriteLine($"Starting meal processing for {recipe.Name}...");
        await ProcessMealsAsync(recipe, recipeProgress, simulationSpeed, cancellationToken);
    }

    private async Task StartProcessing(Recipe recipe, int simulationSpeed, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken); // Pass cancellation token to respect cancellation requests
        try
        {
            if (recipe == null || recipe.IsActive || cancellationToken.IsCancellationRequested) return;
            recipe.IsActive = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    private async Task ProcessMealsAsync(Recipe recipe, RecipeProgress recipeProgress, int simulationSpeed, CancellationToken cancellationToken)
    {
        await StartProcessing(recipe, simulationSpeed, cancellationToken);

        try
        {
            Console.WriteLine("Processing meals...");
            await ProcessStepsAsync(recipe, recipeProgress, simulationSpeed, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during meal processing: {ex.Message}");
        }
    }

    private async Task ProcessStepsAsync(Recipe recipe, RecipeProgress recipeProgress, int simulationSpeed, CancellationToken cancellationToken)
    {
        if (recipe?.Steps == null || recipe.Steps.Count == 0)
        {
            Console.WriteLine("No steps to process.");
            return;
        }

        Console.WriteLine($"Processing {recipe.Name} with {recipe.Steps.Count} steps.");

        // Update UI for initial recipe progress
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            recipeProgress.Status = "In Progress";
            recipeProgress.CurrentStep = recipe.Steps[0].Description;
        });

        // Add the initial progress to the list of ongoing recipes
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            recipesInProgress.Add(recipeProgress);
        });

        for (int i = 0; i < recipe.Steps.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Processing was canceled.");
                break; // Exit the loop if cancellation is requested
            }

            var step = recipe.Steps[i];

            await Task.Delay((int)(step.Duration * 1000 / simulationSpeed), cancellationToken); // Non-blocking delay, respects cancellation token

            // Mark step as completed
            step.IsCompleted = true;

            // Update UI with progress and current step
            Dispatcher.UIThread.Post(() =>
            {
                recipeProgress.Progress = recipeProgress.UpdateProgress(i, recipe.Steps.Count); // Update progress
                recipeProgress.CurrentStep = step.Description;
            });
        }

        // Mark recipe as completed and update UI
        recipe.IsCompleted = true;
        Dispatcher.UIThread.Post(() =>
        {
            recipeProgress.Status = "Completed";
        });

        Console.WriteLine($"{recipe.Name} processing completed.");
    }

    public void CancelProcessing(CancellationTokenSource cancellationTokenSource)
    {
        cancellationTokenSource?.Cancel(); // To trigger cancellation on all tasks using the token
    }


    // Reset the state
    public void ResetState()
    {
        // Clear the recipes in progress
        _recipesInProgress.Clear();

        IsRunning = false;

        // Optionally cancel any ongoing tasks or reset the cancellation token
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        // Reinitialize the semaphore if needed (not typically required unless you've run into a specific issue)
        _semaphore.Dispose();
        _semaphore = new SemaphoreSlim(2, 2); // Recreate it with the original limit
    }

}