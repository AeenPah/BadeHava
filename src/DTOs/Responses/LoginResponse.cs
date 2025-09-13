using BadeHava.Models;

namespace BadeHava.DTOs;

public class LoginResponse
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public string? AccessToken { get; set; }
}