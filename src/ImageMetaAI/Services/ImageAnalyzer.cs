using System.IO;
using System.Text.Json;
using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public class ImageAnalyzer : IImageAnalyzer
{
    private const string Model = "qwen2.5vl:7b";

    private readonly IOllamaClient _ollamaClient;

    public ImageAnalyzer(IOllamaClient ollamaClient)
    {
        _ollamaClient = ollamaClient;
    }

    public async Task<VisionAnalysis> AnalyzeAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
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
            Analyze the image and return only valid JSON.

            Describe only information that is visibly supported by the image.
            Do not guess names, professions, locations, brands, or other facts
            that cannot be reliably determined from the image.

            Rules:
            - Keep the description to 1-2 factual sentences.
            - Return up to 15 relevant objects.
            - Return up to 10 relevant activities.
            - Return up to 10 relevant settings.
            - Do not repeat similar items.
            - Prefer specific visible details over generic terms.
            - If nothing relevant can be identified for a category, return an empty array.

            Return this exact JSON structure:

            {
              "description": "Short factual description of the image.",
              "objects": ["object 1", "object 2"],
              "activities": ["activity 1", "activity 2"],
              "settings": ["setting 1", "setting 2"]
            }
            """;

        var response = await _ollamaClient.GenerateAsync(
            Model,
            prompt,
            imageBase64,
            cancellationToken);

        var result = JsonSerializer.Deserialize<VisionAnalysis>(
            response,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result is null)
        {
            throw new InvalidOperationException(
                "Ollama returned an empty vision analysis.");
        }

        return result;
    }
}
