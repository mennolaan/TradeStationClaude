using TradeStation.Models.Common;
namespace TradeStation.Interfaces;

public interface IToolHandler
{
    Task<IEnumerable<ToolResponse>> HandleToolCallAsync(string name, Dictionary<string, object> arguments, CancellationToken cancellationToken = default);
}