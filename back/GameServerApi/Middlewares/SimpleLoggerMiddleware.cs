using GameServerApi.Exceptions;
using GameServerApi.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
public class SimpleLoggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public SimpleLoggerMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Avant le controller : On log la requête
        _logger.LogDebug($"Request: {context.Request.Method} {context.Request.Path}");

        // On passe la main au middleware suivant
        await _next(context);

        // Après le controller : On log le status code
        _logger.LogDebug($"Response: {context.Response.StatusCode}");
    }
}
