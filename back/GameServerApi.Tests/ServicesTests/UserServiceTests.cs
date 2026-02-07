using GameServerApi.Models;
using GameServerApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using GameServerApi.Data;
using GameServerApi.Exceptions;

namespace GameServerApi.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenValidData()
    {
        // 1. ARRANGE
        
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Register") // Nom unique par test
            .Options;
        var context = new BDDContext(options);

        // Setup Mocks
        var passwordHasher = new PasswordHasher<User>();
        
        var configMock = new Mock<ILogger<JwtService>>();
        var jwtService = new JwtService(configMock.Object);

        var userServiceLoggerMock = new Mock<ILogger<UserService>>();

        // Création du service à tester
        var userService = new UserService(context, passwordHasher, jwtService, userServiceLoggerMock.Object);

        // Données de test
        var userPass = new UserPass { Username = "TestUser", Password = "Password123!" };

        // 2. ACT
        var result = await userService.Register(userPass);

        // 3. ASSERT
        Assert.NotNull(result);
        Assert.Equal("TestUser", result.User.Username);
        
        // Vérifier que l'user est bien en BDD
        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Username == "TestUser");
        Assert.NotNull(userInDb);
        Assert.Equal(result.User.Role, userInDb.Role); // Le premier user doit être Admin
    }
    
    [Fact]
    public async Task RegisterAsync_ShouldThrowException_WhenDuplicateUsername()
    {
        // 1. ARRANGE

        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Register_Duplicate")
            .Options;
        var context = new BDDContext(options);

        // Setup Mocks
        var passwordHasher = new PasswordHasher<User>();

        var configMock = new Mock<ILogger<JwtService>>();
        var jwtService = new JwtService(configMock.Object);

        var userServiceLoggerMock = new Mock<ILogger<UserService>>();

        // Création du service à tester
        var userService = new UserService(context, passwordHasher, jwtService, userServiceLoggerMock.Object);

        // Données de test
        var userPass = new UserPass { Username = "TestUser", Password = "Password123!" };

        // Première inscription (doit réussir)
        var firstResult = await userService.Register(userPass);
        Assert.NotNull(firstResult);

        // 2. ACT & 3. ASSERT
        // Vérifier qu'une exception est levée lors de la deuxième inscription
        var exception = await Assert.ThrowsAsync<GameException>(async () =>
        {
            await userService.Register(userPass);
        });
        
        Assert.Equal("Exception of type 'GameServerApi.Exceptions.GameException' was thrown.", exception.Message);

        
    }
    
[Fact]
    public async Task LoginAsync_ShouldReturnUserPublic_WhenCredentialsAreValid()
    {
        // 1. ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Login_Success")
            .Options;
        var context = new BDDContext(options);
    
        var passwordHasher = new PasswordHasher<User>();
        var configMock = new Mock<ILogger<JwtService>>();
        var jwtService = new JwtService(configMock.Object);
        var userServiceLoggerMock = new Mock<ILogger<UserService>>();
    
        var userService = new UserService(context, passwordHasher, jwtService, userServiceLoggerMock.Object);
    
        // Créer un utilisateur
        var userPass = new UserPass { Username = "TestUser", Password = "Password123!" };
        await userService.Register(userPass);
    
        // 2. ACT
        var loginInfo = new UserPass { Username = "TestUser", Password = "Password123!" };
        var result = await userService.Login(loginInfo);
    
        // 3. ASSERT
        Assert.NotNull(result);
        Assert.Equal("TestUser", result.User.Username);
        Assert.NotNull(result.Token);
    }
    
[Fact]
    public async Task LoginAsync_ShouldThrowException_WhenPasswordIsInvalid()
    {
        // 1. ARRANGE
        var options = new DbContextOptionsBuilder<BDDContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Login_InvalidPassword")
            .Options;
        var context = new BDDContext(options);
    
        var passwordHasher = new PasswordHasher<User>();
        var configMock = new Mock<ILogger<JwtService>>();
        var jwtService = new JwtService(configMock.Object);
        var userServiceLoggerMock = new Mock<ILogger<UserService>>();
    
        var userService = new UserService(context, passwordHasher, jwtService, userServiceLoggerMock.Object);
    
        // Créer un utilisateur
        var userPass = new UserPass { Username = "TestUser", Password = "Password123!" };
        await userService.Register(userPass);
    
        // 2. ACT & 3. ASSERT
        var loginInfo = new UserPass { Username = "TestUser", Password = "WrongPassword!" };
        
        var exception = await Assert.ThrowsAsync<GameException>(async () =>
        {
            await userService.Login(loginInfo);
        });
    
        Assert.Equal("Exception of type 'GameServerApi.Exceptions.GameException' was thrown.", exception.Message);
    }
    

}