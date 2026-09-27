using System.IO;

namespace ImageMetaAI.Services;

public class ImageAnalyzer : IImageAnalyzer
{
    private const string Model = "qwen2.5vl:7b";

    private readonly IOllamaClient _ollamaClient;

    public ImageAnalyzer(IOllamaClient ollamaClient)
    {
        _ollamaClient = ollamaClient;
    }

    public async Task<string> AnalyzeAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException(
                "Image file not found.",
                imagePath);
        }

        var imageBytes = await File.ReadAllBytesAsync(
            imagePath,
            cancellationToken);

        var imageBase64 = Convert.ToBase64String(imageBytes);

        const string prompt = """
            Analyze the image and describe what is visibly shown.
            Provide a concise and factual description suitable for stock photography metadata.
            Do not guess names, professions, locations, brands, or other facts
            that cannot be reliably determined from the image.
            """;

        var response = await _ollamaClient.GenerateAsync(
            Model,
            prompt,
            imageBase64,
            cancellationToken);

        return response.Trim();
    }
}
