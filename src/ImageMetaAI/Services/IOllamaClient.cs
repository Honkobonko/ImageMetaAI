using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public interface IOllamaClient
{
    Task<bool> IsAvailableAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OllamaModel>> GetModelsAsync(
        CancellationToken cancellationToken = default);

    Task<string> GenerateAsync(
        string model,
        string prompt,
        string imageBase64,
        CancellationToken cancellationToken = default);
}
