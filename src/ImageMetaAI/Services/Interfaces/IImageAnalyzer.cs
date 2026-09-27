using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public interface IImageAnalyzer
{
    Task<string> AnalyzeAsync(
        string imagePath,
        CancellationToken cancellationToken = default);
}
