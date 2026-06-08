using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using FluentAssertions;
using GeocodeCache.Application.Options;
using GeocodeCache.Infrastructure.Secrets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GeocodeCache.UnitTests;

public sealed class SecretsManagerApiKeyProviderTests : IDisposable
{
    private const string SecretName = "geocode-cache/google-api-key";
    private readonly IAmazonSecretsManager _secrets = Substitute.For<IAmazonSecretsManager>();

    public SecretsManagerApiKeyProviderTests() =>
        Environment.SetEnvironmentVariable(SecretsManagerApiKeyProvider.EnvironmentVariableName, null);

    public void Dispose() =>
        Environment.SetEnvironmentVariable(SecretsManagerApiKeyProvider.EnvironmentVariableName, null);

    private SecretsManagerApiKeyProvider CreateSut() => new(
        _secrets,
        Options.Create(new GoogleGeocodingOptions { ApiKeySecretName = SecretName }),
        NullLogger<SecretsManagerApiKeyProvider>.Instance);

    [Fact]
    public async Task Prefers_environment_variable_and_skips_secrets_manager()
    {
        Environment.SetEnvironmentVariable(SecretsManagerApiKeyProvider.EnvironmentVariableName, "env-key");

        var key = await CreateSut().GetApiKeyAsync();

        key.Should().Be("env-key");
        await _secrets.DidNotReceive().GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reads_plain_secret_string_from_secrets_manager()
    {
        _secrets.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetSecretValueResponse { SecretString = "secret-key" });

        var key = await CreateSut().GetApiKeyAsync();

        key.Should().Be("secret-key");
    }

    [Fact]
    public async Task Extracts_key_from_json_secret()
    {
        _secrets.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetSecretValueResponse { SecretString = "{\"GOOGLE_API_KEY\":\"json-key\"}" });

        var key = await CreateSut().GetApiKeyAsync();

        key.Should().Be("json-key");
    }

    [Fact]
    public async Task Caches_secret_after_first_fetch()
    {
        _secrets.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetSecretValueResponse { SecretString = "secret-key" });
        var sut = CreateSut();

        await sut.GetApiKeyAsync();
        await sut.GetApiKeyAsync();

        await _secrets.Received(1).GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_no_env_var_and_no_secret_name_configured()
    {
        var sut = new SecretsManagerApiKeyProvider(
            _secrets,
            Options.Create(new GoogleGeocodingOptions { ApiKeySecretName = string.Empty }),
            NullLogger<SecretsManagerApiKeyProvider>.Instance);

        var act = async () => await sut.GetApiKeyAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
