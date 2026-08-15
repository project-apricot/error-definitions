using ApricotFramework.ErrorDefinitions.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApricotFramework.ErrorDefinitions.Examples.Web.Controllers;

/// <summary>
/// Reading and creating authors — the ordinary cases, where an error is part of the contract.
/// </summary>
[ApiController]
[Route("api/authors")]
public sealed class AuthorsController : ControllerBase
{
    /// <summary>
    /// The locales this service publishes in.
    /// </summary>
    private static readonly string[] AllowedLocales = ["en", "hy"];

    /// <summary>
    /// Creates the client used to call this service's own API.
    /// </summary>
    private readonly IHttpClientFactory clientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorsController"/> class.
    /// </summary>
    /// <param name="clientFactory">Creates the client used to call this service's own API.</param>
    public AuthorsController(IHttpClientFactory clientFactory)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);

        this.clientFactory = clientFactory;
    }

    /// <summary>
    /// Gets an author, or reports that there is none.
    /// </summary>
    /// <param name="id">The author's id. Only 1 exists.</param>
    /// <returns>The author.</returns>
    /// <remarks>
    /// One line instead of find, check, throw — and afterwards the compiler knows the value is not null.
    /// </remarks>
    [HttpGet("{id:int}")]
    public Author Get(int id)
    {
        return Ensure.Found(Find(id), DemoErrors.AuthorNotFound, "No author has that id.");
    }

    /// <summary>
    /// Creates an author, reporting everything wrong with the request at once.
    /// </summary>
    /// <param name="input">The author to create.</param>
    /// <returns>The author.</returns>
    /// <remarks>
    /// A caller fixing a form should not have to submit it once per mistake. The locale error carries a
    /// payload, which is what lets a client render its own message rather than showing ours.
    /// </remarks>
    [HttpPost]
    public Author Create(CreateAuthor input)
    {
        ArgumentNullException.ThrowIfNull(input);

        new ErrorCollector()
            .AddIf(string.IsNullOrWhiteSpace(input.Name), Err.Validation(DemoErrors.AuthorInvalidName))
            .AddIf(
                input.Email is null || !input.Email.Contains('@', StringComparison.Ordinal),
                Err.Validation(DemoErrors.AuthorInvalidEmail))
            .AddIf(
                input.Locale is not null && !AllowedLocales.Contains(input.Locale, StringComparer.Ordinal),
                Err.Validation(
                    DemoErrors.InvalidLocale,
                    "That locale is not published.",
                    new Dictionary<string, object?> { ["locale"] = input.Locale, ["allowed"] = AllowedLocales }))
            .ThrowIfAny();

        return new Author(2, input.Name!);
    }

    /// <summary>
    /// Gets an author by calling this service's own API, standing in for one service calling another.
    /// </summary>
    /// <param name="id">The author's id.</param>
    /// <param name="cancellationToken">The token to cancel the call.</param>
    /// <returns>The author the other service returned.</returns>
    /// <remarks>
    /// The point of the reader: the peer's kind and code arrive here and are reported to this caller
    /// unchanged, so a not-found deep in a call chain is still a not-found at the edge. Try
    /// <c>/api/authors/404/via-gateway</c>.
    /// </remarks>
    [HttpGet("{id:int}/via-gateway")]
    public async Task<Author?> GetViaGateway(int id, CancellationToken cancellationToken)
    {
        using var client = this.clientFactory.CreateClient("self");
        using var response = await client.GetAsync(new Uri($"/api/authors/{id}", UriKind.Relative), cancellationToken);

        await response.EnsureNoErrorsAsync(cancellationToken);

        return await response.Content.ReadFromJsonAsync<Author>(cancellationToken);
    }

    /// <summary>
    /// Looks up an author.
    /// </summary>
    /// <param name="id">The author's id.</param>
    /// <returns>The author, or null when there is none.</returns>
    private static Author? Find(int id)
    {
        return id == 1 ? new Author(1, "Ada") : null;
    }
}
