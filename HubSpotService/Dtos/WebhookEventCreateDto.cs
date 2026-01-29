

/// <summary>
/// DTO for creating a new webhook event
/// </summary>
public record WebhookEventCreateDto(
    string IdempotencyKey,
    string WebhookId,
    string WebhookBody,
    string WebhookHeaders,
    string WebhookObjectId,
    string WebhookObjectType,
    string WebhookEventType,
    DateTime SentFromSourceAt
);
