using System.Text.Json.Serialization;

namespace CopilotApps.Shared;

public class EntityTemplate
{
    [JsonPropertyName("tableName")]
    public string? TableName { get; set; }

    [JsonPropertyName("entityContent")]
    public string? EntityContent { get; set; }
}
