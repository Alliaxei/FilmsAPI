public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public decimal Rating { get; set; }
    public string? Description { get; set; }
    public string? Photo { get; set; }
    public int StudioId { get; set; }
    public int AgeRatingId { get; set; }
}
