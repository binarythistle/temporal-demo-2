

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HubSpotService.Services;
using Mapster;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("SqlLiteConnection")));

builder.Services.AddMapster();


builder.Services.AddScoped<HubSpotCompanyService>();

// Configure HttpClient for HubSpot
builder.Services.AddHttpClient("HubSpot", client =>
{
    var token = builder.Configuration["HubSpotToken"];
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Configure HttpClient for Marketplacer
builder.Services.AddHttpClient("Marketplacer", client =>
{
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/webhooks", async (WebhookEventCreateDto createDto, AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration, HubSpotCompanyService hubSpotService) =>
{
    var webhookEvent = createDto.Adapt<WebhookEvent>();
    webhookEvent.CreatedAt = DateTime.UtcNow;

    db.WebhookEvents.Add(webhookEvent);
    await db.SaveChangesAsync();

    // Create HubSpot company DTO from webhook body
    var hubSpotDto = hubSpotService.CreateCompanyDto(createDto.WebhookBody);

    var httpClient = httpClientFactory.CreateClient("HubSpot");
    var hubSpotEndpoint = configuration["HubSpotEndpoint"];
    var jsonContent = JsonSerializer.Serialize(hubSpotDto);
    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync(hubSpotEndpoint, content);
    
    // Parse HubSpot response and save to database
    var responseContent = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"HubSpot API Response: {response.StatusCode} - {responseContent}");

    // Extract hs_object_id from response
    var responseJson = JsonDocument.Parse(responseContent);
    var hubSpotObjectId = responseJson.RootElement.GetProperty("id").GetString();

    // Create and save HubSpot response record
    var hubSpotResponse = new HubSpotResponse
    {
        WebhookEventId = webhookEvent.Id,
        HubSpotObjectId = hubSpotObjectId ?? "unknown",
        ResponseStatusCode = (int)response.StatusCode,
        ResponseBody = responseContent,
        CreatedAt = DateTime.UtcNow
    };

    db.HubSpotResponses.Add(hubSpotResponse);
    await db.SaveChangesAsync();

    // Call back to originating system (Marketplacer) with HubSpot ID
    var marketplacerEndpoint = configuration["MarketplacerEndpont"];
    var sellerId = createDto.WebhookObjectId;
    var updateUrl = $"{marketplacerEndpoint}/{sellerId}";
    
    var updateDto = new UpdateSellerHubSpotIdDto(hubSpotObjectId ?? "unknown");
    var updateJsonContent = JsonSerializer.Serialize(updateDto);
    var updateContent = new StringContent(updateJsonContent, Encoding.UTF8, "application/json");
    
    var marketplacerClient = httpClientFactory.CreateClient("Marketplacer");
    var updateResponse = await marketplacerClient.PutAsync(updateUrl, updateContent);
    var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"Marketplacer PUT Response: {updateResponse.StatusCode} - {updateResponseContent}");

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

app.MapGet("/api/hubspot-responses", async (AppDbContext db) =>
{
    var responses = await db.HubSpotResponses
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();
    var readDtos = responses.Adapt<List<HubSpotResponseReadDto>>();

    return Results.Ok(readDtos);
});

app.MapGet("/api/hubspot-responses/webhook/{webhookEventId}", async (int webhookEventId, AppDbContext db) =>
{
    var response = await db.HubSpotResponses
        .FirstOrDefaultAsync(r => r.WebhookEventId == webhookEventId);

    if (response == null)
        return Results.NotFound(new { message = $"No HubSpot response found for webhook event ID {webhookEventId}" });

    var readDto = response.Adapt<HubSpotResponseReadDto>();
    return Results.Ok(readDto);
});

app.Run();


