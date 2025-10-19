using BadeHava.Data;
using BadeHava.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BadeHava.Services;

public class UserService
{
    private readonly AppDbContext _dbContext;

    public UserService(AppDbContext dbContext)
    {
        this._dbContext = dbContext;
    }

    public async Task<Response<List<FriendResponse>>> UserFriends(int userId)
    {
        var friends = await _dbContext.Friendships
            .Where(f => f.UserId1 == userId || f.UserId2 == userId)
            .Select(f => new FriendResponse
            {
                UserId = f.UserId1 == userId ? f.User2.Id : f.User1.Id,
                Username = f.UserId1 == userId ? f.User2.Username : f.User1.Username,
                AvatarPicUrl = f.UserId1 == userId ? f.User2.AvatarPicUrl : f.User1.AvatarPicUrl,
                CreateAt = f.CreatedAt
            })
            .ToListAsync();


        return new Response<List<FriendResponse>>
        {
            Message = "Success",
            Success = true,
            Data = friends
        };
    }

    public async Task<Response<SearchResponse>> UserSearch(string inputValue, int userId)
    {
        if (string.IsNullOrWhiteSpace(inputValue))
        {
            return new Response<SearchResponse>
            {
                Success = true,
                Message = "No search input provided",
                Data = new SearchResponse { Users = new List<UserSearchResult>() }
            };
        }

        var foundUsers = await _dbContext.Users
            .Where(u =>
                u.Username.Contains(inputValue.ToLower())
                && u.Id != userId)
            .Select(u => new UserSearchResult
            {
                Username = u.Username,
                UserId = u.Id,
                AvatarPicUrl = u.AvatarPicUrl,
            })
            .Take(10)
            .ToListAsync();

        return new Response<SearchResponse>
        {
            Success = true,
            Message = "Found users successfully.",
            Data = new SearchResponse
            {
                Users = foundUsers
            },
        };
    }

    public async Task<Response<object>> userAvatarUrl(int userId, string url)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return new Response<object>
            {
                Success = false,
                Message = "User Not found",
            };

        user.AvatarPicUrl = url;
        await _dbContext.SaveChangesAsync();

        return new Response<object>
        {
            Success = true,
            Message = "Success"
        };
    }
}