using ApricotFramework.ErrorDefinitions.AspNetCore.Impl;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Tests;

/// <summary>
/// Covers the mappers registered by default, and above all that they decline what is not theirs.
/// </summary>
public class BuiltInMapperTests
{
    [Fact]
    public void ErrorDefinitionExceptionMapper_AClassifiedException_ReturnsItsErrors()
    {
        var errors = new ErrorDefinitionExceptionMapper()
            .Map(new DefaultHttpContext(), Err.NotFound("CONTENT_NOT_FOUND").AsException());

        Assert.NotNull(errors);
        Assert.Equal("CONTENT_NOT_FOUND", Assert.Single(errors).Code);
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(FormatException))]
    [InlineData(typeof(OperationCanceledException))]
    public void ErrorDefinitionExceptionMapper_AnythingElse_ReturnsNull(Type exceptionType)
    {
        // The invariant that keeps every consumer's mapper reachable. The predecessor's gRPC client
        // mapper answered everything and was registered first, which left every mapper after it dead.
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.Null(new ErrorDefinitionExceptionMapper().Map(new DefaultHttpContext(), exception));
    }

    [Fact]
    public async Task CancellationExceptionMapper_TheCallerWentAway_ReportsCancelled()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();

        var context = new DefaultHttpContext { RequestAborted = aborted.Token };

        var errors = new CancellationExceptionMapper().Map(context, new OperationCanceledException());

        Assert.NotNull(errors);
        Assert.Equal(ErrorKinds.Cancelled, Assert.Single(errors).Kind);
    }

    [Fact]
    public void CancellationExceptionMapper_SomethingElseRanOutOfTime_ReportsATimeout()
    {
        // From the caller's side this service is the one that failed, so it is a 504 rather than a 499.
        var errors = new CancellationExceptionMapper().Map(new DefaultHttpContext(), new TaskCanceledException());

        Assert.NotNull(errors);
        Assert.Equal(ErrorKinds.Timeout, Assert.Single(errors).Kind);
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ArgumentException))]
    public void CancellationExceptionMapper_AnythingElse_ReturnsNull(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.Null(new CancellationExceptionMapper().Map(new DefaultHttpContext(), exception));
    }

    [Fact]
    public void TypedExceptionErrorMapper_ATypeThatIsNotAnException_Throws()
    {
        Assert.Throws<ArgumentException>(
            "exceptionType",
            () => new TypedExceptionErrorMapper(typeof(string), ErrorKinds.Validation));
    }

    [Fact]
    public void TypedExceptionErrorMapper_ReturnsTheSameErrorEachTime()
    {
        // Built once, since it never varies, and a malformed code therefore fails at registration.
        var mapper = new TypedExceptionErrorMapper(typeof(FormatException), ErrorKinds.Validation, "BAD_FORMAT");
        var context = new DefaultHttpContext();

        Assert.Same(
            mapper.Map(context, new FormatException()),
            mapper.Map(context, new FormatException()));
    }

    [Fact]
    public void AllBuiltInMappers_DeclineAnExceptionTheyDoNotOwn()
    {
        // Asserted over the whole set, so a mapper added later inherits the rule.
        IExceptionErrorMapper[] mappers = [new ErrorDefinitionExceptionMapper(), new CancellationExceptionMapper()];

        Assert.All(
            mappers,
            mapper => Assert.Null(mapper.Map(new DefaultHttpContext(), new NotSupportedException("mine"))));
    }
}
