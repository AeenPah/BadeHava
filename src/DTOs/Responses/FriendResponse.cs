using System.ComponentModel.DataAnnotations;

namespace BadeHava.DTOs;

public class FriendResponse
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public DateTime CreateAt { get; set; }
}