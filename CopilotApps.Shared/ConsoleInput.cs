namespace CopilotApps.Shared;

public static class ConsoleInput
{
    public static bool TryReadRequired(string prompt, string errorMessage, out string value)
    {
        Console.Write(prompt);
        value = Console.ReadLine()?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            Console.WriteLine(errorMessage);
            value = string.Empty;
            return false;
        }

        return true;
    }
}
