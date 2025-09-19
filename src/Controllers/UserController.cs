using System.Security.Claims;
using BadeHava.DTOs;
using BadeHava.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BadeHava.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    public UserController(UserService userService) { this._userService = userService; }

    [Authorize]
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        var result = await _userService.UserSearch(request.searchInput);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("friends")]
    public async Task<IActionResult> Friends()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _userService.UserFriends(int.Parse(userId!));
        return Ok(result);
    }
}