using System.Text.Json.Serialization;

public partial class HubSpotCompanyCreateDto
    {
        [JsonPropertyName("properties")]
        public required Properties Properties { get; set; }
    }

    public partial class Properties
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("domain")]
        public required string Domain { get; set; }

        [JsonPropertyName("industry")]
        public required string Industry { get; set; }

        [JsonPropertyName("phone")]
        public required string Phone { get; set; }
    }