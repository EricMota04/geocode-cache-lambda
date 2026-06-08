using FluentAssertions;
using GeocodeCache.Application.Options;
using GeocodeCache.Application.Services;
using GeocodeCache.Domain.Abstractions;
using GeocodeCache.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GeocodeCache.UnitTests;

public sealed class GeocodeServiceTests
{
    private const string Address = "70 Vanderbilt Ave, New York, NY 10017";
    private const string ExpectedKey = "70 vanderbilt ave, new york, ny 10017";
    private static readonly DateTimeOffset Now = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly IGeocodeCache _cache = Substitute.For<IGeocodeCache>();
    private readonly IGeocodingProvider _provider = Substitute.For<IGeocodingProvider>();
    private readonly CacheOptions _options = new() { TtlDays = 30, NegativeTtlDays = 1 };

    private GeocodeService CreateSut() => new(
        _cache,
        _provider,
        new TestClock(Now),
        Options.Create(_options),
        NullLogger<GeocodeService>.Instance);

    private static GeocodingResult Google(string status, string? json = null) =>
        new() { Status = status, RawJson = json ?? $"{{\"status\":\"{status}\",\"results\":[]}}" };

    [Fact]
    public async Task Returns_from_cache_without_calling_google_when_entry_is_fresh()
    {
        var cached = new CachedGeocode
        {
            AddressKey = ExpectedKey,
            OriginalAddress = Address,
            GoogleResponseJson = "{\"status\":\"OK\",\"results\":[{\"cached\":true}]}",
            GoogleStatus = GoogleGeocodeStatus.Ok,
            IsNegative = false,
            CachedAtUtc = Now.AddDays(-5),
            ExpiresAtUtc = Now.AddDays(25),
        };
        _cache.GetAsync(ExpectedKey, Arg.Any<CancellationToken>()).Returns(cached);

        var outcome = await CreateSut().GetGeocodeAsync(Address);

        outcome.FromCache.Should().BeTrue();
        outcome.GoogleResponseJson.Should().Be(cached.GoogleResponseJson);
        await _provider.DidNotReceive().GeocodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Calls_google_and_caches_with_30_day_ttl_on_miss()
    {
        _cache.GetAsync(ExpectedKey, Arg.Any<CancellationToken>()).Returns((CachedGeocode?)null);
        _provider.GeocodeAsync(Address, Arg.Any<CancellationToken>()).Returns(Google(GoogleGeocodeStatus.Ok));

        var outcome = await CreateSut().GetGeocodeAsync(Address);

        outcome.FromCache.Should().BeFalse();
        outcome.GoogleStatus.Should().Be(GoogleGeocodeStatus.Ok);
        await _cache.Received(1).PutAsync(
            Arg.Is<CachedGeocode>(e =>
                e.AddressKey == ExpectedKey &&
                e.OriginalAddress == Address &&
                !e.IsNegative &&
                e.ExpiresAtUtc == Now.AddDays(30)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refetches_from_google_when_cached_entry_is_expired()
    {
        var expired = new CachedGeocode
        {
            AddressKey = ExpectedKey,
            OriginalAddress = Address,
            GoogleResponseJson = "{\"status\":\"OK\",\"results\":[{\"stale\":true}]}",
            GoogleStatus = GoogleGeocodeStatus.Ok,
            IsNegative = false,
            CachedAtUtc = Now.AddDays(-31),
            ExpiresAtUtc = Now.AddSeconds(-1), // just expired
        };
        _cache.GetAsync(ExpectedKey, Arg.Any<CancellationToken>()).Returns(expired);
        _provider.GeocodeAsync(Address, Arg.Any<CancellationToken>())
            .Returns(Google(GoogleGeocodeStatus.Ok, "{\"status\":\"OK\",\"results\":[{\"fresh\":true}]}"));

        var outcome = await CreateSut().GetGeocodeAsync(Address);

        outcome.FromCache.Should().BeFalse();
        outcome.GoogleResponseJson.Should().Contain("fresh");
        await _provider.Received(1).GeocodeAsync(Address, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Negatively_caches_zero_results_with_short_ttl()
    {
        _cache.GetAsync(ExpectedKey, Arg.Any<CancellationToken>()).Returns((CachedGeocode?)null);
        _provider.GeocodeAsync(Address, Arg.Any<CancellationToken>()).Returns(Google(GoogleGeocodeStatus.ZeroResults));

        var outcome = await CreateSut().GetGeocodeAsync(Address);

        outcome.IsNegative.Should().BeTrue();
        outcome.GoogleStatus.Should().Be(GoogleGeocodeStatus.ZeroResults);
        await _cache.Received(1).PutAsync(
            Arg.Is<CachedGeocode>(e => e.IsNegative && e.ExpiresAtUtc == Now.AddDays(1)),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(GoogleGeocodeStatus.OverQueryLimit)]
    [InlineData(GoogleGeocodeStatus.RequestDenied)]
    [InlineData(GoogleGeocodeStatus.InvalidRequest)]
    [InlineData(GoogleGeocodeStatus.UnknownError)]
    public async Task Does_not_cache_transient_or_config_errors(string status)
    {
        _cache.GetAsync(ExpectedKey, Arg.Any<CancellationToken>()).Returns((CachedGeocode?)null);
        _provider.GeocodeAsync(Address, Arg.Any<CancellationToken>()).Returns(Google(status));

        var outcome = await CreateSut().GetGeocodeAsync(Address);

        outcome.FromCache.Should().BeFalse();
        outcome.GoogleStatus.Should().Be(status);
        await _cache.DidNotReceive().PutAsync(Arg.Any<CachedGeocode>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Throws_for_blank_address(string? address)
    {
        var act = async () => await CreateSut().GetGeocodeAsync(address!);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
