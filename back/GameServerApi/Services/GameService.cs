using GameServerApi.Data;
using GameServerApi.Models;
using Microsoft.EntityFrameworkCore;
using GameServerApi.Exceptions;


namespace GameServerApi.Services;

public class GameService
{
    private readonly BDDContext _context;
    private readonly ILogger<GameService> _logger;
    private static int _cachedGlobalBestScore = 0;
    private static int? _cachedGlobalBestScoreUserId = null;
    private static readonly SemaphoreSlim _cacheLock = new(1, 1);
    
    public GameService(BDDContext context, ILogger<GameService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Méthode pour réinitialiser le cache (utile pour les tests)
    public static void ResetCache()
    {
        _cachedGlobalBestScore = 0;
        _cachedGlobalBestScoreUserId = null;
    }

    public async Task<(Game game, bool isNewHighScore, string? username, int newScore)> Click(int? userId)
    {
        var progression = await _context.Progressions.FirstOrDefaultAsync(x => x.UserId == userId);
        if (progression == null)
        {
            throw new GameException { Code = "NO_PROGRESSION", StatusCode = 404 };
        }
        bool isNewHighScore = false;
        string? username = null;
    
        await _cacheLock.WaitAsync();
        try
        {
            if (_cachedGlobalBestScore == 0 && _cachedGlobalBestScoreUserId == null)
            {
                var globalBestProgression = await _context.Progressions
                    .OrderByDescending(p => p.BestScore)
                    .FirstOrDefaultAsync();
    
                if (globalBestProgression != null)
                {
                    _cachedGlobalBestScore = globalBestProgression.BestScore;
                    _cachedGlobalBestScoreUserId = globalBestProgression.UserId;
                }
            }
            
            progression.TotalClickValue = progression.TotalClickValue <= 0 ? 0 : progression.TotalClickValue;
            
            int countTmp= progression.Count+ progression.Multiplier * (progression.TotalClickValue + 1);

            if (countTmp > 0)
            {
                progression.Count += progression.Multiplier * (progression.TotalClickValue + 1);
                _logger.LogInformation("New click.");
            }
            else
            {
                _logger.LogWarning("integer overshoot reached");
                progression.Count=((progression.Count < 0) ? 0 : progression.Count);
            }

    
            if (progression.Count > progression.BestScore)
            {
                progression.BestScore = progression.Count;
                _logger.LogInformation("New personal best score reached.");
    
                if (progression.BestScore > _cachedGlobalBestScore)
                {
                    if (userId != _cachedGlobalBestScoreUserId)
                    {
                        isNewHighScore = true;
                        var user = await _context.Users.FindAsync(userId);
                        username = user?.Username;
                        _logger.LogInformation("New global high score: {score} by {username}", progression.BestScore, username);
                    }
                    _cachedGlobalBestScore = progression.BestScore;
                    _cachedGlobalBestScoreUserId = userId;
                }
            }
        }
        finally
        {
            _cacheLock.Release();
        }
    
        await _context.SaveChangesAsync();
        return (new Game(progression.Count, progression.Multiplier), isNewHighScore, username, progression.BestScore);
    }
    
    
    public async Task<Progression> GetProgression(int? userId)
    {
        var progression = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (progression == null)
        {
            throw new GameException{ Code = "NO_PROGRESSION", StatusCode = 404 };
        }

        _logger.LogInformation("Progression from user {userId} found.", userId);
        return progression;
    }
    
    public async Task<Progression> InitializeProgression(int? userId)
    {
        var existingProgression = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (existingProgression != null)
        {
            throw new GameException{ Code = "PROGRESSION_EXISTS", StatusCode = 400 };
        }

        var progression = new Progression
        {
            UserId = userId,
            Count = 0,
            Multiplier = 1,
            BestScore = 0
        };

        if (progression == null)
        {
            throw new GameException { Code = "INITIALIZATION_FAILED", StatusCode = 400 };
        }

        _logger.LogInformation("Progression for user {userId} initialized.", userId);
        _context.Progressions.Add(progression);
        await _context.SaveChangesAsync();
        return progression;
    }
    
    public async Task<(Progression progression, string? username, int scoreBeforeReset)> ResetProgression(int? userId)
    {
        var progression = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (progression == null)
        {
            throw new GameException { Code = "NO_PROGRESSION", StatusCode = 400 };
        }

        var resetCost = 100 * Math.Pow(1.5, progression.Multiplier - 1);
        if (progression.Count < resetCost)
        {
            throw new GameException { Code = "INSUFFICIENT_CLICKS", StatusCode = 400 };
        }
        
        var user = await _context.Users.FindAsync(userId);
        int scoreBeforeReset = progression.Count;

        var inventoryEntries = await _context.Inventories.Where(i => i.UserId == userId).ToListAsync();
        _context.Inventories.RemoveRange(inventoryEntries);

        progression.Multiplier += 1;
        progression.BestScore = Math.Max(progression.BestScore, progression.Count);
        progression.Count = 0;
        progression.TotalClickValue = 0;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Progression reset for user {userId}", userId);
        return (progression, user?.Username, scoreBeforeReset);
    }

    public async Task<object> ResetCost(int? userId)
    {
        _logger.LogInformation("Fetching progression...");
        var progression = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);

        if (progression == null)
        { 
            throw new GameException{ Code = "NO_PROGRESSION", StatusCode = 400 };
        }

        var resetCost = 100 * Math.Pow(1.5, progression.Multiplier - 1);
        _logger.LogInformation("Cost reset for user {userId}", userId);

        return new
        {
            cost = (int)resetCost
        };
    }

    public async Task<object> BestScores()
    {
        _logger.LogInformation("Fetching scores...");
        var bestScoreUser = await _context.Progressions.OrderByDescending(p => p.BestScore).FirstOrDefaultAsync();

        if (bestScoreUser == null)
        {
            throw new GameException { Code = "NO_PROGRESSIONS", StatusCode = 404 };
        }

        _logger.LogInformation("Best score : {bscore} ; from user {userId}.", bestScoreUser.BestScore, bestScoreUser.UserId);

        return new { userId = bestScoreUser.UserId , bestScore = bestScoreUser.BestScore };
    }
}