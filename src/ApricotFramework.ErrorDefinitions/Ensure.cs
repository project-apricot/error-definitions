using System.Diagnostics.CodeAnalysis;

namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// Asserts the preconditions of an operation, throwing the right kind of error when one does not
/// hold.
/// </summary>
/// <remarks>
/// Turns the three lines a guard usually takes into one:
/// <code>
/// var author = Ensure.Found(await this.repository.Get(id), ContentErrors.AuthorNotFound);
/// Ensure.Valid(ContentLocales.All.Contains(locale), ContentErrors.InvalidLocale);
/// </code>
/// Annotated so the compiler's flow analysis follows through: the value is known to be non-null
/// afterwards, and the condition known to hold.
/// </remarks>
public static class Ensure
{
    /// <summary>
    /// Requires that a reference is present.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="code">The specific error, in upper snake case. Defaults to <see cref="ErrorCodes.NotFound"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The value, known to be non-null.</returns>
    /// <exception cref="ErrorDefinitionException">Thrown when <paramref name="value"/> is null.</exception>
    public static T Found<T>(
        [NotNull] T? value,
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
        where T : class
    {
        if (value is null)
        {
            Err.NotFound(code, message, payload).Throw();
        }

        return value;
    }

    /// <summary>
    /// Requires that a value is present.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="code">The specific error, in upper snake case. Defaults to <see cref="ErrorCodes.NotFound"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <returns>The value, unwrapped.</returns>
    /// <exception cref="ErrorDefinitionException">Thrown when <paramref name="value"/> has no value.</exception>
    public static T Found<T>(
        [NotNull] T? value,
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
        where T : struct
    {
        if (!value.HasValue)
        {
            Err.NotFound(code, message, payload).Throw();
        }

        return value.Value;
    }

    /// <summary>
    /// Requires that the request is acceptable.
    /// </summary>
    /// <param name="condition">The condition that must hold.</param>
    /// <param name="code">The specific error, in upper snake case. Defaults to <see cref="ErrorCodes.Validation"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <exception cref="ErrorDefinitionException">Thrown when <paramref name="condition"/> is false.</exception>
    public static void Valid(
        [DoesNotReturnIf(false)] bool condition,
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        if (!condition)
        {
            Err.Validation(code, message, payload).Throw();
        }
    }

    /// <summary>
    /// Requires that the caller is permitted to do this.
    /// </summary>
    /// <param name="condition">The condition that must hold.</param>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.AccessDenied"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <exception cref="ErrorDefinitionException">Thrown when <paramref name="condition"/> is false.</exception>
    public static void Allowed(
        [DoesNotReturnIf(false)] bool condition,
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        if (!condition)
        {
            Err.AccessDenied(code, message, payload).Throw();
        }
    }

    /// <summary>
    /// Requires that the caller has proved who it is.
    /// </summary>
    /// <param name="condition">The condition that must hold.</param>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.NotAuthenticated"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <exception cref="ErrorDefinitionException">Thrown when <paramref name="condition"/> is false.</exception>
    public static void Authenticated(
        [DoesNotReturnIf(false)] bool condition,
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        if (!condition)
        {
            Err.NotAuthenticated(code, message, payload).Throw();
        }
    }

    /// <summary>
    /// Requires that the system is in a state where this operation makes sense.
    /// </summary>
    /// <param name="condition">The condition that must hold.</param>
    /// <param name="code">The specific error, in the upper snake case. Defaults to <see cref="ErrorCodes.PreconditionFailed"/>.</param>
    /// <param name="message">A description for whoever reads the response directly.</param>
    /// <param name="payload">Parameters for the text a client renders from the code.</param>
    /// <exception cref="ErrorDefinitionException">Thrown when <paramref name="condition"/> is false.</exception>
    public static void Precondition(
        [DoesNotReturnIf(false)] bool condition,
        string? code = null,
        string? message = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        if (!condition)
        {
            Err.PreconditionFailed(code, message, payload).Throw();
        }
    }
}
