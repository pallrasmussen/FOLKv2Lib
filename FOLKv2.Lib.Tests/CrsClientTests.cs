using System.Threading.Tasks;
using Xunit;
using FOLKv2ws.Clients;
using FOLKv2ws.Domain;
using System;
using System.Threading;

public class CrsClientTests
{
    private sealed class FakeCrsPortTypeClient : CrsPortTypeClient
    {
        private readonly Func<GetPersonRequest1, GetPersonResponse1> _personHandler;
        private readonly Func<GetCommunityPeopleIdsRequest, GetCommunityPeopleIdsResponse1> _idsHandler;
        private readonly Func<LoginRequest1, LoginResponse1> _loginHandler;

        public FakeCrsPortTypeClient(
            Func<LoginRequest1, LoginResponse1> login,
            Func<GetPersonRequest1, GetPersonResponse1> person,
            Func<GetCommunityPeopleIdsRequest, GetCommunityPeopleIdsResponse1> ids)
        {
            _loginHandler = login;
            _personHandler = person;
            _idsHandler = ids;
        }

        public override Task<LoginResponse1> InvokeLoginAsync(LoginRequest1 req) => Task.FromResult(_loginHandler(req));
        public override Task<GetPersonResponse1> InvokeGetPersonAsync(GetPersonRequest1 req) => Task.FromResult(_personHandler(req));
        public override Task<GetCommunityPeopleIdsResponse1> InvokeGetCommunityPeopleIdsAsync(GetCommunityPeopleIdsRequest req) => Task.FromResult(_idsHandler(req));
    }

    private static CrsClient CreateClient(LoginResponse1 loginResp, GetPersonResponse1 personResp, GetCommunityPeopleIdsResponse1 idsResp)
    {
        var fake = new FakeCrsPortTypeClient(_ => loginResp, _ => personResp, _ => idsResp);
        var options = new CrsClientOptions
        {
            Consumer = "c",
            Producer = "p",
            ServiceSubsystemCode = "sub",
            Username = "u",
            Password = "pw"
        };
        return new CrsClient(() => fake, options, null);
    }

    private static LoginResponse1 BuildLoginResponse(string token)
    {
        return new LoginResponse1(
            consumer: null,
            producer: null,
            userId: null,
            id: null,
            service: null,
            protocolVersion: null,
            id1: null,
            client: null,
            service1: null,
            LoginResponse: new LoginResponse
            {
                response = new loginResponse { token = token, expires = DateTime.UtcNow.AddMinutes(10) }
            });
    }

    private static GetPersonResponse1 BuildPersonResponse(int id, string first, string last)
    {
        return new GetPersonResponse1(null, null, null, null, null, null, null, null, null, new GetPersonResponse
        {
            response = new getPersonResponse
            {
                Person = new Person
                {
                    Id = id,
                    IdSpecified = true,
                    Names = new[]
                    {
                        new PersonName { Type = NameType.FirstName, TypeSpecified = true, Value = first },
                        new PersonName { Type = NameType.LastName, TypeSpecified = true, Value = last }
                    }
                }
            }
        });
    }

    private static GetCommunityPeopleIdsResponse1 BuildIdsResponse(params (int Id, int PublicId, int ExternalId)[] entries)
    {
        var list = new List<PersonIds>();
        foreach (var e in entries)
        {
            list.Add(new PersonIds
            {
                Id = e.Id,
                IdSpecified = true,
                PublicId = e.PublicId,
                PublicIdSpecified = true,
                ExternalId = e.ExternalId,
                ExternalIdSpecified = true
            });
        }
        return new GetCommunityPeopleIdsResponse1(null, null, null, null, null, null, null, null, null, new GetCommunityPeopleIdsResponse
        {
            response = new getCommunityPeopleIdsResponse { PeopleIds = list.ToArray() }
        });
    }

    [Fact]
    public async Task GetPersonAsync_MapsNames()
    {
        var client = CreateClient(
            BuildLoginResponse("tok"),
            BuildPersonResponse(5, "John", "Doe"),
            BuildIdsResponse());

        var dto = await client.GetPersonAsync(5);
        Assert.NotNull(dto);
        Assert.Equal(5, dto!.Id);
        Assert.Equal("John", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
    }

    [Fact]
    public async Task GetCommunityPeopleIdsAsync_ReturnsList()
    {
        var client = CreateClient(
            BuildLoginResponse("tok"),
            BuildPersonResponse(5, "John", "Doe"),
            BuildIdsResponse((1,10,100),(2,20,200)));

        var list = await client.GetCommunityPeopleIdsAsync();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, p => p.Id == 1 && p.PublicId == 10 && p.ExternalId == 100);
    }
}
