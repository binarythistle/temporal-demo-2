using Temporalio.Workflows;
using Temporalio.Common;
using SellerSync.Contracts;
using SellerSync.Workflows.Activities;

namespace SellerSync.Workflows.Workflows;

/// <summary>
/// Workflow that handles the seller.created event from Marketplacer.
///
/// TEMPORAL CONCEPT: Workflow Orchestration
/// This workflow orchestrates a multi-step process:
/// 1. Create a company in HubSpot
/// 2. Update the seller in Marketplacer with the HubSpot ID
///
/// Key benefits over the original synchronous approach:
/// - If HubSpot is down, Temporal retries automatically with exponential backoff
/// - If the process crashes mid-workflow, it resumes from where it left off
/// - Full visibility into execution state via Temporal UI
/// - Idempotency via workflow ID (using the webhook's idempotency key)
///
/// TEMPORAL CONCEPT: Saga Pattern
/// This workflow implements a simple saga: HubSpot → Marketplacer
/// If Marketplacer fails after HubSpot succeeds, Temporal preserves the HubSpot ID
/// and keeps retrying the Marketplacer call. No data is lost!
/// </summary>
[Workflow]
public class SellerCreatedWorkflow
{
    /// <summary>
    /// The task queue this workflow and its activities run on.
    /// </summary>
    public const string TaskQueue = "seller-sync-tasks";

    /// <summary>
    /// Executes the seller creation workflow.
    /// </summary>
    [WorkflowRun]
    public async Task<WorkflowResult> RunAsync(SellerCreatedWorkflowInput input)
    {
        // TEMPORAL CONCEPT: RetryPolicy
        // Defines how Temporal should handle activity failures:
        // - InitialInterval: Wait time before first retry (1 second)
        // - MaximumInterval: Cap on wait time between retries (30 seconds)
        // - BackoffCoefficient: Multiplier for each subsequent retry (2x)
        // - MaximumAttempts: Total attempts before giving up (0 = infinite)

        // HubSpot: External API we don't control - bounded retries
        var hubSpotRetryPolicy = new RetryPolicy
        {
            InitialInterval = TimeSpan.FromSeconds(1),
            MaximumInterval = TimeSpan.FromSeconds(30),
            BackoffCoefficient = 2.0f,
            MaximumAttempts = 5
        };

        // Marketplacer: Our service - infinite retries until it's back up
        // MaximumAttempts = 0 means "retry forever"
        var marketplacerRetryPolicy = new RetryPolicy
        {
            InitialInterval = TimeSpan.FromSeconds(1),
            MaximumInterval = TimeSpan.FromSeconds(30),
            BackoffCoefficient = 2.0f,
            MaximumAttempts = 0  // Infinite retries!
        };

        // DEMO DELAY: Give time to kill Marketplacer for chaos testing
        // TEMPORAL CONCEPT: Workflow.DelayAsync is a durable timer
        // Unlike Thread.Sleep or Task.Delay, this timer:
        // - Is tracked in workflow history
        // - Survives worker restarts
        // - Can be seen in the Temporal UI
        await Workflow.DelayAsync(TimeSpan.FromSeconds(10));

        // Step 1: Create company in HubSpot
        // TEMPORAL CONCEPT: Activity Execution
        // - StartToCloseTimeout: Max time for a single attempt (2 minutes)
        // - RetryPolicy: What to do if the activity fails
        //
        // If the activity fails, Temporal will:
        // 1. Record the failure in workflow history
        // 2. Wait according to RetryPolicy
        // 3. Schedule another attempt
        // 4. Repeat until success or MaximumAttempts reached

        var hubSpotResult = await Workflow.ExecuteActivityAsync(
            (HubSpotActivities act) => act.CreateCompanyAsync(input),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromMinutes(2),
                RetryPolicy = hubSpotRetryPolicy
            });

        // Step 2: Update seller in Marketplacer with the HubSpot ID
        // TEMPORAL CONCEPT: Workflow State
        // At this point, hubSpotResult.HubSpotObjectId is stored in workflow history.
        // If the process crashes here and restarts, Temporal will NOT re-run the
        // HubSpot activity - it will replay the result from history and continue.
        //
        // This is the key insight: even if Marketplacer is down for hours,
        // when it comes back up, the workflow will complete successfully
        // with the original HubSpot ID.

        await Workflow.ExecuteActivityAsync(
            (HubSpotActivities act) => act.UpdateSellerInMarketplacerAsync(
                input.WebhookObjectId,
                hubSpotResult.HubSpotObjectId),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromMinutes(2),
                RetryPolicy = marketplacerRetryPolicy  // Infinite retries!
            });

        return new WorkflowResult(
            Success: true,
            HubSpotObjectId: hubSpotResult.HubSpotObjectId,
            Message: $"Successfully created HubSpot company {hubSpotResult.HubSpotObjectId} and updated seller {input.WebhookObjectId}"
        );
    }
}
