﻿using GameServerApi.Data;
using GameServerApi.Exceptions;
using GameServerApi.Models;
using GameServerApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameServerApi.Tests;

public class GameServiceTests
{
    // ===== CLICK TESTS =====
    [Fact]
    public async Task Click_ShouldThrowGameException_WhenProgressionNotFound()
    {
        // 1. ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Click_NotFound")
            .Options;

        using var context = new BDDContext(options);

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // 2. ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.Click(999)
        );

        Assert.Equal("NO_PROGRESSION", exception.Code);
        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task Click_ShouldThrowGameException_WhenUserIdIsNull()
    {
        // 1. ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Click_NullUserId")
            .Options;

        using var context = new BDDContext(options);

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // 2. ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.Click(null)
        );

        Assert.Equal("NO_PROGRESSION", exception.Code);
        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task Click_ShouldIncrementCountCorrectly_WhenProgressionExists()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Click_IncrementCount")
            .Options;

        using var context = new BDDContext(options);

        var testProgression = new Progression
        {
            UserId = 1,
            Count = 100,
            Multiplier = 2,
            TotalClickValue = 5,
            BestScore = 100
        };

        context.Progressions.Add(testProgression);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT
        var (result,isNewHighScore,username,newScore) = await gameService.Click(1);

        // ASSERT
        Assert.NotNull(result);
        Assert.Equal(112, result.getCount());
        Assert.Equal(2, result.getMultiplier());

        var updatedProgression = await context.Progressions.FirstAsync(p => p.UserId == 1);
        Assert.Equal(112, updatedProgression.Count);
    }

    [Fact]
    public async Task Click_ShouldNotUpdateBestScore_WhenCountIsLower()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Click_NoUpdateBestScore")
            .Options;

        using var context = new BDDContext(options);

        var testProgression = new Progression
        {
            UserId = 1,
            Count = 50,
            Multiplier = 1,
            TotalClickValue = 0,
            BestScore = 200
        };

        context.Progressions.Add(testProgression);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT
        var (result,isNewHighScore,username,newScore) = await gameService.Click(1);

        // ASSERT
        Assert.Equal(51, result.getCount());

        var updatedProgression = await context.Progressions.FirstAsync(p => p.UserId == 1);
        Assert.Equal(51, updatedProgression.Count);
        Assert.Equal(200, updatedProgression.BestScore);
    }

    [Fact]
    public async Task Click_ShouldUpdateBestScore_WhenCountExceedsBestScore()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Click_UpdateBestScore")
            .Options;

        using var context = new BDDContext(options);

        var testProgression = new Progression
        {
            UserId = 1,
            Count = 100,
            Multiplier = 5,
            TotalClickValue = 10,
            BestScore = 100
        };

        context.Progressions.Add(testProgression);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT
        var (result,isNewHighScore,username,newScore) = await gameService.Click(1);

        // ASSERT
        Assert.Equal(155, result.getCount());

        var updatedProgression = await context.Progressions.FirstAsync(p => p.UserId == 1);
        Assert.Equal(155, updatedProgression.Count);
        Assert.Equal(155, updatedProgression.BestScore);
    }

    [Fact]
    public async Task Click_ShouldLogInformation_WhenClicked()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Click_LogInfo")
            .Options;

        using var context = new BDDContext(options);

        var testProgression = new Progression
        {
            UserId = 1,
            Count = 10,
            Multiplier = 1,
            TotalClickValue = 0,
            BestScore = 10
        };

        context.Progressions.Add(testProgression);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT
        await gameService.Click(1);

        // ASSERT
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("New click.")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Click_ShouldLogNewBestScore_WhenBestScoreIsBeaten()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Click_LogBestScore")
            .Options;

        using var context = new BDDContext(options);

        var testProgression = new Progression
        {
            UserId = 1,
            Count = 90,
            Multiplier = 2,
            TotalClickValue = 5,
            BestScore = 90
        };

        context.Progressions.Add(testProgression);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT
        await gameService.Click(1);

        // ASSERT
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("New personal best score reached.")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ===== GETPROGRESSION TESTS =====
    [Fact]
    public async Task GetProgression_ShouldReturnProgression_WhenExists()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetProgression_Success")
            .Options;

        using var context = new BDDContext(options);

        var testProgression = new Progression
        {
            UserId = 1,
            Count = 500,
            Multiplier = 3,
            BestScore = 1000
        };

        context.Progressions.Add(testProgression);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT
        var result = await gameService.GetProgression(1);

        // ASSERT
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(500, result.Count);
        Assert.Equal(3, result.Multiplier);
        Assert.Equal(1000, result.BestScore);
    }

    [Fact]
    public async Task GetProgression_ShouldThrowGameException_WhenNotFound()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetProgression_NotFound")
            .Options;

        using var context = new BDDContext(options);
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.GetProgression(999)
        );

        Assert.Equal("NO_PROGRESSION", exception.Code);
        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task GetProgression_ShouldThrowGameException_WhenUserIdIsNull()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetProgression_Null")
            .Options;

        using var context = new BDDContext(options);
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.GetProgression(null)
        );

        Assert.Equal("NO_PROGRESSION", exception.Code);
        Assert.Equal(404, exception.StatusCode);
    }

    // ===== INITIALIZEPROGRESSION TESTS =====
    [Fact]
    public async Task InitializeProgression_ShouldCreateProgression_WhenNotExists()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Initialize_Success")
            .Options;

        using var context = new BDDContext(options);
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT
        var result = await gameService.InitializeProgression(1);

        // ASSERT
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(0, result.Count);
        Assert.Equal(1, result.Multiplier);
        Assert.Equal(0, result.BestScore);
    }

    [Fact]
    public async Task InitializeProgression_ShouldThrowGameException_WhenProgressionExists()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Initialize_AlreadyExists")
            .Options;

        using var context = new BDDContext(options);

        context.Progressions.Add(new Progression { UserId = 1, Count = 100, Multiplier = 1, BestScore = 100 });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.InitializeProgression(1)
        );

        Assert.Equal("PROGRESSION_EXISTS", exception.Code);
        Assert.Equal(400, exception.StatusCode);
    }

    // ===== RESETPROGRESSION TESTS =====
    [Fact]
    public async Task ResetProgression_ShouldThrowGameException_WhenProgressionNotFound()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Reset_NotFound")
            .Options;

        using var context = new BDDContext(options);
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.ResetProgression(999)
        );

        Assert.Equal("NO_PROGRESSION", exception.Code);
        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task ResetProgression_ShouldThrowGameException_WhenInsufficientClicks()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Reset_InsufficientClicks")
            .Options;

        using var context = new BDDContext(options);

        context.Progressions.Add(new Progression { UserId = 1, Count = 50, Multiplier = 1, BestScore = 50 });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.ResetProgression(1)
        );

        Assert.Equal("INSUFFICIENT_CLICKS", exception.Code);
        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task ResetProgression_ShouldResetSuccessfully_WhenConditionsMet()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Reset_Success")
            .Options;

        using var context = new BDDContext(options);

        context.Progressions.Add(new Progression { UserId = 1, Count = 200, Multiplier = 1, BestScore = 200, TotalClickValue = 10 });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        var (progression, username, scoreBeforeReset) = await gameService.ResetProgression(1);


        // ASSERT
        Assert.NotNull(progression);
        Assert.Equal(0, progression.Count);
        Assert.Equal(2, progression.Multiplier);
        Assert.Equal(200, progression.BestScore);
        Assert.Equal(0, progression.TotalClickValue);
    }

    // ===== RESETCOST TESTS =====
    [Fact]
    public async Task ResetCost_ShouldThrowGameException_WhenProgressionNotFound()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_ResetCost_NotFound")
            .Options;

        using var context = new BDDContext(options);
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.ResetCost(999)
        );

        Assert.Equal("NO_PROGRESSION", exception.Code);
        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task ResetCost_ShouldReturnCorrectCost_WhenProgressionExists()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_ResetCost_Success")
            .Options;

        using var context = new BDDContext(options);

        context.Progressions.Add(new Progression { UserId = 1, Count = 200, Multiplier = 1, BestScore = 200 });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT
        var result = await gameService.ResetCost(1);

        // ASSERT
        Assert.NotNull(result);
        var cost = result.GetType().GetProperty("cost")?.GetValue(result);
        Assert.Equal(100, cost);
    }

    // ===== BESTSCORES TESTS =====
    [Fact]
    public async Task BestScores_ShouldThrowGameException_WhenNoProgressions()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_BestScores_NoProgressions")
            .Options;

        using var context = new BDDContext(options);
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.BestScores()
        );

        Assert.Equal("NO_PROGRESSIONS", exception.Code);
        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task BestScores_ShouldReturnBestScore_WhenProgressionsExist()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_BestScores_Success")
            .Options;

        using var context = new BDDContext(options);

        context.Progressions.Add(new Progression { UserId = 1, Count = 100, Multiplier = 1, BestScore = 100 });
        context.Progressions.Add(new Progression { UserId = 2, Count = 500, Multiplier = 2, BestScore = 500 });
        context.Progressions.Add(new Progression { UserId = 3, Count = 200, Multiplier = 1, BestScore = 200 });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);

        // ACT
        var result = await gameService.BestScores();

        // ASSERT
        Assert.NotNull(result);
        var userId = result.GetType().GetProperty("userId")?.GetValue(result);
        var bestScore = result.GetType().GetProperty("bestScore")?.GetValue(result);
        
        Assert.Equal(2, userId);
        Assert.Equal(500, bestScore);
    }
    
    [Fact]
    public async Task Click_ShouldInitializeCache_OnFirstCall()
    {
        // ARRANGE
        GameService.ResetCache();
        
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Click_CacheInit")
            .Options;
    
        using var context = new BDDContext(options);
    
        var user = new User { Id = 1, Username = "Player1", Password = "hashedpassword" };
        context.Users.Add(user);
    
        var progression1 = new Progression { UserId = 1, Count = 500, Multiplier = 1, BestScore = 500 };
        var progression2 = new Progression { UserId = 2, Count = 100, Multiplier = 1, BestScore = 100 };
        
        context.Progressions.AddRange(progression1, progression2);
        await context.SaveChangesAsync();
    
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);
    
        // ACT
        var (_, isNewHighScore, username, newScore) = await gameService.Click(2);
    
        // ASSERT
        Assert.False(isNewHighScore); // Joueur 2 n'a pas battu le record de joueur 1
        Assert.Null(username);
    }
    
    [Fact]
    public async Task Click_ShouldTriggerHighScore_WhenNewPlayerBeatsRecord()
    {
        // ARRANGE
        GameService.ResetCache();
        
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Click_NewRecordHolder")
            .Options;
    
        using var context = new BDDContext(options);
    
        var user1 = new User { Id = 1, Username = "Player1", Password = "hashedpassword1" };
        var user2 = new User { Id = 2, Username = "Player2", Password = "hashedpassword2" };
        context.Users.AddRange(user1, user2);
    
        var progression1 = new Progression { UserId = 1, Count = 100, Multiplier = 1, BestScore = 100, TotalClickValue = 0 };
        var progression2 = new Progression { UserId = 2, Count = 50, Multiplier = 10, BestScore = 50, TotalClickValue = 10 };
        
        context.Progressions.AddRange(progression1, progression2);
        await context.SaveChangesAsync();
    
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);
    
        // Joueur 1 établit le record
        await gameService.Click(1);
    
        // ACT - Joueur 2 bat le record
        var (_, isNewHighScore, username, newScore) = await gameService.Click(2);
    
        // ASSERT
        Assert.True(isNewHighScore);
        Assert.Equal("Player2", username);
        Assert.Equal(160, newScore); // 50 + 10 * (10 + 1)
    }
    
    [Fact]
    public async Task Click_ShouldNotTriggerHighScore_WhenSamePlayerContinues()
    {
        // ARRANGE
        GameService.ResetCache();
        
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Click_SamePlayerContinues")
            .Options;
    
        using var context = new BDDContext(options);
    
        var user1 = new User { Id = 1, Username = "Player1", Password = "hashedpassword" };
        var user2 = new User { Id = 2, Username = "Player2", Password = "hashedpassword2" };
        context.Users.AddRange(user1, user2);
    
        // Player2 a le meilleur score initial (50)
        var progression1 = new Progression { UserId = 1, Count = 0, Multiplier = 2, BestScore = 0, TotalClickValue = 5 };
        var progression2 = new Progression { UserId = 2, Count = 50, Multiplier = 1, BestScore = 50, TotalClickValue = 0 };
        context.Progressions.AddRange(progression1, progression2);
        await context.SaveChangesAsync();
    
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);
    
        // Premier clic de Player1 - bat le record de Player2 (0 + 2 * 6 = 12 < 50, donc pas de high score)
        // Ajustons pour qu'il batte vraiment le record
        progression1.Count = 40; // Avec le clic : 40 + 2 * 6 = 52 > 50
        progression1.BestScore = 40;
        await context.SaveChangesAsync();
    
        // Premier clic - déclenche le record
        var (_, isHighScore1, username1, _) = await gameService.Click(1);
        Assert.True(isHighScore1);
        Assert.Equal("Player1", username1);
    
        // ACT - Deuxième clic du même joueur (52 + 2 * 6 = 64)
        var (_, isHighScore2, username2, _) = await gameService.Click(1);
    
        // ASSERT
        Assert.False(isHighScore2); // Pas de nouvelle notification
        Assert.Null(username2);
    }
    
    [Fact]
    public async Task ResetProgression_ShouldThrowGameException_WhenUserIdIsNull()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Reset_NullUserId")
            .Options;
    
        using var context = new BDDContext(options);
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);
    
        // ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.ResetProgression(null)
        );
    
        Assert.Equal("NO_PROGRESSION", exception.Code);
        Assert.Equal(400, exception.StatusCode);
    }
    
    [Fact]
    public async Task ResetCost_ShouldThrowGameException_WhenUserIdIsNull()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_ResetCost_NullUserId")
            .Options;
    
        using var context = new BDDContext(options);
        var loggerMock = new Mock<ILogger<GameService>>();
        var gameService = new GameService(context, loggerMock.Object);
    
        // ACT & ASSERT
        var exception = await Assert.ThrowsAsync<GameException>(
            () => gameService.ResetCost(null)
        );
    
        Assert.Equal("NO_PROGRESSION", exception.Code);
        Assert.Equal(400, exception.StatusCode);
    }
    
}