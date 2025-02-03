
using System.Text.Json.Serialization;

public class Studio
{
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
     
    // ������ �������, ������������� ������
    public List<Movie> Movies { get; set; } = new List<Movie>();
}       
