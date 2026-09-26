using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public interface IImageAnalyzer
{
    Task<VisionAnalysis> AnalyzeAsync(
        string imagePath,
        CancellationToken cancellationToken = default);
}
