using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using GameServerApi.Hubs;


namespace GameServerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    // On injecte le contexte du Hub
    private readonly IHubContext<ChatHub> _hubContext;

    public NotificationController(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> NotifyUsers(string text)
    {
        // Envoi à tous les clients depuis le contrôleur
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", text);
        return Ok();
    }
}