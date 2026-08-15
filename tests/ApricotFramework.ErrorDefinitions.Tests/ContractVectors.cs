namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// The wire contract, written out by hand rather than read from the library.
/// </summary>
/// <remarks>
/// Everything here is frozen at first publish: a change to any value below is a change other services
/// and their clients can see, so it needs a major version rather than an edit. If a test fails against
/// this table, that is the test working.
/// <para>
/// The JSON is hand-written from RFC 9457's member names and the table below, so a test comparing
/// serializer output to it is checking the library against the specification and not against itself.
/// </para>
/// </remarks>
internal static class ContractVectors
{
    /// <summary>
    /// Gets the frozen mapping of every kind to its default code and HTTP status.
    /// </summary>
    public static IReadOnlyList<(string Kind, string Code, int HttpStatus)> Kinds { get; } =
    [
        ("cancelled", "CANCELLED", 499),
        ("unknown", "UNKNOWN", 500),
        ("validation", "VALIDATION", 400),
        ("timeout", "TIMEOUT", 504),
        ("not_found", "NOT_FOUND", 404),
        ("already_exists", "ALREADY_EXISTS", 409),
        ("access_denied", "ACCESS_DENIED", 403),
        ("not_authenticated", "NOT_AUTHENTICATED", 401),
        ("resource_exhausted", "RESOURCE_EXHAUSTED", 429),
        ("precondition_failed", "PRECONDITION_FAILED", 412),
        ("aborted", "ABORTED", 409),
        ("out_of_range", "OUT_OF_RANGE", 416),
        ("not_implemented", "NOT_IMPLEMENTED", 501),
        ("internal", "INTERNAL", 500),
        ("unavailable", "UNAVAILABLE", 503),
        ("data_loss", "DATA_LOSS", 500),
    ];

    /// <summary>
    /// Gets the canonical kind for each HTTP status the library maps back, where several kinds share
    /// a status.
    /// </summary>
    public static IReadOnlyList<(int HttpStatus, string Kind)> CanonicalKindForStatus { get; } =
    [
        (499, "cancelled"),
        (400, "validation"),
        (401, "not_authenticated"),
        (403, "access_denied"),
        (404, "not_found"),
        (409, "already_exists"),
        (412, "precondition_failed"),
        (416, "out_of_range"),
        (429, "resource_exhausted"),
        (500, "internal"),
        (501, "not_implemented"),
        (503, "unavailable"),
        (504, "timeout"),
    ];

    /// <summary>
    /// A problem document carrying one validation error with parameters, as it goes on the wire.
    /// </summary>
    public const string ValidationWithPayload =
        """
        {"type":"about:blank","title":"Validation failed","status":400,"detail":"The locale is invalid","instance":"/api/content","errors":[{"kind":"validation","code":"CONTENT_INVALID_LOCALE","message":"The locale is invalid","payload":{"locale":"xx"}}]}
        """;

    /// <summary>
    /// A problem document for an error with no message and no parameters, so both optional members
    /// are absent rather than null.
    /// </summary>
    public const string NotFoundBare =
        """
        {"type":"about:blank","title":"Not found","status":404,"errors":[{"kind":"not_found","code":"NOT_FOUND","message":""}]}
        """;

    /// <summary>
    /// A problem document carrying several errors, as multi-error validation produces.
    /// </summary>
    public const string MultipleErrors =
        """
        {"type":"about:blank","title":"Validation failed","status":400,"errors":[{"kind":"validation","code":"PAYER_INVALID_EMAIL","message":""},{"kind":"validation","code":"PAYER_INVALID_AGE","message":""}]}
        """;

    /// <summary>
    /// A problem document for an unhandled failure, which never carries the text of the exception
    /// behind it.
    /// </summary>
    public const string UnhandledInternal =
        """
        {"type":"about:blank","title":"Internal error","status":500,"instance":"/api/things","errors":[{"kind":"internal","code":"INTERNAL","message":""}]}
        """;
}
