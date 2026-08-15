using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// The document a service sends when an operation fails: an RFC 9457 problem detail whose
/// <c>errors</c> member carries the classified errors.
/// </summary>
/// <remarks>
/// The standard members let anything generic — a browser, a proxy log, an API console — get something
/// meaningful without knowing this library; a client that knows it reads <see cref="Errors"/>.
/// <para>
/// A frozen wire contract, with member names pinned by attribute so a host's JSON settings cannot change
/// what other services receive.
/// </para>
/// <para>
/// Two deliberate departures from RFC 9457: <see cref="Type"/> stays <see cref="BlankType"/>, since this
/// library claims no documentation URI, and <see cref="Title"/> describes the kind rather than repeating
/// the status phrase.
/// </para>
/// </remarks>
public sealed record ErrorProblemDetails
{
    /// <summary>
    /// The problem type used when there is no more specific one, as defined by RFC 9457.
    /// </summary>
    public const string BlankType = "about:blank";

    /// <summary>
    /// The media type this document is served as.
    /// </summary>
    public const string MediaType = "application/problem+json";

    /// <summary>
    /// Gets the URI identifying the problem type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type
    {
        get => field ?? BlankType;
        init;
    }

    /// <summary>
    /// Gets the short human-readable summary of the problem type.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title
    {
        get => field ?? ErrorTitles.Default;
        init;
    }

    /// <summary>
    /// Gets the HTTP status code the response carries.
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>
    /// Gets the explanation specific to this occurrence when there is one.
    /// </summary>
    /// <remarks>
    /// Taken from the first error's message, which a service authored. Never the text of an
    /// unhandled exception.
    /// </remarks>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }

    /// <summary>
    /// Gets the reference identifying this occurrence, normally the request path.
    /// </summary>
    [JsonPropertyName("instance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instance { get; init; }

    /// <summary>
    /// Gets the classified errors. Never null and never empty in a document this library produces.
    /// </summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<ErrorDefinition> Errors
    {
        get => field ?? [];
        init;
    }

    /// <summary>
    /// Builds a problem document for the given errors, deriving the status, title and detail from the
    /// first of them.
    /// </summary>
    /// <param name="errors">The errors to report. An empty sequence becomes a single unknown error.</param>
    /// <param name="instance">The reference identifying this occurrence, normally the request path.</param>
    /// <returns>The problem document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <remarks>
    /// The first error decides the status, since a response has only one. For a different status, adjust
    /// the result: <c>From(errors) with { Status = 418 }</c>.
    /// </remarks>
    public static ErrorProblemDetails From(IEnumerable<ErrorDefinition> errors, string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var materialized = errors.ToImmutableArray();
        var reported = materialized.IsEmpty ? [Err.Unknown()] : materialized;
        var first = reported[0];

        return new ErrorProblemDetails
        {
            Status = ErrorKindStatus.ToHttpStatusCode(first.Kind),
            Title = ErrorTitles.ForKind(first.Kind),
            Detail = string.IsNullOrEmpty(first.Message) ? null : first.Message,
            Instance = instance,
            Errors = reported,
        };
    }

    /// <summary>
    /// Builds a problem document for the errors an exception carries.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    /// <param name="instance">The reference identifying this occurrence, normally the request path.</param>
    /// <returns>The problem document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static ErrorProblemDetails From(ErrorDefinitionException exception, string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return From(exception.Errors, instance);
    }
}
