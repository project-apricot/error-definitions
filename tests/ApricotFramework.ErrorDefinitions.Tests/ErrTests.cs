namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// Covers the factories: what a call with no code produces, and which codes are refused.
/// </summary>
public class ErrTests
{
    [Fact]
    public void Shortcuts_WithNoArguments_CarryTheKindsDefaultCode()
    {
        // The idiom that matters most: a generic not-found is a complete answer, so this has to stay
        // a one-word call.
        foreach (var (kind, code, _) in ContractVectors.Kinds)
        {
            var error = Err.From(kind);

            Assert.Equal(kind, error.Kind);
            Assert.Equal(code, error.Code);
            Assert.Equal(string.Empty, error.Message);
            Assert.Null(error.Payload);
        }
    }

    [Fact]
    public void NotFound_WithNoArguments_IsTheDefaultNotFound()
    {
        var error = Err.NotFound();

        Assert.Equal(ErrorKinds.NotFound, error.Kind);
        Assert.Equal(ErrorCodes.NotFound, error.Code);
    }

    [Fact]
    public void Validation_WithACode_KeepsIt()
    {
        var error = Err.Validation("CONTENT_INVALID_LOCALE");

        Assert.Equal(ErrorKinds.Validation, error.Kind);
        Assert.Equal("CONTENT_INVALID_LOCALE", error.Code);
    }

    [Fact]
    public void Validation_WithACodeAndMessage_KeepsBoth()
    {
        var error = Err.Validation("CONTENT_INVALID_LOCALE", "The locale is invalid");

        Assert.Equal("CONTENT_INVALID_LOCALE", error.Code);
        Assert.Equal("The locale is invalid", error.Message);
    }

    [Fact]
    public void Internal_WithOnlyAMessage_LeavesTheDefaultCode()
    {
        // The supported way to report a one-off failure in prose. Passing the same text positionally
        // is what the code check below refuses.
        var error = Err.Internal(message: "The currency 'AMD' is not known");

        Assert.Equal(ErrorCodes.Internal, error.Code);
        Assert.Equal("The currency 'AMD' is not known", error.Message);
    }

    [Theory]
    [InlineData("The currency 'AMD' is not known")]
    [InlineData("Object reference not set to an instance of an object.")]
    [InlineData("not_found")]
    [InlineData("NotFound")]
    [InlineData("NOT FOUND")]
    [InlineData("NOT-FOUND")]
    [InlineData("_NOT_FOUND")]
    [InlineData("1_NOT_FOUND")]
    [InlineData("")]
    [InlineData("ПОЛЬЗОВАТЕЛЬ_НЕ_НАЙДЕН")]
    public void Shortcut_WithACodeThatIsNotUpperSnakeCase_Throws(string candidate)
    {
        // A client uses the code as a lookup key, so prose in this slot is shown to a user verbatim as
        // though it were an identifier. Refusing it is the whole point of the check.
        Assert.Throws<ArgumentException>("code", () => Err.Internal(candidate));
    }

    [Fact]
    public void Shortcut_WithProseAsTheCode_ExplainsTheAlternative()
    {
        var thrown = Assert.Throws<ArgumentException>("code", () => Err.Unknown("The provider is not recognized"));

        Assert.Contains("upper snake case", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("message", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Shortcut_WithAVeryLongCode_DoesNotBuryTheRestOfTheMessage()
    {
        var thrown = Assert.Throws<ArgumentException>("code", () => Err.Unknown(new string('x', 5000)));

        Assert.True(thrown.Message.Length < 500, $"message was {thrown.Message.Length} characters");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("A1")]
    [InlineData("CONTENT_INVALID_LOCALE")]
    [InlineData("PAY_INVALID_PAYMENT_METHOD")]
    [InlineData("X_")]
    public void Shortcut_WithAWellFormedCode_IsAccepted(string code)
    {
        Assert.Equal(code, Err.Validation(code).Code);
    }

    [Fact]
    public void From_WithACustomKind_IsAllowedAndDerivesItsCode()
    {
        // The sixteen kinds are not a closed set: a service may classify something they do not cover.
        var error = Err.From("my_service_kind");

        Assert.Equal("my_service_kind", error.Kind);
        Assert.Equal("MY_SERVICE_KIND", error.Code);
        Assert.True(ErrorNaming.IsValidCode(error.Code));
    }

    [Theory]
    [InlineData("NOT_FOUND")]
    [InlineData("Not_Found")]
    [InlineData("not found")]
    [InlineData("not-found")]
    [InlineData("_not_found")]
    [InlineData("")]
    public void From_WithAKindThatIsNotLowerSnakeCase_Throws(string candidate)
    {
        Assert.Throws<ArgumentException>("kind", () => Err.From(candidate));
    }

    [Fact]
    public void From_WithANullKind_Throws()
    {
        Assert.Throws<ArgumentNullException>("kind", () => Err.From(null!));
    }

    [Fact]
    public void From_WithAPayload_KeepsItAsGiven()
    {
        var payload = new Dictionary<string, object?> { ["locale"] = "xx", ["allowed"] = 3 };

        var error = Err.Validation("CONTENT_INVALID_LOCALE", payload: payload);

        Assert.NotNull(error.Payload);
        Assert.Equal("xx", error.Payload["locale"]);
        Assert.Equal(3, error.Payload["allowed"]);
    }
}
