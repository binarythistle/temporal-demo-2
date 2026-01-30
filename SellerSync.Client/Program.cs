using Temporalio.Client;
using SellerSync.Workflows;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// TEMPORAL CLIENT
//
// The Client project uses TemporalClient to start and query workflows.
// It does NOT run a worker - that's the Worker project's job.
// =============================================================================

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var temporalAddress = config["Temporal:Address"] ?? "localhost:7233";

    return TemporalClient.ConnectAsync(
        new TemporalClientConnectOptions(temporalAddress)
    ).GetAwaiter().GetResult();
});

var app = builder.Build();

// =============================================================================
// API Endpoints
// =============================================================================

app.MapGet("/", () => "SellerSync Client is running. Endpoints: GET /api/ping, POST /api/webhooks, GET /api/workflows/{id}");

// Test endpoint (Phase 1)
app.MapGet("/api/ping", async (TemporalClient client, IConfiguration config, string? input) =>
{
    input ??= "world";
    var taskQueue = config["Temporal:TaskQueue"] ?? SellerCreatedWorkflow.TaskQueue;

    var result = await client.ExecuteWorkflowAsync(
        (PingWorkflow wf) => wf.RunAsync(input),
        new WorkflowOptions(
            id: $"ping-{Guid.NewGuid()}",
            taskQueue: taskQueue
        ));

    return Results.Ok(new { input, result });
});

// =============================================================================
// Webhook Endpoint
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

app.MapPost("/api/webhooks", async (TemporalClient client, IConfiguration config, SellerCreatedWorkflowInput input) =>
{
    var taskQueue = config["Temporal:TaskQueue"] ?? SellerCreatedWorkflow.TaskQueue;

    // Use idempotency key as workflow ID - this ensures exactly-once processing!
    // If a workflow with this ID already exists, Temporal won't create a duplicate.
    var workflowId = $"seller-created-{input.IdempotencyKey}";

    var handle = await client.StartWorkflowAsync(
        (SellerCreatedWorkflow wf) => wf.RunAsync(input),
        new WorkflowOptions(
            id: workflowId,
            taskQueue: taskQueue
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
