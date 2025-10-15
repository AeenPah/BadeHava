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
    public string Message { get; set; } = null!;
    public T? Data { get; set; }

    public static HubResponse<T> Ok(T data, string message) =>
        new HubResponse<T> { Success = true, Data = data, Message = message };

    public static HubResponse<T> Fail(string message) =>
        new HubResponse<T> { Success = false, Message = message };
}
public class HubResponse : HubResponse<object> { }

[Authorize]
public class PresenceHub : Hub
{
    private readonly AppDbContext _dbContext;
    private static Dictionary<string, List<ChatRoomMember>> ChatRooms = new();

    public PresenceHub(AppDbContext appDbContext) { this._dbContext = appDbContext; }

    /* -------------------------------------------------------------------------- */
    /*                                    Types                                   */
    /* -------------------------------------------------------------------------- */

    protected class ChatRoomMember
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string UserName { get; set; } = null!;

        public string? Avatar { get; set; }
    }
    protected class ChatMessage
    {
        [Required]
        public string Message { get; set; } = null!;

        [Required]
        public int From { set; get; }

        public bool Seen { get; set; } = false;
    }
    protected class ChatUser
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Username { get; set; } = null!;

        public string? UserAvatarUrl { get; set; }
    }
    protected class RespondRequest
    {
        [Required]
        public ChatUser user { set; get; } = null!;

        [Required]
        public string Action { set; get; } = null!;
    }

    /* -------------------------------------------------------------------------- */
    /*                                Helper Methods                              */
    /* -------------------------------------------------------------------------- */

    // private async Task<List<ChatRoomMember>?> GetRoomOrFail(string roomId)
    // {
    //     if (!ChatRooms.TryGetValue(roomId, out var members))
    //     {
    //         await Clients.Caller.SendAsync("Response", HubResponse.Fail("Room is not valid"));
    //         return null;
    //     }
    //     return members;
    // }

    // private async Task<ChatRoomMember?> EnsureUserInRoom(List<ChatRoomMember> members, string userId)
    // {
    //     var member = members.FirstOrDefault(m => m.UserId == userId);
    //     if (member is null)
    //     {
    //         await Clients.Caller.SendAsync("Response", HubResponse.Fail("You are not a member of this room"));
    //     }
    //     return member;
    // }

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
        ChatUser data = new()
        {
            UserId = senderId,
            Username = sender.Username,
            UserAvatarUrl = sender.AvatarPicUrl,
        };
        await Clients.User(receiverUserId).SendAsync("ChatRequest", HubResponse<ChatUser>.Ok(data, "New chat request received."));

        // Confirm to sender
        await Clients.Caller.SendAsync("RequestSent", HubResponse<object?>.Ok(null, "Chat request sent successfully!"));
    }

    public async Task AcceptChatRequest(int eventId)
    {
        // Validate event
        var chatReqEvent = await _dbContext.Events
            .Include(e => e.Sender)
            .Include(e => e.Receiver)
            .FirstOrDefaultAsync(e => e.Id == eventId
                && e.ReceiveUserId == int.Parse(Context.UserIdentifier!)
                && e.EventType == Events.EventTypeEnum.ChatRequest
                && e.Status == Events.EventStatusEnum.Pending);

        if (chatReqEvent is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", HubResponse.Fail("Request event is not valid"));
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


        ChatUser receiverUser = new()
        {
            UserId = chatReqEvent.Receiver.Id.ToString(),
            Username = chatReqEvent.Receiver.Username,
            UserAvatarUrl = chatReqEvent.Receiver.AvatarPicUrl,
        };
        ChatRoom chatRoom = new() { RoomId = roomId, RoomsUsers = [receiverUser] };
        await Clients.User(chatReqEvent.SenderUserId.ToString()).SendAsync("UserAcceptChatRequest", HubResponse<ChatRoom>.Ok(chatRoom, "User joined the room."));

        ChatUser senderUser = new()
        {
            UserId = chatReqEvent.Sender.Id.ToString(),
            Username = chatReqEvent.Sender.Username.ToString(),
            UserAvatarUrl = chatReqEvent.Sender.AvatarPicUrl
        };
        ChatRoom chatRoom2 = new() { RoomId = roomId, RoomsUsers = [senderUser] };
        await Clients.Caller.SendAsync("JoinedRoom", HubResponse<ChatRoom>.Ok(chatRoom2, "You joined the room!"));
    }
    protected class ChatRoom
    {
        [Required]
        public string RoomId { get; set; } = null!;

        [Required]
        public List<ChatUser> RoomsUsers { get; set; } = [];
    }

    public async Task JoinChatRoom(string roomId)
    {
        var userId = Context.UserIdentifier!;

        var chatRoom = _dbContext.UserGroupChat.FirstOrDefaultAsync(gc =>
            gc.UserId == int.Parse(userId)
            && gc.GroupChatId == roomId);
        if (chatRoom is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", HubResponse.Fail("No chat room found!"));
            return;
        }

        var otherRoomUsers = _dbContext.UserGroupChat
            .Include(gc => gc.User)
            .Where(gc =>
                gc.UserId != int.Parse(userId)
                && gc.GroupChatId == roomId)
            .Select(gc => new ChatUser
            {
                UserId = gc.User.Id.ToString(),
                Username = gc.User.Username,
                UserAvatarUrl = gc.User.AvatarPicUrl
            })
            .ToList();

        // TODO: May send to other in group that this member joined
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Caller.SendAsync("JoinedChatRoom", HubResponse<List<ChatUser>>.Ok(otherRoomUsers, "Success joined room"));
    }

    public async Task RefuseChatRequest(int eventId)
    {
        // Validate event
        var chatReqEvent = await _dbContext.Events
            .Include(e => e.Receiver)
            .FirstOrDefaultAsync(e =>
                e.Id == eventId
                && e.Status == Events.EventStatusEnum.Pending
                && e.ReceiveUserId == int.Parse(Context.UserIdentifier!));
        if (chatReqEvent is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", HubResponse.Fail("Request chat is not valid"));
            return;
        }

        chatReqEvent.Status = Events.EventStatusEnum.Declined;
        await _dbContext.SaveChangesAsync();

        ChatUser receiverUser = new()
        {
            UserId = chatReqEvent.Receiver.Id.ToString(),
            Username = chatReqEvent.Receiver.Username,
            UserAvatarUrl = chatReqEvent.Receiver.AvatarPicUrl
        };
        await Clients.User(chatReqEvent.SenderUserId.ToString()).SendAsync("ChatReqRefusedBy", HubResponse<ChatUser>.Ok(receiverUser, "Chat Req refused"));
        await Clients.Caller.SendAsync("ChatReqRefused", HubResponse<object?>.Ok(null, "Chat Req refused successfully"));
    }

    public async Task SendMessage(string roomId, string message)
    {
        // List<ChatRoomMember>? members = await GetRoomOrFail(roomId);
        // if (members is null) return;

        // var userId = Context.UserIdentifier!;
        // ChatRoomMember? userMember = await EnsureUserInRoom(members, userId);
        // if (userMember is null) return;

        // TODO: May later add validation for roomId and user
        ChatMessage chatMessage = new()
        {
            From = int.Parse(Context.UserIdentifier!),
            Message = message,
        };

        await Clients.Caller.SendAsync("My-Message", HubResponse<ChatMessage>.Ok(chatMessage, ""));
        await Clients.OthersInGroup(roomId).SendAsync("Message", HubResponse<ChatMessage>.Ok(chatMessage, ""));
    }

    public async Task FriendRequest(int receiverId)
    {
        var senderId = int.Parse(Context.UserIdentifier!);

        var existingRequest = await _dbContext.Events
            .FirstOrDefaultAsync(e => e.SenderUserId == senderId
                && e.ReceiveUserId == receiverId
                && e.EventType == Events.EventTypeEnum.FriendRequest
                && e.Status == Events.EventStatusEnum.Pending
            );
        if (existingRequest is not null)
        {
            await Clients.Caller.SendAsync("FailedRequest", "Already friend request exists.");
            return;
        }

        var existingFriend = await _dbContext.Friendships
            .FirstOrDefaultAsync(f =>
                (f.UserId1 == senderId && f.UserId2 == receiverId)
                || (f.UserId2 == senderId && f.UserId1 == receiverId)
            );
        if (existingFriend is not null)
        {
            await Clients.Caller.SendAsync("FailedRequest", "User is already your friend.");
            return;
        }

        // DB save the request
        var requestEvent = new Events
        {
            EventType = Events.EventTypeEnum.FriendRequest,
            ReceiveUserId = receiverId,
            SenderUserId = senderId,
        };
        _dbContext.Events.Add(requestEvent);
        await _dbContext.SaveChangesAsync();

        await Clients.Caller.SendAsync("FriendReqSend", HubResponse<object?>.Ok(null, "FriendRequest send successfully!"));
    }

    public async Task RespondFriendRequest(int eventId, string action)
    {
        var userId = int.Parse(Context.UserIdentifier!);

        var friendRequest = await _dbContext.Events
            .Include(e => e.Receiver)
            .FirstOrDefaultAsync(e => e.Id == eventId
                && e.ReceiveUserId == userId);
        if (friendRequest is null)
        {
            await Clients.Caller.SendAsync("FailedRequest", HubResponse.Fail("Request does not found!"));
            return;
        }

        switch (action)
        {
            case "Accept":
                friendRequest.Status = Events.EventStatusEnum.Accepted;

                var friendShip = new Friendships
                {
                    UserId1 = userId,
                    UserId2 = friendRequest.SenderUserId
                };
                _dbContext.Friendships.Add(friendShip);
                break;
            case "Decline":
                friendRequest.Status = Events.EventStatusEnum.Declined;
                break;
        }

        await _dbContext.SaveChangesAsync();

        RespondRequest resFriendReq = new()
        {
            user = new()
            {
                UserId = friendRequest.Receiver.Id.ToString(),
                Username = friendRequest.Receiver.Username,
                UserAvatarUrl = friendRequest.Receiver.AvatarPicUrl
            },
            Action = action
        };

        await Clients.Caller.SendAsync("RespondFriendRequest", HubResponse<object?>.Ok(null, "Friend request responded successfully!"));
        await Clients.User(friendRequest.SenderUserId.ToString()).SendAsync("SenderRespondFriendRequest", HubResponse<RespondRequest>.Ok(resFriendReq, $"User {action} your friend request"));
    }
}