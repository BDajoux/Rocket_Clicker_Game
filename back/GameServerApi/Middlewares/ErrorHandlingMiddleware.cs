using GameServerApi.Exceptions;
using GameServerApi.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;


public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task manageGameException(GameException e, HttpContext context)
    {
        context.Response.StatusCode = e.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse(e.Message, e.Code), _jsonOptions));
    }

    private async Task manageException(Exception e, HttpContext context)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse("Internal Server Error", "INTERNAL_SERVER_ERROR"), _jsonOptions));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");

        try
        {
            await _next(context);
        }
        catch (GameException e)
        {
            _logger.LogError("Error message: {error}; code: {code}.", e.Message, e.Code); 
            await manageGameException(e, context);
        }
        catch (Exception e)
        {
            _logger.LogError("Internal Server Error code 500.");
            await manageException(e, context);
        }

        Console.WriteLine($"Response: {context.Response.StatusCode}");
    }
}