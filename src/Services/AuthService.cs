using Microsoft.EntityFrameworkCore;
using BadeHava.Data;
using BadeHava.Models;
using BadeHava.Utils;
using BadeHava.DTOs;

namespace BadeHava.Services;

public class AuthService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;

    public AuthService(AppDbContext dbContext, IConfiguration configuration)
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
                Message = "Username already exist!"
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

    public async Task<Response<LoginResponse>> LoginUser(string username, string password, HttpResponse httpResponse)
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

        // Generate tokens
        string secret = _configuration["JWT:Secret"] ?? string.Empty;
        string issuer = _configuration["JWT:Issuer"] ?? string.Empty;
        string audience = _configuration["JWT:Audience"] ?? string.Empty;
        string refreshToken = TokenHandler.GenerateRefreshToken();
        string accessToken = TokenHandler.GenerateAccessToken(existingUser, secret, issuer, audience);

        // Update Users refreshToken DB
        existingUser.RefreshToken = refreshToken;
        existingUser.RefreshTokenExpire = DateTime.UtcNow.AddDays(3);
        _dbContext.Users.Update(existingUser);
        await _dbContext.SaveChangesAsync();

        // Save refresh Token in the cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            // TODO: make it true later
            Secure = false,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(4)
        };
        httpResponse.Cookies.Append("refreshToken", refreshToken, cookieOptions);

        return new Response<LoginResponse>
        {
            Success = true,
            Message = "Login successful",
            Data = new LoginResponse
            {
                UserId = existingUser.Id,
                AccessToken = accessToken,
                Username = existingUser.Username,
            }
        };
    }

    public async Task<Response<RefreshResponse>> RefreshAuth(HttpRequest httpRequest, HttpResponse httpResponse)
    {
        var refreshToken = httpRequest.Cookies["refreshToken"];

        if (refreshToken is null)
            return new Response<RefreshResponse>
            {
                Success = false,
                Message = "Refresh token does not found!"
            };

        var user = _dbContext.Users.FirstOrDefault(u =>
            u.RefreshToken == refreshToken
            && u.RefreshTokenExpire > DateTime.UtcNow);

        if (user is null)
            return new Response<RefreshResponse>
            {
                Success = false,
                Message = "User does not found!"
            };

        // Generate tokens
        string secret = _configuration["JWT:Secret"] ?? string.Empty;
        string issuer = _configuration["JWT:Issuer"] ?? string.Empty;
        string audience = _configuration["JWT:Audience"] ?? string.Empty;
        string newRefreshToken = TokenHandler.GenerateRefreshToken();
        string newAccessToken = TokenHandler.GenerateAccessToken(user, secret, issuer, audience);

        // Update Users refreshToken DB
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpire = DateTime.UtcNow.AddDays(3);
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        // Save refresh Token in the cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // TODO: make it true later
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(4)
        };
        httpResponse.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);

        return new Response<RefreshResponse>
        {
            Success = true,
            Message = "Refresh token updated successful",
            Data = new RefreshResponse
            {
                UserId = user.Id,
                AccessToken = newAccessToken,
                Username = user.Username,
            }
        };
    }
}