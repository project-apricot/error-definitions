namespace ApricotFramework.ErrorDefinitions;

/// <summary>
/// The shape rules for a kind and a code, and the checks that enforce them.
/// </summary>
/// <remarks>
/// Both are lookup keys a client uses, so both must look like identifiers rather than prose. The code rule
/// is what Google's guidelines require of the equivalent field (<c>ErrorInfo.reason</c>). Public so a
/// service can assert its own catalogue in a test.
/// </remarks>
public static class ErrorNaming
{
    /// <summary>
    /// Reports whether a kind is well-formed: lower snake case, starting with a letter
    /// (<c>not_found</c>).
    /// </summary>
    /// <param name="kind">The kind to check.</param>
    /// <returns><see langword="true"/> when the kind is well-formed.</returns>
    public static bool IsValidKind(string? kind)
    {
        return IsWellFormed(kind, upperCase: false);
    }

    /// <summary>
    /// Reports whether a code is well formed: upper snake case, starting with a letter
    /// (<c>CONTENT_INVALID_LOCALE</c>).
    /// </summary>
    /// <param name="code">The code to check.</param>
    /// <returns><see langword="true"/> when the code is well formed.</returns>
    public static bool IsValidCode(string? code)
    {
        return IsWellFormed(code, upperCase: true);
    }

    /// <summary>
    /// Checks a value against the snake-case rule in the requested case.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="upperCase">Whether letters must be upper case.</param>
    /// <returns><see langword="true"/> when the value is well-formed.</returns>
    private static bool IsWellFormed(string? value, bool upperCase)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        // A leading digit or underscore would read as a fragment rather than a name.
        if (!IsLetter(value[0], upperCase))
        {
            return false;
        }

        foreach (var character in value)
        {
            var allowed = IsLetter(character, upperCase)
                || character is >= '0' and <= '9'
                || character == '_';

            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Reports whether a character is an ASCII letter in the requested case.
    /// </summary>
    /// <param name="character">The character to check.</param>
    /// <param name="upperCase">Whether the letter must be upper case.</param>
    /// <returns><see langword="true"/> when the character is a letter in that case.</returns>
    private static bool IsLetter(char character, bool upperCase)
    {
        // deliberately ASCII-only and culture-free: these are protocol tokens, not display text
        return upperCase
            ? character is >= 'A' and <= 'Z'
            : character is >= 'a' and <= 'z';
    }
}
