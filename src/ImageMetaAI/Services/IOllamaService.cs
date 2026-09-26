namespace ImageMetaAI.Services;

public interface IOllamaService
{
    Task<bool> IsReadyAsync(
        CancellationToken cancellationToken = default);
}
