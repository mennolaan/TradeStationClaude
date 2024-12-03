using TradeStation.Models.Common;

namespace TradeStation.Extensions;

public static class ToolExtensions
{
    public static bool IsValidToolName(this string name, string prefix)
    {
        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}

public static class ToolResponseFactory
{
    public static ToolResponse CreateTextResponse(object content)
    {
        return new ToolResponse
        {
            Type = "text",
            Text = System.Text.Json.JsonSerializer.Serialize(content)
        };
    }
}
