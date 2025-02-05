using System.Numerics;

public class Movie
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public int ReleaseYear { get; set; }
    public int? Duration { get; set; }
    public string? Description { get; set; }
    public string? Photo { get; set; }
    public int? studio_id { get; set; }
    public int? age_rating_id { get; set; }
    public Studio? Studio { get; set; }
    public AgeRating? age_rating { get; set; }
    public List<MovieRating> Rating { get; set; } = new List<MovieRating>(); 
}
