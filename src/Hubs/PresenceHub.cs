using BadeHava.Data;
using BadeHava.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BadeHava.Hubs;

[Authorize]
public class PresenceHub : Hub
{
    private readonly AppDbContext _dbContext;
    // private static Dictionary<string, string> OnlineUsers = new();
    private static Dictionary<string, List<string>> ChatRooms = new();

    public PresenceHub(AppDbContext appDbContext) { this._dbContext = appDbContext; }

    /* -------------------------------------------------------------------------- */
    /*                              Override Methods                              */
    /* -------------------------------------------------------------------------- */
    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"Connected: {Context.ConnectionId}");
        // OnlineUsers[Context.UserIdentifier!] = Context.ConnectionId;
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // OnlineUsers.Remove(Context.UserIdentifier!);
        // var userId = Context.UserIdentifier!;
        // var connectionId = Context.ConnectionId;

        // var roomsWithUser = ChatRooms
        //     .Where(r => r.Value.Contains(userId))
        //     .Select(r => r.Key)
        //     .ToList();

        // foreach (var roomId in roomsWithUser)
        // {
        // ChatRooms[roomId].Remove(userId);

        // if (ChatRooms[roomId].Count() == 0) ChatRooms.Remove(roomId);

        // await Groups.RemoveFromGroupAsync(connectionId, roomId);
        // await Clients.Group(roomId).SendAsync("UserLeft", userId, userId);
        // }

        await base.OnDisconnectedAsync(exception);
    }

    /* -------------------------------------------------------------------------- */
    /*                                   Events                                   */
    /* -------------------------------------------------------------------------- */

    public async Task RequestChat(string receiverUserId)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId))
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "Unauthorized user.");
            return;
        }

        var sender = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(senderId));
        if (sender is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "User not found.");
            return;
        }
        var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(receiverUserId));
        if (receiver is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "User not found.");
            return;
        }

        string roomId = $"{senderId}-{receiverUserId}";
        if (ChatRooms.ContainsKey(roomId))
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "Chat room is already existed.");
            return;
        }

        ChatRooms[roomId] = [senderId, receiverUserId];
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Db Saver Request
        var RequestEvent = new Events
        {
            EventType = Events.EventTypeEnum.ChatRequest,
            ReceiveUserId = int.Parse(receiverUserId),
            SenderUserId = int.Parse(senderId)
        };
        _dbContext.Events.Add(RequestEvent);
        await _dbContext.SaveChangesAsync();

        // Notify receiver
        await Clients.User(receiverUserId).SendAsync("ChatRequest", senderId, new { senderId, senderUsername = sender.Username });

        // Confirm to sender
        await Clients.Caller.SendAsync("RequestSent", receiverUserId, new { roomId, message = "Chat request sent successfully!" });
    }

    public async Task JoinChat(int eventId)
    {
        var chatReqEvent = await _dbContext.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (chatReqEvent is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "Request is not valid");
            return;
        }
        chatReqEvent.Status = Events.EventStatusEnum.Accepted;
        await _dbContext.SaveChangesAsync();

        string roomId = $"{chatReqEvent.SenderUserId}-{chatReqEvent.ReceiveUserId}";

        if (!ChatRooms.TryGetValue(roomId, out var members))
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "Room is not valid");
            return;
        }
        var userId = Context.UserIdentifier!;
        if (!members.Contains(userId))
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "You are not a member of this room");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        await Clients.Group(roomId).SendAsync("UserJoined", userId, userId);
        await Clients.Caller.SendAsync("JoinedRoom", roomId);
    }

    public async Task RefuseChatRequest(int eventId)
    {
        var chatReqEvent = await _dbContext.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (chatReqEvent is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "Request is not valid");
            return;
        }
        chatReqEvent.Status = Events.EventStatusEnum.Declined;
        await _dbContext.SaveChangesAsync();

        string roomId = $"{chatReqEvent.SenderUserId}-{chatReqEvent.ReceiveUserId}";
        ChatRooms.Remove(roomId);
        await Clients.Caller.SendAsync("ChatReqRefused", null, "Chat Req Failed");
    }

    public async Task SendMessage(string roomId, string message)
    {
        var userId = Context.UserIdentifier!;

        if (!ChatRooms.TryGetValue(roomId, out var members))
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "Room is not valid");
            return;
        }
        if (!members.Contains(userId))
        {
            await Clients.Caller.SendAsync("FailedRequest", null, "You are not a member of this room");
            return;
        }

        await Clients.Group(roomId).SendAsync("Message", userId, message);
    }
}