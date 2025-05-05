using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace ResSim.Models;

public class RecipeData
{
    public List<Ingredient>? ingredients;
    public List<Recipe>? recipes;

    public RecipeData(){}
    public RecipeData(List<Ingredient>? ingredients, List<Recipe>? recipes)
    {
        this.ingredients = ingredients ?? new List<Ingredient>();
        this.recipes = recipes ?? new List<Recipe>();
    }

}