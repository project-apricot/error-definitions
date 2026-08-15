using ApricotFramework.ErrorDefinitions.AspNetCore.Impl;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Tests;

/// <summary>
/// Covers the other half of withholding detail from the caller: that it is written down somewhere.
/// </summary>
/// <remarks>
/// Load-bearing rather than cosmetic. The framework's exception-handling middleware logs an exception
/// only when nothing handles it, and this handler answers by default — so if it did not log an
/// unrecognised failure itself, that failure would leave no trace at all, in the response or anywhere
/// else. That was true of the library this replaces.
/// </remarks>
public class ExceptionLoggingTests
{
    [Fact]
    public async Task TryHandleAsync_AnUnrecognisedException_IsLoggedAtErrorWithTheException()
    {
        var logger = new RecordingLogger();
        var cause = new InvalidOperationException("Server=db-prod-01;Password=hunter2");

        await Handler(logger).TryHandleAsync(Context(), cause, TestContext.Current.CancellationToken);

        var entry = Assert.Single(logger.Entries);

        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(cause, entry.Exception);
        Assert.Contains("only record", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryHandleAsync_AClassifiedError_IsNotLoggedAtErrorLevel()
    {
        // A service throwing a not-found is doing its job. Logging that at error level would bury real
        // failures under ordinary traffic.
        var logger = new RecordingLogger();

        await Handler(logger).TryHandleAsync(
            Context(),
            Err.NotFound("CONTENT_AUTHOR_NOT_FOUND").AsException(),
            TestContext.Current.CancellationToken);

        Assert.All(logger.Entries, entry => Assert.True(entry.Level < LogLevel.Warning, $"{entry.Level}"));
    }

    [Fact]
    public async Task TryHandleAsync_AClassifiedError_StillRecordsWhatWasReported()
    {
        var logger = new RecordingLogger { Enabled = LogLevel.Debug };

        await Handler(logger).TryHandleAsync(
            Context(),
            Err.NotFound("CONTENT_AUTHOR_NOT_FOUND").AsException(),
            TestContext.Current.CancellationToken);

        var entry = Assert.Single(logger.Entries);

        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains("CONTENT_AUTHOR_NOT_FOUND", entry.Message, StringComparison.Ordinal);
        Assert.Contains("404", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryHandleAsync_WhenTheResponseHasStarted_IsLoggedAtWarning()
    {
        var logger = new RecordingLogger();
        var context = Context();
        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponse());

        await Handler(logger).TryHandleAsync(context, Err.NotFound().AsException(), TestContext.Current.CancellationToken);

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    /// <summary>
    /// Builds the handler under test with the built-in mappers.
    /// </summary>
    /// <param name="logger">The logger to record into.</param>
    /// <returns>The handler.</returns>
    private static ErrorDefinitionsExceptionHandler Handler(ILogger<ErrorDefinitionsExceptionHandler> logger)
    {
        return new ErrorDefinitionsExceptionHandler(
            [new ErrorDefinitionExceptionMapper(), new CancellationExceptionMapper()],
            new Monitor(new ErrorDefinitionsOptions()),
            logger);
    }

    /// <summary>
    /// Builds a context whose response can be written to.
    /// </summary>
    /// <returns>The context.</returns>
    private static DefaultHttpContext Context()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/things";
        context.Response.Body = new MemoryStream();

        return context;
    }

    /// <summary>
    /// One recorded log entry.
    /// </summary>
    /// <param name="Level">The level it was written at.</param>
    /// <param name="Message">The formatted message.</param>
    /// <param name="Exception">The exception attached, if any.</param>
    private sealed record Entry(LogLevel Level, string Message, Exception? Exception);

    /// <summary>
    /// Records what was logged, standing in for a real logging provider.
    /// </summary>
    private sealed class RecordingLogger : ILogger<ErrorDefinitionsExceptionHandler>
    {
        /// <summary>
        /// Gets the entries written.
        /// </summary>
        public List<Entry> Entries { get; } = [];

        /// <summary>
        /// Gets or sets the lowest level this logger accepts. Defaults to information, matching a host
        /// that has not turned debug logging on.
        /// </summary>
        public LogLevel Enabled { get; set; } = LogLevel.Information;

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => logLevel >= this.Enabled;

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            this.Entries.Add(new Entry(logLevel, formatter(state, exception), exception));
        }
    }

    /// <summary>
    /// A response that claims to have started already.
    /// </summary>
    private sealed class StartedResponse : Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
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

    /// <summary>
    /// Hands out fixed settings.
    /// </summary>
    private sealed class Monitor : IOptionsMonitor<ErrorDefinitionsOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Monitor"/> class.
        /// </summary>
        /// <param name="value">The settings to hand out.</param>
        public Monitor(ErrorDefinitionsOptions value)
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
}
