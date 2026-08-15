namespace ApricotFramework.ErrorDefinitions.AspNetCore;

/// <summary>
/// The one thing about error reporting a host decides.
/// </summary>
/// <remarks>
/// Deliberately not bindable from configuration. What a service answers when it fails is part of its
/// contract, and a contract that an environment variable can change is not one — the same code would
/// mean different things in staging and production, and the change would never pass through a review.
/// So this is set in code, beside the middleware it affects, or not at all.
/// </remarks>
public sealed class ErrorDefinitionsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether an exception no mapper recognises is reported as an
    /// internal error. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Leaving this on means the response is uniform whatever went wrong, which is the point of a
    /// standard error contract — but it also means this handler answers everything, so an exception
    /// handler registered after it never runs. Turn it off in a host that has another handler which must
    /// see unrecognised exceptions; classified errors are still reported either way.
    /// <para>
    /// A pipeline-composition decision, which is why it is here and not in configuration.
    /// </para>
    /// </remarks>
    public bool HandleUnmappedExceptions { get; set; } = true;
}
