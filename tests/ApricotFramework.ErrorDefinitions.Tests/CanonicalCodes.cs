namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// The canonical error codes of <c>google.rpc.Code</c>, transcribed from the specification.
/// </summary>
/// <remarks>
/// An independent source, which is the point: the kind set claims to be these sixteen codes under
/// friendlier names, and a test comparing the library to itself would not check that claim. The
/// numbers are the wire values of <c>google.rpc.Code</c>, which gRPC's <c>StatusCode</c> shares, so
/// they also pin the gRPC mapping.
/// </remarks>
internal static class CanonicalCodes
{
    /// <summary>
    /// Gets each canonical code, as its number and specification name, paired with the kind this
    /// library uses for it.
    /// </summary>
    public static IReadOnlyList<(int Number, string CanonicalName, string Kind)> All { get; } =
    [
        (1, "CANCELLED", ErrorKinds.Cancelled),
        (2, "UNKNOWN", ErrorKinds.Unknown),
        (3, "INVALID_ARGUMENT", ErrorKinds.Validation),
        (4, "DEADLINE_EXCEEDED", ErrorKinds.Timeout),
        (5, "NOT_FOUND", ErrorKinds.NotFound),
        (6, "ALREADY_EXISTS", ErrorKinds.AlreadyExists),
        (7, "PERMISSION_DENIED", ErrorKinds.AccessDenied),
        (8, "RESOURCE_EXHAUSTED", ErrorKinds.ResourceExhausted),
        (9, "FAILED_PRECONDITION", ErrorKinds.PreconditionFailed),
        (10, "ABORTED", ErrorKinds.Aborted),
        (11, "OUT_OF_RANGE", ErrorKinds.OutOfRange),
        (12, "UNIMPLEMENTED", ErrorKinds.NotImplemented),
        (13, "INTERNAL", ErrorKinds.Internal),
        (14, "UNAVAILABLE", ErrorKinds.Unavailable),
        (15, "DATA_LOSS", ErrorKinds.DataLoss),
        (16, "UNAUTHENTICATED", ErrorKinds.NotAuthenticated),
    ];
}
