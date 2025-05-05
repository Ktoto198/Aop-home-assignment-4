using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResSim.Models;
using ResSim.ViewModels;

namespace ResSim;
public class KitchenStation
{
    public string Name { get; set; }
    public Recipe Recipe;
    private readonly MealProcessing mealProcessing = new MealProcessing();

    public KitchenStation(string name, Recipe recipe)
    {
        Name = name;
        Recipe = recipe;
    }

    public async Task ProcessMealsAsync(Recipe recipe, RecipeProgress recipeProgress, int simulationSpeed, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Processing meals at {Name} station...");

        // Use the asynchronous method to process meals concurrently
        await mealProcessing.Run(recipe, recipeProgress, simulationSpeed, cancellationToken);
    }
}

