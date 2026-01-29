

using Mapster;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("SqlLiteConnection")));

builder.Services.AddMapster();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/webhooks", async (WebhookEventCreateDto createDto, AppDbContext db) =>
{
    var webhookEvent = createDto.Adapt<WebhookEvent>();
    webhookEvent.CreatedAt = DateTime.UtcNow;

    db.WebhookEvents.Add(webhookEvent);
    await db.SaveChangesAsync();

    var readDto = webhookEvent.Adapt<WebhookEventReadDto>();

    return Results.Created($"/api/webhooks/{webhookEvent.Id}", readDto);
});

app.MapGet("/api/webhooks", async (AppDbContext db) =>
{
    var webhookEvents = await db.WebhookEvents
        .OrderByDescending(w => w.CreatedAt)
        .ToListAsync();
    var readDtos = webhookEvents.Adapt<List<WebhookEventReadDto>>();

    return Results.Ok(readDtos);
});

app.Run();


