using Microsoft.AspNetCore.WebUtilities;

namespace MyMvcApp.Models;

public sealed record ApiErrorResponse(string Message)
{
    public bool Success => false;
    public object? Data => null;

    public static ApiErrorResponse Create(int statusCode, string? message = null) =>
        new(message ?? DefaultMessage(statusCode));

    private static string DefaultMessage(int statusCode) => statusCode switch
    {
        400 => "The request is invalid.",
        401 => "Authentication is required or the supplied credentials are invalid.",
        403 => "You do not have permission to perform this action.",
        404 => "The requested resource was not found.",
        405 => "The HTTP method is not allowed for this endpoint.",
        413 => "The request is too large.",
        415 => "The request content type is not supported.",
        429 => "Too many requests. Please try again later.",
        >= 500 => "An unexpected error occurred. Please try again later.",
        _ => ReasonPhrases.GetReasonPhrase(statusCode)
    };
}
