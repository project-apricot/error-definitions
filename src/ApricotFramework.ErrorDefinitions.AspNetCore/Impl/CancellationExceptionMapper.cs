using Microsoft.AspNetCore.Http;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Impl;

/// <summary>
/// Maps a cancellation to the kind that describes whose fault it was.
/// </summary>
/// <remarks>
/// Two very different failures arrive as the same exception type, and reporting both as a server error
/// is what makes an error rate unreadable:
/// <list type="bullet">
///   <item>
///     the caller went away, so nobody is waiting for an answer — reported as
///     <see cref="ErrorKinds.Cancelled"/> and status 499;
///   </item>
///   <item>
///     something this service was waiting for ran out of time — reported as
///     <see cref="ErrorKinds.Timeout"/> and status 504, since from the caller's side this service is
///     the one that failed.
///   </item>
/// </list>
/// </remarks>
public sealed class CancellationExceptionMapper : IExceptionErrorMapper
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not OperationCanceledException)
        {
            return null;
        }

        // A TaskCanceledException from an outbound call arrives here too, so the request's token decides.
        return httpContext.RequestAborted.IsCancellationRequested
            ? [Err.Cancelled()]
            : [Err.Timeout()];
    }
}
