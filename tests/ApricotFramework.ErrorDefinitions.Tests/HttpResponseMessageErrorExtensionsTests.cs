using System.Net;
using System.Text;
using ApricotFramework.ErrorDefinitions.Http;

namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// Covers reading a peer's failure, whether or not the peer speaks this contract.
/// </summary>
public class HttpResponseMessageErrorExtensionsTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task ReadErrorProblemDetailsAsync_ASuccess_IsNull(HttpStatusCode status)
    {
        using var response = Respond(status, null, null);

        Assert.Null(await response.ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadErrorProblemDetailsAsync_APeerSpeakingTheContract_KeepsItsKindAndCode()
    {
        // The point of the whole exercise: a not-found deep in a call chain is still a not-found here.
        using var response = Respond(
            HttpStatusCode.NotFound,
            ErrorProblemDetails.MediaType,
            """{"type":"about:blank","title":"Not found","status":404,"errors":[{"kind":"not_found","code":"CONTENT_AUTHOR_NOT_FOUND","message":"No author with that id"}]}""");

        var problem = await response.ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);

        var error = Assert.Single(problem.Errors);

        Assert.Equal(ErrorKinds.NotFound, error.Kind);
        Assert.Equal("CONTENT_AUTHOR_NOT_FOUND", error.Code);
        Assert.Equal("No author with that id", error.Message);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/problem+json")]
    [InlineData("APPLICATION/PROBLEM+JSON")]
    [InlineData("application/vnd.acme.errors+json")]
    public async Task ReadErrorProblemDetailsAsync_AnyJsonMediaType_IsRead(string mediaType)
    {
        using var response = Respond(
            HttpStatusCode.BadRequest,
            mediaType,
            """{"status":400,"errors":[{"kind":"validation","code":"A_CODE"}]}""");

        var problem = await response.ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("A_CODE", Assert.Single(problem.Errors).Code);
    }

    [Fact]
    public async Task ReadErrorProblemDetailsAsync_TheDocumentDisagreesWithTheStatus_TrustsTheStatus()
    {
        // The transport status is what the caller's own stack already acted on, so a peer claiming
        // something else in the body cannot talk the caller out of it.
        using var response = Respond(
            HttpStatusCode.NotFound,
            ErrorProblemDetails.MediaType,
            """{"status":200,"errors":[{"kind":"not_found","code":"NOT_FOUND"}]}""");

        var problem = await response.ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ErrorKinds.NotAuthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorKinds.AccessDenied)]
    [InlineData(HttpStatusCode.NotFound, ErrorKinds.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests, ErrorKinds.ResourceExhausted)]
    [InlineData(HttpStatusCode.BadGateway, ErrorKinds.Internal)]
    [InlineData(HttpStatusCode.UnsupportedMediaType, ErrorKinds.Validation)]
    public async Task ReadErrorProblemDetailsAsync_AnEmptyBody_ClassifiesFromTheStatus(
        HttpStatusCode status,
        string expectedKind)
    {
        // The framework-issued responses this library deliberately does not touch on the server: a 401
        // challenge, an authorization forbid, a routing 404. A caller still gets a typed answer.
        using var response = Respond(status, null, null);

        var problem = await response.ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal((int)status, problem.Status);
        Assert.Equal(expectedKind, Assert.Single(problem.Errors).Kind);
    }

    [Theory]
    [InlineData("text/html", "<html><body>502 Bad Gateway</body></html>")]
    [InlineData("text/plain", "Errors from upstream: something")]
    [InlineData("application/json", "<html>not json at all</html>")]
    [InlineData("application/json", """{"errors":[]}""")]
    [InlineData("application/json", """{"message":"a different api"}""")]
    [InlineData("application/problem+json", """{"title":"One or more validation errors occurred.","status":400,"errors":{"$.name":["not a string"]}}""")]
    [InlineData("application/json", "{")]
    [InlineData(null, """{"status":404,"errors":[{"kind":"not_found","code":"X"}]}""")]
    public async Task ReadErrorProblemDetailsAsync_ABodyThatIsNotTheContract_FallsBackToTheStatus(
        string? mediaType,
        string body)
    {
        // A gateway's error page must not be mistaken for an error document, which is what the media
        // type check and the accept rule are both for.
        using var response = Respond(HttpStatusCode.BadGateway, mediaType, body);

        var problem = await response.ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(502, problem.Status);
        Assert.Equal(ErrorKinds.Internal, Assert.Single(problem.Errors).Kind);
        Assert.Equal(ErrorCodes.Internal, Assert.Single(problem.Errors).Code);
    }

    [Fact]
    public async Task ReadErrorProblemDetailsAsync_ABodyOverTheLimit_FallsBackWithoutBuffering()
    {
        // The body is a value the peer controls, so asking "did that fail" must not be a way to make
        // this process buffer whatever it likes.
        var oversized = BuildOversizedDocument();

        using var response = Respond(HttpStatusCode.BadRequest, ErrorProblemDetails.MediaType, oversized);

        var problem = await response.ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.Validation, Assert.Single(problem.Errors).Code);
    }

    [Fact]
    public async Task ReadErrorProblemDetailsAsync_ABodyOverTheLimitWithNoDeclaredLength_StillFallsBack()
    {
        // A peer that does not declare a length, or declares the wrong one, must not get past the limit.
        using var content = new StreamContent(
            new NonSeekableStream(Encoding.UTF8.GetBytes(BuildOversizedDocument())));
        content.Headers.ContentLength = null;
        content.Headers.Add("Content-Type", ErrorProblemDetails.MediaType);

        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = content };

        var problem = await response.ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.Validation, Assert.Single(problem.Errors).Code);
    }

    [Fact]
    public async Task ReadErrorProblemDetailsAsync_ABodyJustUnderTheLimit_IsStillRead()
    {
        var errors = Enumerable.Range(0, 500).Select(index => Err.Validation($"FIELD_{index}"));
        var document = Serialization.ErrorDefinitionsJson.Serialize(ErrorProblemDetails.From(errors));

        Assert.True(
            Encoding.UTF8.GetByteCount(document) < HttpResponseMessageErrorExtensions.MaxBodyBytes,
            "the fixture must sit under the limit for this test to mean anything");

        using var response = Respond(HttpStatusCode.BadRequest, ErrorProblemDetails.MediaType, document);

        var problem = await response.ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(500, problem.Errors.Count);
    }

    [Fact]
    public async Task EnsureNoErrorsAsync_ASuccess_DoesNothing()
    {
        using var response = Respond(HttpStatusCode.OK, null, null);

        await response.EnsureNoErrorsAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EnsureNoErrorsAsync_AFailure_ThrowsThePeersErrors()
    {
        using var response = Respond(
            HttpStatusCode.Conflict,
            ErrorProblemDetails.MediaType,
            """{"status":409,"errors":[{"kind":"already_exists","code":"CONTENT_SLUG_TAKEN","message":"that slug is in use"}]}""");

        var thrown = await Assert.ThrowsAsync<ErrorDefinitionException>(
            () => response.EnsureNoErrorsAsync(TestContext.Current.CancellationToken));

        Assert.True(thrown.HasKind(ErrorKinds.AlreadyExists));
        Assert.True(thrown.HasCode("CONTENT_SLUG_TAKEN"));
        Assert.Equal("that slug is in use", thrown.Message);
    }

    [Fact]
    public async Task EnsureNoErrorsAsync_AnEmptyBodiedFailure_StillThrowsSomethingClassified()
    {
        using var response = Respond(HttpStatusCode.Unauthorized, null, null);

        var thrown = await Assert.ThrowsAsync<ErrorDefinitionException>(
            () => response.EnsureNoErrorsAsync(TestContext.Current.CancellationToken));

        Assert.True(thrown.HasKind(ErrorKinds.NotAuthenticated));
    }

    [Fact]
    public async Task ReadErrorProblemDetailsAsync_ThroughAnHttpClient_Works()
    {
        // The extensions are on the response, so the tests above need no transport. This one goes
        // through a real HttpClient to prove the ordinary calling pattern.
        using var handler = new StubHandler(() => Respond(
            HttpStatusCode.NotFound,
            ErrorProblemDetails.MediaType,
            """{"status":404,"errors":[{"kind":"not_found","code":"THING_NOT_FOUND"}]}"""));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://peer.invalid") };

        using var response = await client.GetAsync(new Uri("/things/1", UriKind.Relative), TestContext.Current.CancellationToken);
        var problem = await response.ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("THING_NOT_FOUND", Assert.Single(problem.Errors).Code);
    }

    [Fact]
    public async Task ReadErrorProblemDetailsAsync_WithNull_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            "response",
            () => ((HttpResponseMessage)null!).ReadErrorProblemDetailsAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Builds a document larger than the read limit.
    /// </summary>
    /// <returns>The JSON text.</returns>
    private static string BuildOversizedDocument()
    {
        var padding = new string('x', HttpResponseMessageErrorExtensions.MaxBodyBytes);

        return $$"""{"status":400,"errors":[{"kind":"validation","code":"A_CODE","message":"{{padding}}"}]}""";
    }

    /// <summary>
    /// Builds a response with the given status, media type and body.
    /// </summary>
    /// <param name="status">The status code.</param>
    /// <param name="mediaType">The media type, or null for no content type.</param>
    /// <param name="body">The body, or null for no content.</param>
    /// <returns>The response.</returns>
    private static HttpResponseMessage Respond(HttpStatusCode status, string? mediaType, string? body)
    {
        var response = new HttpResponseMessage(status);

        if (body is not null)
        {
            var content = new StringContent(body, Encoding.UTF8);
            content.Headers.Remove("Content-Type");

            if (mediaType is not null)
            {
                content.Headers.Add("Content-Type", mediaType);
            }

            response.Content = content;
        }

        return response;
    }

    /// <summary>
    /// A transport that answers with a prepared response, standing in for a peer service.
    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        /// <summary>
        /// Builds the response to answer with.
        /// </summary>
        private readonly Func<HttpResponseMessage> respond;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubHandler"/> class.
        /// </summary>
        /// <param name="respond">Builds the response to answer with.</param>
        public StubHandler(Func<HttpResponseMessage> respond)
        {
            this.respond = respond;
        }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(this.respond());
        }
    }

    /// <summary>
    /// A stream that reports no length, standing in for a chunked response.
    /// </summary>
    private sealed class NonSeekableStream : Stream
    {
        /// <summary>
        /// The bytes to hand out.
        /// </summary>
        private readonly MemoryStream inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="NonSeekableStream"/> class.
        /// </summary>
        /// <param name="bytes">The bytes to hand out.</param>
        public NonSeekableStream(byte[] bytes)
        {
            this.inner = new MemoryStream(bytes);
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.inner.Read(buffer, offset, count);
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
