using GameServerApi.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using GameServerApi.Data;
using Microsoft.EntityFrameworkCore;
using GameServerApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GameServerApi.Services;
public class PassiveIncomeService  : BackgroundService
{
    private readonly ILogger<PassiveIncomeService> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public PassiveIncomeService(ILogger<PassiveIncomeService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PassiveIncomeService démarré.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var gameService = scope.ServiceProvider.GetRequiredService<GameService>();
                var context = scope.ServiceProvider.GetRequiredService<BDDContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                // Récupérer uniquement les utilisateurs connectés
                var connectedUserIds = ChatHub.GetConnectedUserIds().ToList();

                foreach (var userId in connectedUserIds)
                {
                    try
                    {
                        // Appliquer le click (revenu passif)
                        await gameService.Click(userId);
                        
                        // Récupérer le score actuel
                        var progression = await context.Progressions
                            .Where(p => p.UserId == userId)
                            .FirstOrDefaultAsync(stoppingToken);
                        
                        if (progression != null)
                        {
                            // Récupérer le connectionId de l'utilisateur
                            var connectionId = ChatHub.GetConnectionId(userId);
                            
                            if (connectionId != null)
                            {
                                // Envoyer l'événement ScoreUpdate uniquement au joueur concerné
                                await hubContext.Clients.Client(connectionId)
                                    .SendAsync("ScoreUpdate", progression.Count, stoppingToken);
                                
                                _logger.LogInformation("Score update sent to user {UserId}: {Score}", userId, progression.Count);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors du traitement du revenu passif pour l'utilisateur {UserId}", userId);
                    }
                }
                
                _logger.LogInformation("PassiveIncome appliqué à {count} utilisateurs connectés.", connectedUserIds.Count);
            }
            await Task.Delay(30000, stoppingToken);
        }

        _logger.LogInformation("PassiveIncomeService arrêté.");
    }
}