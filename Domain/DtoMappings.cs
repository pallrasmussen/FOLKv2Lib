using System;
using System.Linq;

namespace FOLKv2ws.Domain;

/// <summary>
/// Central mapping layer from generated WCF types to slim immutable DTOs.
/// Only this file references svcutil-generated classes so regeneration impact is isolated.
/// </summary>
public static class DtoMappings
{
    /// <summary>Convert a generated <see cref="Person"/> to <see cref="PersonDto"/>.</summary>
    public static PersonDto? ToPersonDto(Person? p)
    {
        if (p == null) return null;
        string? first = p.Names?.FirstOrDefault(n => n.Type == NameType.FirstName)?.Value ?? p.Names?.FirstOrDefault()?.Value;
        string? last = p.Names?.FirstOrDefault(n => n.Type == NameType.LastName)?.Value;
        var city = p.Address?.City;
        return new PersonDto(
            Id: p.IdSpecified ? p.Id : 0,
            PublicId: p.PublicIdSpecified ? p.PublicId : null,
            FirstName: first,
            LastName: last,
            JobTitle: p.JobTitle,
            CivilStatus: p.CivilStatusSpecified ? p.CivilStatus.ToString() : null,
            City: city
        );
    }

    /// <summary>Convert generated <see cref="PersonIds"/> to <see cref="PersonIdsDto"/>.</summary>
    public static PersonIdsDto ToPersonIdsDto(PersonIds p) => new(
        Id: p.IdSpecified ? p.Id : 0,
        PublicId: p.PublicIdSpecified ? p.PublicId : null,
        ExternalId: p.ExternalIdSpecified ? p.ExternalId : null);
}
