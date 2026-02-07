using GameServerApi.Controllers;
using GameServerApi.Models;
using GameServerApi.Data;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using GameServerApi.Services;
using GameServerApi.Hubs;

using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace GameServerApi;


public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        
        // Configuration de l'authentification JWT
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.FromMinutes(10),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = "localhost:5000",
                    ValidIssuer = "localhost:5000",
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("TheSecretKeyThatShouldBeStoredInTheConfiguration")
                    ),
                    RoleClaimType = ClaimTypes.Role
                };
            });
        
        builder.Services.AddAuthorization();
        
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddDbContext<BDDContext>();

        // Enregistrer le PasswordHasher comme un service injectable
        builder.Services.AddScoped<PasswordHasher<User>>(); // Enregistrer PasswordHasher pour l'injection
        builder.Services.AddScoped<JwtService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<GameService>();
        builder.Services.AddScoped<InventoryService>();
        
        builder.Services.AddHostedService<PassiveIncomeService>();

        builder.Services.AddScoped<UserController>();
        
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.SetIsOriginAllowed(origin => true) // Allow any origin
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); // SignalR requires credentials
            });
        });


        builder.Services.AddRateLimiter(options =>
        {
            // Rejet avec le code 429 Too Many Requests
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // D�finition d'une politique nomm�e "fixed"
            options.AddFixedWindowLimiter("fixed", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10; // Max 10 requ�tes
                limiterOptions.Window = TimeSpan.FromSeconds(10); // Toutes les 10 secondes
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0; // Pas de file d'attente
            });
            options.AddPolicy("user-limit", context =>
            {
                // On r�cup�re le nom de l'utilisateur (ou son IP s'il n'est pas connect�)
                var username = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString();

                return RateLimitPartition.GetFixedWindowLimiter(username, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromSeconds(5)
                });
            });
        });
        
        builder.Services.AddSignalR();

        var app = builder.Build();
        app.Logger.LogInformation("Application is initialising...");

        app.UseCors();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseMiddleware<SimpleLoggerMiddleware>();
        app.UseMiddleware<ErrorHandlingMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseRateLimiter();

        
        app.MapControllers();
        app.MapHub<ChatHub>("/hub/chat");

        app.Logger.LogInformation("Application is starting up...");
        app.Run();
    }
}
