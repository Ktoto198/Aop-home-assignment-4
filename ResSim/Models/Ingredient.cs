
using System.Text.Json.Serialization;

namespace ResSim.Models;

public class Ingredient
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("quantity")]
    public string QuantityRaw { get; set; } // Backing field for the raw JSON value

    [JsonIgnore]
    public int Quantity // Expose as an int
    {
        get => int.TryParse(QuantityRaw, out int result) ? result : 0; // Default to 0 if parsing fails
        set => QuantityRaw = value.ToString();
    }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }
    
    public Ingredient(){}
    public Ingredient(string name, int quantity, string unit)
    {
        Name = name;
        Quantity = quantity;
        Unit = unit;
    }
}