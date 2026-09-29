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
            Create stock photography metadata from the image description.

            Generate: 
            - A concise, descriptive English stock photo title. 
            - 20 to 30 relevant English keywords. 

            Keywords should be specific and useful for stock photography search. 
            Prefer concrete subjects, objects, actions, environment, composition, visual characteristics, and relevant concepts. 

            Use only information supported by the image description. 
            Do not invent names, locations, brands, professions, events, or other details that cannot be determined from the description. 

            Image description:
            """;

        var fullPrompt = $"{prompt}\n{visionDescription}";

        return await _ollamaClient.GenerateMetadataAsync(
            _modelSettings.MetadataModel,
            fullPrompt,
            cancellationToken);
    }
}
