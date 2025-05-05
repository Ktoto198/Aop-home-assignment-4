using System.Text.Json.Serialization;

namespace ResSim.Models;

public class Step
{
    [JsonPropertyName("step")]
    public string Description { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }
    public bool IsCompleted { get; set; } = false;
    public bool IsActive { get; set; } = false;

    public Step() { }

    public Step(string description, int duration)
    {
        Description = description;
        Duration = duration;
    }
}