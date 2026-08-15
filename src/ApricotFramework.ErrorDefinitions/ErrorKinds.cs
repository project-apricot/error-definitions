namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// The classification of an error: what kind of thing went wrong, independent of the service that
/// raised it and of the transport carrying it.
/// </summary>
/// <remarks>
/// The set is the sixteen non-OK codes of <c>google.rpc.Code</c>, which is also what gRPC's
/// <c>StatusCode</c> and Google's API platform use, under names that read better over HTTP:
/// <list type="table">
///   <listheader><term>Kind</term><description>Canonical code</description></listheader>
///   <item><term><see cref="Validation"/></term><description><c>INVALID_ARGUMENT</c></description></item>
///   <item><term><see cref="Timeout"/></term><description><c>DEADLINE_EXCEEDED</c></description></item>
///   <item><term><see cref="AccessDenied"/></term><description><c>PERMISSION_DENIED</c></description></item>
///   <item><term><see cref="NotAuthenticated"/></term><description><c>UNAUTHENTICATED</c></description></item>
///   <item><term><see cref="NotImplemented"/></term><description><c>UNIMPLEMENTED</c></description></item>
/// </list>
/// The remaining eleven share their canonical name. A consumer switches on the kind to decide
/// <em>behaviour</em> — retry, re-authenticate, show a field error — and on
/// <see cref="ErrorDefinition.Code"/> to decide what text to show.
/// <para>
/// The values are a wire contract. A kind outside this set is allowed, so a service can classify
/// something these sixteen do not cover, and it must be a lower snake case
/// (see <see cref="ErrorNaming.IsValidKind"/>). An unrecognized kind maps to HTTP 500.
/// </para>
/// </remarks>
public static class ErrorKinds
{
    /// <summary>
    /// The operation was canceled, typically by the caller going away.
    /// </summary>
    public const string Cancelled = "cancelled";

    /// <summary>
    /// The cause is not known or does not fit any other kind.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The request was rejected because its content is not acceptable.
    /// </summary>
    public const string Validation = "validation";

    /// <summary>
    /// The operation did not complete within the time allowed for it.
    /// </summary>
    public const string Timeout = "timeout";

    /// <summary>
    /// The thing being operated on does not exist.
    /// </summary>
    public const string NotFound = "not_found";

    /// <summary>
    /// The thing being created already exists.
    /// </summary>
    public const string AlreadyExists = "already_exists";

    /// <summary>
    /// The caller is known but is not permitted to do this.
    /// </summary>
    public const string AccessDenied = "access_denied";

    /// <summary>
    /// The caller has not proved who it is.
    /// </summary>
    public const string NotAuthenticated = "not_authenticated";

    /// <summary>
    /// A quota, rate limit, or other finite resource has run out.
    /// </summary>
    public const string ResourceExhausted = "resource_exhausted";

    /// <summary>
    /// The system is not in a state where this operation makes sense.
    /// </summary>
    public const string PreconditionFailed = "precondition_failed";

    /// <summary>
    /// The operation was abandoned, typically because of a conflict such as a failed optimistic
    /// concurrency check.
    /// </summary>
    public const string Aborted = "aborted";

    /// <summary>
    /// A value was outside the range the operation accepts.
    /// </summary>
    public const string OutOfRange = "out_of_range";

    /// <summary>
    /// The operation is not implemented or is not enabled here.
    /// </summary>
    public const string NotImplemented = "not_implemented";

    /// <summary>
    /// Something broke inside the service.
    /// </summary>
    public const string Internal = "internal";

    /// <summary>
    /// The service, or something it depends on, is temporarily unable to answer. Retrying may work.
    /// </summary>
    public const string Unavailable = "unavailable";

    /// <summary>
    /// Data has been lost or irrecoverably corrupted.
    /// </summary>
    public const string DataLoss = "data_loss";

    /// <summary>
    /// Every kind this library defines, in the order of the canonical code each mirrors.
    /// </summary>
    /// <remarks>
    /// Enumerable on purpose: a client's message catalogue can be generated from it, and a test can
    /// assert the catalogue covers every kind rather than discovering a gap in production.
    /// </remarks>
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
}
