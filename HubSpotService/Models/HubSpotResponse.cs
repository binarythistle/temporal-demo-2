

/// <summary>
/// Represents the response from HubSpot API when creating a company
/// </summary>
public class HubSpotResponse
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the webhook event that triggered this HubSpot API call
    /// </summary>
    public int WebhookEventId { get; set; }

    /// <summary>
    /// The HubSpot object ID (hs_object_id) returned from the API
    /// </summary>
    public required string HubSpotObjectId { get; set; }

    /// <summary>
    /// HTTP status code from the HubSpot API response
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// Full response body from HubSpot API
    /// </summary>
    public required string ResponseBody { get; set; }

    /// <summary>
    /// Timestamp when the response was received and saved
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the related webhook event
    /// </summary>
    public WebhookEvent? WebhookEvent { get; set; }
}
