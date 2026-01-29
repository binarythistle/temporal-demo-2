namespace marketplacer.Dtos;

public record SellerDto(
    int Id,
    string SellerName,
    string? SellerDomain,
    string? HubSpotId
);
