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
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = await _userService.RegisterUser(dto.Username, dto.Password);
        if (user is null) return Conflict(new { message = "This username already exists." });

        return Created("", new { message = "Register successful", username = user.Username });
    }
}