using System.Text.Json.Serialization;

namespace SellerSync.Workflows;

/// <summary>
/// Request body for creating a company in HubSpot.
/// Matches HubSpot CRM API v3 format.
/// </summary>
public class HubSpotCompanyCreateRequest
{
    [JsonPropertyName("properties")]
    public required HubSpotCompanyProperties Properties { get; set; }
}

/// <summary>
/// Properties for a HubSpot company.
/// </summary>
public class HubSpotCompanyProperties
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("industry")]
    public string? Industry { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

/// <summary>
/// Request body for updating a seller in Marketplacer with the HubSpot ID.
/// </summary>
public record UpdateSellerHubSpotIdRequest(
    string HubSpotId
);
