using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Security.Cryptography.X509Certificates;

namespace FOLKv2ws;

// Partial implementation separated from generated code.
public partial class CrsPortTypeClient
{
    /// <summary>
    /// Configure generated endpoint: quotas, timeouts, enforce HTTPS, endpoint override and client cert.
    /// </summary>
    static partial void ConfigureEndpoint(ServiceEndpoint serviceEndpoint, ClientCredentials clientCredentials)
    {
        if (serviceEndpoint.Binding is BasicHttpBinding binding)
        {
            binding.MaxReceivedMessageSize = 4 * 1024 * 1024; // 4 MB
            binding.MaxBufferSize = (int)binding.MaxReceivedMessageSize;
            binding.ReaderQuotas.MaxDepth = 64;
            binding.ReaderQuotas.MaxStringContentLength = 256_000;
            binding.ReaderQuotas.MaxArrayLength = 128_000;
            binding.ReaderQuotas.MaxBytesPerRead = 64_000;
            binding.ReaderQuotas.MaxNameTableCharCount = 128_000;

            binding.OpenTimeout = TimeSpan.FromSeconds(10);
            binding.SendTimeout = TimeSpan.FromSeconds(30);
            binding.ReceiveTimeout = TimeSpan.FromSeconds(30);
            binding.CloseTimeout = TimeSpan.FromSeconds(10);
        }

        var overrideUrl = Environment.GetEnvironmentVariable("CRS_ENDPOINT_URL");
        if (!string.IsNullOrWhiteSpace(overrideUrl) && Uri.TryCreate(overrideUrl, UriKind.Absolute, out var newUri))
        {
            serviceEndpoint.Address = new EndpointAddress(newUri);
        }

        // Enforce HTTPS unless explicitly allowed for local development.
        var allowHttp = string.Equals(Environment.GetEnvironmentVariable("CRS_ALLOW_HTTP"), "true", StringComparison.OrdinalIgnoreCase);
        if (!serviceEndpoint.Address.Uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !allowHttp)
        {
            throw new InvalidOperationException($"Insecure HTTP endpoint '{serviceEndpoint.Address.Uri}' not permitted. Set CRS_ALLOW_HTTP=true only for local development.");
        }

        if (serviceEndpoint.Binding is BasicHttpBinding securityBinding)
        {
            securityBinding.Security.Mode = BasicHttpSecurityMode.Transport; // Always Transport (requires HTTPS)
        }

        // Client certificate (mutual TLS) support via environment variables (optional)
        var pfxPath = Environment.GetEnvironmentVariable("CRS_CLIENT_CERT_PFX");
        var pfxPwd = Environment.GetEnvironmentVariable("CRS_CLIENT_CERT_PWD");
        if (!string.IsNullOrWhiteSpace(pfxPath) && System.IO.File.Exists(pfxPath))
        {
            try
            {
                // Use loader API to avoid SYSLIB0057 warnings.
                X509Certificate2 cert = string.IsNullOrEmpty(pfxPwd)
                    ? X509CertificateLoader.LoadPkcs12FromFile(pfxPath, default, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.MachineKeySet)
                    : X509CertificateLoader.LoadPkcs12FromFile(pfxPath, pfxPwd, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.MachineKeySet);
                clientCredentials.ClientCertificate.Certificate = cert;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Failed to load client certificate: {ex.Message}");
            }
        }
    }
}

public partial class CrsPortTypeClient
{
    /// <summary>Invoke Login operation (testable wrapper).</summary>
    public virtual System.Threading.Tasks.Task<LoginResponse1> InvokeLoginAsync(LoginRequest1 req) => ((CrsPortType)this).LoginAsync(req);
    /// <summary>Invoke GetPerson operation (testable wrapper).</summary>
    public virtual System.Threading.Tasks.Task<GetPersonResponse1> InvokeGetPersonAsync(GetPersonRequest1 req) => ((CrsPortType)this).GetPersonAsync(req);
    /// <summary>Invoke GetCommunityPeopleIds operation (testable wrapper).</summary>
    public virtual System.Threading.Tasks.Task<GetCommunityPeopleIdsResponse1> InvokeGetCommunityPeopleIdsAsync(GetCommunityPeopleIdsRequest req) => ((CrsPortType)this).GetCommunityPeopleIdsAsync(req);
}
