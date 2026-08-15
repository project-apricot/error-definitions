namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// Covers the precondition helpers: which kind each throws, and that a satisfied precondition is
/// invisible.
/// </summary>
public class EnsureTests
{
    [Fact]
    public void Found_WithAReference_ReturnsIt()
    {
        var value = new object();

        Assert.Same(value, Ensure.Found(value));
    }

    [Fact]
    public void Found_WithNull_ThrowsNotFound()
    {
        var thrown = Assert.Throws<ErrorDefinitionException>(() => Ensure.Found((string?)null));

        Assert.Equal(ErrorKinds.NotFound, thrown.FirstError().Kind);
        Assert.Equal(ErrorCodes.NotFound, thrown.FirstError().Code);
    }

    [Fact]
    public void Found_WithNullAndACode_ThrowsThatCode()
    {
        var thrown = Assert.Throws<ErrorDefinitionException>(
            () => Ensure.Found((string?)null, "CONTENT_AUTHOR_NOT_FOUND", "No author with that id"));

        Assert.Equal("CONTENT_AUTHOR_NOT_FOUND", thrown.FirstError().Code);
        Assert.Equal("No author with that id", thrown.FirstError().Message);
    }

    [Fact]
    public void Found_WithAValueType_UnwrapsIt()
    {
        int? value = 42;

        Assert.Equal(42, Ensure.Found(value));
    }

    [Fact]
    public void Found_WithAnEmptyValueType_ThrowsNotFound()
    {
        Assert.Throws<ErrorDefinitionException>(() => Ensure.Found((int?)null));
    }

    [Fact]
    public void Valid_WhenTheConditionHolds_DoesNothing()
    {
        Ensure.Valid(true);
    }

    [Fact]
    public void Valid_WhenTheConditionFails_ThrowsValidation()
    {
        var thrown = Assert.Throws<ErrorDefinitionException>(() => Ensure.Valid(false, "CONTENT_INVALID_LOCALE"));

        Assert.Equal(ErrorKinds.Validation, thrown.FirstError().Kind);
        Assert.Equal("CONTENT_INVALID_LOCALE", thrown.FirstError().Code);
    }

    [Fact]
    public void Allowed_WhenTheConditionFails_ThrowsAccessDenied()
    {
        var thrown = Assert.Throws<ErrorDefinitionException>(() => Ensure.Allowed(false));

        Assert.Equal(ErrorKinds.AccessDenied, thrown.FirstError().Kind);
    }

    [Fact]
    public void Authenticated_WhenTheConditionFails_ThrowsNotAuthenticated()
    {
        var thrown = Assert.Throws<ErrorDefinitionException>(() => Ensure.Authenticated(false));

        Assert.Equal(ErrorKinds.NotAuthenticated, thrown.FirstError().Kind);
    }

    [Fact]
    public void Precondition_WhenTheConditionFails_ThrowsPreconditionFailed()
    {
        var thrown = Assert.Throws<ErrorDefinitionException>(() => Ensure.Precondition(false));

        Assert.Equal(ErrorKinds.PreconditionFailed, thrown.FirstError().Kind);
    }

    [Fact]
    public void Valid_WithAMalformedCode_ThrowsAboutTheCode()
    {
        // A guard is often the first place a bad code is written, so it must not disguise the mistake
        // as the error it was guarding against.
        Assert.Throws<ArgumentException>("code", () => Ensure.Valid(false, "that is not allowed"));
    }

    [Fact]
    public void Found_WithAPayload_CarriesIt()
    {
        var thrown = Assert.Throws<ErrorDefinitionException>(
            () => Ensure.Found((string?)null, "CONTENT_AUTHOR_NOT_FOUND", payload: new Dictionary<string, object?> { ["id"] = 7 }));

        var payload = thrown.FirstError().Payload;

        Assert.NotNull(payload);
        Assert.Equal(7, payload["id"]);
    }
}
