namespace MyMvcApp.Services;

// Only explicitly identified request errors are exposed to API callers.
public sealed class RequestValidationException(string message, int statusCode = StatusCodes.Status400BadRequest)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
