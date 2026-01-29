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
/// 2. (Phase 3) Update the seller in Marketplacer with the HubSpot ID
///
/// Key benefits over the original synchronous approach:
/// - If HubSpot is down, Temporal retries automatically with exponential backoff
/// - If the process crashes mid-workflow, it resumes from where it left off
/// - Full visibility into execution state via Temporal UI
/// - Idempotency via workflow ID (using the webhook's idempotency key)
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
        // - MaximumAttempts: Total attempts before giving up (5)
        //
        // With these settings, retry intervals would be: 1s, 2s, 4s, 8s, 16s (capped at 30s)

        var retryPolicy = new RetryPolicy
        {
            InitialInterval = TimeSpan.FromSeconds(1),
            MaximumInterval = TimeSpan.FromSeconds(30),
            BackoffCoefficient = 2.0f,
            MaximumAttempts = 5
        };

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
                RetryPolicy = retryPolicy
            });

        // TODO (Phase 3): Add Marketplacer callback activity here
        // The HubSpot result contains the HubSpotObjectId we need to send back

        return new WorkflowResult(
            Success: true,
            HubSpotObjectId: hubSpotResult.HubSpotObjectId,
            Message: $"Successfully created HubSpot company {hubSpotResult.HubSpotObjectId} for seller {input.WebhookObjectId}"
        );
    }
}
