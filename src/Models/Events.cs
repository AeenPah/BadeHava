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
    public enum EventStatusEnum
    {
        Pending,
        Accepted,
        Declined
    }

    [Key]
    public int Id { get; set; }

    public EventTypeEnum EventType { get; set; }

    public EventStatusEnum Status { get; set; } = EventStatusEnum.Pending;

    [Required]
    public int SenderUserId { get; set; }

    [Required]
    public int ReceiveUserId { get; set; }

    public bool Seen { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SenderUserId))]
    public User Sender { get; set; } = null!;

    [ForeignKey(nameof(ReceiveUserId))]
    public User Receiver { get; set; } = null!;
}
