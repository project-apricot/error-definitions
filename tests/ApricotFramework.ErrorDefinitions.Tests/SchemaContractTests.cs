using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApricotFramework.ErrorDefinitions.Tests;

/// <summary>
/// Keeps the published contract in <c>schemas/</c> in step with the library.
/// </summary>
/// <remarks>
/// Those files are what a non-.NET client generates its types and message catalogue from, so they are
/// the contract as much as this assembly is. Nothing checks a document nobody reads: without these
/// tests the two drift, and a client copies a value that stopped being true — which is exactly how a
/// misspelled kind reached three separate hand-written clients.
/// </remarks>
public class SchemaContractTests
{
    [Fact]
    public void ErrorKinds_ListsExactlyTheKindsTheLibraryDefines()
    {
        var published = Kinds().Select(kind => kind.GetProperty("kind").GetString()!).ToList();

        Assert.Equal(ErrorKinds.All, published);
    }

    [Fact]
    public void ErrorKinds_PublishesTheSameDefaultCodeForEachKind()
    {
        foreach (var kind in Kinds())
        {
            var name = kind.GetProperty("kind").GetString()!;

            Assert.Equal(ErrorCodes.ForKind(name), kind.GetProperty("defaultCode").GetString());
        }
    }

    [Fact]
    public void ErrorKinds_PublishesTheSameStatusesForEachKind()
    {
        foreach (var kind in Kinds())
        {
            var name = kind.GetProperty("kind").GetString()!;

            Assert.Equal(ErrorKindStatus.ToHttpStatusCode(name), kind.GetProperty("httpStatus").GetInt32());
            Assert.Equal(ErrorKindStatus.ToGrpcStatusCode(name), kind.GetProperty("grpcStatus").GetInt32());
        }
    }

    [Fact]
    public void ErrorKinds_PublishesTheSameTitleForEachKind()
    {
        foreach (var kind in Kinds())
        {
            var name = kind.GetProperty("kind").GetString()!;

            Assert.Equal(ErrorTitles.ForKind(name), kind.GetProperty("title").GetString());
        }
    }

    [Fact]
    public void ErrorKinds_PublishesTheCanonicalNameEachKindMirrors()
    {
        var expected = CanonicalCodes.All.ToDictionary(code => code.Kind, code => code.CanonicalName, StringComparer.Ordinal);

        foreach (var kind in Kinds())
        {
            var name = kind.GetProperty("kind").GetString()!;

            Assert.Equal(expected[name], kind.GetProperty("canonicalName").GetString());
        }
    }

    [Fact]
    public void ErrorKinds_PublishesTheSameReverseMapping()
    {
        var published = Document("error-kinds.json").RootElement.GetProperty("canonicalKindForHttpStatus");

        foreach (var member in published.EnumerateObject())
        {
            var status = int.Parse(member.Name, System.Globalization.CultureInfo.InvariantCulture);

            Assert.Equal(ErrorKindStatus.FromHttpStatusCode(status), member.Value.GetString());
        }

        Assert.Equal(ContractVectors.CanonicalKindForStatus.Count, published.EnumerateObject().Count());
    }

    [Fact]
    public void ErrorKinds_PublishesTheSameFallbackRules()
    {
        var fallback = Document("error-kinds.json").RootElement.GetProperty("httpStatusFallback");

        Assert.Equal(ErrorKindStatus.FromHttpStatusCode(418), fallback.GetProperty("clientError").GetString());
        Assert.Equal(ErrorKindStatus.FromHttpStatusCode(599), fallback.GetProperty("serverError").GetString());
        Assert.Equal(ErrorKindStatus.FromHttpStatusCode(200), fallback.GetProperty("otherwise").GetString());
    }

    [Fact]
    public void ErrorKinds_PublishesThePatternsTheLibraryEnforces()
    {
        var rules = Document("error-kinds.json").RootElement.GetProperty("rules");

        // Checked by behaviour rather than by comparing strings, so the published pattern has to
        // describe what the code actually accepts.
        AssertPatternMatchesBehaviour(rules.GetProperty("kindPattern").GetString()!, ErrorNaming.IsValidKind);
        AssertPatternMatchesBehaviour(rules.GetProperty("codePattern").GetString()!, ErrorNaming.IsValidCode);
    }

    [Fact]
    public void Schema_ListsEveryKindAsAnExample()
    {
        var examples = Document("error-problem-details.schema.json")
            .RootElement
            .GetProperty("$defs")
            .GetProperty("errorDefinition")
            .GetProperty("properties")
            .GetProperty("kind")
            .GetProperty("examples")
            .EnumerateArray()
            .Select(example => example.GetString());

        Assert.Equal(ErrorKinds.All, examples);
    }

    [Fact]
    public void Schema_NamesEveryMemberOfTheProblemDocument()
    {
        AssertSchemaMatchesMembers<ErrorProblemDetails>(
            Document("error-problem-details.schema.json").RootElement.GetProperty("properties"));
    }

    [Fact]
    public void Schema_NamesEveryMemberOfAnError()
    {
        AssertSchemaMatchesMembers<ErrorDefinition>(
            Document("error-problem-details.schema.json")
                .RootElement
                .GetProperty("$defs")
                .GetProperty("errorDefinition")
                .GetProperty("properties"));
    }

    [Fact]
    public void Schema_ExamplesAreDocumentsTheLibraryCanRead()
    {
        // The examples are what someone implementing a client copies, so they have to be real.
        foreach (var example in Document("error-problem-details.schema.json").RootElement.GetProperty("examples").EnumerateArray())
        {
            Assert.True(
                Serialization.ErrorDefinitionsJson.TryParse(example.GetRawText(), out _),
                $"the example is not a readable problem document: {example.GetRawText()}");
        }
    }

    [Fact]
    public void Meta_ListsTheFilesThatArePresent()
    {
        var listed = Document("meta.json")
            .RootElement
            .GetProperty("files")
            .EnumerateArray()
            .Select(file => file.GetString())
            .Order(StringComparer.Ordinal);

        // Every schema file is embedded, so what is present is what the assembly carries.
        var present = typeof(SchemaContractTests).Assembly
            .GetManifestResourceNames()
            .Where(name => name.Contains(".Schemas.", StringComparison.Ordinal))
            .Select(name => name[(name.IndexOf(".Schemas.", StringComparison.Ordinal) + ".Schemas.".Length)..])
            .Where(name => !name.Equals("meta.json", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

        Assert.Equal(listed, present);
    }

    /// <summary>
    /// Asserts that a published pattern describes what a check actually accepts.
    /// </summary>
    /// <param name="pattern">The published pattern.</param>
    /// <param name="isValid">The check the library applies.</param>
    private static void AssertPatternMatchesBehaviour(string pattern, Func<string?, bool> isValid)
    {
        string[] candidates =
        [
            "not_found",
            "NOT_FOUND",
            "Not_Found",
            "not found",
            "not-found",
            "_not_found",
            "1_not_found",
            "a",
            "A",
            "a1_b2",
            "A1_B2",
            string.Empty,
        ];

        foreach (var candidate in candidates)
        {
            var matches = System.Text.RegularExpressions.Regex.IsMatch(
                candidate,
                pattern,
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromSeconds(1));

            Assert.Equal(matches, isValid(candidate));
        }
    }

    /// <summary>
    /// Asserts that the schema names exactly the JSON members a type has.
    /// </summary>
    /// <typeparam name="T">The type to compare against.</typeparam>
    /// <param name="properties">The schema's properties object.</param>
    private static void AssertSchemaMatchesMembers<T>(JsonElement properties)
    {
        var expected = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name)
            .Order(StringComparer.Ordinal);

        var published = properties.EnumerateObject().Select(member => member.Name).Order(StringComparer.Ordinal);

        Assert.Equal(expected, published);
    }

    /// <summary>
    /// Reads a published schema file, which is embedded so the test does not depend on a path.
    /// </summary>
    /// <param name="fileName">The file to read.</param>
    /// <returns>The parsed document.</returns>
    private static JsonDocument Document(string fileName)
    {
        var assembly = typeof(SchemaContractTests).Assembly;
        var name = assembly.GetManifestResourceNames().Single(candidate => candidate.EndsWith($".Schemas.{fileName}", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(name)!;

        return JsonDocument.Parse(stream);
    }

    /// <summary>
    /// Reads the published kinds.
    /// </summary>
    /// <returns>Each published kind.</returns>
    private static List<JsonElement> Kinds()
    {
        return [.. Document("error-kinds.json").RootElement.GetProperty("kinds").EnumerateArray()];
    }
}
