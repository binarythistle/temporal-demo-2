

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("SqlLiteConnection")));

builder.Services.AddMapster();

// Configure HttpClient for HubSpot
builder.Services.AddHttpClient("HubSpot", client =>
{
    var token = builder.Configuration["HubSpotToken"];
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/webhooks", async (WebhookEventCreateDto createDto, AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration) =>
{
    var webhookEvent = createDto.Adapt<WebhookEvent>();
    webhookEvent.CreatedAt = DateTime.UtcNow;

    db.WebhookEvents.Add(webhookEvent);
    await db.SaveChangesAsync();

    // Call HubSpot API with hard-coded values
    var hubSpotDto = new HubSpotCompanyCreateDto
    {
        Properties = new Properties
        {
            Name = "Test Company Name",
            Domain = "testcompany.com",
            Industry = "RETAIL",
            Phone = "555-123-4567"
        }
    };

    var httpClient = httpClientFactory.CreateClient("HubSpot");
    var hubSpotEndpoint = configuration["HubSpotEndpoint"];
    var jsonContent = JsonSerializer.Serialize(hubSpotDto);
    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync(hubSpotEndpoint, content);
    
    // Log response for debugging
    var responseContent = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"HubSpot API Response: {response.StatusCode} - {responseContent}");

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


