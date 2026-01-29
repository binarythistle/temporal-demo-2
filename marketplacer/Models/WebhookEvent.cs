public class WebhookEvent
{
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier to ensure the webhook event is processed only once, 
    /// preventing duplicate processing from retries.
    /// </summary>
    public required string IdempotencyKey { get; set; }

    /// <summary>
    /// The identifier of the webhook from the source system
    /// in this case Marketplacer
    /// </summary>
    public required string WebhookId { get; set; }

    /// <summary>
    /// The webhook body
    /// </summary>
    public required string WebhookBody { get; set; }

    /// <summary>
    /// The webhook headers
    /// </summary>
    public required string WebhookHeaders { get; set; }

    /// <summary>
    /// Marketplacer webhook events relate Create, Update and Delete operations on a range of objects.
    /// E.g.: Products, Orders, Sellers. Each object has a unique id
    /// This property holds the object id that triggered the webhook
    /// </summary>
    public required string WebhookObjectId { get; set; }

    /// <summary>
    /// The type of object the triggered the webhook
    /// E.g.: Product, Order or Seller
    /// </summary>
    public required string WebhookObjectType { get; set; }

    /// <summary>
    /// Webhooks are triggered from either: Create, Update or Destroy object operations
    /// This property holds that operation type
    /// </summary>
    public required string WebhookEventType { get; set; }

    /// <summary>
    /// The timestamp the record was created in this app (Marketplacer)
    /// </summary>
    public DateTime CreatedAt { get; set; }

  
}