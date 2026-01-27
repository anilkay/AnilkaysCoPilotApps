//Not work as expected i don't know why.
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Configuration;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var everythingSdkPath = configuration["EverythingSearch:SdkPath"] 
    ?? throw new InvalidOperationException("EverythingSearch:SdkPath is not configured in appsettings.json");

Console.WriteLine("File Search System - Natural Language Search");
Console.WriteLine("=============================================");
Console.Write("Describe the files you want to search for: ");
var userInput = Console.ReadLine();

if (string.IsNullOrWhiteSpace(userInput))
{
    Console.WriteLine("Error: Please enter a valid search query.");
    return;
}

var jsonExample = """{"query": "*.py", "max_results": 50, "sort_by": 6}""";

var prompt = $"""
You are a file search assistant. Analyze the user's natural language request and use the "everything-search" MCP tool to search the file system.
You search with the Everything Search Engine via the MCP tool.

MCP Tool Parameters:
- query (required): Search query. Example: "*.pdf", "ext:py", "datemodified:today"
- max_results (optional): Maximum number of results (default: 100, max: 1000)
- match_path (optional): Search in full path instead of filename only (default: false)
- match_case (optional): Case-sensitive search (default: false)
- match_whole_word (optional): Match whole words only (default: false)
- match_regex (optional): Enable regex search (default: false)
- sort_by (optional): Sort order (default: 1)
  - 1: Filename (A-Z), 2: Filename (Z-A)
  - 3: Path (A-Z), 4: Path (Z-A)
  - 5: Size (smallest first), 6: Size (largest first)
  - 7: Extension (A-Z), 8: Extension (Z-A)
  - 11: Creation date (oldest first), 12: Creation date (newest first)
  - 13: Modification date (oldest first), 14: Modification date (newest first)

Example Queries:
- "*.py" -> All Python files
- "ext:pdf" -> All PDF files  
- "ext:py datemodified:today" -> Python files modified today

User's request: {userInput}
Example MCP call: {jsonExample}
Please analyze this request, call the MCP tool with appropriate parameters, and present the results to the user.
please call MCP Servers using JSON format.
return only the search results in your final answer. result contains file paths only.
""";

await using var client = new CopilotClient();

await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "GPT-5-mini",
    Streaming = false,
    McpServers = new Dictionary<string, object>
    {
        ["everything-search"] = new McpLocalServerConfig
        {
            Type = "stdio",
            Command = "python",
            Args = new List<string> { "-m", "mcp_server_everything_search" },
            Env = new Dictionary<string, string>
            {
                ["EVERYTHING_SDK_PATH"] = everythingSdkPath
            },
        }

    }
});

session.On(ev =>
{
    if (ev is AssistantMessageDeltaEvent deltaEvent)
    {
        Console.Write(deltaEvent.Data.DeltaContent);
    }
    if (ev is SessionIdleEvent)
    {
        Console.WriteLine();
    }
});

Console.WriteLine("\nSearching...\n");

var response = await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, timeout: TimeSpan.FromMinutes(1));

Console.WriteLine("Search Results:");
Console.WriteLine("===============");
Console.WriteLine(response.Data.Content);
