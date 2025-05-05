using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ResSim.Models;
public class Recipe
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("difficulty")]
    public string? Difficulty { get; set; }

    [JsonPropertyName("equipment")]
    public List<string>? Equipment { get; set; } = new List<string>();

    [JsonPropertyName("steps")]
    public List<Step>? Steps { get; set; } = new List<Step>();

    // Indicates whether the recipe is currently being prepared
    public bool IsActive { get; set; } = false;

    // Indicates whether the recipe has been fully completed
    public bool IsCompleted { get; set; } = false;

    public Recipe() { }
    public Recipe(string name, string difficulty, List<string?> equiment, List<Step?> steps)
    {
        Name = name;
        Difficulty = difficulty;
        Equipment = equiment ?? new List<string>();
        Steps = steps ?? new List<Step>();
    }

}