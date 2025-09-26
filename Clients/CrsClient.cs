using System;
using System.ServiceModel;
using FOLKv2ws.Domain;
using System.Linq;
using Polly;
using Microsoft.Extensions.Logging;

namespace FOLKv2ws.Clients;

/// <summary>
/// High-level CRS client providing token management, DTO mapping, resilience and logging.
/// </summary>
public sealed class CrsClient : ICrsClient
{
    private readonly Func<CrsPortTypeClient> _clientFactory;
    private readonly CrsClientOptions _options;
    private readonly ILogger<CrsClient>? _logger;

    private CrsPortTypeClient? _client;
    private string? _token;
    private DateTimeOffset _tokenExpires = DateTimeOffset.MinValue;
    private bool _disposed;

    private readonly AsyncPolicy _retryPolicy;
    private readonly AsyncPolicy _circuitBreaker;

    /// <summary>Create a new <see cref="CrsClient"/>.</summary>
    public CrsClient(Func<CrsPortTypeClient> clientFactory, CrsClientOptions options, ILogger<CrsClient>? logger = null)
    {
        _clientFactory = clientFactory;
        _options = options;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<TimeoutException>()
            .Or<CommunicationException>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));

        _circuitBreaker = Policy
            .Handle<TimeoutException>()
            .Or<CommunicationException>()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }

    private CrsPortTypeClient Client => _client ??= _clientFactory();

    /// <summary>Ensure a valid login token exists (logging in if expired/absent).</summary>
    public async Task<string> EnsureTokenAsync(CancellationToken ct = default)
    {
        if (!IsTokenValid())
        {
            await AcquireTokenAsync(ct).ConfigureAwait(false);
        }
        return _token!;
    }

    private bool IsTokenValid() => !string.IsNullOrEmpty(_token) && _tokenExpires > DateTimeOffset.UtcNow.AddMinutes(1);

    private async Task AcquireTokenAsync(CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var login = new Login { request = new loginRequest { username = _options.Username, password = _options.Password } };
        var request = new LoginRequest1(
            consumer: _options.Consumer,
            producer: _options.Producer,
            userId: _options.Username,
            id: correlationId,
            service: "Login",
            protocolVersion: _options.ProtocolVersion,
            id1: Guid.NewGuid().ToString("N"),
            client: new XRoadClientIdentifierType { objectType = XRoadObjectType.SUBSYSTEM, subsystemCode = _options.ServiceSubsystemCode },
            service1: new XRoadServiceIdentifierType { objectType = XRoadObjectType.SERVICE, serviceCode = "Login" },
            Login: login);

        var response = await Client.InvokeLoginAsync(request).ConfigureAwait(false);
        ThrowIfFault("Login", response?.LoginResponse?.response?.faultCode, response?.LoginResponse?.response?.faultString);

        var token = response?.LoginResponse?.response?.token;
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Login failed (no token).");
        _token = token;
        var expires = response?.LoginResponse?.response?.expires;
        _tokenExpires = expires.HasValue ? DateTime.SpecifyKind(expires.Value, DateTimeKind.Utc) : DateTimeOffset.UtcNow.AddHours(1);
    }

    /// <summary>Retrieve a person and map to <see cref="PersonDto"/>.</summary>
    public async Task<PersonDto?> GetPersonAsync(int id, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var token = await EnsureTokenAsync(ct).ConfigureAwait(false);
        var wrapper = new GetPerson
        {
            request = new GetPersonRequest
            {
                requestBody = new GetPersonRequestRequestBody
                {
                    Id = id,
                    IdSpecified = true,
                    IncludeNames = true,
                    IncludeNamesSpecified = true
                },
                requestHeader = new GetPersonRequestRequestHeader { token = token }
            }
        };

        var request = new GetPersonRequest1(
            consumer: _options.Consumer,
            producer: _options.Producer,
            userId: _options.Username,
            id: correlationId,
            service: "GetPerson",
            protocolVersion: _options.ProtocolVersion,
            id1: Guid.NewGuid().ToString("N"),
            client: new XRoadClientIdentifierType { objectType = XRoadObjectType.SUBSYSTEM, subsystemCode = _options.ServiceSubsystemCode },
            service1: new XRoadServiceIdentifierType { objectType = XRoadObjectType.SERVICE, serviceCode = "GetPerson" },
            GetPerson: wrapper);

        var response = await ExecuteIdempotentAsync(() => Client.InvokeGetPersonAsync(request));
        var fault = response?.GetPersonResponse?.response;
        ThrowIfFault("GetPerson", fault?.faultCode, fault?.faultString);
        return DtoMappings.ToPersonDto(fault?.Person);
    }

    /// <summary>Retrieve community people IDs (paged) mapped to ID DTOs.</summary>
    public async Task<IReadOnlyList<PersonIdsDto>> GetCommunityPeopleIdsAsync(int startIndex = 0, int? count = null, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var token = await EnsureTokenAsync(ct).ConfigureAwait(false);
        var wrapper = new GetCommunityPeopleIds
        {
            request = new communityPeopleIdsRequest
            {
                requestBody = new communityPeopleIdsRequestRequestBody
                {
                    StartIndex = startIndex,
                    StartIndexSpecified = true,
                    Count = count.GetValueOrDefault(),
                    CountSpecified = count.HasValue
                },
                requestHeader = new communityPeopleIdsRequestRequestHeader { token = token }
            }
        };

        var request = new GetCommunityPeopleIdsRequest(
            consumer: _options.Consumer,
            producer: _options.Producer,
            userId: _options.Username,
            id: correlationId,
            service: "GetCommunityPeopleIds",
            protocolVersion: _options.ProtocolVersion,
            id1: Guid.NewGuid().ToString("N"),
            client: new XRoadClientIdentifierType { objectType = XRoadObjectType.SUBSYSTEM, subsystemCode = _options.ServiceSubsystemCode },
            service1: new XRoadServiceIdentifierType { objectType = XRoadObjectType.SERVICE, serviceCode = "GetCommunityPeopleIds" },
            GetCommunityPeopleIds: wrapper);

        var response = await ExecuteIdempotentAsync(() => Client.InvokeGetCommunityPeopleIdsAsync(request));
        var fault = response?.GetCommunityPeopleIdsResponse?.response;
        ThrowIfFault("GetCommunityPeopleIds", fault?.faultCode, fault?.faultString);
        return fault?.PeopleIds?.Select(DtoMappings.ToPersonIdsDto).ToList() ?? new List<PersonIdsDto>();
    }

    private async Task<T> ExecuteIdempotentAsync<T>(Func<Task<T>> action)
    {
        return await _retryPolicy.WrapAsync(_circuitBreaker)
            .ExecuteAsync(async () => await action().ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    private static void ThrowIfFault(string op, string? faultCode, string? faultString)
    {
        if (!string.IsNullOrWhiteSpace(faultCode))
        {
            throw new RemoteFaultException(op, faultCode, faultString);
        }
    }

    /// <summary>Dispose the WCF client channel.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (_client is { } c)
        {
            try
            {
                if (c.State == CommunicationState.Faulted) c.Abort();
                else await Task.Run(c.Close).ConfigureAwait(false);
            }
            catch { c.Abort(); }
        }
        GC.SuppressFinalize(this);
    }
}
