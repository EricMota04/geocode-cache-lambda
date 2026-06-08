using System.Text.RegularExpressions;

namespace GeocodeCache.Domain.Common;

/// <summary>
/// Produces the canonical cache key for an address so that trivially different spellings
/// (extra spaces, casing) resolve to the same cached entry.
/// </summary>
public static partial class AddressKey
{
    /// <summary>
    /// Normalizes an address: trims, collapses internal whitespace runs to a single space,
    /// and lower-cases using invariant culture.
    /// </summary>
    /// <param name="address">The raw address supplied by the caller.</param>
    /// <returns>The normalized cache key.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="address"/> is null or blank.</exception>
    public static string Normalize(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Address must not be null or blank.", nameof(address));
        }

        var collapsed = WhitespacePattern().Replace(address.Trim(), " ");
        return collapsed.ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
