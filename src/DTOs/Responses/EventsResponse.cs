using System.ComponentModel.DataAnnotations;
using static BadeHava.Models.Events;

namespace BadeHava.DTOs;

public class EventsResponse
{
    public class UserType
    {

        [Required]
        public string Username { get; set; } = null!;
        [Required]
        public int UserId { get; set; }
    }

    [Required]
    public int EventId { get; set; }

    public EventTypeEnum EventType { get; set; }

    public EventStatusEnum Status { get; set; } = EventStatusEnum.Pending;

    [Required]
    public UserType Sender { get; set; } = null!;

    public bool Seen { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public static implicit operator EventsResponse(List<EventsResponse> v)
    {
        throw new NotImplementedException();
    }
}