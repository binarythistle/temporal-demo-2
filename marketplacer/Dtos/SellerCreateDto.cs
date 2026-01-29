namespace marketplacer.Dtos;

public record SellerCreateDto(
    string SellerName,
    string? SellerDomain,
    string? SellerIndustry,
    string? SellerPhone,
    string? HubSpotId
);
