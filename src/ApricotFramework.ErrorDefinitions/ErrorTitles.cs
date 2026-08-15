using System.Collections.Frozen;

namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// The short human-readable summary carried as the problem document's title.
/// </summary>
/// <remarks>
/// RFC 9457 asks that a title describe the problem <em>type</em>, so these are per kind and never
/// interpolate anything from the request. It exists for whoever reads the response without a catalogue;
/// a client renders from <see cref="ErrorDefinition.Code"/> instead.
/// </remarks>
public static class ErrorTitles
{
    /// <summary>
    /// The title used for a kind this library does not define.
    /// </summary>
    public const string Default = "Error";

    /// <summary>
    /// Maps each kind to its title.
    /// </summary>
    private static readonly FrozenDictionary<string, string> KindTitles =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ErrorKinds.Cancelled] = "Request cancelled",
            [ErrorKinds.Unknown] = "Unknown error",
            [ErrorKinds.Validation] = "Validation failed",
            [ErrorKinds.Timeout] = "Request timed out",
            [ErrorKinds.NotFound] = "Not found",
            [ErrorKinds.AlreadyExists] = "Already exists",
            [ErrorKinds.AccessDenied] = "Access denied",
            [ErrorKinds.NotAuthenticated] = "Not authenticated",
            [ErrorKinds.ResourceExhausted] = "Resource exhausted",
            [ErrorKinds.PreconditionFailed] = "Precondition failed",
            [ErrorKinds.Aborted] = "Aborted",
            [ErrorKinds.OutOfRange] = "Out of range",
            [ErrorKinds.NotImplemented] = "Not implemented",
            [ErrorKinds.Internal] = "Internal error",
            [ErrorKinds.Unavailable] = "Service unavailable",
            [ErrorKinds.DataLoss] = "Data loss",
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Gets the title for the given kind.
    /// </summary>
    /// <param name="kind">The kind of the error, which may be null or unrecognised.</param>
    /// <returns>The kind's title, or <see cref="Default"/> when the kind is not one of the sixteen.</returns>
    public static string ForKind(string? kind)
    {
        return kind is not null && KindTitles.TryGetValue(kind, out var title) ? title : Default;
    }
}
