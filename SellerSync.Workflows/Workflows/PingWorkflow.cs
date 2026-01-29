using Temporalio.Workflows;
using SellerSync.Workflows.Activities;

namespace SellerSync.Workflows.Workflows;

/// <summary>
/// A simple workflow to verify Temporal is working correctly.
///
/// TEMPORAL CONCEPT: Workflows orchestrate the execution of Activities.
/// - Workflows must be DETERMINISTIC (no I/O, no random, no DateTime.Now)
/// - All "real work" happens in Activities
/// - Workflows survive process crashes - they resume from where they left off
/// </summary>
[Workflow]
public class PingWorkflow
{
    /// <summary>
    /// The task queue this workflow listens on.
    /// Both the workflow starter and worker must use the same task queue.
    /// </summary>
    public const string TaskQueue = "seller-sync-tasks";

    /// <summary>
    /// The entry point for this workflow.
    /// </summary>
    /// <param name="input">A simple string input to echo back</param>
    /// <returns>The response from the ping activity</returns>
    [WorkflowRun]
    public async Task<string> RunAsync(string input)
    {
        // TEMPORAL CONCEPT: ExecuteActivityAsync
        // - Schedules the activity on the task queue
        // - Waits for a worker to pick it up and execute it
        // - Returns the result (or throws if the activity fails after retries)
        //
        // ActivityOptions:
        // - StartToCloseTimeout: Max time for a single activity attempt
        // - RetryPolicy: How to handle failures (we'll add this in later phases)

        var result = await Workflow.ExecuteActivityAsync(
            (PingActivities act) => act.Ping(input),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(30)
            });

        return result;
    }
}
