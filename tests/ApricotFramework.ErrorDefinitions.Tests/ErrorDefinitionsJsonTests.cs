using System.Text;
using System.Text.Json;
using ApricotFramework.ErrorDefinitions.Serialization;

namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// Pins the serialized form against hand-written vectors, and covers what reading a body that is not
/// one does.
/// </summary>
/// <remarks>
/// The expected JSON comes from <see cref="ContractVectors"/>, transcribed from RFC 9457's member
/// names rather than produced by this library. A failure here is a change other services can see.
/// </remarks>
public class ErrorDefinitionsJsonTests
{
    [Fact]
    public void Serialize_ValidationWithPayload_MatchesTheVector()
    {
        var problem = ErrorProblemDetails.From(
            [
                Err.Validation(
                    "CONTENT_INVALID_LOCALE",
                    "The locale is invalid",
                    new Dictionary<string, object?> { ["locale"] = "xx" }),
            ],
            "/api/content");

        Assert.Equal(ContractVectors.ValidationWithPayload, ErrorDefinitionsJson.Serialize(problem));
    }

    [Fact]
    public void Serialize_ErrorWithNothingOptional_OmitsDetailInstanceAndPayload()
    {
        var problem = ErrorProblemDetails.From([Err.NotFound()]);

        Assert.Equal(ContractVectors.NotFoundBare, ErrorDefinitionsJson.Serialize(problem));
    }

    [Fact]
    public void Serialize_SeveralErrors_KeepsThemAllInOrder()
    {
        var problem = ErrorProblemDetails.From(
            [Err.Validation("PAYER_INVALID_EMAIL"), Err.Validation("PAYER_INVALID_AGE")]);

        Assert.Equal(ContractVectors.MultipleErrors, ErrorDefinitionsJson.Serialize(problem));
    }

    [Fact]
    public void Serialize_UnhandledFailure_MatchesTheVector()
    {
        var problem = ErrorProblemDetails.From([Err.Internal()], "/api/things");

        Assert.Equal(ContractVectors.UnhandledInternal, ErrorDefinitionsJson.Serialize(problem));
    }

    [Fact]
    public void Serialize_MemberNamesDoNotDependOnAnyNamingPolicy()
    {
        // The predecessor serialized through the host's options, so a naming policy could change the
        // contract. This asserts the names rather than trusting the attributes.
        using var document = JsonDocument.Parse(ErrorDefinitionsJson.Serialize(ErrorProblemDetails.From([Err.NotFound()])));

        Assert.Equal(
            ["type", "title", "status", "errors"],
            document.RootElement.EnumerateObject().Select(member => member.Name));

        var error = document.RootElement.GetProperty("errors")[0];

        Assert.Equal(["kind", "code", "message"], error.EnumerateObject().Select(member => member.Name));
    }

    [Fact]
    public void Serialize_PayloadKeys_AreWrittenExactlyAsGiven()
    {
        // A client matches these against placeholders in its own catalogue, so no naming policy may
        // touch them.
        var problem = ErrorProblemDetails.From(
            [Err.Validation("X", payload: new Dictionary<string, object?> { ["someKey"] = 1, ["other_key"] = 2 })]);

        Assert.Contains("\"someKey\":1", ErrorDefinitionsJson.Serialize(problem), StringComparison.Ordinal);
        Assert.Contains("\"other_key\":2", ErrorDefinitionsJson.Serialize(problem), StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_TheVector_ReadsEveryMemberBack()
    {
        Assert.True(ErrorDefinitionsJson.TryParse(ContractVectors.ValidationWithPayload, out var problem));

        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal("The locale is invalid", problem.Detail);
        Assert.Equal("/api/content", problem.Instance);

        var error = Assert.Single(problem.Errors);

        Assert.Equal(ErrorKinds.Validation, error.Kind);
        Assert.Equal("CONTENT_INVALID_LOCALE", error.Code);
        Assert.NotNull(error.Payload);
    }

    [Fact]
    public void TryParse_PayloadValues_ArriveAsJsonElements()
    {
        // Deliberately asymmetric with what the writer put in: the reader cannot know what a value was
        // meant to be. Asserted so the behaviour is documented rather than discovered.
        Assert.True(ErrorDefinitionsJson.TryParse(ContractVectors.ValidationWithPayload, out var problem));

        var value = Assert.Single(problem!.Errors).Payload!["locale"];

        Assert.IsType<JsonElement>(value);
        Assert.Equal("xx", ((JsonElement)value!).GetString());
    }

    [Fact]
    public void SerializeToUtf8Bytes_RoundTrips()
    {
        var problem = ErrorProblemDetails.From([Err.NotFound("CONTENT_AUTHOR_NOT_FOUND")]);

        Assert.True(ErrorDefinitionsJson.TryParse(ErrorDefinitionsJson.SerializeToUtf8Bytes(problem), out var read));

        Assert.Equal("CONTENT_AUTHOR_NOT_FOUND", Assert.Single(read!.Errors).Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("<html><body>502 Bad Gateway</body></html>")]
    [InlineData("""{"errors":[]}""")]
    [InlineData("""{"errors":null}""")]
    [InlineData("""{"errors":"nope"}""")]
    [InlineData("""{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"$.name":["The JSON value could not be converted to System.String."]}}""")]
    [InlineData("""{"message":"some other api"}""")]
    [InlineData("""{"type":"about:blank","title":"x","status":400,"errors":[{"kind":"validation","code":"X"}]""")]
    public void TryParse_SomethingThatIsNotAProblemDocument_IsANegativeResultNotAThrow(string? json)
    {
        // A caller cannot know what it is about to parse, so failing to parse has to be an answer. The
        // long case is real: ASP.NET's model-binding failure is problem+json with `errors` as an object,
        // so a peer using [ApiController] sends exactly that.
        Assert.False(ErrorDefinitionsJson.TryParse(json, out var problem));
        Assert.Null(problem);
    }

    [Fact]
    public void TryParse_NullMembers_ReadAsEmptyRatherThanNull()
    {
        // The members are declared non-null, so an explicit JSON null must not leave one behind them.
        Assert.True(ErrorDefinitionsJson.TryParse(
            """{"type":null,"title":null,"status":400,"errors":[{"kind":null,"code":null,"message":null}]}""",
            out var problem));

        Assert.Equal(ErrorProblemDetails.BlankType, problem!.Type);
        Assert.Equal(ErrorTitles.Default, problem.Title);

        var error = Assert.Single(problem.Errors);

        Assert.Equal(string.Empty, error.Kind);
        Assert.Equal(string.Empty, error.Code);
        Assert.Equal(string.Empty, error.Message);
    }

    [Fact]
    public void TryParse_UnknownMembers_AreIgnored()
    {
        // A peer on a newer version may add members. That must not stop this one reading the rest.
        Assert.True(ErrorDefinitionsJson.TryParse(
            """{"status":404,"errors":[{"kind":"not_found","code":"NOT_FOUND","target":"id"}],"traceId":"abc"}""",
            out var problem));

        Assert.Equal("NOT_FOUND", Assert.Single(problem!.Errors).Code);
    }

    [Fact]
    public void TryParse_CaseIsSignificant()
    {
        // Reading case-insensitively would let a peer send `Kind` and be understood, which quietly
        // widens the contract to something never agreed.
        Assert.False(ErrorDefinitionsJson.TryParse("""{"Status":404,"Errors":[{"Kind":"not_found"}]}""", out _));
    }

    [Fact]
    public void TryParse_NonAsciiText_SurvivesUnchanged()
    {
        var problem = ErrorProblemDetails.From([Err.Validation("X", "Սխալ լոկալ 🙂")]);

        Assert.True(ErrorDefinitionsJson.TryParse(ErrorDefinitionsJson.Serialize(problem), out var read));

        Assert.Equal("Սխալ լոկալ 🙂", Assert.Single(read!.Errors).Message);
    }

    [Fact]
    public void TryParse_ALargeDocument_StillReads()
    {
        // Nothing here bounds the input; that is the caller's job at the transport, where the size is
        // known before the body is buffered.
        var many = Enumerable.Range(0, 2000).Select(index => Err.Validation($"FIELD_{index}"));

        Assert.True(ErrorDefinitionsJson.TryParse(
            ErrorDefinitionsJson.Serialize(ErrorProblemDetails.From(many)),
            out var read));

        Assert.Equal(2000, read!.Errors.Count);
    }

    [Fact]
    public void TryParse_MutatedVector_Fails()
    {
        // The mutation check: corrupting one byte of a golden vector must break the golden test, or the
        // vector is not pinning anything.
        var corrupted = Encoding.UTF8.GetBytes(ContractVectors.ValidationWithPayload);
        corrupted[0] = (byte)'x';

        Assert.False(ErrorDefinitionsJson.TryParse(corrupted, out _));
    }

    [Fact]
    public void Serialize_MutatedExpectation_WouldFail()
    {
        // The other half of the mutation check: the comparison is exact, so a single character
        // difference is caught.
        var actual = ErrorDefinitionsJson.Serialize(ErrorProblemDetails.From([Err.NotFound()]));

        Assert.NotEqual(ContractVectors.NotFoundBare.Replace("not_found", "not_foundX", StringComparison.Ordinal), actual);
    }

    [Fact]
    public void Serialize_WithNull_Throws()
    {
        Assert.Throws<ArgumentNullException>("problem", () => ErrorDefinitionsJson.Serialize(null!));
    }
}
