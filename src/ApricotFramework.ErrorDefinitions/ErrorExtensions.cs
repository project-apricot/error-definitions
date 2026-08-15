using System.Diagnostics.CodeAnalysis;

namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// Turns error definitions into the exception that carries them.
/// </summary>
public static class ErrorExtensions
{
    /// <summary>
    /// Wraps the error in an exception, for throwing.
    /// </summary>
    /// <param name="error">The error to carry.</param>
    /// <param name="message">A message for the exception, defaulting to the error's kind and code.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    /// <returns>The exception carrying the error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static ErrorDefinitionException AsException(
        this ErrorDefinition error,
        string? message = null,
        Exception? innerException = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ErrorDefinitionException([error], message, innerException);
    }

    /// <summary>
    /// Wraps the errors in a single exception, for throwing.
    /// </summary>
    /// <param name="errors">The errors to carry. An empty sequence becomes a single unknown error.</param>
    /// <param name="message">A message for the exception, defaulting to the first error's kind and code.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    /// <returns>The exception carrying the errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    public static ErrorDefinitionException AsException(
        this IEnumerable<ErrorDefinition> errors,
        string? message = null,
        Exception? innerException = null)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return new ErrorDefinitionException(errors, message, innerException);
    }

    /// <summary>
    /// Throws the error.
    /// </summary>
    /// <param name="error">The error to throw.</param>
    /// <param name="message">A message for the exception, defaulting to the error's kind and code.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    /// <exception cref="ErrorDefinitionException">Always.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    /// <remarks>
    /// Attributed as never returning, so the compiler treats a call as terminating: it can stand as
    /// the last statement of a non-void method, or in a switch arm, without the flow analysis
    /// complaining about a missing return or a possible null afterward.
    /// </remarks>
    [DoesNotReturn]
    public static void Throw(
        this ErrorDefinition error,
        string? message = null,
        Exception? innerException = null)
    {
        throw error.AsException(message, innerException);
    }
}
