using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadeHava.Models;


public class Events
{
    public enum EventTypeEnum
    {
        FriendRequest,
        Block,
        ChatRequest
    }

    [Key]
    public int Id { get; set; }

    public EventTypeEnum EventType { get; set; }

    [Required]
    public int SenderUserId { get; set; }

    [Required]
    public int ReceiveUserId { get; set; }

    public bool Seen = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SenderUserId))]
    public User Sender { get; set; } = null!;

    [ForeignKey(nameof(ReceiveUserId))]
    public User Receiver { get; set; } = null!;
}
