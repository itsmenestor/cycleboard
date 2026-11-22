using System.Net;
using CycleBoard.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CycleBoard.WebApi.Middleware;

public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            DomainValidationException => (HttpStatusCode.BadRequest, "Validation Error"),
            ForbiddenAccessException  => (HttpStatusCode.Forbidden, "Forbidden"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            KeyNotFoundException      => (HttpStatusCode.NotFound, "Not Found"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = status != HttpStatusCode.InternalServerError
                ? exception.Message
                : "An unexpected error occurred. Please try again."
        };

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(problem);
    }
}
