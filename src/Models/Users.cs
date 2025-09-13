using System.ComponentModel.DataAnnotations;

namespace BadeHava.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required byte[] Salt { get; set; }
    public string? RefreshToken { get; set; } = null;
    public DateTime? RefreshTokenExpire { get; set; } = null;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}