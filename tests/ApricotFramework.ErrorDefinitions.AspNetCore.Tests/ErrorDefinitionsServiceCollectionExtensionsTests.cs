using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;
using ApricotFramework.ErrorDefinitions.AspNetCore.Impl;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Tests;

/// <summary>
/// Covers what registration puts in the container, and that a library can extend it from either side
/// of the call.
/// </summary>
public class ErrorDefinitionsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddErrorDefinitions_RegistersTheHandlerAndTheBuiltInMappers()
    {
        using var provider = Build(services => services.AddErrorDefinitions());

        Assert.IsType<ErrorDefinitionsExceptionHandler>(Assert.Single(provider.GetServices<IExceptionHandler>()));

        var mappers = provider.GetServices<IExceptionErrorMapper>().ToList();

        Assert.Collection(
            mappers,
            mapper => Assert.IsType<ErrorDefinitionExceptionMapper>(mapper),
            mapper => Assert.IsType<CancellationExceptionMapper>(mapper));
    }

    [Fact]
    public void AddErrorDefinitions_WithNothingConfigured_ReportsUnrecognisedExceptions()
    {
        using var provider = Build(services => services.AddErrorDefinitions());

        Assert.True(provider.GetRequiredService<IOptions<ErrorDefinitionsOptions>>().Value.HandleUnmappedExceptions);
    }

    [Fact]
    public void AddErrorDefinitions_CalledTwice_RegistersOneOfEachService()
    {
        // A library may call this as well as the host, and two handlers would each try to answer.
        using var provider = Build(services => services
            .AddErrorDefinitions()
            .AddErrorDefinitions());

        Assert.Single(provider.GetServices<IExceptionHandler>());
        Assert.Equal(2, provider.GetServices<IExceptionErrorMapper>().Count());
    }

    [Fact]
    public void AddErrorDefinitions_WithAnAction_ConfiguresTheOneSettingThereIs()
    {
        using var provider = Build(services => services.AddErrorDefinitions(options => options.HandleUnmappedExceptions = false));

        Assert.False(provider.GetRequiredService<IOptions<ErrorDefinitionsOptions>>().Value.HandleUnmappedExceptions);
    }

    [Fact]
    public void AddErrorDefinitions_ReadsNothingFromConfiguration()
    {
        // Deliberate. What a service answers when it fails is part of its contract, so an environment must
        // not be able to change it — and there is no overload that would let it.
        var overloads = typeof(ErrorDefinitionsServiceCollectionExtensions)
            .GetMethods()
            .Where(method => method.Name == nameof(ErrorDefinitionsServiceCollectionExtensions.AddErrorDefinitions))
            .SelectMany(method => method.GetParameters());

        Assert.DoesNotContain(typeof(IConfiguration), overloads.Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void AddExceptionErrorMapper_BeforeAddErrorDefinitions_IsConsultedFirst()
    {
        // Registration order is what decides precedence, and a library registering ahead of the host has
        // to be able to take priority over the built-in mappers.
        using var provider = Build(services => services
            .AddExceptionErrorMapper<StubMapper>()
            .AddErrorDefinitions());

        Assert.IsType<StubMapper>(provider.GetServices<IExceptionErrorMapper>().First());
    }

    [Fact]
    public void AddExceptionErrorMapper_AfterAddErrorDefinitions_IsConsultedLast()
    {
        using var provider = Build(services => services
            .AddErrorDefinitions()
            .AddExceptionErrorMapper<StubMapper>());

        Assert.IsType<StubMapper>(provider.GetServices<IExceptionErrorMapper>().Last());
    }

    [Fact]
    public void AddExceptionErrorMapper_TheSameTypeTwice_RegistersItOnce()
    {
        // Two libraries may each register the same mapper without knowing about one another.
        using var provider = Build(services => services
            .AddErrorDefinitions()
            .AddExceptionErrorMapper<StubMapper>()
            .AddExceptionErrorMapper<StubMapper>());

        Assert.Single(provider.GetServices<IExceptionErrorMapper>().OfType<StubMapper>());
    }

    [Fact]
    public void AddExceptionErrorMapper_AnInstance_IsResolved()
    {
        var mapper = new StubMapper();

        using var provider = Build(services => services
            .AddErrorDefinitions()
            .AddExceptionErrorMapper(mapper));

        Assert.Same(mapper, provider.GetServices<IExceptionErrorMapper>().OfType<StubMapper>().Single());
    }

    [Fact]
    public void MapExceptionToError_RegistersAMapperForThatTypeOnly()
    {
        using var provider = Build(services => services
            .AddErrorDefinitions()
            .MapExceptionToError<InvalidTimeZoneException>(ErrorKinds.NotAuthenticated, "AUTH_NO_PRINCIPAL"));

        var mapper = Assert.Single(provider.GetServices<IExceptionErrorMapper>().OfType<TypedExceptionErrorMapper>());
        var context = new DefaultHttpContext();

        var mapped = mapper.Map(context, new InvalidTimeZoneException("nope"));

        Assert.NotNull(mapped);
        Assert.Equal(ErrorKinds.NotAuthenticated, Assert.Single(mapped).Kind);
        Assert.Equal("AUTH_NO_PRINCIPAL", Assert.Single(mapped).Code);
        Assert.Null(mapper.Map(context, new InvalidOperationException("other")));
    }

    [Fact]
    public void MapExceptionToError_MatchesSubclassesToo()
    {
        using var provider = Build(services => services
            .AddErrorDefinitions()
            .MapExceptionToError<ArgumentException>(ErrorKinds.Validation));

        var mapper = Assert.Single(provider.GetServices<IExceptionErrorMapper>().OfType<TypedExceptionErrorMapper>());

        Assert.NotNull(mapper.Map(new DefaultHttpContext(), new ArgumentNullException("param")));
    }

    [Fact]
    public void MapExceptionToError_WithAMalformedCode_FailsWhereTheMistakeIs()
    {
        // At registration, not on the first request that happens to fail.
        Assert.Throws<ArgumentException>(
            "code",
            () => new ServiceCollection().MapExceptionToError<InvalidOperationException>(ErrorKinds.Validation, "not a code"));
    }

    [Fact]
    public void MapExceptionToError_WithAMalformedKind_FailsWhereTheMistakeIs()
    {
        Assert.Throws<ArgumentException>(
            "kind",
            () => new ServiceCollection().MapExceptionToError<InvalidOperationException>("Validation"));
    }

    [Fact]
    public void AddErrorDefinitions_WithNullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>("services", () => ((IServiceCollection)null!).AddErrorDefinitions());
    }

    [Fact]
    public void AddErrorDefinitions_WithANullAction_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            "configure",
            () => new ServiceCollection().AddErrorDefinitions((Action<ErrorDefinitionsOptions>)null!));
    }

    [Fact]
    public void AddErrorDefinitions_TheHandler_ResolvesWithScopeValidationOn()
    {
        using var provider = Build(services =>
        {
            services.AddLogging();
            services.AddErrorDefinitions();
        });

        Assert.NotNull(provider.GetRequiredService<IExceptionHandler>());
    }

    /// <summary>
    /// Builds a provider with scope validation on, so a lifetime mistake fails the test.
    /// </summary>
    /// <param name="register">Registers the services under test.</param>
    /// <returns>The provider.</returns>
    private static ServiceProvider Build(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();

        register(services);

        return services.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>
    /// A mapper that recognises nothing, standing in for a consumer's own.
    /// </summary>
    private sealed class StubMapper : IExceptionErrorMapper
    {
        /// <inheritdoc />
        public IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception) => null;
    }
}
