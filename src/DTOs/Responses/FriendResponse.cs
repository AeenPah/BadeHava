using System.ComponentModel.DataAnnotations;

namespace BadeHava.DTOs;

public class FriendResponse
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string Username { get; set; } = null!;

    public string? AvatarPicUrl { get; set; }

    [Required]
    public DateTime CreateAt { get; set; }
}