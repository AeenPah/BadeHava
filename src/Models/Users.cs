using System.ComponentModel.DataAnnotations;

namespace BadeHava.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    [Required]
    public byte[] Salt { get; set; } = null!;

    public string? RefreshToken { get; set; } = null;

    public DateTime? RefreshTokenExpire { get; set; } = null;

    public string? AvatarPicUrl { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}