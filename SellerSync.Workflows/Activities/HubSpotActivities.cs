using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Temporalio.Activities;

namespace SellerSync.Workflows;

/// <summary>
/// Activities for interacting with external APIs (HubSpot and Marketplacer).
///
/// TEMPORAL CONCEPT: Activities
/// - Activities are where "real work" happens: API calls, database operations, etc.
/// - Activities CAN fail - Temporal will retry them according to the RetryPolicy
/// - Activities should be idempotent when possible (same input = same result)
/// - If an activity throws an exception, Temporal captures it and can retry
/// </summary>
public class HubSpotActivities
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HubSpotActivities> _logger;

    public HubSpotActivities(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HubSpotActivities> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Creates a company in HubSpot CRM.
    ///
    /// This activity:
    /// 1. Parses seller data from the WebhookBody JSON string
    /// 2. Transforms it into HubSpot's expected format
    /// 3. Calls the HubSpot API
    /// 4. Returns the created company's HubSpot ID
    ///
    /// If the API call fails, the exception propagates up and Temporal will retry
    /// according to the RetryPolicy defined in the workflow.
    /// </summary>
    [Activity]
    public async Task<HubSpotApiResult> CreateCompanyAsync(SellerCreatedWorkflowInput input)
    {
        _logger.LogInformation(
            "Creating HubSpot company for webhook {WebhookId}, seller {SellerId}",
            input.WebhookId, input.WebhookObjectId);

        // Parse the WebhookBody JSON to extract seller data
        // WebhookBody format: {"sellerId": 1, "sellerName": "Acme", "sellerDomain": "acme.com", ...}
        var sellerData = JsonDocument.Parse(input.WebhookBody).RootElement;

        // Build the HubSpot request by extracting data from WebhookBody
        var hubSpotRequest = new HubSpotCompanyCreateRequest
        {
            Properties = new HubSpotCompanyProperties
            {
                Name = GetJsonPropertyOrDefault(sellerData, "sellerName", "Unknown Seller"),
                Domain = GetJsonPropertyOrDefault(sellerData, "sellerDomain", "unknown.com"),
                Industry = GetJsonPropertyOrDefault(sellerData, "sellerIndustry", "RETAIL"),
                Phone = GetJsonPropertyOrDefault(sellerData, "sellerPhone", "555-000-0000")
            }
        };

        _logger.LogInformation(
            "Sending to HubSpot: Name={Name}, Domain={Domain}",
            hubSpotRequest.Properties.Name, hubSpotRequest.Properties.Domain);

        // Get the configured HubSpot client (has Bearer token in header)
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        var hubSpotEndpoint = _configuration["HubSpot:Endpoint"]
            ?? "https://api.hubapi.com/crm/v3/objects/companies";

        // Make the API call
        var response = await httpClient.PostAsJsonAsync(hubSpotEndpoint, hubSpotRequest);
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogInformation(
            "HubSpot API response: {StatusCode} - {Response}",
            response.StatusCode, responseContent);

        // If the call failed, throw an exception so Temporal can retry
        // TEMPORAL CONCEPT: Throwing exceptions in activities
        // When an activity throws, Temporal catches it and can retry based on RetryPolicy
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"HubSpot API returned {response.StatusCode}: {responseContent}");
        }

        // Parse the response to get the HubSpot object ID
        var responseJson = JsonDocument.Parse(responseContent);
        var hubSpotObjectId = responseJson.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("HubSpot response missing 'id' field");

        _logger.LogInformation(
            "Successfully created HubSpot company {HubSpotId} for seller {SellerId}",
            hubSpotObjectId, input.WebhookObjectId);

        return new HubSpotApiResult(
            HubSpotObjectId: hubSpotObjectId,
            StatusCode: (int)response.StatusCode
        );
    }

    /// <summary>
    /// Updates a seller in Marketplacer with the HubSpot company ID.
    ///
    /// This activity:
    /// 1. Calls Marketplacer's PUT /api/sellers/{id} endpoint
    /// 2. Sends only the HubSpotId field (partial update)
    ///
    /// TEMPORAL CONCEPT: Saga Pattern
    /// This is the second step in our "saga". If this fails after HubSpot succeeded,
    /// Temporal will keep retrying until it succeeds or exhausts retries.
    /// The workflow state (including the HubSpot ID) is preserved across retries.
    /// </summary>
    [Activity]
    public async Task UpdateSellerInMarketplacerAsync(string sellerId, string hubSpotId)
    {
        _logger.LogInformation(
            "Updating seller {SellerId} in Marketplacer with HubSpot ID {HubSpotId}",
            sellerId, hubSpotId);

        var httpClient = _httpClientFactory.CreateClient("Marketplacer");
        var marketplacerEndpoint = _configuration["Marketplacer:Endpoint"]
            ?? "http://localhost:5027/api/sellers";

        var updateUrl = $"{marketplacerEndpoint}/{sellerId}";

        // Marketplacer accepts partial updates - we only send the HubSpotId
        var updatePayload = new { HubSpotId = hubSpotId };

        var response = await httpClient.PutAsJsonAsync(updateUrl, updatePayload);
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogInformation(
            "Marketplacer API response: {StatusCode} - {Response}",
            response.StatusCode, responseContent);

        // If the call failed, throw so Temporal can retry
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Marketplacer API returned {response.StatusCode}: {responseContent}");
        }

        _logger.LogInformation(
            "Successfully updated seller {SellerId} with HubSpot ID {HubSpotId}",
            sellerId, hubSpotId);
    }

    /// <summary>
    /// Helper to safely extract a property from JSON, with a default value.
    /// </summary>
    private static string GetJsonPropertyOrDefault(JsonElement element, string propertyName, string defaultValue)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            return prop.GetString() ?? defaultValue;
        }
        return defaultValue;
    }
}
