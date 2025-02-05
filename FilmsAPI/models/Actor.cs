using System.Text.Json.Serialization;

public class Actor
{
    public int Id { get; set; }

    public string first_name { get; set; } = string.Empty;

    public string last_name { get; set; } = string.Empty;

    [JsonPropertyName("birth_date")]
    public DateTime birth_date { get; set; }

    [JsonPropertyName("biography")]
    public string? biography { get; set; }

    public string? PhotoFilePath { get; set; }
}
