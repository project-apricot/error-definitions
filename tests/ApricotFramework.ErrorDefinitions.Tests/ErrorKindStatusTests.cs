namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// Covers both status mappings in both directions, including what happens for a status or kind
/// nobody mapped.
/// </summary>
public class ErrorKindStatusTests
{
    [Fact]
    public void ToHttpStatusCode_EveryKind_MatchesTheFrozenContract()
    {
        foreach (var (kind, _, status) in ContractVectors.Kinds)
        {
            Assert.Equal(status, ErrorKindStatus.ToHttpStatusCode(kind));
        }
    }

    [Fact]
    public void ToHttpStatusCode_Cancelled_Is499()
    {
        // Not an IANA code, so it has no framework constant and is easy to lose in a refactor.
        Assert.Equal(499, ErrorKindStatus.ClientClosedRequest);
        Assert.Equal(ErrorKindStatus.ClientClosedRequest, ErrorKindStatus.ToHttpStatusCode(ErrorKinds.Cancelled));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NOT_FOUND")]
    [InlineData("Not_Found")]
    [InlineData("valdation")]
    [InlineData("my_service_kind")]
    public void ToHttpStatusCode_UnrecognisedKind_Is500(string? kind)
    {
        // Ordinal lookup: a kind in the wrong case is a different kind, not a typo quietly forgiven.
        Assert.Equal(500, ErrorKindStatus.ToHttpStatusCode(kind));
    }

    [Fact]
    public void FromHttpStatusCode_MappedStatuses_ReturnTheCanonicalKind()
    {
        foreach (var (status, kind) in ContractVectors.CanonicalKindForStatus)
        {
            Assert.Equal(kind, ErrorKindStatus.FromHttpStatusCode(status));
        }
    }

    [Theory]
    [InlineData(405)]
    [InlineData(415)]
    [InlineData(418)]
    [InlineData(422)]
    public void FromHttpStatusCode_UnmappedClientError_IsValidation(int status)
    {
        Assert.Equal(ErrorKinds.Validation, ErrorKindStatus.FromHttpStatusCode(status));
    }

    [Theory]
    [InlineData(502)]
    [InlineData(507)]
    [InlineData(599)]
    public void FromHttpStatusCode_UnmappedServerError_IsInternal(int status)
    {
        Assert.Equal(ErrorKinds.Internal, ErrorKindStatus.FromHttpStatusCode(status));
    }

    [Theory]
    [InlineData(200)]
    [InlineData(204)]
    [InlineData(302)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(600)]
    public void FromHttpStatusCode_NotAnError_IsUnknown(int status)
    {
        Assert.Equal(ErrorKinds.Unknown, ErrorKindStatus.FromHttpStatusCode(status));
    }

    [Fact]
    public void FromHttpStatusCode_RoundTripsEveryKindThatOwnsItsStatus()
    {
        // 409 and 500 are each claimed by more than one kind, so only one of them can survive the
        // round trip. Every other kind must.
        var shared = new[] { 409, 500 };

        foreach (var (kind, _, status) in ContractVectors.Kinds.Where(entry => !shared.Contains(entry.HttpStatus)))
        {
            Assert.Equal(kind, ErrorKindStatus.FromHttpStatusCode(status));
        }
    }

    [Fact]
    public void ToGrpcStatusCode_EveryKind_IsItsCanonicalCodeNumber()
    {
        foreach (var (number, _, kind) in CanonicalCodes.All)
        {
            Assert.Equal(number, ErrorKindStatus.ToGrpcStatusCode(kind));
        }
    }

    [Fact]
    public void FromGrpcStatusCode_EveryCanonicalNumber_ReturnsItsKind()
    {
        foreach (var (number, _, kind) in CanonicalCodes.All)
        {
            Assert.Equal(kind, ErrorKindStatus.FromGrpcStatusCode(number));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void FromGrpcStatusCode_NotAFailureCode_IsUnknown(int code)
    {
        Assert.Equal(ErrorKinds.Unknown, ErrorKindStatus.FromGrpcStatusCode(code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("my_service_kind")]
    public void ToGrpcStatusCode_UnrecognisedKind_IsUnknownAndNeverOk(string? kind)
    {
        var code = ErrorKindStatus.ToGrpcStatusCode(kind);

        Assert.Equal(2, code);
        Assert.NotEqual(ErrorKindStatus.GrpcOk, code);
    }

    [Fact]
    public void BothMappings_CoverExactlyTheSameKinds()
    {
        // The two tables live together so they cannot drift. This is the assertion that says so.
        Assert.All(
            ErrorKinds.All,
            kind =>
            {
                Assert.NotEqual(ErrorKindStatus.GrpcOk, ErrorKindStatus.ToGrpcStatusCode(kind));
                Assert.Equal(kind, ErrorKindStatus.FromGrpcStatusCode(ErrorKindStatus.ToGrpcStatusCode(kind)));
            });
    }
}
