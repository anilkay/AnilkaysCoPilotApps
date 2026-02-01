using GitHub.Copilot.SDK;

namespace CopilotApps.Shared;

/// <summary>
/// Provides debug logging functionality for Copilot SDK session events.
/// </summary>
public static class SessionDebugLogger
{
    /// <summary>
    /// Gets the event handler action for logging session events to the console.
    /// </summary>
    public static Action<object> EventHandler => ev =>
    {
        if (ev is AssistantMessageDeltaEvent deltaEvent)
        {
            Console.Write(deltaEvent.Data.DeltaContent.ToString());
        }
        else if (ev is SessionIdleEvent)
        {
            Console.WriteLine();
        }
        else if (ev is ToolExecutionStartEvent toolExecutionStartEvent)
        {
            Console.Write($"Tool Execution Started: {toolExecutionStartEvent.Data.ToolName}");
            Console.WriteLine($" Parameters: {toolExecutionStartEvent.Data.Arguments}");
        }
        else if (ev is ToolExecutionCompleteEvent toolExecutionCompleteEvent)
        {
            Console.Write($"Tool Execution Completed: {toolExecutionCompleteEvent.Data.Success}");
            Console.WriteLine($" Result: {toolExecutionCompleteEvent.Data.Result?.Content}");
        }
        else if (ev is PendingMessagesModifiedEvent messagesModifiedEvent)
        {
            Console.WriteLine($"Pending Messages Modified. Total Pending Messages: {messagesModifiedEvent.Data}");
        }
        else if (ev is AssistantReasoningDeltaEvent assistantReasoningDeltaEvent)
        {
            Console.WriteLine($"Reasoning: {assistantReasoningDeltaEvent.Data.DeltaContent.ToString().Trim()}");
        }
        else
        {
            Console.WriteLine($"\nEvent: {ev.GetType().Name}");
        }
    };
}
