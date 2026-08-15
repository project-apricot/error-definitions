using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Impl;

/// <summary>
/// Reports a failed request as a problem document, asking the registered mappers what the failure was.
/// </summary>
/// <remarks>
/// The exception path only. A response the framework writes itself — a challenge, a forbid, a routing
/// miss, a model-binding rejection — never reaches an exception handler, and rewriting those is out of
/// scope: a challenge may be a redirect, and an OAuth error body follows its own specification. A caller
/// classifies those from the status instead, via <see cref="Http.HttpResponseMessageErrorExtensions"/>.
/// </remarks>
public sealed class ErrorDefinitionsExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// What an exception no mapper recognised is reported as.
    /// </summary>
    /// <remarks>
    /// Fixed rather than configurable: the same answer whatever the service and whatever the environment.
    /// </remarks>
    private static readonly IReadOnlyList<ErrorDefinition> UnrecognisedErrors = [Err.Internal()];

    /// <summary>
    /// The mappers to consult, in registration order.
    /// </summary>
    private readonly IReadOnlyList<IExceptionErrorMapper> mappers;

    /// <summary>
    /// The current settings.
    /// </summary>
    private readonly IOptionsMonitor<ErrorDefinitionsOptions> options;

    /// <summary>
    /// Where failures are recorded, since the response deliberately says nothing about them.
    /// </summary>
    private readonly ILogger<ErrorDefinitionsExceptionHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDefinitionsExceptionHandler"/> class.
    /// </summary>
    /// <param name="mappers">The mappers to consult, in registration order.</param>
    /// <param name="options">The settings to read.</param>
    /// <param name="logger">Where to record failures.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public ErrorDefinitionsExceptionHandler(
        IEnumerable<IExceptionErrorMapper> mappers,
        IOptionsMonitor<ErrorDefinitionsOptions> options,
        ILogger<ErrorDefinitionsExceptionHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(mappers);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        this.mappers = [.. mappers];
        this.options = options;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var settings = this.options.CurrentValue;
        var errors = this.Map(httpContext, exception);
        var recognised = errors is not null;

        if (errors is null)
        {
            if (!settings.HandleUnmappedExceptions)
            {
                ErrorDefinitionsLog.LeftUnhandled(this.logger, exception);

                return false;
            }

            // Nothing from the exception reaches the body; its text routinely holds credentials and SQL.
            errors = UnrecognisedErrors;
        }

        if (httpContext.Response.HasStarted)
        {
            // Writing now would throw; returning false leaves the failure to the framework.
            ErrorDefinitionsLog.ResponseAlreadyStarted(this.logger, exception);

            return false;
        }

        var status = ErrorKindStatus.ToHttpStatusCode(errors[0].Kind);

        if (recognised)
        {
            ErrorDefinitionsLog.Reported(this.logger, errors[0].Kind, errors[0].Code, status, exception);
        }
        else
        {
            // The only record that will exist: handling it here stops the middleware logging it at all.
            ErrorDefinitionsLog.Unrecognised(this.logger, errors[0].Code, exception);
        }

        await httpContext.WriteErrorProblemDetailsAsync(errors, status, cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Asks each mapper in turn what the exception was.
    /// </summary>
    /// <param name="httpContext">The request being answered.</param>
    /// <param name="exception">The exception to map.</param>
    /// <returns>The first non-null answer, or null when no mapper recognised the exception.</returns>
    private IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception)
    {
        foreach (var mapper in this.mappers)
        {
            var mapped = mapper.Map(httpContext, exception);

            // An empty list would produce a document with no errors, so it counts as not recognised.
            if (mapped is { Count: > 0 })
            {
                return mapped;
            }
        }

        return null;
    }
}
