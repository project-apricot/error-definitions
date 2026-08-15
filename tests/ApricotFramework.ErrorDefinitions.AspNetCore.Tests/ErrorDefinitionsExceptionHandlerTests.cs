using System.Text;
using System.Text.Json;
using ApricotFramework.ErrorDefinitions.AspNetCore.Impl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Tests;

/// <summary>
/// Covers what reaches the caller when a request fails, and — as much to the point — what does not.
/// </summary>
public class ErrorDefinitionsExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_AClassifiedError_ReportsItAsAProblemDocument()
    {
        var context = Context("/api/content");

        var handled = await Handler().TryHandleAsync(
            context,
            Err.Validation("CONTENT_INVALID_LOCALE", "The locale is invalid").AsException(),
            TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        using var document = JsonDocument.Parse(BodyOf(context));

        Assert.Equal("about:blank", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("Validation failed", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("The locale is invalid", document.RootElement.GetProperty("detail").GetString());
        Assert.Equal("/api/content", document.RootElement.GetProperty("instance").GetString());

        var error = document.RootElement.GetProperty("errors")[0];

        Assert.Equal("validation", error.GetProperty("kind").GetString());
        Assert.Equal("CONTENT_INVALID_LOCALE", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_SeveralErrors_ReportsThemAllWithOneStatus()
    {
        var context = Context("/api/payers");

        await Handler().TryHandleAsync(
            context,
            new ErrorCollector()
                .Add(Err.Validation("PAYER_INVALID_EMAIL"))
                .Add(Err.Validation("PAYER_INVALID_AGE"))
                .ToErrors()
                .AsException(),
            TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(BodyOf(context));

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal(2, document.RootElement.GetProperty("errors").GetArrayLength());
    }

    [Fact]
    public async Task TryHandleAsync_AnUnmappedException_SaysNothingAboutIt()
    {
        // The whole reason details are withheld: an unhandled exception's text routinely holds
        // connection strings, SQL and paths.
        var context = Context("/api/things");
        var secret = "Login failed for user 'sa'; Server=db-prod-01;Password=hunter2";

        var handled = await Handler().TryHandleAsync(
            context,
            new InvalidOperationException(secret),
            TestContext.Current.CancellationToken);

        var body = BodyOf(context);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.DoesNotContain("hunter2", body, StringComparison.Ordinal);
        Assert.DoesNotContain("db-prod-01", body, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Login failed", body, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(body);
        var error = document.RootElement.GetProperty("errors")[0];

        Assert.Equal("internal", error.GetProperty("kind").GetString());
        Assert.Equal("INTERNAL", error.GetProperty("code").GetString());
        Assert.Equal(string.Empty, error.GetProperty("message").GetString());
        Assert.False(document.RootElement.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task TryHandleAsync_AnUnmappedExceptionWhenNotHandlingThem_LeavesItAlone()
    {
        // A host with another handler that must see unrecognised exceptions.
        var context = Context("/api/things");

        var handled = await Handler(options => options.HandleUnmappedExceptions = false).TryHandleAsync(
            context,
            new InvalidOperationException("boom"),
            TestContext.Current.CancellationToken);

        Assert.False(handled);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(string.Empty, BodyOf(context));
    }

    [Fact]
    public async Task TryHandleAsync_AClassifiedErrorWhenNotHandlingUnmapped_IsStillReported()
    {
        // Turning off the catch-all must not turn off the library's own reason for existing.
        var context = Context("/api/content");

        var handled = await Handler(options => options.HandleUnmappedExceptions = false).TryHandleAsync(
            context,
            Err.NotFound().AsException(),
            TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WhenTheResponseHasStarted_LeavesItAlone()
    {
        // Assigning a status or writing a body now would throw, losing the original failure.
        var context = Context("/api/content");
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var handled = await Handler().TryHandleAsync(
            context,
            Err.NotFound().AsException(),
            TestContext.Current.CancellationToken);

        Assert.False(handled);
    }

    [Fact]
    public async Task TryHandleAsync_ACustomKind_IsReportedAs500ByDefault()
    {
        var context = Context("/api/things");

        await Handler().TryHandleAsync(
            context,
            Err.From("legally_blocked").AsException(),
            TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(BodyOf(context));

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("legally_blocked", document.RootElement.GetProperty("errors")[0].GetProperty("kind").GetString());
        Assert.Equal("LEGALLY_BLOCKED", document.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_TheFirstMapperToRecogniseTheExceptionWins()
    {
        var context = Context("/api/things");
        var handler = Handler(
            mappers:
            [
                new StubMapper(recognises: false, ErrorKinds.Aborted),
                new StubMapper(recognises: true, ErrorKinds.AccessDenied),
                new StubMapper(recognises: true, ErrorKinds.Timeout),
            ]);

        await handler.TryHandleAsync(context, new InvalidOperationException("boom"), TestContext.Current.CancellationToken);

        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_AMapperReturningNothing_IsTreatedAsNotRecognising()
    {
        // An empty list would otherwise produce a document with no errors, which is not the contract.
        var context = Context("/api/things");
        var handler = Handler(mappers: [new EmptyMapper()]);

        await handler.TryHandleAsync(context, new InvalidOperationException("boom"), TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(BodyOf(context));

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(1, document.RootElement.GetProperty("errors").GetArrayLength());
        Assert.Equal("INTERNAL", document.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_AMapperCanPutStructuredDataInThePayload()
    {
        // What a handler-per-library could not do: a captcha's reason reached the response instead of
        // being flattened into a message and dropped.
        var context = Context("/api/verify");
        var handler = Handler(mappers: [new CaptchaMapper()]);

        await handler.TryHandleAsync(context, new FormatException("rejected"), TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(BodyOf(context));

        var payload = document.RootElement.GetProperty("errors")[0].GetProperty("payload");

        Assert.Equal("timeout-or-duplicate", payload.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_ASubclassOfTheException_IsRecognisedWithNoMapper()
    {
        var context = Context("/api/verify");

        var handled = await Handler().TryHandleAsync(context, new DerivedFailure(), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ACancelledRequest_IsReportedAsCancelled()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();

        var context = Context("/api/things");
        context.RequestAborted = aborted.Token;

        await Handler().TryHandleAsync(context, new OperationCanceledException(), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKindStatus.ClientClosedRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ACancellationTheCallerDidNotCause_IsReportedAsATimeout()
    {
        // A downstream call that ran out of time is this service failing, not the caller leaving.
        var context = Context("/api/things");

        await Handler().TryHandleAsync(context, new TaskCanceledException(), TestContext.Current.CancellationToken);

        Assert.Equal(504, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WithNullArguments_Throws()
    {
        var handler = Handler();

        await Assert.ThrowsAsync<ArgumentNullException>(
            "httpContext",
            async () => await handler.TryHandleAsync(null!, new InvalidOperationException(), TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ArgumentNullException>(
            "exception",
            async () => await handler.TryHandleAsync(Context("/x"), null!, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Builds the handler under test.
    /// </summary>
    /// <param name="configure">Adjusts the settings.</param>
    /// <param name="mappers">The mappers to consult, defaulting to the built-in ones.</param>
    /// <returns>The handler.</returns>
    private static ErrorDefinitionsExceptionHandler Handler(
        Action<ErrorDefinitionsOptions>? configure = null,
        IEnumerable<IExceptionErrorMapper>? mappers = null)
    {
        var options = new ErrorDefinitionsOptions();
        configure?.Invoke(options);

        return new ErrorDefinitionsExceptionHandler(
            mappers ?? [new ErrorDefinitionExceptionMapper(), new CancellationExceptionMapper()],
            new StubOptionsMonitor(options),
            NullLogger<ErrorDefinitionsExceptionHandler>.Instance);
    }

    /// <summary>
    /// Builds a context whose response body can be read back.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <returns>The context.</returns>
    private static DefaultHttpContext Context(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        return context;
    }

    /// <summary>
    /// Reads back what was written to the response.
    /// </summary>
    /// <param name="context">The context written to.</param>
    /// <returns>The body as text.</returns>
    private static string BodyOf(HttpContext context)
    {
        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Hands out fixed settings, standing in for the options system.
    /// </summary>
    private sealed class StubOptionsMonitor : IOptionsMonitor<ErrorDefinitionsOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubOptionsMonitor"/> class.
        /// </summary>
        /// <param name="value">The settings to hand out.</param>
        public StubOptionsMonitor(ErrorDefinitionsOptions value)
        {
            this.CurrentValue = value;
        }

        /// <inheritdoc />
        public ErrorDefinitionsOptions CurrentValue { get; }

        /// <inheritdoc />
        public ErrorDefinitionsOptions Get(string? name) => this.CurrentValue;

        /// <inheritdoc />
        public IDisposable? OnChange(Action<ErrorDefinitionsOptions, string?> listener) => null;
    }

    /// <summary>
    /// A mapper that answers every exception, or none, with a fixed kind.
    /// </summary>
    private sealed class StubMapper : IExceptionErrorMapper
    {
        /// <summary>
        /// Whether this mapper claims the exception.
        /// </summary>
        private readonly bool recognises;

        /// <summary>
        /// The kind it reports.
        /// </summary>
        private readonly string kind;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubMapper"/> class.
        /// </summary>
        /// <param name="recognises">Whether this mapper claims the exception.</param>
        /// <param name="kind">The kind it reports.</param>
        public StubMapper(bool recognises, string kind)
        {
            this.recognises = recognises;
            this.kind = kind;
        }

        /// <inheritdoc />
        public IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception)
        {
            return this.recognises ? [Err.From(this.kind)] : null;
        }
    }

    /// <summary>
    /// A mapper that claims the exception but reports nothing.
    /// </summary>
    private sealed class EmptyMapper : IExceptionErrorMapper
    {
        /// <inheritdoc />
        public IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception) => [];
    }

    /// <summary>
    /// A mapper that carries a failure's own structured detail into the error's payload.
    /// </summary>
    private sealed class CaptchaMapper : IExceptionErrorMapper
    {
        /// <inheritdoc />
        public IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception)
        {
            return exception is FormatException
                ?
                [
                    Err.Validation(
                        "CAPTCHA_REJECTED",
                        payload: new Dictionary<string, object?> { ["reason"] = "timeout-or-duplicate" }),
                ]
                : null;
        }
    }

    /// <summary>
    /// A library's own failure expressed by deriving from the exception rather than mapping it.
    /// </summary>
    private sealed class DerivedFailure : ErrorDefinitionException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DerivedFailure"/> class.
        /// </summary>
        public DerivedFailure()
            : base([Err.AlreadyExists("CONTENT_SLUG_TAKEN")])
        {
        }
    }

    /// <summary>
    /// A response that claims to have started already.
    /// </summary>
    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        /// <inheritdoc />
        public int StatusCode { get; set; } = 200;

        /// <inheritdoc />
        public string? ReasonPhrase { get; set; }

        /// <inheritdoc />
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        /// <inheritdoc />
        public Stream Body { get; set; } = Stream.Null;

        /// <inheritdoc />
        public bool HasStarted => true;

        /// <inheritdoc />
        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        /// <inheritdoc />
        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
