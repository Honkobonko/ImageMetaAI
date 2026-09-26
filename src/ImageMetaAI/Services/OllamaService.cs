namespace ImageMetaAI.Services;

public class OllamaService : IOllamaService
{
    private const string RequiredVisionModel = "qwen2.5vl:7b";
    private const string RequiredMetadataModel = "gemma4:26b";

    private readonly IOllamaClient _ollamaClient;

    public OllamaService(IOllamaClient ollamaClient)
    {
        _ollamaClient = ollamaClient;
    }

    public async Task<bool> IsReadyAsync(
        CancellationToken cancellationToken = default)
    {
        if (!await _ollamaClient.IsAvailableAsync(cancellationToken))
        {
            return false;
        }

        var models = await _ollamaClient.GetModelsAsync(
            cancellationToken);

        var modelNames = models
            .Select(model => model.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return modelNames.Contains(RequiredVisionModel)
            && modelNames.Contains(RequiredMetadataModel);
    }
}
