using System.Text.Json.Serialization;

public class MovieRating
{
    public int Id { get; set; }

    [JsonPropertyName("movies_id")]
    public int MoviesId { get; set; }

    public int UsersId { get; set; }

    [JsonPropertyName("review_text")]
    public string? ReviewText { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Movie? Movie { get; set; }
    public User? User { get; set; }
}
