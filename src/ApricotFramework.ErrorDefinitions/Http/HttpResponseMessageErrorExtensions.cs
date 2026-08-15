using ApricotFramework.ErrorDefinitions.Serialization;

namespace ApricotFramework.ErrorDefinitions.Http;

/// <summary>
/// Reads another service's failure off an HTTP response.
/// </summary>
/// <remarks>
/// A caller gets the same typed errors whether the peer speaks this contract or not: a problem document
/// comes through unchanged, and anything else — a bodiless 401, a routing 404, a proxy's HTML page — is
/// classified from the status. Nothing here throws while parsing.
/// </remarks>
public static class HttpResponseMessageErrorExtensions
{
    /// <summary>
    /// The most of a response body that will be read while looking for a problem document.
    /// </summary>
    /// <remarks>
    /// The body is a value the peer controls, so asking "did that fail" must not become a way to make this
    /// process buffer anything it likes. A body over the limit is classified by status instead.
    /// </remarks>
    public const int MaxBodyBytes = 64 * 1024;

    /// <summary>
    /// How much is read from the stream at a time.
    /// </summary>
    private const int ReadChunkBytes = 8 * 1024;

    /// <summary>
    /// Reads the errors a failed response reports.
    /// </summary>
    /// <param name="response">The response to read.</param>
    /// <param name="cancellationToken">The token to cancel the read.</param>
    /// <returns>
    /// The problem document the response carried; one synthesized from the status code when it
    /// carried none that could be read; or null when the response was a success.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is null.</exception>
    /// <remarks>
    /// Reads and buffers the response content, so call it once.
    /// </remarks>
    public static async Task<ErrorProblemDetails?> ReadErrorProblemDetailsAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        var status = (int)response.StatusCode;

        if (LooksLikeJson(response))
        {
            var body = await ReadBoundedAsync(response.Content, cancellationToken).ConfigureAwait(false);

            if (body is not null && ErrorDefinitionsJson.TryParse(body, out var parsed) && parsed is not null)
            {
                // The peer's own status wins over whatever the document claims, since the status is
                // what the caller's own transport already acted on.
                return parsed with { Status = status };
            }
        }

        return FromStatus(status);
    }

    /// <summary>
    /// Throws the errors a failed response reports and does nothing for a successful one.
    /// </summary>
    /// <param name="response">The response to check.</param>
    /// <param name="cancellationToken">The token to cancel the read.</param>
    /// <returns>A task that completes when the response has been checked.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is null.</exception>
    /// <exception cref="ErrorDefinitionException">Thrown when the response reports a failure.</exception>
    /// <remarks>
    /// Lets a service pass a downstream failure on as its own: the kind and code the peer reported
    /// arrive at this service's own exception handler and are reported unchanged, so a not-found deep
    /// in a call chain is still not-found at the edge.
    /// </remarks>
    public static async Task EnsureNoErrorsAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var problem = await response.ReadErrorProblemDetailsAsync(cancellationToken).ConfigureAwait(false);

        if (problem is not null)
        {
            // A peer may fill in only one of detail and message, so prefer whichever it sent.
            var described = problem.Detail;

            if (string.IsNullOrEmpty(described))
            {
                described = problem.Errors.Count > 0 && !string.IsNullOrEmpty(problem.Errors[0].Message)
                    ? problem.Errors[0].Message
                    : null;
            }

            throw new ErrorDefinitionException(problem.Errors, described);
        }
    }

    /// <summary>
    /// Builds a problem document for a response whose body said nothing usable.
    /// </summary>
    /// <param name="status">The status code of the response.</param>
    /// <returns>A document carrying one error classified from the status.</returns>
    private static ErrorProblemDetails FromStatus(int status)
    {
        var kind = ErrorKindStatus.FromHttpStatusCode(status);

        // Corrected to the status received, since several kinds share one and the fallback is coarse.
        return ErrorProblemDetails.From([Err.From(kind)]) with { Status = status };
    }

    /// <summary>
    /// Reports whether the response claims to carry JSON.
    /// </summary>
    /// <param name="response">The response to check.</param>
    /// <returns><see langword="true"/> when the media type is JSON or a JSON-structured type.</returns>
    private static bool LooksLikeJson(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;

        if (string.IsNullOrEmpty(mediaType))
        {
            return false;
        }

        // Case-insensitive, and `+json` covers a peer serving the document under its own vendor type.
        return mediaType.Equals(ErrorProblemDetails.MediaType, StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads the content, giving up rather than buffering more than <see cref="MaxBodyBytes"/>.
    /// </summary>
    /// <param name="content">The content to read.</param>
    /// <param name="cancellationToken">The token to cancel the read.</param>
    /// <returns>The bytes read, or null when the content is longer than the limit.</returns>
    private static async Task<byte[]?> ReadBoundedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        // A declared length settles it without reading; absent or wrong, the loop enforces the limit.
        if (content.Headers.ContentLength > MaxBodyBytes)
        {
            return null;
        }

        var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[ReadChunkBytes];

            while (true)
            {
                var read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);

                if (read == 0)
                {
                    return buffer.ToArray();
                }

                if (buffer.Length + read > MaxBodyBytes)
                {
                    return null;
                }

                buffer.Write(chunk, 0, read);
            }
        }
    }
}
