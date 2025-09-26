using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using FOLKv2ws.Domain;
using Xunit;

public class SerializationTests
{
    private static T RoundTrip<T>(T obj)
    {
        var ser = new DataContractSerializer(typeof(T));
        using var ms = new MemoryStream();
        ser.WriteObject(ms, obj!);
        ms.Position = 0;
        return (T)ser.ReadObject(ms)!;
    }

    [Fact]
    public void Person_RoundTrip_PreservesNames()
    {
        var p = new Person
        {
            Id = 42,
            IdSpecified = true,
            Names = new []
            {
                new PersonName { Type = NameType.FirstName, TypeSpecified = true, Value = "Alice" },
                new PersonName { Type = NameType.LastName, TypeSpecified = true, Value = "Smith" }
            }
        };

        var copy = RoundTrip(p);
        Assert.True(copy.IdSpecified);
        Assert.Equal(42, copy.Id);
        Assert.NotNull(copy.Names);
        Assert.Equal(2, copy.Names.Length);
        Assert.Contains(copy.Names, n => n.Type == NameType.FirstName && n.Value == "Alice");
    }

    [Fact]
    public void PersonIds_RoundTrip_PreservesFields()
    {
        var ids = new PersonIds
        {
            Id = 5, IdSpecified = true,
            PublicId = 15, PublicIdSpecified = true,
            ExternalId = 25, ExternalIdSpecified = true
        };
        var copy = RoundTrip(ids);
        Assert.True(copy.IdSpecified);
        Assert.Equal(5, copy.Id);
        Assert.True(copy.PublicIdSpecified);
        Assert.Equal(15, copy.PublicId);
        Assert.True(copy.ExternalIdSpecified);
        Assert.Equal(25, copy.ExternalId);
    }

    [Fact]
    public void DtoMappings_NullHandling()
    {
        Person? p = null;
        var dto = DtoMappings.ToPersonDto(p);
        Assert.Null(dto);
    }

    [Fact]
    public void DtoMappings_FirstNameFallback()
    {
        var p = new Person
        {
            Id = 7, IdSpecified = true,
            Names = new [] { new PersonName { Value = "OnlyName" } }
        };
        var dto = DtoMappings.ToPersonDto(p)!;
        Assert.Equal("OnlyName", dto.FirstName);
        Assert.Null(dto.LastName);
    }
}
