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
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(visionDescription))
        {
            throw new ArgumentException(
                "Vision description must not be empty.",
                nameof(visionDescription));
        }

        const string prompt = """
            Create stock photography metadata from the following image description.

            Return valid JSON only using this structure:
            {
              "title": "one concise English stock-photo title",
              "keywords": [
                "20 to 30 relevant English keywords"
              ]
            }

            Do not invent people, locations, brands, events, or other details
            that are not supported by the description.

            Image description:
            """;

        var fullPrompt = $"{prompt}\n{visionDescription}";

        return await _ollamaClient.GenerateMetadataAsync(
            _modelSettings.MetadataModel,
            fullPrompt,
            cancellationToken);
    }
}
