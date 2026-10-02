using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApi.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Continuar con el siguiente eslabón del pipeline HTTP
            await _next(context);
        }
        catch (Exception ex)
        {
            // Registrar el error completo en los logs del servidor (Console / ILogger)
            _logger.LogError(ex, "Ocurrió un error no controlado durante la petición: {Message}", ex.Message);

            // Manejar y retornar la respuesta estandarizada
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Mapear excepciones comunes a códigos de estado HTTP específicos
        var statusCode = exception switch
        {
            BadHttpRequestException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError // Error 500 por defecto
        };

        context.Response.StatusCode = statusCode;

        // Construir un objeto estándar ProblemDetails (RFC 7807)
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                (int)HttpStatusCode.BadRequest => "Solicitud incorrecta",
                (int)HttpStatusCode.Unauthorized => "No autorizado",
                (int)HttpStatusCode.NotFound => "Recurso no encontrado",
                _ => "Error interno del servidor"
            },
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        // Serializar a JSON con formato camelCase
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(problemDetails, options);

        await context.Response.WriteAsync(json);
    }
}

// Clase de extensión para registrar fácilmente el middleware en el pipeline
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware();
    }
}