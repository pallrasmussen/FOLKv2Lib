using System.Security.Cryptography.X509Certificates;
using System.Net.Http;
using System.Security.Authentication;

namespace FOLKv2ws.Rest;

/// <summary>
/// Builds HttpClient instances configured for mutual TLS using a client PKCS#12 (.pfx) certificate
/// and validating the server certificate against a supplied root / issuing .cer without installing it globally.
/// Adds optional leaf certificate pin predicate (thumbprint / public key hash).
/// </summary>
public static class MtlsHttpClientFactory
{
    /// <summary>
    /// Create an HttpClient configured for mutual TLS and custom server validation.
    /// </summary>
    /// <param name="baseAddress">Base URI for the API.</param>
    /// <param name="pfxPath">Path to client PKCS#12 file.</param>
    /// <param name="pfxPassword">Password for the PFX (empty if none).</param>
    /// <param name="serverCertPath">Trusted server/root certificate file (.cer).</param>
    /// <param name="timeout">Optional request timeout.</param>
    /// <param name="protocols">Enabled TLS protocols.</param>
    /// <param name="serverCertPinPredicate">Optional pin predicate to validate leaf cert.</param>
    public static HttpClient Create(
        string baseAddress,
        string pfxPath,
        string pfxPassword,
        string serverCertPath,
        TimeSpan? timeout = null,
        SslProtocols protocols = SslProtocols.Tls12 | SslProtocols.Tls13,
        Func<X509Certificate2, bool>? serverCertPinPredicate = null)
    {
        X509Certificate2 clientCert = string.IsNullOrEmpty(pfxPassword)
            ? X509CertificateLoader.LoadPkcs12FromFile(pfxPath, default, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.MachineKeySet)
            : X509CertificateLoader.LoadPkcs12FromFile(pfxPath, pfxPassword, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.MachineKeySet);

        var trustedRoot = X509CertificateLoader.LoadCertificateFromFile(serverCertPath);

        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            SslProtocols = protocols
        };
        handler.ClientCertificates.Add(clientCert);

        handler.ServerCertificateCustomValidationCallback = (_, serverCert, _, _) =>
        {
            if (serverCert is null) return false;
            using var chain = new X509Chain
            {
                ChainPolicy =
                {
                    RevocationMode = X509RevocationMode.NoCheck,
                    VerificationFlags = X509VerificationFlags.IgnoreNotTimeNested
                }
            };
            chain.ChainPolicy.ExtraStore.Add(trustedRoot);
            var leaf = new X509Certificate2(serverCert);
            if (!chain.Build(leaf)) return false;
            if (serverCertPinPredicate != null && !serverCertPinPredicate(leaf)) return false;
            return true;
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
            BaseAddress = new Uri(baseAddress, UriKind.Absolute)
        };
    }

    /// <summary>
    /// Helper to build a pin predicate from expected thumbprints (case-insensitive, no spaces).
    /// </summary>
    public static Func<X509Certificate2, bool> PinByThumbprints(params string[] thumbprints)
    {
        var set = thumbprints.Select(t => t.Replace(" ", string.Empty).ToUpperInvariant()).ToHashSet();
        return cert => set.Contains(cert.Thumbprint?.Replace(" ", string.Empty).ToUpperInvariant() ?? string.Empty);
    }
}
