namespace marketplacer.Dtos;

public record SellerUpdateDto(
    string? SellerName,
    string? SellerDomain,
    string? HubSpotId
);
