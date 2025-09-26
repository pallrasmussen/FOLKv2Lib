using System.Net.Http;

namespace FOLKv2ws.Rest;

/// <summary>
/// Default implementation of <see cref="IRestClient"/> wrapping an <see cref="HttpClient"/>.
/// </summary>
public sealed class RestClient : IRestClient, IAsyncDisposable
{
    /// <summary>Gets the underlying configured <see cref="HttpClient"/>.</summary>
    public HttpClient HttpClient { get; }
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="RestClient"/> class.</summary>
    /// <param name="httpClient">The configured <see cref="HttpClient"/> instance (lifecycle managed externally).</param>
    public RestClient(HttpClient httpClient) => HttpClient = httpClient;

    /// <summary>Gets the response body as a string for a relative or absolute <paramref name="path"/>.</summary>
    /// <param name="path">Relative or absolute request URI.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<string> GetStringAsync(string path, CancellationToken ct = default) => HttpClient.GetStringAsync(path, ct);

    /// <summary>Sends a prepared <see cref="HttpRequestMessage"/> returning the raw response.</summary>
    /// <param name="request">Request message.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default) => HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

    /// <summary>Dispose underlying resources.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        HttpClient.Dispose();
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
}
