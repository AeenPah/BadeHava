using System.ComponentModel.DataAnnotations;
using BadeHava.Data;
using BadeHava.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BadeHava.Hubs;

public class HubResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static HubResponse<T> Ok(T data, string? message = null) =>
        new HubResponse<T> { Success = true, Data = data, Message = message };

    public static HubResponse<T> Fail(string message) =>
        new HubResponse<T> { Success = false, Message = message };
}
public class HubResponse : HubResponse<object> { }

public class ChatRoomMember
{
    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public string UserName { get; set; } = null!;

    public string? Avatar { get; set; }
}


[Authorize]
public class PresenceHub : Hub
{
    private readonly AppDbContext _dbContext;
    private static Dictionary<string, List<ChatRoomMember>> ChatRooms = new();

    public PresenceHub(AppDbContext appDbContext) { this._dbContext = appDbContext; }

    /* -------------------------------------------------------------------------- */
    /*                                Helper Methods                              */
    /* -------------------------------------------------------------------------- */

    private async Task<List<ChatRoomMember>?> GetRoomOrFail(string roomId)
    {
        if (!ChatRooms.TryGetValue(roomId, out var members))
        {
            await Clients.Caller.SendAsync("Response", HubResponse.Fail("Room is not valid"));
            return null;
        }
        return members;
    }

    private async Task<ChatRoomMember?> EnsureUserInRoom(List<ChatRoomMember> members, string userId)
    {
        var member = members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            await Clients.Caller.SendAsync("Response", HubResponse.Fail("You are not a member of this room"));
        }
        return member;
    }

    /* -------------------------------------------------------------------------- */
    /*                              Override Methods                              */
    /* -------------------------------------------------------------------------- */
    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"Connected: {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    /* -------------------------------------------------------------------------- */
    /*                                   Events                                   */
    /* -------------------------------------------------------------------------- */

    public async Task RequestChat(string receiverUserId)
    {
        var senderId = Context.UserIdentifier!;

        var sender = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(senderId));
        if (sender is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", HubResponse.Fail("User not found."));
            return;
        }
        var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(receiverUserId));
        if (receiver is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", HubResponse.Fail("User not found."));
            return;
        }

        var existedEvent = await _dbContext.Events.FirstOrDefaultAsync(e =>
            e.SenderUserId == int.Parse(senderId)
            && e.ReceiveUserId == int.Parse(receiverUserId)
            && e.EventType == Events.EventTypeEnum.ChatRequest
            && e.Status == Events.EventStatusEnum.Pending);
        if (existedEvent is not null)
        {
            await Clients.Caller.SendAsync("FailedRequest", HubResponse.Fail("Request is already exist!"));
            return;
        }

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
        await Clients.User(receiverUserId).SendAsync("ChatRequest", new { senderId, senderUsername = sender.Username });

        // Confirm to sender
        await Clients.Caller.SendAsync("RequestSent", "Chat request sent successfully!");
    }

    public async Task AcceptChatRequest(int eventId)
    {
        // Validate event
        var chatReqEvent = await _dbContext.Events
            .Include(e => e.Sender)
            .Include(e => e.Receiver)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (chatReqEvent is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", HubResponse.Fail("Request is not valid"));
            return;
        }
        chatReqEvent.Status = Events.EventStatusEnum.Accepted;

        string roomId = $"{chatReqEvent.SenderUserId}-{chatReqEvent.ReceiveUserId}";
        UserGroupChat[] userGroupChats =
        {
            new UserGroupChat { GroupChatId = roomId, UserId = chatReqEvent.SenderUserId },
            new UserGroupChat { GroupChatId = roomId, UserId = chatReqEvent.ReceiveUserId }
        };

        _dbContext.UserGroupChat.AddRange(userGroupChats);
        await _dbContext.SaveChangesAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        await Clients.User(chatReqEvent.SenderUserId.ToString()).SendAsync("UserAcceptChatRequest", roomId);

        await Clients.Caller.SendAsync("JoinedRoom", new { chatReqEvent.Sender.Username, chatReqEvent.Sender.Id, chatReqEvent.Sender.AvatarPicUrl });
    }

    public async Task JoinChatRoom(string roomId)
    {
        var userId = Context.UserIdentifier!;

        var chatRoom = _dbContext.UserGroupChat.FirstOrDefaultAsync(gc => gc.UserId == int.Parse(userId) && gc.GroupChatId == roomId);
        if (chatRoom is null)
        {
            await Clients.Caller.SendAsync("Failed Request", HubResponse.Fail("No chat room found!"));
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Caller.SendAsync("JoinedChatRoom", "Success");
    }

    public async Task RefuseChatRequest(int eventId)
    {
        // Validate event
        var chatReqEvent = await _dbContext.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (chatReqEvent is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", HubResponse.Fail("Request is not valid"));
            return;
        }
        chatReqEvent.Status = Events.EventStatusEnum.Declined;
        await _dbContext.SaveChangesAsync();

        await Clients.User(chatReqEvent.SenderUserId.ToString()).SendAsync("ChatReqRefused", $"Chat Req refused from {chatReqEvent.Receiver.Username} successfully");
        await Clients.Caller.SendAsync("ChatReqRefused", "Chat Req refused successfully");
    }

    public async Task SendMessage(string roomId, string message)
    {
        List<ChatRoomMember>? members = await GetRoomOrFail(roomId);
        if (members is null) return;

        var userId = Context.UserIdentifier!;
        ChatRoomMember? userMember = await EnsureUserInRoom(members, userId);
        if (userMember is null) return;

        await Clients.Caller.SendAsync("My-Message", message);
        await Clients.OthersInGroup(roomId).SendAsync("Message", userId, message);
    }
}