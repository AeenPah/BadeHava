using BadeHava.Data;
using BadeHava.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BadeHava.Hubs;

public class PresenceHub : Hub
{
    private readonly AppDbContext _dbContext;
    private static Dictionary<string, string> OnlineUsers = new();

    public PresenceHub(AppDbContext appDbContext) { this._dbContext = appDbContext; }

    /* -------------------------------------------------------------------------- */
    /*                              Override Methods                              */
    /* -------------------------------------------------------------------------- */
    public override Task OnConnectedAsync()
    {
        OnlineUsers[Context.UserIdentifier!] = Context.ConnectionId;
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        OnlineUsers.Remove(Context.UserIdentifier!);
        return base.OnDisconnectedAsync(exception);
    }

    /* -------------------------------------------------------------------------- */
    /*                                   Events                                   */
    /* -------------------------------------------------------------------------- */
    public async Task RequestFriendship(string receiverUserId)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId))
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "Unauthorized user.");
            return;
        }

        var existingRequest = await _dbContext.Events
              .FirstOrDefaultAsync(e => e.SenderUserId == senderId && e.ReceiveUserId == receiverUserId);
        if (existingRequest is not null)
        {
            await Clients.User(senderId).SendAsync("FailedRequest", null, "Friend request already sent!");
            return;
        }

        // DB save the request
        var requestEvent = new Events
        {
            EventType = "FriendRequest",
            ReceiveUserId = receiverUserId,
            SenderUserId = senderId!,
        };
        _dbContext.Events.Add(requestEvent);
        await _dbContext.SaveChangesAsync();

        // Notify receiver
        await Clients.User(receiverUserId).SendAsync("FriendRequest", senderId, "You have a new friend request!");

        // Confirm to sender
        await Clients.User(senderId).SendAsync("RequestSent", receiverUserId, "Friend request sent successfully!");
    }
}