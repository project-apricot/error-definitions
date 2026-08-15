using System.Collections.Frozen;

namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// The code used for an error of a given kind when the caller supplies none.
/// </summary>
/// <remarks>
/// A kind is often a complete answer on its own, and these are the codes such an error carries. The set
/// is enumerable because a client renders text from a code: a catalogue can be generated from
/// <see cref="All"/> and tested for coverage. A catalogue missing these displays a plain not-found as an
/// unknown error.
/// </remarks>
public static class ErrorCodes
{
    /// <summary>
    /// The default code for <see cref="ErrorKinds.Cancelled"/>.
    /// </summary>
    public const string Cancelled = "CANCELLED";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.Unknown"/>.
    /// </summary>
    public const string Unknown = "UNKNOWN";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.Validation"/>.
    /// </summary>
    public const string Validation = "VALIDATION";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.Timeout"/>.
    /// </summary>
    public const string Timeout = "TIMEOUT";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.NotFound"/>.
    /// </summary>
    public const string NotFound = "NOT_FOUND";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.AlreadyExists"/>.
    /// </summary>
    public const string AlreadyExists = "ALREADY_EXISTS";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.AccessDenied"/>.
    /// </summary>
    public const string AccessDenied = "ACCESS_DENIED";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.NotAuthenticated"/>.
    /// </summary>
    public const string NotAuthenticated = "NOT_AUTHENTICATED";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.ResourceExhausted"/>.
    /// </summary>
    public const string ResourceExhausted = "RESOURCE_EXHAUSTED";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.PreconditionFailed"/>.
    /// </summary>
    public const string PreconditionFailed = "PRECONDITION_FAILED";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.Aborted"/>.
    /// </summary>
    public const string Aborted = "ABORTED";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.OutOfRange"/>.
    /// </summary>
    public const string OutOfRange = "OUT_OF_RANGE";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.NotImplemented"/>.
    /// </summary>
    public const string NotImplemented = "NOT_IMPLEMENTED";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.Internal"/>.
    /// </summary>
    public const string Internal = "INTERNAL";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.Unavailable"/>.
    /// </summary>
    public const string Unavailable = "UNAVAILABLE";

    /// <summary>
    /// The default code for <see cref="ErrorKinds.DataLoss"/>.
    /// </summary>
    public const string DataLoss = "DATA_LOSS";

    /// <summary>
    /// Maps each kind to the code an error of that kind carries when none is supplied.
    /// </summary>
    private static readonly FrozenDictionary<string, string> KindDefaults =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ErrorKinds.Cancelled] = Cancelled,
            [ErrorKinds.Unknown] = Unknown,
            [ErrorKinds.Validation] = Validation,
            [ErrorKinds.Timeout] = Timeout,
            [ErrorKinds.NotFound] = NotFound,
            [ErrorKinds.AlreadyExists] = AlreadyExists,
            [ErrorKinds.AccessDenied] = AccessDenied,
            [ErrorKinds.NotAuthenticated] = NotAuthenticated,
            [ErrorKinds.ResourceExhausted] = ResourceExhausted,
            [ErrorKinds.PreconditionFailed] = PreconditionFailed,
            [ErrorKinds.Aborted] = Aborted,
            [ErrorKinds.OutOfRange] = OutOfRange,
            [ErrorKinds.NotImplemented] = NotImplemented,
            [ErrorKinds.Internal] = Internal,
            [ErrorKinds.Unavailable] = Unavailable,
            [ErrorKinds.DataLoss] = DataLoss,
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Every default code this library defines, in the same order as <see cref="ErrorKinds.All"/>.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Cancelled,
        Unknown,
        Validation,
        Timeout,
        NotFound,
        AlreadyExists,
        AccessDenied,
        NotAuthenticated,
        ResourceExhausted,
        PreconditionFailed,
        Aborted,
        OutOfRange,
        NotImplemented,
        Internal,
        Unavailable,
        DataLoss
    ];

    /// <summary>
    /// Gets the code an error of the given kind carries when no code is supplied.
    /// </summary>
    /// <param name="kind">The kind of the error.</param>
    /// <returns>
    /// The default code for a kind this library defines, otherwise the kind upper-cased, which is a
    /// well-formed code for any well-formed kind.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="kind"/> is null.</exception>
    public static string ForKind(string kind)
    {
        ArgumentNullException.ThrowIfNull(kind);

        // Ordinal, so a kind in the wrong case is a custom kind rather than a typo silently forgiven.
        return KindDefaults.TryGetValue(kind, out var code) ? code : kind.ToUpperInvariant();
    }
}
