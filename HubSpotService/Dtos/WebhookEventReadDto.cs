namespace HubSpotService.Models;

/// <summary>
/// DTO for reading webhook event data
/// </summary>
public record WebhookEventReadDto(
    int Id,
    string IdempotencyKey,
    string WebhookId,
    string WebhookBody,
    string WebhookHeaders,
    string WebhookObjectId,
    string WebhookObjectType,
    string WebhookEventType,
    DateTime CreatedAt,
    DateTime SentFromSourceAt
);
