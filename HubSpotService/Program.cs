using HubSpotService.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("SqlLiteConnection")));


var app = builder.Build();

app.MapPost("/api/webhooks", async (WebhookEventCreateDto createDto, AppDbContext db) =>
{
    var webhookEvent = new WebhookEvent
    {
        IdempotencyKey = createDto.IdempotencyKey,
        WebhookId = createDto.WebhookId,
        WebhookBody = createDto.WebhookBody,
        WebhookHeaders = createDto.WebhookHeaders,
        WebhookObjectId = createDto.WebhookObjectId,
        WebhookObjectType = createDto.WebhookObjectType,
        WebhookEventType = createDto.WebhookEventType,
        SentFromSourceAt = createDto.SentFromSourceAt,
        CreatedAt = DateTime.UtcNow
    };

    db.WebhookEvents.Add(webhookEvent);
    await db.SaveChangesAsync();

    var readDto = new WebhookEventReadDto(
        webhookEvent.Id,
        webhookEvent.IdempotencyKey,
        webhookEvent.WebhookId,
        webhookEvent.WebhookBody,
        webhookEvent.WebhookHeaders,
        webhookEvent.WebhookObjectId,
        webhookEvent.WebhookObjectType,
        webhookEvent.WebhookEventType,
        webhookEvent.CreatedAt,
        webhookEvent.SentFromSourceAt
    );

    return Results.Created($"/api/webhooks/{webhookEvent.Id}", readDto);
});

app.MapGet("/api/webhooks", async (AppDbContext db) =>
{
    var webhookEvents = await db.WebhookEvents
        .Select(w => new WebhookEventReadDto(
            w.Id,
            w.IdempotencyKey,
            w.WebhookId,
            w.WebhookBody,
            w.WebhookHeaders,
            w.WebhookObjectId,
            w.WebhookObjectType,
            w.WebhookEventType,
            w.CreatedAt,
            w.SentFromSourceAt
        ))
        .ToListAsync();

    return Results.Ok(webhookEvents);
});

app.Run();


