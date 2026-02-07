using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using GameServerApi.Models;
using Microsoft.Extensions.Logging;

namespace GameServerApi.Services;

public class JwtService
{
    private readonly ILogger<JwtService> _logger;
    private readonly string _jwtKey= "TheSecretKeyThatShouldBeStoredInTheConfiguration";

    public JwtService(ILogger<JwtService> logger)
    {
        _logger = logger;
    }
    
    public string GenerateToken(User user)
    {
        _logger.LogInformation("Generating token...");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtKey)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: "localhost:5000",
            audience: "localhost:5000",
            claims: claims,
            expires: DateTime.Now.AddMinutes(3000),
            signingCredentials: credentials
        );
        _logger.LogInformation("Token Generated!");

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}