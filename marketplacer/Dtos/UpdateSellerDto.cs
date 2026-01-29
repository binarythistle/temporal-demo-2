namespace marketplacer.Dtos;

public record UpdateSellerDto(
    string? SellerName,
    string? SellerDomain,
    string? HubSpotId
);
