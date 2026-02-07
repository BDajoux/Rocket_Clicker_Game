using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameServerApi.Tests.ServicesTests;

public class PassiveIncomeServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldLogStartMessage_WhenServiceStarts()
    {
        // ARRANGE
        var loggerMock = new Mock<ILogger<PassiveIncomeService>>();
        var serviceProvider = CreateServiceProvider();

        var service = new PassiveIncomeService(loggerMock.Object, serviceProvider);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // ACT
        _ = service.StartAsync(cts.Token);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        // ASSERT
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PassiveIncomeService démarré.")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessAllProgressions_WhenUsersExist()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_PassiveIncome_ProcessUsers")
            .Options;

        var context = new BDDContext(options);

        // Ajouter des progressions de test
        context.Progressions.Add(new Progression { UserId = 1, Count = 100, Multiplier = 1, BestScore = 100, TotalClickValue = 0 });
        context.Progressions.Add(new Progression { UserId = 2, Count = 200, Multiplier = 2, BestScore = 200, TotalClickValue = 0 });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<PassiveIncomeService>>();
        var gameLoggerMock = new Mock<ILogger<GameService>>();
        var serviceProvider = CreateServiceProviderWithContext(options, gameLoggerMock.Object);

        var service = new PassiveIncomeService(loggerMock.Object, serviceProvider);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // ACT
        _ = service.StartAsync(cts.Token);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // ASSERT
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PassiveIncome appliqué à 2 utilisateurs.")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotProcessAnyUsers_WhenNoProgressionsExist()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_PassiveIncome_NoUsers")
            .Options;

        var loggerMock = new Mock<ILogger<PassiveIncomeService>>();
        var gameLoggerMock = new Mock<ILogger<GameService>>();
        var serviceProvider = CreateServiceProviderWithContext(options, gameLoggerMock.Object);

        var service = new PassiveIncomeService(loggerMock.Object, serviceProvider);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // ACT
        _ = service.StartAsync(cts.Token);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // ASSERT
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PassiveIncome appliqué à 0 utilisateurs.")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncrementUserCount_WhenProcessingUsers()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_PassiveIncome_IncrementCount")
            .Options;

        var context = new BDDContext(options);

        context.Progressions.Add(new Progression { UserId = 1, Count = 100, Multiplier = 1, BestScore = 100, TotalClickValue = 0 });
        await context.SaveChangesAsync();

        var initialCount = context.Progressions.First(p => p.UserId == 1).Count;

        var loggerMock = new Mock<ILogger<PassiveIncomeService>>();
        var gameLoggerMock = new Mock<ILogger<GameService>>();
        var serviceProvider = CreateServiceProviderWithContext(options, gameLoggerMock.Object);

        var service = new PassiveIncomeService(loggerMock.Object, serviceProvider);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // ACT
        _ = service.StartAsync(cts.Token);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // ASSERT
        var newContext = new BDDContext(options);
        var updatedProgression = await newContext.Progressions.FirstAsync(p => p.UserId == 1);
        Assert.True(updatedProgression.Count > initialCount, "Le count devrait avoir augmenté");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallGameServiceClick_ForEachUser()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_PassiveIncome_CallGameService")
            .Options;

        var context = new BDDContext(options);

        context.Progressions.Add(new Progression { UserId = 1, Count = 100, Multiplier = 1, BestScore = 100, TotalClickValue = 0 });
        context.Progressions.Add(new Progression { UserId = 2, Count = 200, Multiplier = 2, BestScore = 200, TotalClickValue = 0 });
        context.Progressions.Add(new Progression { UserId = 3, Count = 300, Multiplier = 1, BestScore = 300, TotalClickValue = 0 });
        await context.SaveChangesAsync();

        var initialCount1 = context.Progressions.First(p => p.UserId == 1).Count;
        var initialCount2 = context.Progressions.First(p => p.UserId == 2).Count;
        var initialCount3 = context.Progressions.First(p => p.UserId == 3).Count;

        var loggerMock = new Mock<ILogger<PassiveIncomeService>>();
        var gameLoggerMock = new Mock<ILogger<GameService>>();
        var serviceProvider = CreateServiceProviderWithContext(options, gameLoggerMock.Object);

        var service = new PassiveIncomeService(loggerMock.Object, serviceProvider);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // ACT
        _ = service.StartAsync(cts.Token);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // ASSERT - Vrifier que tous les comptes ont augment
        var newContext = new BDDContext(options);
        var updated1 = await newContext.Progressions.FirstAsync(p => p.UserId == 1);
        var updated2 = await newContext.Progressions.FirstAsync(p => p.UserId == 2);
        var updated3 = await newContext.Progressions.FirstAsync(p => p.UserId == 3);

        Assert.True(updated1.Count > initialCount1, "Le count de l'utilisateur 1 devrait avoir augmenté");
        Assert.True(updated2.Count > initialCount2, "Le count de l'utilisateur 2 devrait avoir augmenté");
        Assert.True(updated3.Count > initialCount3, "Le count de l'utilisateur 3 devrait avoir augmenté");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleCancellation_Gracefully()
    {
        // ARRANGE
        var loggerMock = new Mock<ILogger<PassiveIncomeService>>();
        var serviceProvider = CreateServiceProvider();

        var service = new PassiveIncomeService(loggerMock.Object, serviceProvider);
        var cts = new CancellationTokenSource();

        // ACT
        _ = service.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // ASSERT - Le service doit avoir dmarr
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PassiveIncomeService démarré.")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldContinueRunning_UntilCancelled()
    {
        // ARRANGE
        var loggerMock = new Mock<ILogger<PassiveIncomeService>>();
        var serviceProvider = CreateServiceProvider();

        var service = new PassiveIncomeService(loggerMock.Object, serviceProvider);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        // ACT
        _ = service.StartAsync(cts.Token);
        await Task.Delay(250);
        await service.StopAsync(CancellationToken.None);

        // ASSERT - Le service doit avoir dmarr
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PassiveIncomeService démarré.")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Helper methods
    private IServiceProvider CreateServiceProvider()
    {
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var services = new ServiceCollection();
        services.AddScoped(_ => new BDDContext(options));
        services.AddScoped<GameService>(sp =>
        {
            var context = sp.GetRequiredService<BDDContext>();
            var logger = new Mock<ILogger<GameService>>().Object;
            return new GameService(context, logger);
        });

        return services.BuildServiceProvider();
    }

    private IServiceProvider CreateServiceProviderWithContext(DbContextOptions<BDDContext> options, ILogger<GameService> gameLogger)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new BDDContext(options));
        services.AddScoped(sp =>
        {
            var context = sp.GetRequiredService<BDDContext>();
            return new GameService(context, gameLogger);
        });

        return services.BuildServiceProvider();
    }
}
