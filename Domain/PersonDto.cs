namespace FOLKv2ws.Domain;

/// <summary>Flattened person projection.</summary>
public sealed record PersonDto(
    int Id,
    int? PublicId,
    string? FirstName,
    string? LastName,
    string? JobTitle,
    string? CivilStatus,
    string? City
);

/// <summary>Lightweight identifier trio for a person.</summary>
/// <param name="Id">Internal identifier.</param>
/// <param name="PublicId">Public identifier if issued.</param>
/// <param name="ExternalId">External system identifier.</param>
public sealed record PersonIdsDto(int Id, int? PublicId, int? ExternalId);
