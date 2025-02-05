public class Favorite
{
    public int Id { get; set; }
    public int users_id { get; set; }
    public int movies_id { get; set; }

    public Movie Movie { get; set; } = new Movie();
}
