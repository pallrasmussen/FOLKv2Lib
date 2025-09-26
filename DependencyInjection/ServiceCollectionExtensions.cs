using System.ServiceModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FOLKv2ws.Clients;
using FOLKv2ws.Rest;

namespace FOLKv2ws.DependencyInjection;

/// <summary>DI helpers for registering FOLKv2 related clients and services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Configuration section path for CRS options.</summary>
    public const string CrsConfigSection = "Folk:Crs"; // Example section
    /// <summary>Configuration section path for REST client options.</summary>
    public const string RestConfigSection = "Folk:Rest";

    /// <summary>Adds CRS WCF client, REST mTLS client (optional) and related options to the service collection.</summary>
    /// <param name="services">DI service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The provided <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddFolkServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind CRS options (basic binding only)
        services.AddOptions<CrsClientOptions>()
            .Bind(configuration.GetSection(CrsConfigSection));

        services.AddTransient<CrsPortTypeClient>(_ => new CrsPortTypeClient());

        services.AddSingleton<ICrsClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CrsClientOptions>>().Value;
            return new CrsClient(() => sp.GetRequiredService<CrsPortTypeClient>(), opts);
        });

        var restSection = configuration.GetSection(RestConfigSection);
        var baseUrl = restSection["BaseUrl"];
        var pfxPath = restSection["ClientPfxPath"];
        var pfxPwd = restSection["ClientPfxPassword"];
        var serverCer = restSection["ServerCerPath"];

        if (!string.IsNullOrWhiteSpace(baseUrl) &&
            !string.IsNullOrWhiteSpace(pfxPath) &&
            !string.IsNullOrWhiteSpace(serverCer))
        {
            services.AddSingleton<IRestClient>(sp =>
            {
                var http = MtlsHttpClientFactory.Create(
                    baseAddress: baseUrl!,
                    pfxPath: pfxPath!,
                    pfxPassword: pfxPwd ?? string.Empty,
                    serverCertPath: serverCer!);
                return new RestClient(http);
            });
        }
        else
        {
            services.AddHttpClient<IRestClient, RestClient>();
        }

        return services;
    }
}
