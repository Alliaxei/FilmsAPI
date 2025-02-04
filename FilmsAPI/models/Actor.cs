using System.Text.Json.Serialization;

public class Actor
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("birth_date")]
    public DateTime BirthDate { get; set; }

    [JsonPropertyName("biography")]
    public string? Biography { get; set; }

    public string? PhotoFilePath { get; set; }
}
