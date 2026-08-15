namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// Covers how the problem document derives its standard members from the errors it carries.
/// </summary>
public class ErrorProblemDetailsTests
{
    [Fact]
    public void From_DerivesStatusTitleAndDetailFromTheFirstError()
    {
        var problem = ErrorProblemDetails.From(
            [Err.Validation("CONTENT_INVALID_LOCALE", "The locale is invalid")],
            "/api/content");

        Assert.Equal(ErrorProblemDetails.BlankType, problem.Type);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Equal("The locale is invalid", problem.Detail);
        Assert.Equal("/api/content", problem.Instance);
    }

    [Fact]
    public void From_WithNoMessage_LeavesDetailUnset()
    {
        // Absent rather than empty, so a generic consumer does not render a blank line.
        var problem = ErrorProblemDetails.From([Err.NotFound()]);

        Assert.Null(problem.Detail);
        Assert.Null(problem.Instance);
    }

    [Fact]
    public void From_WithSeveralErrors_TakesTheStatusFromTheFirst()
    {
        // A response has one status, and the first error is the reason a service is reporting.
        var problem = ErrorProblemDetails.From([Err.NotFound(), Err.Validation()]);

        Assert.Equal(404, problem.Status);
        Assert.Equal("Not found", problem.Title);
        Assert.Equal(2, problem.Errors.Count);
    }

    [Fact]
    public void From_WithAnEmptySequence_ReportsOneUnknownError()
    {
        var problem = ErrorProblemDetails.From([]);

        Assert.Equal(500, problem.Status);
        Assert.Equal(ErrorKinds.Unknown, Assert.Single(problem.Errors).Kind);
    }

    [Fact]
    public void From_WithACustomKind_FallsBackTo500AndTheDefaultTitle()
    {
        var problem = ErrorProblemDetails.From([Err.From("my_service_kind")]);

        Assert.Equal(500, problem.Status);
        Assert.Equal(ErrorTitles.Default, problem.Title);
    }

    [Fact]
    public void From_EveryKind_ProducesTheContractStatus()
    {
        foreach (var (kind, _, status) in ContractVectors.Kinds)
        {
            Assert.Equal(status, ErrorProblemDetails.From([Err.From(kind)]).Status);
        }
    }

    [Fact]
    public void From_AnException_CarriesItsErrors()
    {
        var thrown = new ErrorDefinitionException([Err.AccessDenied("CONTENT_FORBIDDEN")]);

        var problem = ErrorProblemDetails.From(thrown, "/api/content/1");

        Assert.Equal(403, problem.Status);
        Assert.Equal("CONTENT_FORBIDDEN", Assert.Single(problem.Errors).Code);
        Assert.Equal("/api/content/1", problem.Instance);
    }

    [Fact]
    public void From_WithNullErrors_Throws()
    {
        Assert.Throws<ArgumentNullException>("errors", () => ErrorProblemDetails.From((IEnumerable<ErrorDefinition>)null!));
    }

    [Fact]
    public void With_OverridesTheStatusForAKindTheLibraryDoesNotMap()
    {
        // The documented way to take a status that is not the kind's default.
        var problem = ErrorProblemDetails.From([Err.From("my_service_kind")]) with { Status = 418 };

        Assert.Equal(418, problem.Status);
    }

    [Fact]
    public void Defaults_AreNeverNullEvenWhenConstructedDirectly()
    {
        var problem = new ErrorProblemDetails();

        Assert.Equal(ErrorProblemDetails.BlankType, problem.Type);
        Assert.Equal(ErrorTitles.Default, problem.Title);
        Assert.Empty(problem.Errors);
    }

    [Fact]
    public void MediaType_IsProblemJson()
    {
        // The content type is how a client tells a real error document from a proxy's HTML page, so
        // it is part of the contract rather than a detail of the handler.
        Assert.Equal("application/problem+json", ErrorProblemDetails.MediaType);
    }
}
