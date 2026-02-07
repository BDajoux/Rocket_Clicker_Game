using GameServerApi.Data;
using GameServerApi.Exceptions;
using GameServerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;



namespace GameServerApi.Services;


public class InventoryService
{
    private readonly BDDContext _context;
    private readonly ILogger<GameService> _logger;

    public InventoryService(BDDContext context, ILogger<GameService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Boolean> Seed()
    {
        _context.Items.RemoveRange(_context.Items);
        _context.Inventories.RemoveRange(_context.Inventories);
        await _context.SaveChangesAsync();

        HttpClient client = new HttpClient();
        _logger.LogInformation("Fetching items from http://localhost:8000/Rocket%20Clicker%20Game_files/items.json...");
        HttpResponseMessage response = await client.GetAsync("http://localhost:8000/Rocket%20Clicker%20Game_files/items.json");

        var items = await response.Content.ReadFromJsonAsync<List<Item>>();

        if (items == null)
        {
            _logger.LogInformation("No items to load.");
            return false;
        }

        // Ajout des items à la base
        await _context.Items.AddRangeAsync(items);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Items added to database.");

        return true;
    }

    public async Task<IEnumerable<object>> GetInventory(int? userId)
    {

        if (userId == null)
        {
            throw new GameException { Code = "INVALID_USER_ID", StatusCode = 401 };
        }

        _logger.LogInformation("Fetching inventory...");
        var inventory = await _context.Inventories
            .Where(i => i.UserId == userId)
            .Join(
                _context.Items,
                inv => inv.ItemId,
                item => item.Id,
                (inv, item) => new
                {
                    inv.Id,
                    inv.UserId,
                    inv.ItemId,
                    inv.Quantity,
                    Item = item
                })
            .ToListAsync();

        if (inventory.Count == 0)
        {
            _logger.LogInformation("Inventory empty.");
        } else {
            _logger.LogInformation("Inventory loaded.");
        }

        return inventory;
    }

    public async Task<(IEnumerable<object> inventory, string? username, Item? purchasedItem)> BuyObject(int itemId, int? userId)
    {
        _logger.LogInformation("Start of transaction.");
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _logger.LogInformation("Fetching progression of user with id {userId}...", userId);
            var progression = await _context.Progressions
                .SingleOrDefaultAsync(p => p.UserId == userId);

            if (progression == null)
            {
                throw new GameException { Code = "USER_NOT_FOUND", StatusCode = 404 };
            }

            _logger.LogInformation("Fetching item with id {itemId}...", itemId);
            var item = await _context.Items.SingleOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                throw new GameException { Code = "ITEM_NOT_FOUND", StatusCode = 400 };
            }

            _logger.LogInformation("Fetching inventory...");
            var inventory = await _context.Inventories.SingleOrDefaultAsync(p => p.UserId == userId && p.ItemId == item.Id);

            if (inventory != null && inventory.Quantity >= item.MaxQuantity)
            {
                throw new GameException { Code = "INVENTORY_FULL", StatusCode = 400 };
            }

            if (progression.Count < item.Price)
            {
                throw new GameException { Code = "NOT_ENOUGH_MONEY", StatusCode = 400 };
            }

            if(progression.TotalClickValue + item.ClickValue < 0)
            {
                progression.TotalClickValue = progression.TotalClickValue<0?0: progression.TotalClickValue;
                //throw new GameException { Code = "NEGATIVE_BONUS_VALUE", StatusCode = 400 };
            } 
            else
            {
                _logger.LogInformation("totalCLickValue : {totalClickValue} + {added}.", progression.TotalClickValue, item.ClickValue);
                progression.TotalClickValue += item.ClickValue;
                _logger.LogInformation("Count : {count} - {price}.", progression.Count, item.Price);
                progression.Count -= item.Price;
            }

            

            if (inventory == null)
            {
                _logger.LogInformation("No inventory. Creating new one...");
                inventory = new InventoryEntry
                {
                    UserId = userId,
                    ItemId = itemId,
                    Quantity = 1
                };
                _context.Inventories.Add(inventory);
            }
            else
            {
                _logger.LogInformation("Incremented inventory : {quantity}", ++inventory.Quantity);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            var user = await _context.Users.FindAsync(userId);
            var updatedInventory = await GetInventory(userId);

            return (updatedInventory, user?.Username, item);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    public async Task<Item[]> GetItems()
    {
        _logger.LogInformation("Fetching items...");
        var items = await _context.Items.ToArrayAsync();

        if (items.Length == 0)
        {
            throw new GameException { Code = "NO_ITEMS", StatusCode = 404 };
        }
        return items;
    }
}