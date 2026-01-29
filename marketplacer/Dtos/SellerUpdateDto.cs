namespace marketplacer.Dtos;

public record SellerUpdateDto(
    string? SellerName,
    string? SellerDomain,
    string? SellerIndustry,
    string? SellerPhone,
    string? HubSpotId
);
