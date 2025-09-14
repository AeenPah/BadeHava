using System.Security.Claims;
using BadeHava.DTOs;
using BadeHava.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BadeHava.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    public AuthController(AuthService userService) { this._authService = userService; }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterUser(request.Username, request.Password);
        if (!result.Success) return Conflict(result);

        return Created("", result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginUser(request.Username, request.Password, Response);
        if (!result.Success) return Conflict(result);

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var result = await _authService.RefreshAuth(Request, Response);
        if (!result.Success) return Unauthorized(result);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return BadRequest();

        var result = await _authService.Logout(int.Parse(userId));
        if (!result.Success) return BadRequest(result);

        return Ok(result);
    }
}