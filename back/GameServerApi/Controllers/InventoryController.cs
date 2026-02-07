using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using GameServerApi.Hubs;
using Microsoft.AspNetCore.SignalR;


namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("user-limit")]

    public class InventoryController : ControllerBase
    {
        private readonly InventoryService _inventoryService;
        private readonly ILogger<InventoryController> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public InventoryController(InventoryService context, ILogger<InventoryController> logger, IHubContext<ChatHub> hubContext)
        {
            _inventoryService = context;
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

        [HttpGet("Seed")]
        [AllowAnonymous]
        public async Task<Boolean> Seed()
        {

            return await _inventoryService.Seed();
        }
        
        [HttpPost("Buy/{itemId}")]
        [Authorize]

        public async Task<IActionResult> BuyObject(int itemId)
        {
            var userId = GetUserId();

            var (inventory, username, purchasedItem) = await _inventoryService.BuyObject(itemId, userId);

            if (purchasedItem != null && purchasedItem.Price > 10000)
            {
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveMessage",
                    "SYSTEM",
                    $"{username} vient d'acquérir {purchasedItem.Name} !"
                );
            }
            
            return Ok(inventory);
        }
        
        [HttpGet("UserInventory")]
        [Authorize]

        public async Task<ActionResult<IEnumerable<object>>> GetInventory()
        {
            var userId = GetUserId();

            var inventory = await _inventoryService.GetInventory(userId);

            return Ok(inventory);
        }

        [HttpGet("Items")]
        [AllowAnonymous]
        public async Task<ActionResult<Item[]>> GetItems() {

            var items =await _inventoryService.GetItems();
            return Ok(items);
        }
    }
}