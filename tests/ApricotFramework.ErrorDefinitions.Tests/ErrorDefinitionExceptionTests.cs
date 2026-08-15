namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// Covers the exception's invariants: that it always carries an error, that it copies what it was
/// given, and that a message stays a message.
/// </summary>
public class ErrorDefinitionExceptionTests
{
    [Fact]
    public void Constructor_WithNothing_CarriesOneUnknownError()
    {
        // Never empty, so nothing downstream needs a branch for the empty case.
        var thrown = new ErrorDefinitionException();

        Assert.Equal(ErrorKinds.Unknown, Assert.Single(thrown.Errors).Kind);
    }

    [Fact]
    public void Constructor_WithAMessage_PutsTheTextInTheMessageNotTheCode()
    {
        // The predecessor bound a single string to the code, so an exception's text was published as
        // an error code and clients rendered the sentence as an identifier.
        var thrown = new ErrorDefinitionException("Could not reach the provider");

        var error = Assert.Single(thrown.Errors);

        Assert.Equal("Could not reach the provider", error.Message);
        Assert.Equal(ErrorCodes.Unknown, error.Code);
        Assert.Equal("Could not reach the provider", thrown.Message);
    }

    [Fact]
    public void Constructor_WithAnEmptySequence_CarriesOneUnknownError()
    {
        var thrown = new ErrorDefinitionException([]);

        Assert.Equal(ErrorKinds.Unknown, Assert.Single(thrown.Errors).Kind);
    }

    [Fact]
    public void Constructor_WithNullErrors_Throws()
    {
        Assert.Throws<ArgumentNullException>("errors", () => new ErrorDefinitionException((IEnumerable<ErrorDefinition>)null!));
    }

    [Fact]
    public void Constructor_MutatingTheListAfterwards_DoesNotChangeWhatIsReported()
    {
        var errors = new List<ErrorDefinition> { Err.NotFound() };

        var thrown = new ErrorDefinitionException(errors);
        errors.Add(Err.Validation());
        errors.Clear();

        Assert.Equal(ErrorKinds.NotFound, Assert.Single(thrown.Errors).Kind);
    }

    [Fact]
    public void Constructor_WithALazySequence_EnumeratesItOnce()
    {
        var enumerations = 0;

        IEnumerable<ErrorDefinition> Lazy()
        {
            enumerations++;
            yield return Err.NotFound();
        }

        _ = new ErrorDefinitionException(Lazy());

        Assert.Equal(1, enumerations);
    }

    [Fact]
    public void Message_WithNoMessageGiven_NamesTheKindAndCode()
    {
        // So a log line identifies the failure without a caller having to repeat it, and without
        // leaking anything from the payload.
        var thrown = new ErrorDefinitionException([Err.Validation("CONTENT_INVALID_LOCALE")]);

        Assert.Equal("validation: CONTENT_INVALID_LOCALE", thrown.Message);
    }

    [Fact]
    public void Constructor_WithAnInnerException_KeepsIt()
    {
        var cause = new InvalidOperationException("boom");

        var thrown = new ErrorDefinitionException([Err.Internal()], "wrapped", cause);

        Assert.Same(cause, thrown.InnerException);
        Assert.Equal("wrapped", thrown.Message);
    }

    [Fact]
    public void FirstError_IsTheOneThatDecidesHowTheFailureIsReported()
    {
        var thrown = new ErrorDefinitionException([Err.NotFound(), Err.Validation()]);

        Assert.Equal(ErrorKinds.NotFound, thrown.FirstError().Kind);
    }

    [Fact]
    public void HasKind_FindsAKindAnywhereInTheList()
    {
        // The one thing a caller catching this actually does, and previously had to hand-roll as LINQ
        // over a possibly-null list.
        var thrown = new ErrorDefinitionException([Err.Validation(), Err.NotFound()]);

        Assert.True(thrown.HasKind(ErrorKinds.NotFound));
        Assert.False(thrown.HasKind(ErrorKinds.Internal));
    }

    [Fact]
    public void HasCode_FindsACodeAnywhereInTheList()
    {
        var thrown = new ErrorDefinitionException([Err.Validation("A_ONE"), Err.Validation("A_TWO")]);

        Assert.True(thrown.HasCode("A_TWO"));
        Assert.False(thrown.HasCode("A_THREE"));
    }

    [Theory]
    [InlineData("NOT_FOUND")]
    [InlineData("not_found")]
    public void HasKind_ComparesOrdinally(string candidate)
    {
        var thrown = new ErrorDefinitionException([Err.NotFound()]);

        Assert.Equal(string.Equals(candidate, ErrorKinds.NotFound, StringComparison.Ordinal), thrown.HasKind(candidate));
    }

    [Fact]
    public void HasKind_WithNull_Throws()
    {
        var thrown = new ErrorDefinitionException();

        Assert.Throws<ArgumentNullException>("kind", () => thrown.HasKind(null!));
    }

    [Fact]
    public void Subclass_IsStillUnderstoodAsCarryingErrors()
    {
        // A library with its own failure derives from this and needs no mapper registered at all.
        var thrown = new CaptchaRejected();

        Assert.Equal(ErrorKinds.Validation, thrown.FirstError().Kind);
        Assert.Equal("CAPTCHA_REJECTED", thrown.FirstError().Code);
        Assert.IsAssignableFrom<ErrorDefinitionException>(thrown);
    }

    /// <summary>
    /// A library's own failure, standing in for one that derives from the exception rather than
    /// registering a mapper.
    /// </summary>
    private sealed class CaptchaRejected : ErrorDefinitionException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CaptchaRejected"/> class.
        /// </summary>
        public CaptchaRejected()
            : base([Err.Validation("CAPTCHA_REJECTED", payload: new Dictionary<string, object?> { ["reason"] = "timeout" })])
        {
        }
    }
}
