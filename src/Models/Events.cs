using System.ComponentModel.DataAnnotations;

namespace BadeHava.Models;

public class Events
{
    [Key]
    public int Id { get; set; }
    public required string EventType { get; set; }
    public required string SenderUserId { get; set; }
    public required string ReceiveUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}