namespace marketplacer.Dtos;

public record CreateSellerDto(
    string SellerName,
    string? SellerDomain,
    string? HubSpotId
);
