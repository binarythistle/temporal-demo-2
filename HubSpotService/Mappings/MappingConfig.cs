using Mapster;

public class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // WebhookEventCreateDto to WebhookEvent
        config.NewConfig<WebhookEventCreateDto, WebhookEvent>()
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Ignore(dest => dest.Id);

        // WebhookEvent to WebhookEventReadDto
        config.NewConfig<WebhookEvent, WebhookEventReadDto>();
    }
}