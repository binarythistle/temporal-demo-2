using marketplacer.Dtos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("SqlLiteConnection")));
builder.Services.AddHttpClient();


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/sellers", async (SellerCreateDto dto, AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration) =>
{
    var seller = new Seller
    {
        SellerName = dto.SellerName,
        SellerDomain = dto.SellerDomain,
        SellerIndustry = dto.SellerIndustry,
        SellerPhone = dto.SellerPhone,
        HubSpotId = dto.HubSpotId
    };

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
            WebhookBody: $"{{\"sellerId\": {seller.Id}, \"sellerName\": \"{seller.SellerName}\"}}",
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

    return Results.Created($"/api/sellers/{seller.Id}", new SellerReadDto(
        seller.Id,
        seller.SellerName,
        seller.SellerDomain,
        seller.SellerIndustry,
        seller.SellerPhone,
        seller.HubSpotId
    ));
});

app.MapGet("/api/sellers/{id}", async (int id, AppDbContext db) =>
{
    var seller = await db.Sellers.FindAsync(id);

    return seller is null
        ? Results.NotFound()
        : Results.Ok(new SellerReadDto(
            seller.Id,
            seller.SellerName,
            seller.SellerDomain,
            seller.SellerIndustry,
            seller.SellerPhone,
            seller.HubSpotId
        ));
});

app.MapGet("/api/sellers", async (AppDbContext db) =>
{
    var sellers = await db.Sellers
        .Select(s => new SellerReadDto(
            s.Id,
            s.SellerName,
            s.SellerDomain,
            s.SellerIndustry,
            s.SellerPhone,
            s.HubSpotId
        ))
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

    if (dto.SellerName is not null)
    {
        seller.SellerName = dto.SellerName;
    }
    if (dto.SellerDomain is not null)
    {
        seller.SellerDomain = dto.SellerDomain;
    }
    if (dto.SellerIndustry is not null)
    {
        seller.SellerIndustry = dto.SellerIndustry;
    }
    if (dto.SellerPhone is not null)
    {
        seller.SellerPhone = dto.SellerPhone;
    }
    if (dto.HubSpotId is not null)
    {
        seller.HubSpotId = dto.HubSpotId;
    }

    await db.SaveChangesAsync();

    return Results.Ok(new SellerReadDto(
        seller.Id,
        seller.SellerName,
        seller.SellerDomain,
        seller.SellerIndustry,
        seller.SellerPhone,
        seller.HubSpotId
    ));
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
        .Select(w => new WebhookEventReadDto(
            w.Id,
            w.IdempotencyKey,
            w.WebhookId,
            w.WebhookBody,
            w.WebhookHeaders,
            w.WebhookObjectId,
            w.WebhookObjectType,
            w.WebhookEventType,
            w.CreatedAt
        ))
        .ToListAsync();

    return Results.Ok(webhooks);
});

app.Run();

