using Microsoft.AspNetCore.Http;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Impl;

/// <summary>
/// Maps one exception type to one kind and code.
/// </summary>
/// <remarks>
/// What most mappers would otherwise be: a type test and a single error. Registered through
/// <c>MapExceptionToError&lt;TException&gt;</c>, so the common case is one line rather than a class.
/// </remarks>
public sealed class TypedExceptionErrorMapper : IExceptionErrorMapper
{
    /// <summary>
    /// The exception type this mapper recognises, including its subclasses.
    /// </summary>
    private readonly Type exceptionType;

    /// <summary>
    /// The error reported for it, built once because it never varies.
    /// </summary>
    private readonly IReadOnlyList<ErrorDefinition> errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypedExceptionErrorMapper"/> class.
    /// </summary>
    /// <param name="exceptionType">The exception type to recognise, including its subclasses.</param>
    /// <param name="kind">The kind to report it as.</param>
    /// <param name="code">The code to report, defaulting to the kind's default code.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exceptionType"/> or <paramref name="kind"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="exceptionType"/> is not an exception type, when
    /// <paramref name="kind"/> is not lower snake case, or when <paramref name="code"/> is not upper
    /// snake case.
    /// </exception>
    public TypedExceptionErrorMapper(Type exceptionType, string kind, string? code = null)
    {
        ArgumentNullException.ThrowIfNull(exceptionType);
        ArgumentNullException.ThrowIfNull(kind);

        if (!typeof(Exception).IsAssignableFrom(exceptionType))
        {
            throw new ArgumentException($"'{exceptionType}' is not an exception type.", nameof(exceptionType));
        }

        this.exceptionType = exceptionType;

        // Built here, so a malformed code fails at startup rather than on the first failing request.
        this.errors = [Err.From(kind, code)];
    }

    /// <inheritdoc />
    public IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return this.exceptionType.IsInstanceOfType(exception) ? this.errors : null;
    }
}
