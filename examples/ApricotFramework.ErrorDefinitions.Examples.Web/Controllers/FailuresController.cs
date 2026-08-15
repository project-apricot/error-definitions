using Microsoft.AspNetCore.Mvc;

namespace ApricotFramework.ErrorDefinitions.Examples.Web.Controllers;

/// <summary>
/// The three ways an exception that is not an <see cref="ErrorDefinitionException"/> reaches the caller.
/// </summary>
[ApiController]
[Route("api/failures")]
public sealed class FailuresController : ControllerBase
{
    /// <summary>
    /// Fails with a library's own exception, mapped by a mapper that keeps its reason.
    /// </summary>
    /// <returns>Nothing; always throws.</returns>
    /// <remarks>
    /// See <see cref="CaptchaRejectedMapper"/>. The reason ends up in the error's payload rather than
    /// flattened into a message, which is what a client needs to say something specific.
    /// </remarks>
    [HttpGet("captcha")]
    public IActionResult Captcha()
    {
        throw new CaptchaRejectedException("timeout-or-duplicate");
    }

    /// <summary>
    /// Fails with an exception that is only ever one kind, mapped in one line at registration.
    /// </summary>
    /// <returns>Nothing; always throws.</returns>
    [HttpGet("legacy")]
    public IActionResult Legacy()
    {
        throw new InvalidTimeZoneException("the legacy scheduler is down");
    }

    /// <summary>
    /// Fails with something nothing recognises.
    /// </summary>
    /// <returns>Nothing; always throws.</returns>
    /// <remarks>
    /// The response says <c>internal</c> and nothing else — no message, no exception type — because this
    /// text is exactly the kind that holds credentials. It is in the log, at error level, and the log is
    /// the only place it appears.
    /// </remarks>
    [HttpGet("unhandled")]
    public IActionResult Unhandled()
    {
        throw new InvalidOperationException("Login failed for user 'sa'. Server=db-prod-01;Password=hunter2");
    }
}
