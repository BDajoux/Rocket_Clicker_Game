using GameServerApi.Models;
using GameServerApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using GameServerApi.Data;
using GameServerApi.Exceptions;

namespace GameServerApi.Tests.ServicesTests;

public class InventoryServiceTests
{
[Fact]
    public async Task BuyObjectAsync_ShouldDebitMoneyAndAddItem_WhenUserHasEnoughMoney()
    {
        // 1. ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Purchase_Success")
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new BDDContext(options);
    
        var inventoryServiceLoggerMock = new Mock<ILogger<GameService>>();
        var inventoryService = new InventoryService(context, inventoryServiceLoggerMock.Object);
    
        // Créer un utilisateur
        var user = new User 
        { 
            Username = "TestUser", 
            Password = "hashedPassword",
            Role = Role.UserRole
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Créer une progression avec de l'argent
        var progression = new Progression
        {
            UserId = user.Id,
            Count = 1000,
            Multiplier = 1,
            BestScore = 0,
            TotalClickValue = 0
        };
        context.Progressions.Add(progression);
        await context.SaveChangesAsync();
    
        // Créer un item à acheter
        var item = new Item 
        { 
            Name = "Sword",
            Price = 100,
            MaxQuantity = 10,
            ClickValue = 5
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();
    
        // 2. ACT
        await inventoryService.BuyObject(item.Id, user.Id);
    
        // 3. ASSERT
        // Vérifier que l'argent a été débité
        var updatedProgression = await context.Progressions.FirstOrDefaultAsync(p => p.UserId == user.Id);
        Assert.NotNull(updatedProgression);
        Assert.Equal(900, updatedProgression.Count); // 1000 - 100
        Assert.Equal(5, updatedProgression.TotalClickValue); // ClickValue ajouté
    
        // Vérifier que l'item a été ajouté à l'inventaire
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.UserId == user.Id && i.ItemId == item.Id);
        Assert.NotNull(inventory);
        Assert.Equal(1, inventory.Quantity);
    }
    
[Fact]
    public async Task BuyObjectAsync_ShouldThrowException_WhenUserDoesNotHaveEnoughMoney()
    {
        // 1. ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Purchase_Success")
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new BDDContext(options);
    
        var inventoryServiceLoggerMock = new Mock<ILogger<GameService>>();
        var inventoryService = new InventoryService(context, inventoryServiceLoggerMock.Object);
    
        // Créer un utilisateur
        var user = new User 
        { 
            Username = "PoorUser", 
            Password = "hashedPassword",
            Role = Role.UserRole
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Créer une progression avec peu d'argent
        var progression = new Progression
        {
            UserId = user.Id,
            Count = 50,
            Multiplier = 1,
            BestScore = 0,
            TotalClickValue = 0
        };
        context.Progressions.Add(progression);
        await context.SaveChangesAsync();
    
        // Créer un item coûteux
        var item = new Item 
        { 
            Name = "ExpensiveSword",
            Price = 100,
            MaxQuantity = 10,
            ClickValue = 10
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();
    
        // 2. ACT & 3. ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(async () =>
        {
            await inventoryService.BuyObject(item.Id, user.Id);
        });
    
        Assert.Equal("NOT_ENOUGH_MONEY", exception.Code);
        Assert.Equal(400, exception.StatusCode);
    
        // Vérifier que l'argent n'a pas été débité
        var updatedProgression = await context.Progressions.FirstOrDefaultAsync(p => p.UserId == user.Id);
        Assert.NotNull(updatedProgression);
        Assert.Equal(50, updatedProgression.Count);
        Assert.Equal(0, updatedProgression.TotalClickValue);
    
        // Vérifier que l'item n'a pas été ajouté
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.UserId == user.Id && i.ItemId == item.Id);
        Assert.Null(inventory);
    }
    
}