using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadeHava.Models;

public class Friendships
{
    [Key]
    public int FriendshipId { get; set; }

    [Required]
    public int UserId1 { get; set; }

    [Required]
    public int UserId2 { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    [ForeignKey(nameof(UserId1))]
    public User User1 { get; set; } = null!;

    [ForeignKey(nameof(UserId2))]
    public User User2 { get; set; } = null!;
}