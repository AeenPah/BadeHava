using BadeHava.DTOs;
using BadeHava.Services;
using Microsoft.AspNetCore.Mvc;

namespace BadeHava.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    public UserController(UserService userService) { this._userService = userService; }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        var result = await _userService.UserSearch(request.searchInput);

        return Ok(result);
    }
}