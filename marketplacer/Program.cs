using Mapster;
using marketplacer.Dtos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("SqlLiteConnection")));

builder.Services.AddMapster();

builder.Services.AddHttpClient();


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/sellers", async (SellerCreateDto dto, AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration) =>
{
    var seller = dto.Adapt<Seller>();

    db.Sellers.Add(seller);
    await db.SaveChangesAsync();

    // Send webhook to HubSpot service
    var webhookEndpoint = configuration["HubSpotServiceWebhookEndpoint"];
    if (!string.IsNullOrEmpty(webhookEndpoint))
    {
        var httpClient = httpClientFactory.CreateClient();
        var webhookPayload = new WebhookEventCreateDto(
            IdempotencyKey: Guid.NewGuid().ToString(),
            WebhookId: $"webhook-{Guid.NewGuid()}",
            WebhookBody: $"{{\"sellerId\": {seller.Id}, \"sellerName\": \"{seller.SellerName}\", \"sellerDomain\": {(seller.SellerDomain != null ? $"\"{seller.SellerDomain}\"" : "null")}, \"sellerIndustry\": {(seller.SellerIndustry != null ? $"\"{seller.SellerIndustry}\"" : "null")}, \"sellerPhone\": {(seller.SellerPhone != null ? $"\"{seller.SellerPhone}\"" : "null")}}}",
            WebhookHeaders: "{\"Content-Type\": \"application/json\"}",
            WebhookObjectId: seller.Id.ToString(),
            WebhookObjectType: "Seller",
            WebhookEventType: "seller.created",
            SentFromSourceAt: DateTime.UtcNow
        );

        try
        {
            await httpClient.PostAsJsonAsync(webhookEndpoint, webhookPayload);
            
            // Store webhook event in database
            var webhookEvent = new WebhookEvent
            {
                IdempotencyKey = webhookPayload.IdempotencyKey,
                WebhookId = webhookPayload.WebhookId,
                WebhookBody = webhookPayload.WebhookBody,
                WebhookHeaders = webhookPayload.WebhookHeaders,
                WebhookObjectId = webhookPayload.WebhookObjectId,
                WebhookObjectType = webhookPayload.WebhookObjectType,
                WebhookEventType = webhookPayload.WebhookEventType,
                CreatedAt = DateTime.UtcNow
            };
            
            db.WebhookEvents.Add(webhookEvent);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request
            Console.WriteLine($"Failed to send webhook: {ex.Message}");
        }
    }

    return Results.Created($"/api/sellers/{seller.Id}", seller.Adapt<SellerReadDto>());
});

app.MapGet("/api/sellers/{id}", async (int id, AppDbContext db) =>
{
    var seller = await db.Sellers.FindAsync(id);

    return seller is null
        ? Results.NotFound()
        : Results.Ok(seller.Adapt<SellerReadDto>());
});

app.MapGet("/api/sellers", async (AppDbContext db) =>
{
    var sellers = await db.Sellers
        .ProjectToType<SellerReadDto>()
        .ToListAsync();

    return Results.Ok(sellers);
});

app.MapPut("/api/sellers/{id}", async (int id, SellerUpdateDto dto, AppDbContext db) =>
{
    
    var seller = await db.Sellers.FindAsync(id);

    if (seller is null)
    {
        return Results.NotFound();
    }

    // Allows for partial updates
    var config = TypeAdapterConfig.GlobalSettings.Clone();
    config.Default.IgnoreNullValues(true);
    dto.Adapt(seller, config);

    await db.SaveChangesAsync();

    return Results.Ok(seller.Adapt<SellerReadDto>());
});

app.MapDelete("/api/sellers/{id}", async (int id, AppDbContext db) =>
{
    var seller = await db.Sellers.FindAsync(id);

    if (seller is null)
    {
        return Results.NotFound();
    }

    db.Sellers.Remove(seller);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/api/webhooks", async (AppDbContext db) =>
{
    var webhooks = await db.WebhookEvents
        .OrderByDescending(w => w.CreatedAt)
        .ProjectToType<WebhookEventReadDto>()
        .ToListAsync();

    return Results.Ok(webhooks);
});

app.Run();

