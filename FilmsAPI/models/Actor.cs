using System.Text.Json.Serialization;

public class Actor
{
    public int Id { get; set; }
    
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("birth_date")]
    public DateTime BirthDate { get; set; }

    [JsonPropertyName("biography")]
    public string? Biography { get; set; }

    public string? PhotoFilePath { get; set; }
}
