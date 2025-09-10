using Microsoft.EntityFrameworkCore;
using BadeHava.Data;
using BadeHava.Models;
using BadeHava.Utils;

namespace BadeHava.Services;

public class UserService
{
    private readonly AppDbContext _dbContext;

    public UserService(AppDbContext dbContext) { this._dbContext = dbContext; }

    public async Task<User?> RegisterUser(string username, string password)
    {
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (existingUser is not null)
            return null;

        string PasswordHash = PasswordHasher.Hash(password);

        User user = new User
        {
            PasswordHash = PasswordHash,
            Username = username
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }
}