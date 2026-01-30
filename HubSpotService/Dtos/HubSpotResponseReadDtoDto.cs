/// <summary>
/// DTO for reading HubSpot API response data
/// </summary>
public record HubSpotResponseReadDto(
    int Id,
    int WebhookEventId,
    string HubSpotObjectId,
    int ResponseStatusCode,
    string ResponseBody,
    DateTime CreatedAt
);