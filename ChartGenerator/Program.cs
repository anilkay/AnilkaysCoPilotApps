//Not work as expected i'm working on it.
using CopilotApps.Shared;
using GitHub.Copilot.SDK;


var selectedModel = await ModelSelection.SelectModelAsync();

await using var client = new CopilotClient();

Console.WriteLine("Chart Generator");
if (!ConsoleInput.TryReadRequired("Describe what you want to generate a chart for: ", "Error: Please enter a valid chart description.", out var userInput))
{
    return;
}



await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = selectedModel ?? "gpt-5-mini",
    Streaming = true,
    ExcludedTools = ["view", "glob", "report_intent", "grep"],
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = "You are an assistant that helps users generate charts based on their natural language queries."
    },
    McpServers = new Dictionary<string, object>
    {
        ["chart-generator"] = new McpRemoteServerConfig
        {
            Type = "http",
            Url = "https://chart.mcp.cloudcertainty.com/mcp",
            Tools = ["*"]
        }
    }
});

var prompt = $"""
You are the Chart Generator assistant. Generate charts based on the user's natural language query.
Please analyze the following request, call the MCP tool with appropriate parameters, and return the results as a chart image URL.
User's request: {userInput}
return only the chart image URL, no additional text.
""";

session.On(ev => SessionDebugLogger.EventHandler(ev));


var result = await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, TimeSpan.FromMinutes(4));

Console.WriteLine("Chart Generation Results:");
Console.WriteLine(result?.Data.Content);

var url = result?.Data.Content?.Trim();
if (!string.IsNullOrWhiteSpace(url))
{
    try
    {
        var httpClient = new HttpClient();
        var imageBytes = await httpClient.GetByteArrayAsync(url);
        var fileName = $"generated_chart_{Guid.NewGuid():N}.png";
        await System.IO.File.WriteAllBytesAsync(fileName, imageBytes);
        Console.WriteLine($"\nChart image saved as: {fileName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error downloading chart image: {ex.Message}");
    }
}

