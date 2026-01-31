using Refractored.GitHub.Copilot.SDK.Helpers;

namespace CopilotApps.Shared;

public static class ModelSelection
{
    public static Task<string?> SelectModelAsync()
    {
        return ModelSelector.SelectModelAsync();
    }
}
