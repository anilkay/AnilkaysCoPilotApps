// Program entry for AI-based commit message generator
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Linq;

var modelOption = new Option<string>("--model")
{
    Description = "AI model to use (e.g., gpt-5-mini)"
};

var workdirOption = new Option<string>("--workdir")
{
    Description = "Git working directory to diff"
};

var rootCommand = new RootCommand("AI-based commit message generator")
{
    modelOption,
    workdirOption
};

var parseResult = rootCommand.Parse(args);
if (parseResult.Errors.Count > 0)
{
    foreach (var error in parseResult.Errors)
    {
        Console.Error.WriteLine(error.Message);
    }
    return 1;
}

var optionResults = parseResult.CommandResult.Children.OfType<OptionResult>().ToList();
var model = optionResults.FirstOrDefault(r => r.Option == modelOption)?.GetValueOrDefault<string>();
var workingDir = optionResults.FirstOrDefault(r => r.Option == workdirOption)?.GetValueOrDefault<string>();

if (string.IsNullOrWhiteSpace(model))
{
    Console.Error.WriteLine("--model is required.");
    return 1;
}

if (string.IsNullOrWhiteSpace(workingDir))
{
    workingDir = Directory.GetCurrentDirectory();
}

var getDiffs = AIFunctionFactory.Create(
    async () =>
    {
        var diffs = new List<string>();
        var stagedDiff = await GetStagedDiffAsync(workingDir);
        if (!string.IsNullOrWhiteSpace(stagedDiff))
        {
            diffs.Add(stagedDiff);
        }
        return diffs;
    },
    "get_diff",
    "Get diff from git"
    );

var diff = await GetStagedDiffAsync(workingDir);
if (string.IsNullOrWhiteSpace(diff))
{
    Console.Error.WriteLine("No staged changes found. Stage files before generating a commit message.");
    return 1;
}

var prompt = $"""
You are a senior engineer who writes excellent Git commit messages.

Summarize the staged changes below into a single commit message:
- Use present-tense, imperative mood.
- Keep the subject under 72 characters.
- Add bullet points in the body only if needed for clarity.
- Do not include code fences or extra prose.
Use get_diff tool to get the diff of the staged changes.
""";

await using var client = new CopilotClient();
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = model,
    Streaming = false,
    Tools = [getDiffs]
});

var response = await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt });
if (response is null)
{
    Console.Error.WriteLine("No response from AI model.");
    return 1;
}

Console.WriteLine(response.Data.Content.Trim());
return 0;

static async Task<string> GetStagedDiffAsync(string workingDirectory)
{
    var diff = await RunGitAsync("diff --cached --unified=3", workingDirectory);
    if (string.IsNullOrWhiteSpace(diff))
    {
        diff = await RunGitAsync("diff --unified=3", workingDirectory);
    }
    return diff ?? string.Empty;
}

static async Task<string> RunGitAsync(string arguments, string workingDirectory)
{
    try
    {
        var startInfo = new ProcessStartInfo("git", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return string.Empty;
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine($"git {arguments} failed: {error}".Trim());
            return string.Empty;
        }

        return output;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error running git: {ex.Message}");
        return string.Empty;
    }
}
