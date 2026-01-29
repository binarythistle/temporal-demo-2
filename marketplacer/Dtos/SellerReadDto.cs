namespace marketplacer.Dtos;

public record SellerReadDto(
    int Id,
    string SellerName,
    string? SellerDomain,
    string? HubSpotId
);
