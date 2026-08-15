namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// Covers gathering several errors and reporting them in one go.
/// </summary>
public class ErrorCollectorTests
{
    [Fact]
    public void NewCollector_HasNothing()
    {
        var collector = new ErrorCollector();

        Assert.Equal(0, collector.Count);
        Assert.False(collector.HasErrors);
        Assert.Empty(collector.ToErrors());
    }

    [Fact]
    public void ThrowIfAny_WithNothingGathered_DoesNothing()
    {
        new ErrorCollector().ThrowIfAny();
    }

    [Fact]
    public void AddIf_KeepsOnlyTheConditionsThatHeld()
    {
        var collector = new ErrorCollector()
            .AddIf(true, Err.Validation("PAYER_INVALID_EMAIL"))
            .AddIf(false, Err.Validation("PAYER_INVALID_AGE"))
            .AddIf(true, Err.Validation("PAYER_INVALID_NAME"));

        Assert.Equal(2, collector.Count);
        Assert.Equal(["PAYER_INVALID_EMAIL", "PAYER_INVALID_NAME"], collector.ToErrors().Select(error => error.Code));
    }

    [Fact]
    public void ThrowIfAny_ReportsEverythingGatheredInOrder()
    {
        // The reason to gather at all: a caller fixing a form should not have to submit it once per
        // mistake.
        var collector = new ErrorCollector()
            .Add(Err.Validation("PAYER_INVALID_EMAIL"))
            .Add(Err.Validation("PAYER_INVALID_AGE"));

        var thrown = Assert.Throws<ErrorDefinitionException>(() => collector.ThrowIfAny());

        Assert.Equal(["PAYER_INVALID_EMAIL", "PAYER_INVALID_AGE"], thrown.Errors.Select(error => error.Code));
    }

    [Fact]
    public void ToErrors_IsASnapshot()
    {
        var collector = new ErrorCollector().Add(Err.Validation("ONE"));

        var snapshot = collector.ToErrors();
        collector.Add(Err.Validation("TWO"));

        Assert.Single(snapshot);
        Assert.Equal(2, collector.Count);
    }

    [Fact]
    public void Add_WithNull_Throws()
    {
        Assert.Throws<ArgumentNullException>("error", () => new ErrorCollector().Add(null!));
    }

    [Fact]
    public void AddIf_WithAMalformedCode_ThrowsEvenWhenTheConditionIsFalse()
    {
        // Building the error eagerly is deliberate: a bad code fails the first time the method runs,
        // not only on the input that happens to trip that branch.
        Assert.Throws<ArgumentException>("code", () => new ErrorCollector().AddIf(false, Err.Validation("not a code")));
    }

    [Fact]
    public void ThrowIfAny_WithAMessage_UsesIt()
    {
        var collector = new ErrorCollector().Add(Err.Validation("ONE"));

        var thrown = Assert.Throws<ErrorDefinitionException>(() => collector.ThrowIfAny("the payer is not valid"));

        Assert.Equal("the payer is not valid", thrown.Message);
    }
}
