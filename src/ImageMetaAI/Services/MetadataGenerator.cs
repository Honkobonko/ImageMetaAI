using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public class MetadataGenerator : IMetadataGenerator
{
    private readonly IOllamaClient _ollamaClient;
    private readonly OllamaModelSettings _modelSettings;

    public MetadataGenerator(
        IOllamaClient ollamaClient,
        OllamaModelSettings modelSettings)
    {
        _ollamaClient = ollamaClient;
        _modelSettings = modelSettings;
    }

    public async Task<ImageMetadata> GenerateAsync(
        string visionDescription,
        CancellationToken cancellationToken = default,
        string? additionalInformation = null)
    {
        if (string.IsNullOrWhiteSpace(visionDescription))
        {
            throw new ArgumentException(
                "Vision description must not be empty.",
                nameof(visionDescription));
        }

        var prompt = PromptLoader.Load(
            "ShutterstockMetadataPrompt.txt");

        var fullPrompt = prompt
            .Replace(
                "{{IMAGE_DESCRIPTION}}",
                visionDescription)
            .Replace(
                "{{ADDITIONAL_INFORMATION}}",
                string.IsNullOrWhiteSpace(additionalInformation)
                    ? "None provided."
                    : additionalInformation);

        return await _ollamaClient.GenerateMetadataAsync(
            _modelSettings.MetadataModel,
            fullPrompt,
            cancellationToken);
    }
}
