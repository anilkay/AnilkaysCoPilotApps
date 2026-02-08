using CopilotApps.Shared;
using GitHub.Copilot.SDK;

var configuration = AppConfiguration.LoadAppSettings(optional: true);

var nixosConfigPath = configuration["Nixos:ConfigurationFilePath"] ?? "";

var outputPath = configuration["Nixos:OutputFilePath"] ?? "";
var createBackup = bool.TryParse(configuration["Nixos:CreateBackup"], out var backup) && backup;

Console.WriteLine("NixOS Config Updater");
Console.WriteLine("===================");

if (string.IsNullOrWhiteSpace(nixosConfigPath))
{
    Console.WriteLine("Missing configuration: Nixos:ConfigurationFilePath (appsettings.json)");
    return;
}

if (!File.Exists(nixosConfigPath))
{
    Console.WriteLine($"Configuration file not found: {nixosConfigPath}");
    return;
}

if (string.IsNullOrWhiteSpace(outputPath))
{
    outputPath = nixosConfigPath;
}

if (createBackup && string.Equals(outputPath, nixosConfigPath, StringComparison.OrdinalIgnoreCase))
{
    var backupPath = nixosConfigPath + ".bak";
    File.Copy(nixosConfigPath, backupPath, overwrite: true);
    Console.WriteLine($"Backup created: {backupPath}");
}

var content = await File.ReadAllTextAsync(nixosConfigPath);

var model = await ModelSelection.SelectModelAsync();

Console.WriteLine();
if (!ConsoleInput.TryReadRequired(
        "Describe the change you want to apply to the NixOS configuration: ",
        "Error: Please enter a valid request.",
        out var userRequest))
{
    return;
}

await using var client = new CopilotClient();
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = model ?? "gpt-5-mini",
    Streaming = true,
    ExcludedTools = ["view", "glob", "report_intent", "grep"],
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = "You are an assistant that updates NixOS configuration files. Return only the full updated Nix file content, no markdown, no explanations."
    }
    //TODO add Skills to this tool because NixOS configuration has specific syntax and structure, it would be good to have a skill that can parse and modify the NixOS configuration file content, so the model can call the skill with specific parameters to get better results.
});

session.On(ev => SessionDebugLogger.EventHandler(ev));

var prompt = $"""
You will be given a NixOS configuration file content. Apply the user's requested change.

Rules:
- Return ONLY the full updated file content.
- Preserve formatting as much as possible.
- If the request is unclear or cannot be applied safely, return the original content unchanged.

User request:
{userRequest}

File path (for context): {nixosConfigPath}

Current file content:
{content}
""";

Console.WriteLine("\nApplying update...\n");
var response = await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, timeout: TimeSpan.FromMinutes(4));

var updatedContent = response?.Data.Content ?? string.Empty;
updatedContent = updatedContent.Replace("\r\n", "\n");
updatedContent = updatedContent.Replace("\n", Environment.NewLine);

if (string.IsNullOrWhiteSpace(updatedContent))
{
    Console.WriteLine("No output produced.");
    return;
}

if (!string.Equals(updatedContent, content, StringComparison.Ordinal))
{
    await File.WriteAllTextAsync(outputPath, updatedContent);
    Console.WriteLine($"Updated: {outputPath}");
}
else
{
    Console.WriteLine("No changes.");
}
