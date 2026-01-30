namespace SellerSync.Workflows;

/// <summary>
/// Input to the SellerCreatedWorkflow.
/// Matches the WebhookEventCreateDto format that Marketplacer sends.
/// </summary>
public record SellerCreatedWorkflowInput(
    /// <summary>
    /// Unique key to ensure exactly-once processing.
    /// Used as the Temporal Workflow ID for idempotency.
    /// </summary>
    string IdempotencyKey,

    /// <summary>
    /// Unique identifier for this webhook event.
    /// </summary>
    string WebhookId,

    /// <summary>
    /// JSON string containing the seller data.
    /// Example: {"sellerId": 1, "sellerName": "Acme", "sellerDomain": "acme.com", ...}
    /// </summary>
    string WebhookBody,

    /// <summary>
    /// JSON string containing HTTP headers.
    /// </summary>
    string WebhookHeaders,

    /// <summary>
    /// The ID of the object this webhook is about (e.g., seller ID).
    /// </summary>
    string WebhookObjectId,

    /// <summary>
    /// The type of object (e.g., "Seller").
    /// </summary>
    string WebhookObjectType,

    /// <summary>
    /// The event type (e.g., "seller.created").
    /// </summary>
    string WebhookEventType,

    /// <summary>
    /// When the webhook was sent from Marketplacer.
    /// </summary>
    DateTime SentFromSourceAt
);

/// <summary>
/// Result from calling the HubSpot API to create a company.
/// </summary>
public record HubSpotApiResult(
    /// <summary>
    /// The HubSpot object ID for the created company.
    /// This is what we send back to Marketplacer.
    /// </summary>
    string HubSpotObjectId,

    /// <summary>
    /// HTTP status code from HubSpot API.
    /// </summary>
    int StatusCode
);

/// <summary>
/// Final result of the SellerCreatedWorkflow.
/// </summary>
public record WorkflowResult(
    /// <summary>
    /// Whether the workflow completed successfully.
    /// </summary>
    bool Success,

    /// <summary>
    /// The HubSpot object ID that was created.
    /// </summary>
    string HubSpotObjectId,

    /// <summary>
    /// Human-readable message about what happened.
    /// </summary>
    string Message
);
