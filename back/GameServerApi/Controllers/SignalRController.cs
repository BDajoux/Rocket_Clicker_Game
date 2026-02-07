using GameServerApi.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace GameServerApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SignalRController : ControllerBase
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<SignalRController> _logger;

    public SignalRController(IHubContext<ChatHub> hubContext, ILogger<SignalRController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Envoyer une mise à jour de score à un utilisateur spécifique
    /// Cette méthode est appelée par le PassiveIncomeService pour envoyer les mises à jour
    /// </summary>
    [HttpPost("ScoreUpdate")]
    public async Task<IActionResult> SendScoreUpdate([FromBody] ScoreUpdateRequest request)
    {
        try
        {
            var connectionId = ChatHub.GetConnectionId(request.UserId);
            
            if (connectionId != null)
            {
                // Envoyer uniquement au joueur concerné
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("ScoreUpdate", request.Score);
                    
                _logger.LogInformation("Score update sent to user {UserId}: {Score}", request.UserId, request.Score);
                
                return Ok(new { success = true, userId = request.UserId, score = request.Score });
            }
            
            return NotFound(new { success = false, error = "User not connected" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending score update to user {UserId}", request.UserId);
            return StatusCode(500, new { success = false, error = "Internal server error" });
        }
    }
}

public record ScoreUpdateRequest(int UserId, long Score);
