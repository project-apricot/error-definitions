namespace ApricotFramework.ErrorDefinitions.Examples.Web;

/// <summary>
/// A captcha check that failed, carrying why it failed.
/// </summary>
/// <remarks>
/// Stands in for a library's own failure type: something that exists already, knows more than its
/// classification, and cannot be changed to throw an <see cref="ErrorDefinitionException"/> instead.
/// <see cref="CaptchaRejectedMapper"/> is how it reaches the response with its reason intact.
/// </remarks>
internal sealed class CaptchaRejectedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptchaRejectedException"/> class.
    /// </summary>
    /// <param name="reason">Why the captcha was rejected.</param>
    public CaptchaRejectedException(string reason)
        : base($"The captcha was rejected: {reason}")
    {
        this.Reason = reason;
    }

    /// <summary>
    /// Gets why the captcha was rejected.
    /// </summary>
    public string Reason { get; }
}
