using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using SellerSync.Workflows.Activities;
using SellerSync.Workflows.Workflows;

var builder = WebApplication.CreateBuilder(args);

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

// Register activities - these are instantiated by the worker
builder.Services.AddSingleton<PingActivities>();

// Add Temporal worker as a hosted service
builder.Services
    .AddHostedTemporalWorker(
        clientTargetHost: "localhost:7233",  // Temporal server address
        clientNamespace: "default",           // Temporal namespace
        taskQueue: PingWorkflow.TaskQueue)    // Task queue to poll
    .AddScopedActivities<PingActivities>()    // Register our activities
    .AddWorkflow<PingWorkflow>();             // Register our workflow

// Also add a Temporal client for starting workflows from our API endpoints
builder.Services.AddSingleton(sp =>
{
    return TemporalClient.ConnectAsync(new TemporalClientConnectOptions("localhost:7233")).GetAwaiter().GetResult();
});

var app = builder.Build();

// =============================================================================
// TEMPORAL CONCEPT: Starting a Workflow
//
// To start a workflow, we use TemporalClient.ExecuteWorkflowAsync() or
// TemporalClient.StartWorkflowAsync().
//
// - ExecuteWorkflowAsync: Starts workflow AND waits for result (blocking)
// - StartWorkflowAsync: Starts workflow and returns handle (non-blocking)
//
// Key parameters:
// - Workflow method to call (type-safe via lambda)
// - WorkflowOptions with:
//   - id: Unique identifier for this workflow instance
//   - taskQueue: Must match what the worker is polling
// =============================================================================

app.MapGet("/", () => "SellerSync Worker is running. Try GET /api/ping?input=hello");

app.MapGet("/api/ping", async (TemporalClient client, string? input) =>
{
    input ??= "world";

    // Start the workflow and wait for result
    var result = await client.ExecuteWorkflowAsync(
        (PingWorkflow wf) => wf.RunAsync(input),
        new WorkflowOptions(
            id: $"ping-{Guid.NewGuid()}",     // Unique workflow ID
            taskQueue: PingWorkflow.TaskQueue  // Must match worker's task queue
        ));

    return Results.Ok(new { input, result, workflowId = $"ping-{Guid.NewGuid()}" });
});

app.Run();
