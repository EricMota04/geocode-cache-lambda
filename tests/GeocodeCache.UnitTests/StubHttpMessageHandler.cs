using System.Net;

namespace GeocodeCache.UnitTests;

/// <summary>A configurable <see cref="HttpMessageHandler"/> for testing typed HTTP clients offline.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responder = responder;

    /// <summary>The last request the handler observed (for asserting the outbound URL).</summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    public static StubHttpMessageHandler Returning(HttpStatusCode statusCode, string body) =>
        new(_ => new HttpResponseMessage(statusCode) { Content = new StringContent(body) });

    public static StubHttpMessageHandler Throwing(Exception exception) =>
        new(_ => throw exception);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_responder(request));
    }
}
