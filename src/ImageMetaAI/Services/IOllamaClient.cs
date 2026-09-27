using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public interface IOllamaClient
{
    Task<bool> IsAvailableAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OllamaModel>> GetModelsAsync(
        CancellationToken cancellationToken = default);

    Task<string> GenerateVisionAsync(
    string model,
    string prompt,
    string imageBase64,
    CancellationToken cancellationToken = default);

    Task<ImageMetadata> GenerateMetadataAsync(
        string model,
        string prompt,
        CancellationToken cancellationToken = default);
}
