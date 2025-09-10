using Microsoft.EntityFrameworkCore;
using BadeHava.Data;
using BadeHava.Models;
using BadeHava.Utils;
using BadeHava.DTOs;

namespace BadeHava.Services;

public class UserService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;

    public UserService(AppDbContext dbContext, IConfiguration configuration)
    {
        this._dbContext = dbContext;
        this._configuration = configuration;
    }

    public async Task<Response<RegisterResponse>> RegisterUser(string username, string password)
    {
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (existingUser is not null)
            return new Response<RegisterResponse>
            {
                Success = false,
                Message = "User does not exist"
            };

        var (PasswordHash, salt) = PasswordHasher.Hash(password);

        User user = new User
        {
            PasswordHash = PasswordHash,
            Salt = salt,
            Username = username
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return new Response<RegisterResponse>
        {
            Success = true,
            Message = "User registered successfully",
            Data = new RegisterResponse
            {
                User = user
            }
        };
    }

    public async Task<Response<LoginResponse>> LoginUser(string username, string password)
    {
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (existingUser is null)
            return new Response<LoginResponse>
            {
                Success = false,
                Message = "User does not exist"
            };

        if (!PasswordHasher.Validate(password, existingUser.PasswordHash, existingUser.Salt))
            return new Response<LoginResponse>
            {
                Success = false,
                Message = "Incorrect password"
            };

        string secret = _configuration["JWT:Secret"] ?? string.Empty;
        string refreshToken = TokenHandler.GenerateRefreshToken();
        string accessToken = TokenHandler.GenerateAccessToken(existingUser, secret);

        return new Response<LoginResponse>
        {
            Success = true,
            Message = "Login successful",
            Data = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = existingUser
            }
        };
    }

}