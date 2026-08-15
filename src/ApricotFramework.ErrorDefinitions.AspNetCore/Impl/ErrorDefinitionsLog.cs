using Microsoft.Extensions.Logging;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Impl;

/// <summary>
/// The log messages the handler writes.
/// </summary>
/// <remarks>
/// The middleware logs an exception only when nothing handles it, and this handler answers by default, so
/// an unrecognised failure is logged here at error level or nowhere at all. A classified error is a
/// service doing its job, so those are debug.
/// </remarks>
internal static partial class ErrorDefinitionsLog
{
    /// <summary>
    /// Records a failure the service classified deliberately.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="kind">The kind reported.</param>
    /// <param name="code">The code reported.</param>
    /// <param name="status">The status sent.</param>
    /// <param name="exception">The exception behind it.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Reported {Kind}/{Code} with status {Status}.")]
    public static partial void Reported(ILogger logger, string kind, string code, int status, Exception exception);

    /// <summary>
    /// Records a failure nothing recognised, which the caller is told nothing about.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="code">The code reported in its place.</param>
    /// <param name="exception">The exception the caller is not told about.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "An unrecognised exception was reported to the caller as {Code} with no detail. This log is the only record of it.")]
    public static partial void Unrecognised(ILogger logger, string code, Exception exception);

    /// <summary>
    /// Records that the response could not be replaced because it had already begun.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="exception">The exception that could not be reported.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "The response had already started, so the failure could not be reported as a problem document.")]
    public static partial void ResponseAlreadyStarted(ILogger logger, Exception exception);

    /// <summary>
    /// Records that an exception was passed on because no mapper recognised it.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="exception">The exception that was left alone.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "No mapper recognised the exception and unmapped exceptions are not handled here, so it was left to the rest of the pipeline.")]
    public static partial void LeftUnhandled(ILogger logger, Exception exception);
}
