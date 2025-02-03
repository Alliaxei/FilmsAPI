public class Review
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int UserId { get; set; }
    public string ReviewText { get; set; } = string.Empty;
    public decimal Rating { get; set; }
}
