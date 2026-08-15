using ApricotFramework.ErrorDefinitions.AspNetCore.Impl;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;

/// <summary>
/// Registers error reporting in a service collection.
/// </summary>
/// <remarks>
/// Nothing here reads configuration, and that is on purpose. What a service answers when it fails is
/// part of its contract; settings that could change it would let an environment alter the contract
/// without a code review, and would let the same error mean different things in staging and production.
/// </remarks>
public static class ErrorDefinitionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds error reporting.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// The host must still add the exception-handling middleware — <c>app.UseExceptionHandler()</c> —
    /// since only the host knows where in its pipeline that belongs.
    /// </remarks>
    public static IServiceCollection AddErrorDefinitions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddErrorDefinitionsCore(services);
    }

    /// <summary>
    /// Adds error reporting, choosing whether unrecognised exceptions are reported here.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the settings.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.
    /// </exception>
    public static IServiceCollection AddErrorDefinitions(
        this IServiceCollection services,
        Action<ErrorDefinitionsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<ErrorDefinitionsOptions>().Configure(configure);

        return AddErrorDefinitionsCore(services);
    }

    /// <summary>
    /// Adds a mapper that teaches the pipeline about a failure of its own.
    /// </summary>
    /// <typeparam name="TMapper">The mapper to add.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Additive, and deduplicated by implementation type, so a library may register its mapper without
    /// caring whether another library already did. Mappers are consulted in registration order.
    /// </remarks>
    public static IServiceCollection AddExceptionErrorMapper<TMapper>(this IServiceCollection services)
        where TMapper : class, IExceptionErrorMapper
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IExceptionErrorMapper, TMapper>());

        return services;
    }

    /// <summary>
    /// Adds a mapper instance that teaches the pipeline about a failure of its own.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="mapper">The mapper to add.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="mapper"/> is null.
    /// </exception>
    /// <remarks>
    /// Added without deduplication, since two instances of the same type are a reasonable thing to
    /// want — one per exception type they each recognise.
    /// </remarks>
    public static IServiceCollection AddExceptionErrorMapper(this IServiceCollection services, IExceptionErrorMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(mapper);

        services.AddSingleton(mapper);

        return services;
    }

    /// <summary>
    /// Reports one exception type as one kind and code.
    /// </summary>
    /// <typeparam name="TException">The exception type to recognise, including its subclasses.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="kind">The kind to report it as.</param>
    /// <param name="code">The code to report, defaulting to the kind's default code.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="kind"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="kind"/> is not lower snake case, or <paramref name="code"/> is not
    /// upper snake case.
    /// </exception>
    /// <remarks>
    /// The whole of what most mappers do, in one line:
    /// <c>services.MapExceptionToError&lt;NotAuthenticatedException&gt;(ErrorKinds.NotAuthenticated);</c>
    /// Write a full <see cref="IExceptionErrorMapper"/> when the failure carries something worth putting
    /// in the error's payload.
    /// </remarks>
    public static IServiceCollection MapExceptionToError<TException>(
        this IServiceCollection services,
        string kind,
        string? code = null)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(kind);

        return services.AddExceptionErrorMapper(new TypedExceptionErrorMapper(typeof(TException), kind, code));
    }

    /// <summary>
    /// Registers the built-in mappers and the handler.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    private static IServiceCollection AddErrorDefinitionsCore(IServiceCollection services)
    {
        // An unmapped failure is reported without detail, so the log is its only record.
        services.AddLogging();

        // TryAddEnumerable throughout, so calling this twice leaves one of each.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IExceptionErrorMapper, ErrorDefinitionExceptionMapper>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IExceptionErrorMapper, CancellationExceptionMapper>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IExceptionHandler, ErrorDefinitionsExceptionHandler>());

        // Required: the exception-handling middleware refuses to be constructed without an error path, an
        // inline handler or an IProblemDetailsService, and IExceptionHandler registrations do not count.
        services.AddProblemDetails();

        return services;
    }
}
