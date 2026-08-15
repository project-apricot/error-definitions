using System.Collections.Frozen;

namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// Translates between an error kind and the status code of a transport, in both directions.
/// </summary>
/// <remarks>
/// Both tables live here so an HTTP mapping and a gRPC mapping cannot drift apart in separate packages.
/// <para>
/// The reverse direction matters as much as the forward one: a response may carry no error body at all —
/// a 401 challenge, a routing 404, a proxy's 502 — and still needs classifying.
/// <see cref="FromHttpStatusCode"/> never fails, falling back by range.
/// </para>
/// </remarks>
public static class ErrorKindStatus
{
    /// <summary>
    /// The status used for <see cref="ErrorKinds.Cancelled"/>: the caller closed the request before
    /// an answer could be sent.
    /// </summary>
    /// <remarks>
    /// An nginx convention rather than a registered code, hence no framework constant. Used because
    /// reporting a caller's own disconnect as a server error makes 500 rates unreadable.
    /// </remarks>
    public const int ClientClosedRequest = 499;

    /// <summary>
    /// The gRPC status code of a call that succeeded, for completeness alongside the sixteen failure
    /// codes.
    /// </summary>
    public const int GrpcOk = 0;

    /// <summary>
    /// The HTTP status each kind maps to.
    /// </summary>
    private static readonly FrozenDictionary<string, int> KindToHttp =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [ErrorKinds.Cancelled] = ClientClosedRequest,
            [ErrorKinds.Unknown] = 500,
            [ErrorKinds.Validation] = 400,
            [ErrorKinds.Timeout] = 504,
            [ErrorKinds.NotFound] = 404,
            [ErrorKinds.AlreadyExists] = 409,
            [ErrorKinds.AccessDenied] = 403,
            [ErrorKinds.NotAuthenticated] = 401,
            [ErrorKinds.ResourceExhausted] = 429,
            [ErrorKinds.PreconditionFailed] = 412,
            [ErrorKinds.Aborted] = 409,
            [ErrorKinds.OutOfRange] = 416,
            [ErrorKinds.NotImplemented] = 501,
            [ErrorKinds.Internal] = 500,
            [ErrorKinds.Unavailable] = 503,
            [ErrorKinds.DataLoss] = 500,
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// The kind each HTTP status maps back to.
    /// </summary>
    /// <remarks>
    /// Not the inverse of <see cref="KindToHttp"/>, which is not one to one — 409 and 500 are each
    /// claimed by more than one kind. The canonical answer per status is chosen here and pinned by tests.
    /// </remarks>
    private static readonly FrozenDictionary<int, string> HttpToKind =
        new Dictionary<int, string>
        {
            [ClientClosedRequest] = ErrorKinds.Cancelled,
            [400] = ErrorKinds.Validation,
            [401] = ErrorKinds.NotAuthenticated,
            [403] = ErrorKinds.AccessDenied,
            [404] = ErrorKinds.NotFound,
            [409] = ErrorKinds.AlreadyExists,
            [412] = ErrorKinds.PreconditionFailed,
            [416] = ErrorKinds.OutOfRange,
            [429] = ErrorKinds.ResourceExhausted,
            [500] = ErrorKinds.Internal,
            [501] = ErrorKinds.NotImplemented,
            [503] = ErrorKinds.Unavailable,
            [504] = ErrorKinds.Timeout,
        }.ToFrozenDictionary();

    /// <summary>
    /// The gRPC status code each kind maps to.
    /// </summary>
    /// <remarks>
    /// One to one in both directions: the kinds <em>are</em> the sixteen non-OK <c>google.rpc.Code</c>
    /// values, so this is a renaming rather than a lossy mapping.
    /// </remarks>
    private static readonly FrozenDictionary<string, int> KindToGrpc =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [ErrorKinds.Cancelled] = 1,
            [ErrorKinds.Unknown] = 2,
            [ErrorKinds.Validation] = 3,
            [ErrorKinds.Timeout] = 4,
            [ErrorKinds.NotFound] = 5,
            [ErrorKinds.AlreadyExists] = 6,
            [ErrorKinds.AccessDenied] = 7,
            [ErrorKinds.ResourceExhausted] = 8,
            [ErrorKinds.PreconditionFailed] = 9,
            [ErrorKinds.Aborted] = 10,
            [ErrorKinds.OutOfRange] = 11,
            [ErrorKinds.NotImplemented] = 12,
            [ErrorKinds.Internal] = 13,
            [ErrorKinds.Unavailable] = 14,
            [ErrorKinds.DataLoss] = 15,
            [ErrorKinds.NotAuthenticated] = 16,
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// The kind each gRPC status code maps back to.
    /// </summary>
    private static readonly FrozenDictionary<int, string> GrpcToKind =
        KindToGrpc.ToFrozenDictionary(entry => entry.Value, entry => entry.Key);

    /// <summary>
    /// Gets the HTTP status code for a kind.
    /// </summary>
    /// <param name="kind">The kind of the error, which may be null or a kind this library does not define.</param>
    /// <returns>The mapped status, or 500 when the kind is unrecognized.</returns>
    public static int ToHttpStatusCode(string? kind)
    {
        return kind is not null && KindToHttp.TryGetValue(kind, out var status) ? status : 500;
    }

    /// <summary>
    /// Gets the kind that best describes an HTTP status code.
    /// </summary>
    /// <param name="statusCode">The status code of a response.</param>
    /// <returns>
    /// The canonical kind for a status this library maps; otherwise
    /// <see cref="ErrorKinds.Validation"/> for any other 4xx, <see cref="ErrorKinds.Internal"/> for
    /// any other 5xx, and <see cref="ErrorKinds.Unknown"/> for anything else, including a success
    /// status.
    /// </returns>
    public static string FromHttpStatusCode(int statusCode)
    {
        if (HttpToKind.TryGetValue(statusCode, out var kind))
        {
            return kind;
        }

        // An unmapped status still has to classify, or a caller has no answer for what it did not expect.
        if (statusCode is >= 400 and < 500)
        {
            return ErrorKinds.Validation;
        }

        return statusCode is >= 500 and < 600 ? ErrorKinds.Internal : ErrorKinds.Unknown;
    }

    /// <summary>
    /// Gets the gRPC status code for a kind.
    /// </summary>
    /// <param name="kind">The kind of the error, which may be null or a kind this library does not define.</param>
    /// <returns>
    /// The mapped code, or 2 (<c>UNKNOWN</c>) when the kind is unrecognized. Never 0, since a kind
    /// always describes a failure.
    /// </returns>
    public static int ToGrpcStatusCode(string? kind)
    {
        return kind is not null && KindToGrpc.TryGetValue(kind, out var code) ? code : 2;
    }

    /// <summary>
    /// Gets the kind for a gRPC status code.
    /// </summary>
    /// <param name="grpcStatusCode">The gRPC status code, as a <c>google.rpc.Code</c> value.</param>
    /// <returns>The mapped kind, or <see cref="ErrorKinds.Unknown"/> for 0 and anything unrecognised.</returns>
    public static string FromGrpcStatusCode(int grpcStatusCode)
    {
        return GrpcToKind.GetValueOrDefault(grpcStatusCode, ErrorKinds.Unknown);
    }
}
