using CopilotApps.Shared;
using GitHub.Copilot.SDK;
using System.Text.Json;

var model = await ModelSelection.SelectModelAsync();
await using var client = new CopilotClient();

Console.WriteLine("GitHub Search - Natural Language Search");
if (!ConsoleInput.TryReadRequired("Describe what you want to search for on GitHub: ", "Error: Please enter a valid search query.", out var userInput))
{
    return;
}

await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = model ?? "gpt-5-mini",
    Streaming = false,
    McpServers = new Dictionary<string, object>
    {
        ["github-search"] = new McpRemoteServerConfig
        {
            Type = "http",
            Url = "https://api.githubcopilot.com/mcp/",
            Tools = ["*"]
        }
    }
});

var jsonExample = @"[
  {
    ""repoName"": ""repository name"",
    ""repolang"": ""primary programming language"",
    ""repo_address"": ""repository URL""
  }
]";

var prompt = $"""
You are a GitHub search assistant. Search GitHub repositories based on the user's natural language query.

Please analyze the following request, call the MCP tool with appropriate parameters, and return the results as a JSON array with the following format for each repository:
{jsonExample}

Only return the JSON array, no additional text.

User's request: {userInput}
""";

var response = await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, TimeSpan.FromMinutes(4));
Console.WriteLine("Search Results:");
Console.WriteLine(response?.Data.Content);

// Parse and write to CSV
try
{
    var jsonContent = response?.Data.Content?.Trim();
    var headers = new[] { "repoName", "repolang", "repo_address" };
    var csvFileName = await CsvWriter.WriteDictionaryCsvFromJsonAsync(jsonContent, headers, "github_search_results");

    if (!string.IsNullOrWhiteSpace(csvFileName))
    {
        Console.WriteLine($"\nResults written to CSV file: {csvFileName}");
    }
}
catch (JsonException ex)
{
    Console.WriteLine($"Error parsing JSON response: {ex.Message}");
}
catch (IOException ex)
{
    Console.WriteLine($"Error writing to CSV file: {ex.Message}");
}
