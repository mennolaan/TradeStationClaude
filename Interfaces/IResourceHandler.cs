namespace TradeStation.Interfaces;

public interface IResourceHandler
{
    Task<string> HandleResourceCallAsync(Uri uri, CancellationToken cancellationToken = default);
}