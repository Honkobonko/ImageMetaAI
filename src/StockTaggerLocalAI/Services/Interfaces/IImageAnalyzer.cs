using StockTaggerLocalAI.Models;

namespace StockTaggerLocalAI.Services;

public interface IImageAnalyzer
{
    Task<string> AnalyzeAsync(
        string imagePath,
        CancellationToken cancellationToken = default);
}
