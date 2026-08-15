using Microsoft.AspNetCore.Http;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Impl;

/// <summary>
/// Maps an <see cref="ErrorDefinitionException"/> to the errors it already carries.
/// </summary>
/// <remarks>
/// Always registered, and first, so a service throwing classified errors needs no configuration. Covers
/// subclasses, so a library can express its own failure by deriving rather than writing a mapper.
/// </remarks>
public sealed class ErrorDefinitionExceptionMapper : IExceptionErrorMapper
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // Null for anything else — being first, a mapper that answered everything would hide the rest.
        return exception is ErrorDefinitionException defined ? defined.Errors : null;
    }
}
