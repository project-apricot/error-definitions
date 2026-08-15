using System.Text.Json.Serialization;

namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// One classified error: what kind of thing went wrong, which specific error it was, and enough
/// information for a client to say so in its own words.
/// </summary>
/// <remarks>
/// Construct these through <see cref="Err"/>, so the kind and code are checked. The type stays tolerant
/// because it is also what a caller deserializes from another service: the text members turn an explicit
/// JSON null into an empty string rather than leaving a null behind a non-nullable member.
/// </remarks>
public sealed record ErrorDefinition
{
    /// <summary>
    /// Gets the classification of the error — one of <see cref="ErrorKinds"/>, or a service's own
    /// kind. A client switches on this to decide behaviour.
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind
    {
        get => field ?? string.Empty;
        init;
    }

    /// <summary>
    /// Gets the specific error within its kind, in the upper snake case. A client uses this as the key
    /// for the text it shows.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code
    {
        get => field ?? string.Empty;
        init;
    }

    /// <summary>
    /// Gets a description of the error for whoever reads the response directly. Never null; empty
    /// when there is nothing to add beyond the code.
    /// </summary>
    /// <remarks>
    /// Not intended for display to an end user — it is neither localised nor guaranteed to be
    /// stable. Render from <see cref="Code"/> instead.
    /// </remarks>
    [JsonPropertyName("message")]
    public string Message
    {
        get => field ?? string.Empty;
        init;
    }

    /// <summary>
    /// Gets the parameters of the error: the values a client substitutes into the text it renders
    /// for <see cref="Code"/>, such as the offending field or a limit that was exceeded.
    /// </summary>
    /// <remarks>
    /// Keys are written exactly as given, since a client matches them against its own placeholders. Values
    /// keep their JSON type on the way out but arrive back as <see cref="System.Text.Json.JsonElement"/> —
    /// deliberately asymmetric, since a reader cannot know what a value was meant to be.
    /// </remarks>
    [JsonPropertyName("payload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object?>? Payload { get; init; }
}
