using MyMvcApp.Models;
using MyMvcApp.Services;

namespace MyMvcApp.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller disconnected; there is no response to write.
            context.Abort();
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
                throw;

            var (statusCode, message) = ex switch
            {
                RequestValidationException validation => (validation.StatusCode, validation.Message),
                BadHttpRequestException badRequest => (badRequest.StatusCode, (string?)null),
                EmailDeliveryException email => (StatusCodes.Status503ServiceUnavailable, email.Message),
                _ => (StatusCodes.Status500InternalServerError, (string?)null)
            };

            if (statusCode >= 500)
                logger.LogError(ex, "Request failed with HTTP {StatusCode}.", statusCode);

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(statusCode, message),
                cancellationToken: context.RequestAborted);
        }
    }
}
