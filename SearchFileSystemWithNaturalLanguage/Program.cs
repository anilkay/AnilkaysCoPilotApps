//Not work as expected  i'm working on it.
using CopilotApps.Shared;
using GitHub.Copilot.SDK;

// Build configuration
var configuration = AppConfiguration.LoadAppSettings();

var everythingSdkPath = configuration["EverythingSearch:SdkPath"] 
    ?? throw new InvalidOperationException("EverythingSearch:SdkPath is not configured in appsettings.json");

Console.WriteLine("File Search System - Natural Language Search");
Console.WriteLine("=============================================");
if (!ConsoleInput.TryReadRequired("Describe the files you want to search for: ", "Error: Please enter a valid search query.", out var userInput))
{
    return;
}

var prompt = $"""
You are a file search assistant. Analyze the user's natural language request and use the search the file system.
You search entire file system based on the user's natural language query.
Use MCP tool everything-search to perform the search.
User's request: {userInput}
Please analyze this request, call the MCP tool with appropriate parameters, and present the results to the user.
return only the search results in your final answer. result contains file paths only.
""";

await using var client = new CopilotClient();

await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "GPT-5.2",
    Streaming = true,
    ExcludedTools = ["view", "glob", "report_intent", "grep", "powershell", "task", "write_powershell", "create", "update_todo","read_powershell"],
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = "You are an assistant that helps users search for files on their local file system. Use availble MCP server for this."
    },
    McpServers = new Dictionary<string, object>
    {
        ["everything-search"] = new McpLocalServerConfig
        {
            Type = "local",
            Command = "python",
            Args = new List<string> { "-m", "mcp_server_everything_search" },
            Env = new Dictionary<string, string>
            {
                ["EVERYTHING_SDK_PATH"] = everythingSdkPath
            },
            Tools = ["*"]
        }

    }
});

session.On(ev => SessionDebugLogger.EventHandler(ev));

Console.WriteLine("\nSearching...\n");

var response = await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, timeout: TimeSpan.FromMinutes(1));

Console.WriteLine("Search Results:");
Console.WriteLine("===============");
Console.WriteLine(response?.Data.Content);


await session.DisposeAsync();
