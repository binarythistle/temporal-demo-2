using System.Text.Json;

namespace HubSpotService.Services;

public class HubSpotCompanyService
{
    public HubSpotCompanyCreateDto CreateCompanyDto(string webhookBody)
    {
        var root = JsonDocument.Parse(webhookBody).RootElement;

        return new HubSpotCompanyCreateDto
        {
            Properties = new Properties
            {
                Name = root.GetProperty("sellerName").GetString() ?? "Unknown Seller",
                Domain = TryGetPropertyValue(root, "sellerDomain", "testcompany.com"),
                Industry = TryGetPropertyValue(root, "sellerIndustry", "RETAIL"),
                Phone = TryGetPropertyValue(root, "sellerPhone", "555-123-4567")
            }
        };
    }

    private static string TryGetPropertyValue(JsonElement element, string propertyName, string defaultValue)
    {
        return element.TryGetProperty(propertyName, out var prop) 
            ? prop.GetString() ?? defaultValue 
            : defaultValue;
    }
}
