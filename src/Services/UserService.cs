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

    public async Task<Response<SearchResponse>> UserSearch(string inputValue)
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
            .Where(u => u.Username.Contains(inputValue.ToLower()))
            .Select(u => new UserSearchResult
            {
                Username = u.Username,
                UserId = u.Id
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
}