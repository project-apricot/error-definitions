namespace ApricotFramework.ErrorDefinitions.Examples.Web;

/// <summary>
/// An author, as this service returns one.
/// </summary>
/// <param name="Id">The author's id.</param>
/// <param name="Name">The author's name.</param>
public sealed record Author(int Id, string Name);

/// <summary>
/// The body of a create-author request.
/// </summary>
/// <param name="Name">The author's name.</param>
/// <param name="Email">The author's email.</param>
/// <param name="Locale">The locale to publish in.</param>
/// <remarks>
/// Every member is nullable so that model binding always succeeds and the validation below is the thing
/// that rejects a bad request. A binding failure — a string where a number belongs, an unparseable body —
/// is answered by the framework in its own shape, which this library deliberately does not rewrite.
/// </remarks>
public sealed record CreateAuthor(string? Name, string? Email, string? Locale);
