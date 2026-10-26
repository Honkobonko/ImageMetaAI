using StockTaggerLocalAI.Models;

namespace StockTaggerLocalAI.Services;

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
        MetadataPlatform platform,
        CancellationToken cancellationToken = default,
        string? additionalInformation = null)
    {
        if (string.IsNullOrWhiteSpace(visionDescription))
        {
            throw new ArgumentException(
                "Vision description must not be empty.",
                nameof(visionDescription));
        }

        var promptFileName = platform switch
        {
            MetadataPlatform.Shutterstock =>
                "ShutterstockMetadataPrompt.txt",

            MetadataPlatform.AdobeStock =>
                "AdobeStockMetadataPrompt.txt",

            _ => throw new ArgumentOutOfRangeException(
                nameof(platform),
                platform,
                "Unsupported metadata platform.")
        };

        var prompt = PromptLoader.Load(promptFileName);

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
            platform,
            cancellationToken);
    }
}
