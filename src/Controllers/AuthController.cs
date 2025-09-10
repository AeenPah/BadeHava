using BadeHava.DTOs;
using BadeHava.Services;
using Microsoft.AspNetCore.Mvc;

namespace BadeHava.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    public AuthController(UserService userService) { this._userService = userService; }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _userService.RegisterUser(request.Username, request.Password);
        if (!result.Success) return Conflict(result);

        return Created("", result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _userService.LoginUser(request.Username, request.Password);
        if (!result.Success) return Conflict(result);

        return Ok(result);
    }
}