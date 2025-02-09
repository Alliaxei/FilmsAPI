using System.Text.Json.Serialization;
public class UserResponse
{
    [JsonPropertyName("user")]
    public User? User { get; set; }
}

public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("roles_id")]
    public int RolesId { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    public string? Password { get; set; }
    public string? Password_confirmation { get; set; }
    public string? ApiToken { get; set; }  
    public string? Avatar { get; set; }
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }
    public DateTime? CreatedAt { get; set; }  
    public DateTime? UpdatedAt { get; set; }  
}
