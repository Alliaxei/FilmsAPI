public class User
{
    public int Id { get; set; }
    public int RolesId { get; set; } 
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Password_confirmation { get; set; }
    public string? ApiToken { get; set; }  
    public string? Avatar { get; set; }
    public string? Gender { get; set; }
    public DateTime? CreatedAt { get; set; }  
    public DateTime? UpdatedAt { get; set; }  
}
