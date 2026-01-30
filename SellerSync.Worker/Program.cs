using System.Net.Http.Headers;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using SellerSync.Workflows;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// HTTP Client Configuration
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

// Marketplacer API client (for Phase 3)
builder.Services.AddHttpClient("Marketplacer", client =>
{
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

// =============================================================================
// TEMPORAL CONCEPT: Worker Registration
//
// A Worker is a process that:
// 1. Connects to the Temporal server
// 2. Polls a Task Queue for work (workflows and activities to execute)
// 3. Executes the workflows/activities and reports results back
//
// AddHostedTemporalWorker sets this up as a background service that runs
// alongside our ASP.NET web application.
// =============================================================================

// Add Temporal worker as a hosted service
builder.Services
    .AddHostedTemporalWorker(
        clientTargetHost: "localhost:7233",
        clientNamespace: "default",
        taskQueue: SellerCreatedWorkflow.TaskQueue)
    // Register activities - Temporal will resolve dependencies via DI
    .AddScopedActivities<PingActivities>()
    .AddScopedActivities<HubSpotActivities>()
    // Register workflows
    .AddWorkflow<PingWorkflow>()
    .AddWorkflow<SellerCreatedWorkflow>();

// Temporal client for starting workflows from API endpoints
builder.Services.AddSingleton(sp =>
{
    return TemporalClient.ConnectAsync(
        new TemporalClientConnectOptions("localhost:7233")
    ).GetAwaiter().GetResult();
});

var app = builder.Build();

// =============================================================================
// API Endpoints
// =============================================================================

app.MapGet("/", () => "SellerSync Worker is running. Endpoints: GET /api/ping, POST /api/webhooks");

// Test endpoint (Phase 1)
app.MapGet("/api/ping", async (TemporalClient client, string? input) =>
{
    input ??= "world";

    var result = await client.ExecuteWorkflowAsync(
        (PingWorkflow wf) => wf.RunAsync(input),
        new WorkflowOptions(
            id: $"ping-{Guid.NewGuid()}",
            taskQueue: PingWorkflow.TaskQueue
        ));

    return Results.Ok(new { input, result });
});

// =============================================================================
// Webhook Endpoint (Phase 2)
//
// This endpoint receives webhooks from Marketplacer when a seller is created.
// Instead of processing synchronously (like HubSpotService does), we:
// 1. Start a Temporal workflow
// 2. Return immediately with 202 Accepted
// 3. The workflow runs durably in the background
//
// TEMPORAL CONCEPT: Workflow ID as Idempotency Key
// By using the webhook's idempotency key as the workflow ID, we get
// exactly-once processing for free. If the same webhook is sent twice,
// the second attempt will return the existing workflow instead of creating
// a duplicate.
// =============================================================================

app.MapPost("/api/webhooks", async (TemporalClient client, SellerCreatedWorkflowInput input) =>
{
    // Use idempotency key as workflow ID - this ensures exactly-once processing!
    // If a workflow with this ID already exists, Temporal won't create a duplicate.
    var workflowId = $"seller-created-{input.IdempotencyKey}";

    var handle = await client.StartWorkflowAsync(
        (SellerCreatedWorkflow wf) => wf.RunAsync(input),
        new WorkflowOptions(
            id: workflowId,
            taskQueue: SellerCreatedWorkflow.TaskQueue
        ));

    // Return immediately - the workflow runs in the background
    return Results.Accepted($"/api/workflows/{workflowId}", new
    {
        workflowId = handle.Id,
        message = "Workflow started. Check Temporal UI for progress.",
        temporalUi = $"http://localhost:8233/namespaces/default/workflows/{handle.Id}"
    });
});

// Get workflow status (useful for checking progress)
app.MapGet("/api/workflows/{workflowId}", async (TemporalClient client, string workflowId) =>
{
    try
    {
        var handle = client.GetWorkflowHandle(workflowId);
        var description = await handle.DescribeAsync();

        return Results.Ok(new
        {
            workflowId = description.Id,
            status = description.Status.ToString(),
            workflowType = description.WorkflowType,
            startTime = description.StartTime,
            closeTime = description.CloseTime,
            temporalUi = $"http://localhost:8233/namespaces/default/workflows/{workflowId}"
        });
    }
    catch (Exception ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.Run();
