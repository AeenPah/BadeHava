namespace BadeHava.DTOs;

public class RefreshResponse
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public string? AccessToken { get; set; }
}