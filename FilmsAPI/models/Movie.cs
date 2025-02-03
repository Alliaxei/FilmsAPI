public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int ReleaseYear { get; set; }
    public int Duration { get; set; }
    public string Description { get; set; }
    public string Photo { get; set; }
    public Studio Studio { get; set; }
    public AgeRating AgeRating { get; set; }
    public List<MovieRating> Rating { get; set; } = new List<MovieRating>(); 
}
