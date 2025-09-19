using System.Security.Claims;
using BadeHava.DTOs;
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
    [HttpPost("friend-request")]
    public async Task<IActionResult> PostFriendRequest([FromBody] EventRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _eventService.RequestFriendship(userId!, request.receiverUserId);
        if (!result.Success) return BadRequest(result);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("respond-to-friend-request")]
    public async Task<IActionResult> PostRespondToFriendRequest([FromBody] EventRespondToFriendRequestRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _eventService.RespondToFriendRequest(userId!, request.EventId, request.Action);
        if (!result.Success) return BadRequest(result);

        return Ok(result);
    }

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