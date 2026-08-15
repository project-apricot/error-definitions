using System.Text.Json;

namespace ApricotFramework.ErrorDefinitions.Serialization;

/// <summary>
/// Reads and writes the problem document, using settings this library owns.
/// </summary>
/// <remarks>
/// Everything goes through here so the contract does not depend on the host: a service that changes its
/// own JSON naming policy must not change the errors other services receive from it.
/// <para>
/// Reads never throw. A caller cannot know what it is about to parse, so failing to parse is a result.
/// </para>
/// </remarks>
public static class ErrorDefinitionsJson
{
    /// <summary>
    /// The settings used for every read and write.
    /// </summary>
    /// <remarks>
    /// Reflection-based because the source generator cannot write <see cref="ErrorDefinition.Payload"/>'s
    /// open <see cref="object"/> values; the library therefore claims no trimming or AOT support. Not
    /// exposed, since a caller who could reach them could mutate the contract.
    /// </remarks>
    private static readonly JsonSerializerOptions Options = new()
    {
        // Every member is named by attribute; set explicitly so that is a decision, not a default.
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = false,
        WriteIndented = false
    };

    /// <summary>
    /// Writes a problem document.
    /// </summary>
    /// <param name="problem">The document to write.</param>
    /// <returns>The JSON text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="problem"/> is null.</exception>
    public static string Serialize(ErrorProblemDetails problem)
    {
        ArgumentNullException.ThrowIfNull(problem);

        return JsonSerializer.Serialize(problem, Options);
    }

    /// <summary>
    /// Writes a problem document as UTF-8 bytes.
    /// </summary>
    /// <param name="problem">The document to write.</param>
    /// <returns>The JSON bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="problem"/> is null.</exception>
    public static byte[] SerializeToUtf8Bytes(ErrorProblemDetails problem)
    {
        ArgumentNullException.ThrowIfNull(problem);

        return JsonSerializer.SerializeToUtf8Bytes(problem, Options);
    }

    /// <summary>
    /// Tries to read a problem document.
    /// </summary>
    /// <param name="json">The text to read.</param>
    /// <param name="problem">The document read, or null when the text is not one.</param>
    /// <returns><see langword="true"/> when the text is a problem document carrying at least one error.</returns>
    public static bool TryParse(string? json, out ErrorProblemDetails? problem)
    {
        problem = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            return Accept(JsonSerializer.Deserialize<ErrorProblemDetails>(json, Options), out problem);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to read a problem document from UTF-8 bytes.
    /// </summary>
    /// <param name="utf8Json">The bytes to read.</param>
    /// <param name="problem">The document read, or null when the bytes are not one.</param>
    /// <returns><see langword="true"/> when the bytes are a problem document carrying at least one error.</returns>
    public static bool TryParse(ReadOnlySpan<byte> utf8Json, out ErrorProblemDetails? problem)
    {
        problem = null;

        if (utf8Json.IsEmpty)
        {
            return false;
        }

        try
        {
            return Accept(JsonSerializer.Deserialize<ErrorProblemDetails>(utf8Json, Options), out problem);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Decides whether what was read is usable as a problem document.
    /// </summary>
    /// <param name="candidate">The document read, which may be null or carry nothing.</param>
    /// <param name="problem">The accepted document, or null.</param>
    /// <returns><see langword="true"/> when the document carries at least one error.</returns>
    private static bool Accept(ErrorProblemDetails? candidate, out ErrorProblemDetails? problem)
    {
        // Valid JSON carrying no errors is not this contract: `{}`, `null` and an unrelated API all parse.
        if (candidate is null || candidate.Errors.Count == 0)
        {
            problem = null;

            return false;
        }

        problem = candidate;

        return true;
    }
}
