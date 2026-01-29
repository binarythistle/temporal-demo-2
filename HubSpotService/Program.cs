

using Mapster;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("SqlLiteConnection")));

builder.Services.AddMapster();

var app = builder.Build();

app.MapPost("/api/webhooks", async (WebhookEventCreateDto createDto, AppDbContext db) =>
{
    var webhookEvent = createDto.Adapt<WebhookEvent>();

    db.WebhookEvents.Add(webhookEvent);
    await db.SaveChangesAsync();

    var readDto = webhookEvent.Adapt<WebhookEventReadDto>();

    return Results.Created($"/api/webhooks/{webhookEvent.Id}", readDto);
});

app.MapGet("/api/webhooks", async (AppDbContext db) =>
{
    var webhookEvents = await db.WebhookEvents.ToListAsync();
    var readDtos = webhookEvents.Adapt<List<WebhookEventReadDto>>();

    return Results.Ok(readDtos);
});

app.Run();


