using System.Text;
using System.Text.Json;
using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Tests;

/// <summary>
/// Covers writing the standard shape from somewhere other than an exception handler.
/// </summary>
public class HttpContextErrorExtensionsTests
{
    [Fact]
    public async Task WriteErrorProblemDetailsAsync_SetsTheStatusContentTypeAndLength()
    {
        var context = Context("/api/content");

        await context.WriteErrorProblemDetailsAsync(
            [Err.NotFound("CONTENT_AUTHOR_NOT_FOUND")],
            cancellationToken: TestContext.Current.CancellationToken);

        var body = BodyOf(context);

        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(Encoding.UTF8.GetByteCount(body), context.Response.ContentLength);
    }

    [Fact]
    public async Task WriteErrorProblemDetailsAsync_UsesTheRequestPathAsTheInstance()
    {
        var context = Context("/api/content/7");

        await context.WriteErrorProblemDetailsAsync(
            [Err.NotFound()],
            cancellationToken: TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(BodyOf(context));

        Assert.Equal("/api/content/7", document.RootElement.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task WriteErrorProblemDetailsAsync_DoesNotEchoTheQueryString()
    {
        // A query string carries values the caller passed, and echoing those into an error body puts
        // request data into logs and screenshots.
        var context = Context("/api/search");
        context.Request.QueryString = new QueryString("?token=secret-value");

        await context.WriteErrorProblemDetailsAsync(
            [Err.Validation()],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.DoesNotContain("secret-value", BodyOf(context), StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteErrorProblemDetailsAsync_WithAnExplicitStatus_UsesIt()
    {
        var context = Context("/api/things");

        await context.WriteErrorProblemDetailsAsync(
            [Err.From("legally_blocked")],
            451,
            TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(BodyOf(context));

        Assert.Equal(451, context.Response.StatusCode);
        Assert.Equal(451, document.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task WriteErrorProblemDetailsAsync_ADocumentDirectly_WritesItAsGiven()
    {
        var context = Context("/api/things");
        var problem = ErrorProblemDetails.From([Err.Aborted("CONTENT_CONFLICT")], "/somewhere/else");

        await context.WriteErrorProblemDetailsAsync(problem, TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(BodyOf(context));

        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal("/somewhere/else", document.RootElement.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task WriteErrorProblemDetailsAsync_IsNotAffectedByTheHostsJsonOptions()
    {
        // The point of writing through the library's own serializer: a service changing its naming
        // policy changes its own payloads, not the errors other services receive.
        var context = Context("/api/content");

        await context.WriteErrorProblemDetailsAsync(
            [Err.Validation("A_CODE")],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("\"kind\":", BodyOf(context), StringComparison.Ordinal);
        Assert.Contains("\"errors\":", BodyOf(context), StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteErrorProblemDetailsAsync_WithNullArguments_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            "httpContext",
            () => ((HttpContext)null!).WriteErrorProblemDetailsAsync([Err.NotFound()], cancellationToken: TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ArgumentNullException>(
            "errors",
            () => Context("/x").WriteErrorProblemDetailsAsync((IEnumerable<ErrorDefinition>)null!, cancellationToken: TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ArgumentNullException>(
            "problem",
            () => Context("/x").WriteErrorProblemDetailsAsync((ErrorProblemDetails)null!, TestContext.Current.CancellationToken));
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
}
