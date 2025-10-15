using System.Security.Claims;
using BadeHava.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BadeHava.Controllers;


[ApiController]
[Route("api/[controller]")]
public class EventController : ControllerBase
{
    private readonly EventService _eventService;
    public EventController(EventService eventService) { this._eventService = eventService; }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _eventService.GetEvents(int.Parse(userId!));
        if (!result.Success) return BadRequest(result);

        return Ok(result);
    }
}