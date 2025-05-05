using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ResSim.Views;
using ResSim.Models;

namespace ResSim;

public class Parser
{
    MainWindow mainWindow = new MainWindow();
    private RecipeData recipeData;
    private List<Recipe> recipes = new List<Recipe>();
    private List<Ingredient> ingredients = new List<Ingredient>();

    Ingredient ingredient;
    Recipe recipe;
    Step step;

    public string? fileName;

    public Parser(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions { IncludeFields = true };

        recipeData = JsonSerializer.Deserialize<RecipeData>(jsonString, options)
                     ?? throw new InvalidOperationException("Failed to deserialize RecipeData.");

        // add new ingredients to the recipe's Ingredients collection
        ProcessIngredients(recipeData.ingredients);

        ProcessRecipes(recipeData.recipes);
    }

    private void ProcessIngredients(List<Ingredient> ingredients)
    {
        // Create a new list to store processed ingredients
        var processedIngredients = new List<Ingredient>();

        foreach (var ing in ingredients)
        {
            var ingredient = new Ingredient(ing.Name, ing.Quantity, ing.Unit);
            processedIngredients.Add(ingredient);
        }

        // Replace the original list with the processed list
        this.ingredients = processedIngredients;
    }

    private void ProcessRecipes(List<Recipe> recipes)
    {
        // Create a new list to store processed recipes
        var processedRecipes = new List<Recipe>();

        foreach (var rec in recipes)
        {
            recipe = new Recipe(rec.Name, rec.Difficulty, rec.Equipment, rec.Steps);
            // Process steps using the ProcessSteps method
            var newSteps = new List<Step>();
            ProcessSteps(newSteps); // Delegate step processing to the method
            recipe.Steps.AddRange(newSteps);
            processedRecipes.Add(recipe);
        }

        // Replace the original list with the processed list
        this.recipes = processedRecipes;
    }
    private void ProcessSteps(List<Step> steps)
    {
        // This method can be used to process the step data further if needed
        // For now, it just initializes the steps list
        foreach (var st in steps)
        {
            step = new Step(st.Description, st.Duration);
            steps.Add(step);
        }
    }


    public RecipeData GetRecipeData()
    {
        return recipeData;
    }

    public List<Recipe> GetRecipes()
    {
        return recipes;
    }

    public List<Ingredient> GetIngredients()
    {
        return ingredients;
    }
}