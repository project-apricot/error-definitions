using Microsoft.AspNetCore.Http;

namespace ApricotFramework.ErrorDefinitions.AspNetCore;

/// <summary>
/// Turns an exception a library or service owns into classified errors.
/// </summary>
/// <remarks>
/// A library registers one of these instead of an
/// <see cref="Microsoft.AspNetCore.Diagnostics.IExceptionHandler"/> of its own, so mappers are consulted
/// by one handler rather than competing to answer first.
/// <para>
/// They return full error definitions rather than only a kind because a failure usually knows more than
/// its classification — which field, which limit, which reason — and that belongs in the payload.
/// </para>
/// </remarks>
public interface IExceptionErrorMapper
{
    /// <summary>
    /// Maps an exception to the errors that describe it.
    /// </summary>
    /// <param name="httpContext">The request being answered.</param>
    /// <param name="exception">The exception to map.</param>
    /// <returns>
    /// The errors describing the exception, or null when this mapper does not recognise it.
    /// </returns>
    /// <remarks>
    /// <strong>Return null for an exception you do not recognise.</strong> Mappers are consulted in
    /// registration order and the first non-null answer wins, so a mapper that answers everything
    /// silently disables every mapper after it.
    /// </remarks>
    IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception);
}
