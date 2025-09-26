using System;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using FOLKv2ws.Clients;
using FOLKv2ws.Domain;

public class AdditionalCrsClientTests
{
    private sealed class FaultingLoginClient : CrsPortTypeClient
    {
        public override Task<LoginResponse1> InvokeLoginAsync(LoginRequest1 req)
        {
            // Simulate remote fault via faultCode
            return Task.FromResult(new LoginResponse1(null,null,null,null,null,null,null,null,null,new LoginResponse
            {
                response = new loginResponse { faultCode = "ERR_AUTH", faultString = "Auth failed" }
            }));
        }
    }

    [Fact]
    public async Task EnsureTokenAsync_Fault_ThrowsRemoteFaultException()
    {
        var options = new CrsClientOptions
        {
            Consumer = "c",
            Producer = "p",
            ServiceSubsystemCode = "sub",
            Username = "user",
            Password = "pw"
        };
        var client = new CrsClient(() => new FaultingLoginClient(), options, null);
        await Assert.ThrowsAsync<RemoteFaultException>(() => client.EnsureTokenAsync());
    }

    private sealed class TransientGetPersonClient : CrsPortTypeClient
    {
        private int _calls;
        public int Calls => _calls;
        public override Task<LoginResponse1> InvokeLoginAsync(LoginRequest1 req)
        {
            return Task.FromResult(new LoginResponse1(null,null,null,null,null,null,null,null,null,new LoginResponse
            {
                response = new loginResponse { token = "tok", expires = DateTime.UtcNow.AddMinutes(5) }
            }));
        }
        public override Task<GetPersonResponse1> InvokeGetPersonAsync(GetPersonRequest1 req)
        {
            _calls++;
            if (_calls < 3) throw new TimeoutException("Transient timeout");
            return Task.FromResult(new GetPersonResponse1(null,null,null,null,null,null,null,null,null,new GetPersonResponse
            {
                response = new getPersonResponse
                {
                    Person = new Person { Id = 9, IdSpecified = true, Names = new [] { new PersonName { Value = "X" } } }
                }
            }));
        }
    }

    [Fact]
    public async Task GetPersonAsync_RetriesOnTimeout_SucceedsOnThirdCall()
    {
        var fake = new TransientGetPersonClient();
        var options = new CrsClientOptions
        {
            Consumer = "c",
            Producer = "p",
            ServiceSubsystemCode = "sub",
            Username = "user",
            Password = "pw"
        };
        var client = new CrsClient(() => fake, options, null);
        var dto = await client.GetPersonAsync(9);
        Assert.NotNull(dto);
        Assert.Equal(9, dto!.Id);
        Assert.True(fake.Calls >= 3); // two failures + success
    }
}
