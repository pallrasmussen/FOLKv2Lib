namespace FOLKv2ws.Clients;

/// <summary>Configuration options for <see cref="CrsClient"/>.</summary>
public sealed class CrsClientOptions
{
    /// <summary>X-Road consumer ID.</summary>
    public required string Consumer { get; init; }
    /// <summary>X-Road producer ID.</summary>
    public required string Producer { get; init; }
    /// <summary>Subsystem code used in client identifier.</summary>
    public required string ServiceSubsystemCode { get; init; }
    /// <summary>X-Road protocol version (default 4.0).</summary>
    public string ProtocolVersion { get; init; } = "4.0";
    /// <summary>Login username.</summary>
    public required string Username { get; init; }
    /// <summary>Login password (retrieve from secure secret provider in production).</summary>
    public required string Password { get; init; }
}
