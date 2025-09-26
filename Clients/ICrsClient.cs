using FOLKv2ws.Domain;

namespace FOLKv2ws.Clients;

/// <summary>
/// High-level CRS client abstraction hiding WCF headers and token lifecycle.
/// </summary>
public interface ICrsClient : IAsyncDisposable
{
    /// <summary>Ensures a valid login token is cached (logs in if needed).</summary>
    Task<string> EnsureTokenAsync(CancellationToken ct = default);

    /// <summary>Fetch a person by internal Id.</summary>
    Task<PersonDto?> GetPersonAsync(int id, CancellationToken ct = default);

    /// <summary>Fetch community people IDs (paged).</summary>
    Task<IReadOnlyList<PersonIdsDto>> GetCommunityPeopleIdsAsync(int startIndex = 0, int? count = null, CancellationToken ct = default);
}
