using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public interface IOllamaClient
{
    Task<bool> IsAvailableAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OllamaModel>> GetModelsAsync(
        CancellationToken cancellationToken = default);
}
