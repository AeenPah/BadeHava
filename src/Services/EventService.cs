using BadeHava.Data;
using BadeHava.DTOs;
using BadeHava.Models;
using Microsoft.EntityFrameworkCore;

namespace BadeHava.Services;

public class EventService
{
    private readonly AppDbContext _dbContext;
    public EventService(AppDbContext dbContext)
    {
        this._dbContext = dbContext;
    }

    public async Task<Response<List<EventsResponse>>> GetEvents(int userId)
    {
        var notifications = await _dbContext.Events
                   .Include(e => e.Receiver)
                   .Where(e => e.ReceiveUserId == userId && e.Status == Events.EventStatusEnum.Pending)
                   .Select(e => new EventsResponse
                   {
                       EventId = e.Id,
                       EventType = e.EventType,
                       Sender = new EventsResponse.UserType
                       {
                           UserId = e.SenderUserId,
                           Username = e.Sender.Username,
                           AvatarPicUrl = e.Sender.AvatarPicUrl
                       },
                       CreatedAt = e.CreatedAt,
                       Seen = e.Seen,
                       Status = e.Status
                   })
                   .ToListAsync();

        return new Response<List<EventsResponse>>
        {
            Success = true,
            Message = "Success",
            Data = notifications,
        };
    }
}