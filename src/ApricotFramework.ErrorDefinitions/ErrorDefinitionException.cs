using System.Collections.Immutable;

namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// An exception carrying one or more classified errors, for a service to throw where the failure is
/// part of its contract rather than a defect.
/// </summary>
/// <remarks>
/// <see cref="Errors"/> is never empty: every constructor leaves at least one error, so nothing that
/// handles this exception has to carry a fallback for the empty case.
/// <para>
/// Left open for subclassing on purpose. A library with its own failure — a rejected captcha, an
/// unmet authorisation requirement — can derive from this and be understood by the ASP.NET
/// integration with no mapper registered at all.
/// </para>
/// </remarks>
public class ErrorDefinitionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDefinitionException"/> class carrying a
    /// single unknown error.
    /// </summary>
    /// <remarks>
    /// Every public constructor routes through <see cref="Materialize"/> explicitly; a collection
    /// expression would bind to the private constructor and skip it, which the compiler cannot see.
    /// </remarks>
    public ErrorDefinitionException()
        : this(Materialize([]), null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDefinitionException"/> class carrying a
    /// single unknown error with the given message.
    /// </summary>
    /// <param name="message">The message describing what went wrong.</param>
    public ErrorDefinitionException(string? message)
        : this(Materialize([Err.Unknown(message: message)]), message, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDefinitionException"/> class carrying a
    /// single unknown error with the given message, wrapping another exception.
    /// </summary>
    /// <param name="message">The message describing what went wrong.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public ErrorDefinitionException(string? message, Exception? innerException)
        : this(Materialize([Err.Unknown(message: message)]), message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDefinitionException"/> class carrying the
    /// given errors.
    /// </summary>
    /// <param name="errors">The errors to carry. An empty sequence becomes a single unknown error.</param>
    public ErrorDefinitionException(IEnumerable<ErrorDefinition> errors)
        : this(errors, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDefinitionException"/> class carrying the
    /// given errors and message.
    /// </summary>
    /// <param name="errors">The errors to carry. An empty sequence becomes a single unknown error.</param>
    /// <param name="message">The message describing what went wrong.</param>
    public ErrorDefinitionException(IEnumerable<ErrorDefinition> errors, string? message)
        : this(errors, message, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDefinitionException"/> class carrying the
    /// given errors and message, wrapping another exception.
    /// </summary>
    /// <param name="errors">The errors to carry. An empty sequence becomes a single unknown error.</param>
    /// <param name="message">The message describing what went wrong.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    public ErrorDefinitionException(IEnumerable<ErrorDefinition> errors, string? message, Exception? innerException)
        : this(Materialize(errors), message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDefinitionException"/> class from errors
    /// that have already been copied and checked.
    /// </summary>
    /// <param name="errors">The errors to carry, non-empty.</param>
    /// <param name="message">The message describing what went wrong.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    private ErrorDefinitionException(ImmutableArray<ErrorDefinition> errors, string? message, Exception? innerException)
        : base(message ?? DescribeFirst(errors), innerException)
    {
        // The one place the non-empty invariant is applied, so no constructor route can miss it.
        this.Errors = errors.IsDefaultOrEmpty ? [Err.Unknown()] : errors;
    }

    /// <summary>
    /// Gets the errors this exception carries. Never empty.
    /// </summary>
    public IReadOnlyList<ErrorDefinition> Errors { get; }

    /// <summary>
    /// Gets the error that determines how the failure is reported — the status of a response, the
    /// title of a problem document.
    /// </summary>
    /// <returns>The first error carried.</returns>
    public ErrorDefinition FirstError()
    {
        return this.Errors[0];
    }

    /// <summary>
    /// Reports whether any error carried is of the given kind.
    /// </summary>
    /// <param name="kind">The kind to look for.</param>
    /// <returns><see langword="true"/> when at least one error has that kind.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="kind"/> is null.</exception>
    public bool HasKind(string kind)
    {
        ArgumentNullException.ThrowIfNull(kind);

        return this.Errors.Any(error => string.Equals(error.Kind, kind, StringComparison.Ordinal));
    }

    /// <summary>
    /// Reports whether any error carried has the given code.
    /// </summary>
    /// <param name="code">The code to look for.</param>
    /// <returns><see langword="true"/> when at least one error has that code.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="code"/> is null.</exception>
    public bool HasCode(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return this.Errors.Any(error => string.Equals(error.Code, code, StringComparison.Ordinal));
    }

    /// <summary>
    /// Copies the errors a caller supplied, once.
    /// </summary>
    /// <param name="errors">The errors a caller supplied.</param>
    /// <returns>The errors, copied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    private static ImmutableArray<ErrorDefinition> Materialize(IEnumerable<ErrorDefinition> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        // Copied, so a caller mutating the list afterwards cannot change what a thrown exception reports.
        // Once, so a lazy sequence is not enumerated twice.
        return errors.ToImmutableArray();
    }

    /// <summary>
    /// Builds the exception message from the errors, for when the caller gives none.
    /// </summary>
    /// <param name="errors">The errors the exception will carry, which may be empty here.</param>
    /// <returns>The first error's kind and code. Never the payload, which may hold request data.</returns>
    private static string DescribeFirst(ImmutableArray<ErrorDefinition> errors)
    {
        // Runs before the constructor body applies the non-empty invariant, so it cannot assume it.
        return errors.IsDefaultOrEmpty
            ? $"{ErrorKinds.Unknown}: {ErrorCodes.Unknown}"
            : $"{errors[0].Kind}: {errors[0].Code}";
    }
}
