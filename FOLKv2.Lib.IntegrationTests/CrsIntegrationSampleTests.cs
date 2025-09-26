using System;
using System.Threading.Tasks;
using Xunit;
using FOLKv2ws.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

// These tests are illustrative and require real endpoint + certs/secrets.
// They are skipped unless RUN_INTEGRATION env var is set to 'true'.
namespace FOLKv2ws.IntegrationTests;

public class CrsIntegrationSampleTests
{
    private static bool Enabled => string.Equals(Environment.GetEnvironmentVariable("RUN_INTEGRATION"), "true", StringComparison.OrdinalIgnoreCase);

    private static IServiceProvider BuildProvider()
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var sc = new ServiceCollection();
        sc.AddFolkServices(config);
        return sc.BuildServiceProvider();
    }

    [Fact(Skip = "Requires external CRS service; set RUN_INTEGRATION=true to enable and remove Skip dynamically.")]
    public async Task GetPerson_RoundTrip()
    {
        if (!Enabled) return; // runtime guard
        var sp = BuildProvider();
        var client = sp.GetRequiredService<ICrsClient>();
        var person = await client.GetPersonAsync(1);
        Assert.NotNull(person);
    }
}
