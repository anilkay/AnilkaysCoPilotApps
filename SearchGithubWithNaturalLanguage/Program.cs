using GitHub.Copilot.SDK;
using Refractored.GitHub.Copilot.SDK.Helpers;
using System.Text.Json;
using System.Text;

var model = await ModelSelector.SelectModelAsync();
await using var client = new CopilotClient();

Console.WriteLine("GitHub Search - Natural Language Search");
Console.Write("Describe what you want to search for on GitHub: ");
var userInput = Console.ReadLine();

if (string.IsNullOrWhiteSpace(userInput))
{
    Console.WriteLine("Error: Please enter a valid search query.");
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
    if (!string.IsNullOrWhiteSpace(jsonContent))
    {
        var repositories = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonContent);
        
        if (repositories != null && repositories.Count > 0)
        {
            var csvFileName = $"github_search_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            using (var writer = new StreamWriter(csvFileName, false, Encoding.UTF8))
            {
                // Write header
                writer.WriteLine("repoName,repolang,repo_address");
                
                // Write data rows
                foreach (var repo in repositories)
                {
                    var repoName = repo.ContainsKey("repoName") ? EscapeCsv(repo["repoName"]) : "";
                    var repolang = repo.ContainsKey("repolang") ? EscapeCsv(repo["repolang"]) : "";
                    var repoAddress = repo.ContainsKey("repo_address") ? EscapeCsv(repo["repo_address"]) : "";
                    
                    writer.WriteLine($"{repoName},{repolang},{repoAddress}");
                }
            }
            
            Console.WriteLine($"\nResults written to CSV file: {csvFileName}");
        }
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

static string EscapeCsv(string value)
{
    if (string.IsNullOrEmpty(value))
        return "\"\"";
    
    if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
    
    return value;
}
