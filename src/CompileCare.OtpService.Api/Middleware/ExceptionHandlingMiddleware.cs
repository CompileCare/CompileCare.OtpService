using CompileCare.OtpService.Api.Exceptions;
using CompileCare.OtpService.Api.Models;
using System.Net;
using System.Text.Json;

namespace CompileCare.OtpService.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred");

            var response = exception switch
            {
                OtpExpiredException => new ApiResponse(false, exception.Message),
                OtpInvalidException => new ApiResponse(false, exception.Message),
                OtpCooldownException => new ApiResponse(false, exception.Message),
                OtpException => new ApiResponse(false, exception.Message),
                _ => new ApiResponse(false, "An internal server error occurred")
            };

            var statusCode = exception switch
            {
                OtpException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
