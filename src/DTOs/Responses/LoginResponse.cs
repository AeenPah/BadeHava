using BadeHava.Models;

namespace BadeHava.DTOs;

public class LoginResponse
{
    public User? User { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}