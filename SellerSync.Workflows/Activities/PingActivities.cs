using Temporalio.Activities;

namespace SellerSync.Workflows;

/// <summary>
/// Simple activity for testing Temporal connectivity.
/// Activities are where the actual work happens - they can do I/O, call APIs, etc.
/// </summary>
public class PingActivities
{
    /// <summary>
    /// A simple activity that returns "pong" - used to verify the Temporal setup is working.
    /// </summary>
    [Activity]
    public string Ping(string input)
    {
        // Activities can contain any code: API calls, database operations, etc.
        // For now, just return a simple response.
        return $"pong: {input}";
    }
}
