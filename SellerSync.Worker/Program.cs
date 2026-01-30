using System.Net.Http.Headers;
using Temporalio.Extensions.Hosting;
using SellerSync.Workflows;

var builder = Host.CreateApplicationBuilder(args);

// =============================================================================
// HTTP Client Configuration for Activities
//
// HttpClientFactory manages HTTP connections efficiently and allows us to
// configure different clients for different APIs (HubSpot, Marketplacer).
// =============================================================================

// HubSpot API client - includes Bearer token authentication
builder.Services.AddHttpClient("HubSpot", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var token = config["HubSpotToken"]; // From user-secrets or appsettings

    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

// Marketplacer API client
builder.Services.AddHttpClient("Marketplacer", client =>
{
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

// =============================================================================
// TEMPORAL WORKER REGISTRATION
//
// A Worker is a process that:
// 1. Connects to the Temporal server
// 2. Polls a Task Queue for work (workflows and activities to execute)
// 3. Executes the workflows/activities and reports results back
//
// This worker has no API endpoints - it only processes Temporal tasks.
// =============================================================================

var temporalAddress = builder.Configuration["Temporal:Address"] ?? "localhost:7233";
var temporalNamespace = builder.Configuration["Temporal:Namespace"] ?? "default";
var taskQueue = builder.Configuration["Temporal:TaskQueue"] ?? SellerCreatedWorkflow.TaskQueue;

builder.Services
    .AddHostedTemporalWorker(
        clientTargetHost: temporalAddress,
        clientNamespace: temporalNamespace,
        taskQueue: taskQueue)
    // Register activities - Temporal will resolve dependencies via DI
    .AddScopedActivities<PingActivities>()
    .AddScopedActivities<HubSpotActivities>()
    // Register workflows
    .AddWorkflow<PingWorkflow>()
    .AddWorkflow<SellerCreatedWorkflow>();

var host = builder.Build();

// Debug: Check if HubSpot token is loaded
var hubSpotToken = builder.Configuration["HubSpotToken"];
Console.WriteLine($"SellerSync Worker starting...");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"HubSpotToken: {(string.IsNullOrEmpty(hubSpotToken) ? "NOT SET" : $"SET ({hubSpotToken.Length} chars)")}");
Console.WriteLine($"Temporal: {temporalAddress} / {temporalNamespace}");
Console.WriteLine($"Task Queue: {taskQueue}");

await host.RunAsync();
