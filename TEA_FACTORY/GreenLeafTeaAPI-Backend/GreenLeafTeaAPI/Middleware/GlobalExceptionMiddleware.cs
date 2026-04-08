using System.Net;
using System.Text.Json;

namespace GreenLeafTeaAPI.Middleware
{
    /// <summary>
    /// Global exception handler middleware.
    /// Catches unhandled exceptions and returns a consistent JSON error response
    /// instead of leaking stack traces to the client.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Access denied."),
                ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found."),
                InvalidOperationException opEx => (HttpStatusCode.BadRequest, opEx.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                status = (int)statusCode,
                message,
                // Only include details in Development
                detail = context.RequestServices
                    .GetRequiredService<IWebHostEnvironment>()
                    .IsDevelopment() ? exception.Message : null
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    /// <summary>
    /// Extension method for cleaner registration in Program.cs
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}
