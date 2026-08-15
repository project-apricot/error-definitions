using ApricotFramework.ErrorDefinitions.Serialization;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;

/// <summary>
/// Writes a problem document to a response.
/// </summary>
public static class HttpContextErrorExtensions
{
    /// <summary>
    /// Writes the errors to the response as a problem document, setting the status and content type.
    /// </summary>
    /// <param name="httpContext">The request being answered.</param>
    /// <param name="errors">The errors to report. An empty sequence becomes a single unknown error.</param>
    /// <param name="statusCode">The status to send, defaulting to the one the first error's kind maps to.</param>
    /// <param name="cancellationToken">The token to cancel the write.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpContext"/> or <paramref name="errors"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the response has already started.</exception>
    /// <remarks>
    /// Takes errors rather than an exception, so an endpoint, a filter or middleware can answer in the
    /// standard shape without throwing.
    /// </remarks>
    public static Task WriteErrorProblemDetailsAsync(
        this HttpContext httpContext,
        IEnumerable<ErrorDefinition> errors,
        int? statusCode = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(errors);

        var problem = ErrorProblemDetails.From(errors, InstanceOf(httpContext));

        if (statusCode.HasValue)
        {
            problem = problem with { Status = statusCode.Value };
        }

        return httpContext.WriteErrorProblemDetailsAsync(problem, cancellationToken);
    }

    /// <summary>
    /// Writes a problem document to the response, setting the status and content type from it.
    /// </summary>
    /// <param name="httpContext">The request being answered.</param>
    /// <param name="problem">The document to write.</param>
    /// <param name="cancellationToken">The token to cancel the write.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpContext"/> or <paramref name="problem"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the response has already started.</exception>
    public static async Task WriteErrorProblemDetailsAsync(
        this HttpContext httpContext,
        ErrorProblemDetails problem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(problem);

        httpContext.Response.StatusCode = problem.Status;

        // How a client tells this document from a gateway's error page, so it is part of the contract.
        httpContext.Response.ContentType = ErrorProblemDetails.MediaType;

        // The library's own serializer, so a host's JSON options cannot change what peers receive.
        var body = ErrorDefinitionsJson.SerializeToUtf8Bytes(problem);

        httpContext.Response.ContentLength = body.Length;

        await httpContext.Response.Body.WriteAsync(body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the reference identifying this occurrence.
    /// </summary>
    /// <param name="httpContext">The request being answered.</param>
    /// <returns>The request path, or null when there is none.</returns>
    private static string? InstanceOf(HttpContext httpContext)
    {
        // The path only: a query string carries caller data, which does not belong in an error body.
        return httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : null;
    }
}
