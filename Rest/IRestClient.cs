using System.Net.Http.Headers;

namespace FOLKv2ws.Rest;

/// <summary>
/// Abstraction over an HTTP client configured for FOLK REST (optionally mTLS + pinning).
/// </summary>
public interface IRestClient : IAsyncDisposable
{
    /// <summary>
    /// Retrieve the response content as a string for the given <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Relative or absolute request path/URI.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response body as string.</returns>
    Task<string> GetStringAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Send a prepared <see cref="HttpRequestMessage"/> and return the raw response.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default);

    /// <summary>Gets the underlying configured <see cref="HttpClient"/> instance.</summary>
    HttpClient HttpClient { get; }
}
