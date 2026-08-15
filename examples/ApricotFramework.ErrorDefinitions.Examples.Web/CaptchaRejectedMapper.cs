using ApricotFramework.ErrorDefinitions.AspNetCore;

namespace ApricotFramework.ErrorDefinitions.Examples.Web;

/// <summary>
/// Reports a rejected captcha, keeping the reason the failure already knew.
/// </summary>
/// <remarks>
/// A full mapper rather than the one-line <c>MapExceptionToError</c> registration, because there is
/// something worth carrying: the reason goes into the error's payload, so a client can render
/// "the captcha expired" rather than a generic rejection. Flattening it into a message would leave the
/// client nothing to switch on.
/// </remarks>
internal sealed class CaptchaRejectedMapper : IExceptionErrorMapper
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception)
    {
        // Null for anything else, so every other mapper still gets its turn.
        return exception is CaptchaRejectedException rejected
            ?
            [
                Err.Validation(
                    DemoErrors.CaptchaRejected,
                    "The captcha was rejected.",
                    new Dictionary<string, object?> { ["reason"] = rejected.Reason }),
            ]
            : null;
    }
}
