namespace ImageMetaAI.Services;

public class OllamaService : IOllamaService
{
    private readonly OllamaModelSettings _modelSettings;

    private readonly IOllamaClient _ollamaClient;

    public OllamaService(IOllamaClient ollamaClient, OllamaModelSettings modelSettings)
    {
        _ollamaClient = ollamaClient;
        _modelSettings = modelSettings;
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

        return modelNames.Contains(_modelSettings.VisionModel)
            && modelNames.Contains(_modelSettings.MetadataModel);
    }
}
