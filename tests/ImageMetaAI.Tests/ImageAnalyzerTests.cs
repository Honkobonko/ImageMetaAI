using ImageMetaAI.Models;
using ImageMetaAI.Services;

namespace ImageMetaAI.Tests;

public class ImageAnalyzerTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsResponse_WhenOllamaReturnsText()
    {
        var imagePath = CreateTempImage();

        try
        {
            var client = new FakeOllamaClient
            {
                Response = """
                    A freshly baked loaf of bread with herbs, roasted tomatoes and sliced olives.
                    """
            };

            var modelSettings = new OllamaModelSettings
            {
                VisionModel = "test-vision-model",
                MetadataModel = "test-metadata-model"
            };

            var analyzer = new ImageAnalyzer(
                client,
                modelSettings);

            var result = await analyzer.AnalyzeAsync(imagePath);

            Assert.Equal(
                "A freshly baked loaf of bread with herbs, roasted tomatoes and sliced olives.",
                result);
        }
        finally
        {
            File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_Throws_WhenImageDoesNotExist()
    {
        var client = new FakeOllamaClient();

        var modelSettings = new OllamaModelSettings
        {
            VisionModel = "test-vision-model",
            MetadataModel = "test-metadata-model"
        };

        var analyzer = new ImageAnalyzer(
            client,
            modelSettings);

        var missingPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid() + ".jpg");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => analyzer.AnalyzeAsync(missingPath));
    }

    [Fact]
    public async Task AnalyzeAsync_SendsImageAsBase64()
    {
        var imagePath = CreateTempImage();

        try
        {
            var imageBytes = await File.ReadAllBytesAsync(imagePath);

            var client = new FakeOllamaClient
            {
                Response = "Test image description."
            };

            var modelSettings = new OllamaModelSettings
            {
                VisionModel = "test-vision-model",
                MetadataModel = "test-metadata-model"
            };

            var analyzer = new ImageAnalyzer(
                client,
                modelSettings);

            await analyzer.AnalyzeAsync(imagePath);

            Assert.Equal(
                modelSettings.VisionModel,
                client.Model);

            Assert.NotEmpty(client.Prompt);

            Assert.Equal(
                Convert.ToBase64String(imageBytes),
                client.ImageBase64);
        }
        finally
        {
            File.Delete(imagePath);
        }
    }

    private static string CreateTempImage()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid() + ".jpg");

        File.WriteAllBytes(
            path,
            [1, 2, 3, 4, 5]);

        return path;
    }

    private sealed class FakeOllamaClient : IOllamaClient
    {
        public string Response { get; init; } = string.Empty;

        public string Model { get; private set; } = string.Empty;

        public string Prompt { get; private set; } = string.Empty;

        public string ImageBase64 { get; private set; } = string.Empty;

        public Task<bool> IsAvailableAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<OllamaModel>> GetModelsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<OllamaModel>>([]);
        }

        public Task<string> GenerateAsync(
            string model,
            string prompt,
            string imageBase64,
            CancellationToken cancellationToken = default)
        {
            Model = model;
            Prompt = prompt;
            ImageBase64 = imageBase64;

            return Task.FromResult(Response);
        }
    }
}

