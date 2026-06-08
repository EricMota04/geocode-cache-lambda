using FluentAssertions;
using GeocodeCache.Domain.Common;

namespace GeocodeCache.UnitTests;

public sealed class AddressKeyTests
{
    [Theory]
    [InlineData("70 Vanderbilt Ave, New York, NY 10017", "70 vanderbilt ave, new york, ny 10017")]
    [InlineData("  70 Vanderbilt Ave  ", "70 vanderbilt ave")]
    [InlineData("70   Vanderbilt\tAve", "70 vanderbilt ave")]
    [InlineData("MIXED Case ADDRESS", "mixed case address")]
    public void Normalize_collapses_whitespace_trims_and_lowercases(string input, string expected)
    {
        AddressKey.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Normalize_produces_same_key_for_spacing_and_case_variants()
    {
        var a = AddressKey.Normalize("70 Vanderbilt Ave, New York, NY 10017");
        var b = AddressKey.Normalize("  70  vanderbilt   ave,  NEW york, ny 10017 ");

        a.Should().Be(b);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Normalize_throws_for_blank_input(string? input)
    {
        var act = () => AddressKey.Normalize(input!);
        act.Should().Throw<ArgumentException>();
    }
}
