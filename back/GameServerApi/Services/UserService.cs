using GameServerApi.Data;
using GameServerApi.Exceptions;
using GameServerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace GameServerApi.Services;

public class UserService
{
    private readonly BDDContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly JwtService _jwtService;
    private readonly ILogger<UserService> _logger;

    public UserService(BDDContext context, PasswordHasher<User> passwordHasher, JwtService jwtService, ILogger<UserService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<UserPublic> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            throw new GameException{Code = "USER_NOT_FOUND", StatusCode = 404 };
        }
        _logger.LogInformation("User {userId} found.", id);
        return new UserPublic(user);
    }

    public async Task<AuthResult> Register(UserPass uInfo)
    {
        var user = new User(uInfo);
        var pseudoExists = await _context.Users.AnyAsync(u => u.Username == uInfo.Username);

        if (pseudoExists)
        {
            throw new GameException { Code = "REGISTRATION_FAILED", StatusCode = 400 };
        }

        var adminExists = await _context.Users.AnyAsync(u => u.Role == Role.AdminRole);
        user.Role = adminExists ? Role.UserRole : Role.AdminRole;

        user.Password = _passwordHasher.HashPassword(user, uInfo.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("Register success.");

        return new AuthResult
        {
            Token = token,
            User = user
        };
    }

    public async Task<AuthResult> Login(UserPass loginInfo)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginInfo.Username);

        if (user == null)
        {
            throw new GameException { StatusCode = 404, Code = "USER_NOT_FOUND" };
        }

        if (!user.VerifyPassword(loginInfo.Password, _passwordHasher))
        {
            throw new GameException { StatusCode = 401, Code = "INVALID_PASSWORD" };
        }

        var userDto = new UserPublic(user);
        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("Loggin success.");
        
        return new AuthResult
        {
            Token = token,
            User = user
        };
    }

    public async Task<User> UpdateUser(UserUpdate uUpdate)
    {
        var user = await _context.Users.FindAsync(uUpdate.Id);
        if (user == null || !user.VerifyPassword(uUpdate.Password, _passwordHasher))
        {
            throw new GameException { Code = "USER_OR_PWD_NOT_FOUND", StatusCode = 404 };
        }

        if (string.IsNullOrEmpty(uUpdate.Username))
        {
            throw new GameException { Code = "PSEUDO_EMPTY", StatusCode = 401 };
        }

        if (await _context.Users.AnyAsync(u => u.Username == uUpdate.Username && u.Id != uUpdate.Id))
        {
            throw new GameException { Code = "PSEUDO_TAKEN", StatusCode = 409 };
        }

        user.Username = uUpdate.Username;

        if (!string.IsNullOrEmpty(uUpdate.Password))
        {
            var hashedPassword = _passwordHasher.HashPassword(user, uUpdate.Password);
            user.SetPasswordHash(hashedPassword);
        }
        else
        {
            throw new GameException { Code = "PWD_EMPTY", StatusCode = 401 };
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User update success.");

        return user;
    }

    public async Task<bool> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            throw new GameException { Code = "USER_NOT_FOUND", StatusCode = 404 };
        }

        var userProgressions = await _context.Progressions
            .Where(p => p.UserId == id)
            .ToListAsync();
        
        var userInventories = await _context.Inventories
            .Where(i => i.UserId == id)
            .ToListAsync();

        _context.Progressions.RemoveRange(userProgressions);
        _context.Inventories.RemoveRange(userInventories);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User deletion success.");

        return true;
    }

    public async Task<IEnumerable<UserPublic>> GetAllUsers()
    {
        var users = await _context.Users.ToListAsync();
        return users.Select(user => new UserPublic(user)).ToList();
    }

    public async Task<IEnumerable<UserPublic>> GetAllAdmin()
    {
        var users = await _context.Users
            .Where(u => u.Role == Role.AdminRole)
            .ToListAsync();
        return users.Select(user => new UserPublic(user)).ToList();
    }

    public async Task<IEnumerable<UserPublic>> SearchUsersByName(string name)
    {
        var users = await _context.Users
            .Where(u => u.Username.Contains(name))
            .ToListAsync();

        if (!users.Any())
        {
            throw new GameException { Code = "NO_MATCHING_USERS", StatusCode = 404 };
        }

        _logger.LogInformation("Users found.");

        return users.Select(user => new UserPublic(user)).ToList();
    }
}

public class AuthResult
{
    public required string Token { get; set; }
    public required User User { get; set; }
}
