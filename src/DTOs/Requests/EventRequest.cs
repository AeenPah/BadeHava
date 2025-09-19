using System.ComponentModel.DataAnnotations;

namespace BadeHava.DTOs;

public class EventRequest
{
    [Required]
    public string receiverUserId { get; set; } = null!;
}


public class EventRespondToFriendRequestRequest
{
    [Required]
    public int EventId { get; set; }

    [Required]
    public string Action { get; set; } = null!;
}