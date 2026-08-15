namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// Creates error definitions.
/// </summary>
/// <remarks>
/// One shortcut per kind: <c>throw Err.Validation(ContentErrors.InvalidLocale).AsException();</c>
/// <para>
/// Passing no code is fine — the error carries that kind's <see cref="ErrorCodes">default code</see>.
/// Passing a <em>sentence</em> is not: a client uses the code as a lookup key and would render it as an
/// identifier, so a code that is not upper snake case is rejected. Use <c>message:</c> for prose.
/// </para>
/// </remarks>
public static class Err
{
    /// <summary>
    /// How much of an offending value an exception message repeats, so a long one stays readable.
    /// </summary>
    private const int MaxReportedValueLength = 60;

    /// <summary>
    /// Creates an error of any kind, including one this library does not define.
    /// </summary>
    /// <param name="kind">The classification of the error, in the lower snake case.</param>
    /// <param name="code">The specific error, in the upper snake case. Defaults to the kind's default code.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="kind"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="kind"/> is not a lower snake case, or is not an<paramref name="code"/>
    /// upper snake case.
    /// </exception>
    public static ErrorDefinition From(
        string kind,
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        ArgumentNullException.ThrowIfNull(kind);

        if (!ErrorNaming.IsValidKind(kind))
        {
            throw new ArgumentException(
                $"'{Abbreviate(kind)}' is not a valid error kind. A kind is lower snake case, such as '{ErrorKinds.NotFound}'.",
                nameof(kind));
        }

        var effectiveCode = code ?? ErrorCodes.ForKind(kind);

        if (!ErrorNaming.IsValidCode(effectiveCode))
        {
            throw new ArgumentException(
                $"'{Abbreviate(effectiveCode)}' is not a valid error code. A code is upper snake case, such as 'CONTENT_INVALID_LOCALE'. "
                + "To describe the error in prose, pass it as the message instead.",
                nameof(code));
        }

        return new ErrorDefinition
        {
            Kind = kind,
            Code = effectiveCode,
            Message = message ?? string.Empty,
            Payload = payload,
        };
    }

    /// <summary>
    /// Creates a canceled error: the operation was canceled, typically by the caller going away.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.Cancelled"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition Cancelled(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.Cancelled, code, message, payload);
    }

    /// <summary>
    /// Creates an unknown error: the cause is not known or does not fit any other kind.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.Unknown"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition Unknown(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.Unknown, code, message, payload);
    }

    /// <summary>
    /// Creates a validation error: the request was rejected because its content is not acceptable.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.Validation"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition Validation(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.Validation, code, message, payload);
    }

    /// <summary>
    /// Creates a timeout error: the operation did not complete within the time allowed for it.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.Timeout"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition Timeout(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.Timeout, code, message, payload);
    }

    /// <summary>
    /// Creates a not-found error: the thing being operated on does not exist.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.NotFound"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition NotFound(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.NotFound, code, message, payload);
    }

    /// <summary>
    /// Creates an already-exists error: the thing being created already exists.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.AlreadyExists"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition AlreadyExists(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.AlreadyExists, code, message, payload);
    }

    /// <summary>
    /// Creates an access-denied error: the caller is known but is not permitted to do this.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.AccessDenied"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition AccessDenied(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.AccessDenied, code, message, payload);
    }

    /// <summary>
    /// Creates a not-authenticated error: the caller has not proved who it is.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.NotAuthenticated"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition NotAuthenticated(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.NotAuthenticated, code, message, payload);
    }

    /// <summary>
    /// Creates a resource-exhausted error: a quota, rate limit, or other finite resource has run out.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.ResourceExhausted"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition ResourceExhausted(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.ResourceExhausted, code, message, payload);
    }

    /// <summary>
    /// Creates a precondition-failed error: the system is not in a state where this operation makes
    /// sense.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.PreconditionFailed"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition PreconditionFailed(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.PreconditionFailed, code, message, payload);
    }

    /// <summary>
    /// Creates an aborted error: the operation was abandoned, typically because of a conflict.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.Aborted"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition Aborted(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.Aborted, code, message, payload);
    }

    /// <summary>
    /// Creates an out-of-range error: a value was outside the range the operation accepts.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.OutOfRange"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition OutOfRange(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.OutOfRange, code, message, payload);
    }

    /// <summary>
    /// Creates a not-implemented error: the operation is not implemented or is not enabled here.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.NotImplemented"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition NotImplemented(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.NotImplemented, code, message, payload);
    }

    /// <summary>
    /// Creates an internal error: something broke inside the service.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.Internal"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition Internal(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.Internal, code, message, payload);
    }

    /// <summary>
    /// Creates an unavailable error: the service, or something it depends on, is temporarily unable
    /// to answer.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.Unavailable"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition Unavailable(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.Unavailable, code, message, payload);
    }

    /// <summary>
    /// Creates a data-loss error: data has been lost or irrecoverably corrupted.
    /// </summary>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.DataLoss"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The error definition.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is not upper snake case.</exception>
    public static ErrorDefinition DataLoss(
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        return From(ErrorKinds.DataLoss, code, message, payload);
    }

    /// <summary>
    /// Shortens a value for inclusion in an exception message.
    /// </summary>
    /// <param name="value">The value to shorten.</param>
    /// <returns>The value, truncated if it is long enough to bury the rest of the message.</returns>
    private static string Abbreviate(string value)
    {
        return value.Length <= MaxReportedValueLength
            ? value
            : string.Concat(value.AsSpan(0, MaxReportedValueLength), "…");
    }
}
