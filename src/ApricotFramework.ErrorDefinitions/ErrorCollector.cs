namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// Gathers errors so that a caller learns about all of them at once, rather than one per round trip.
/// </summary>
/// <remarks>
/// The shape validation wants: check everything, then throw once.
/// <code>
/// var errors = new ErrorCollector()
///     .AddIf(string.IsNullOrWhiteSpace(input.Email), Err.Validation(PayerErrors.InvalidEmail))
///     .AddIf(input.Age &lt; 0, Err.Validation(PayerErrors.InvalidAge));
///
/// errors.ThrowIfAny();
/// </code>
/// Not thread safe: build one per operation.
/// </remarks>
public sealed class ErrorCollector
{
    /// <summary>
    /// The errors gathered so far, in the order they were added.
    /// </summary>
    private readonly List<ErrorDefinition> errors = [];

    /// <summary>
    /// Gets the number of errors gathered.
    /// </summary>
    public int Count => this.errors.Count;

    /// <summary>
    /// Gets a value indicating whether anything has been gathered.
    /// </summary>
    public bool HasErrors => this.errors.Count > 0;

    /// <summary>
    /// Adds an error.
    /// </summary>
    /// <param name="error">The error to add.</param>
    /// <returns>This collector, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public ErrorCollector Add(ErrorDefinition error)
    {
        ArgumentNullException.ThrowIfNull(error);

        this.errors.Add(error);

        return this;
    }

    /// <summary>
    /// Adds an error when a condition holds.
    /// </summary>
    /// <param name="condition">Whether the error applies.</param>
    /// <param name="error">The error to add.</param>
    /// <returns>This collector, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    /// <remarks>
    /// The error is built either way, so a malformed code fails on the first run rather than only on the
    /// branch that trips it.
    /// </remarks>
    public ErrorCollector AddIf(bool condition, ErrorDefinition error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return condition ? this.Add(error) : this;
    }

    /// <summary>
    /// Copies out the errors gathered.
    /// </summary>
    /// <returns>The errors, in the order they were added.</returns>
    public IReadOnlyList<ErrorDefinition> ToErrors()
    {
        return [.. this.errors];
    }

    /// <summary>
    /// Throws the errors gathered, if there are any.
    /// </summary>
    /// <param name="message">A message for the exception, defaulting to the first error's kind and code.</param>
    /// <exception cref="ErrorDefinitionException">Thrown when anything has been gathered.</exception>
    public void ThrowIfAny(string? message = null)
    {
        if (this.errors.Count > 0)
        {
            throw new ErrorDefinitionException(this.ToErrors(), message);
        }
    }
}
