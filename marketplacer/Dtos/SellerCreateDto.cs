namespace marketplacer.Dtos;

public record SellerCreateDto(
    string SellerName,
    string? SellerDomain,
    string? HubSpotId
);
