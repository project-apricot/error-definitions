namespace ApricotFramework.ErrorDefinitions.Examples.Web;

/// <summary>
/// The error codes this service can report.
/// </summary>
/// <remarks>
/// The convention worth copying: one class per service listing every code it sends, each prefixed with
/// the service's own name so codes stay unique across a fleet without any central registry.
/// <para>
/// Keeping them in one place is what makes a client's message catalogue reviewable — the set a client
/// must have text for is this file plus <see cref="ErrorCodes.All"/>, and a test can assert the
/// catalogue covers both.
/// </para>
/// </remarks>
internal static class DemoErrors
{
    /// <summary>
    /// No author has the requested id.
    /// </summary>
    public const string AuthorNotFound = "DEMO_AUTHOR_NOT_FOUND";

    /// <summary>
    /// The author's name is missing or blank.
    /// </summary>
    public const string AuthorInvalidName = "DEMO_AUTHOR_INVALID_NAME";

    /// <summary>
    /// The author's email is missing or not an email.
    /// </summary>
    public const string AuthorInvalidEmail = "DEMO_AUTHOR_INVALID_EMAIL";

    /// <summary>
    /// The locale is not one this service publishes in.
    /// </summary>
    public const string InvalidLocale = "DEMO_INVALID_LOCALE";

    /// <summary>
    /// The captcha was rejected.
    /// </summary>
    public const string CaptchaRejected = "DEMO_CAPTCHA_REJECTED";

    /// <summary>
    /// A dependency this service does not own failed.
    /// </summary>
    public const string LegacySubsystemFailed = "DEMO_LEGACY_SUBSYSTEM_FAILED";
}
