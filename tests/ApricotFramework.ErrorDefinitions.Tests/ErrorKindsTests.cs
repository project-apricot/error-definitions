namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// Pins the kind vocabulary: its values, its size, and its claim to be the canonical code set.
/// </summary>
public class ErrorKindsTests
{
    [Fact]
    public void All_ListsEveryKindOnce()
    {
        Assert.Equal(ContractVectors.Kinds.Count, ErrorKinds.All.Count);
        Assert.Equal(ErrorKinds.All.Count, ErrorKinds.All.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void All_MatchesTheFrozenContract()
    {
        // Written out rather than compared pairwise so a failure names the difference.
        Assert.Equal(
            ContractVectors.Kinds.Select(entry => entry.Kind),
            ErrorKinds.All);
    }

    [Fact]
    public void ResourceExhausted_IsSingular()
    {
        // The value it replaced was plural while its name was singular, which every hand-written
        // client had to copy. Called out on its own so nobody "tidies" it back.
        Assert.Equal("resource_exhausted", ErrorKinds.ResourceExhausted);
    }

    [Fact]
    public void All_AreValidKinds()
    {
        Assert.All(ErrorKinds.All, kind => Assert.True(ErrorNaming.IsValidKind(kind), kind));
    }

    [Fact]
    public void All_CoverTheCanonicalCodeSet()
    {
        // The vocabulary claims to be google.rpc.Code under friendlier names. This is the only test
        // that checks the claim against something outside the library.
        Assert.Equal(
            CanonicalCodes.All.Select(code => code.Kind).Order(StringComparer.Ordinal),
            ErrorKinds.All.Order(StringComparer.Ordinal));
    }
}
