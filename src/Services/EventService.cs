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

    public async Task<Response<object>> RequestFriendship(string senderId, string receiverUserId)
    {
        var existingRequest = await _dbContext.Events
                      .FirstOrDefaultAsync(e => e.SenderUserId == int.Parse(senderId)
                        && e.ReceiveUserId == int.Parse(receiverUserId)
                        && e.EventType == Events.EventTypeEnum.FriendRequest
                        && e.Status == Events.EventStatusEnum.Pending);
        if (existingRequest is not null)
        {
            return new Response<object>
            {
                Success = false,
                Message = "Already requested!"
            };
        }

        // DB save the request
        var requestEvent = new Events
        {
            EventType = Events.EventTypeEnum.FriendRequest,
            ReceiveUserId = int.Parse(receiverUserId),
            SenderUserId = int.Parse(senderId!),
        };
        _dbContext.Events.Add(requestEvent);
        await _dbContext.SaveChangesAsync();

        return new Response<object>
        {
            Success = true,
            Message = "Success"
        };
    }

    public async Task<Response<object>> RespondToFriendRequest(string userId, int eventId, string action)
    {
        var friendRequest = await _dbContext.Events
              .FirstOrDefaultAsync(e => e.Id == eventId
                                    && e.ReceiveUserId == int.Parse(userId));
        if (friendRequest is null)
        {
            return new Response<object>
            {
                Success = false,
                Message = "Request does not found!"
            };
        }

        switch (action)
        {
            case "Accept":
                friendRequest.Status = Events.EventStatusEnum.Accepted;

                var friendShip = new Friendships
                {
                    UserId1 = int.Parse(userId),
                    UserId2 = friendRequest.SenderUserId
                };
                _dbContext.Friendships.Add(friendShip);
                break;
            case "Decline":
                friendRequest.Status = Events.EventStatusEnum.Declined;
                break;
            default:
                break;
        }

        await _dbContext.SaveChangesAsync();

        return new Response<object>
        {
            Success = true,
            Message = "Success"
        };
    }
}