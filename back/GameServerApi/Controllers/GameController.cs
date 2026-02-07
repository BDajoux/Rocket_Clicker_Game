using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using GameServerApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GameServerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("user-limit")]
    public class GameController : ControllerBase
    {
        private readonly GameService _gameService;
        private readonly ILogger<GameController> _logger;
        private readonly IHubContext<ChatHub> _hubContext;
        

        public GameController(GameService gameService, ILogger<GameController> logger, IHubContext<ChatHub> hubContext)
        {
            _gameService = gameService;
            _logger = logger;
            _hubContext = hubContext;
            
        }

        private int? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogWarning("User {userName} not found.", userIdClaim?.Value ?? "Null");
                return null;
            }

            _logger.LogInformation("User {userName} found with id {userId}.", userIdClaim.Value, userId);
            return userId;
        }

        [HttpGet("Click")]
        [Authorize]

        public async Task<ActionResult<object>> GetClick()
        {
            var userId = GetUserId();
            var (game, isNewHighScore, username, newScore) = await _gameService.Click(userId);

            if (isNewHighScore)
            {
                await _hubContext.Clients.All.SendAsync(
                    "NewHighScore",
                    username,
                    newScore
                );
            }

            return Ok(game);
        }
        
        [HttpGet("Progression")]
        [Authorize]

        public async Task<ActionResult<Progression>> GetProgression()
        {
            var userId = GetUserId();
            var res = await _gameService.GetProgression(userId);
            return Ok(res);
        }

        [HttpGet("Initialize")]
        [Authorize]

        public async Task<ActionResult<Progression>> InitializeProgression()
        {
            var userId = GetUserId();
            var res = await _gameService.InitializeProgression(userId);
            return Ok(res);
        }
        
        [HttpPost("Reset")]
        [Authorize]

        public async Task<ActionResult<Progression>> ResetProgression()
        {
            var userId = GetUserId();
            var (progression, username, scoreBeforeReset) = await _gameService.ResetProgression(userId);
            
            await _hubContext.Clients.All.SendAsync(
                "PlayerReset",
                username, scoreBeforeReset
            );
            return Ok(progression);
        }
        
        [HttpGet("ResetCost")]
        [Authorize]

        public async Task<ActionResult<object>> GetResetCost()
        {
            var userId = GetUserId();
            var res = await _gameService.ResetCost(userId);
            return Ok(res);
        }
        
        [HttpGet("BestScore")]
        [Authorize]

        public async Task<ActionResult<IEnumerable<object>>> GetBestScores()
        {
            var res = await _gameService.BestScores();
            return Ok(res);
        }
        
    }
}