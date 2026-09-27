using ImageMetaAI.Models;
using ImageMetaAI.Services;

namespace ImageMetaAI.Tests;

public class OllamaServiceTests
{
    [Fact]
    public async Task IsReadyAsync_ReturnsFalse_WhenOllamaIsUnavailable()
    {
        var client = new FakeOllamaClient
        {
            IsAvailable = false
        };

        var modelSettings = CreateModelSettings();

        var service = new OllamaService(
            client,
            modelSettings);

        var result = await service.IsReadyAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsReadyAsync_ReturnsTrue_WhenRequiredModelsAreInstalled()
    {
        var modelSettings = CreateModelSettings();

        var client = new FakeOllamaClient
        {
            IsAvailable = true,
            Models =
            [
                new OllamaModel { Name = modelSettings.VisionModel },
                new OllamaModel { Name = modelSettings.MetadataModel }
            ]
        };

        var service = new OllamaService(
            client,
            modelSettings);

        var result = await service.IsReadyAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsReadyAsync_ReturnsFalse_WhenVisionModelIsMissing()
    {
        var modelSettings = CreateModelSettings();

        var client = new FakeOllamaClient
        {
            IsAvailable = true,
            Models =
            [
                new OllamaModel { Name = modelSettings.MetadataModel }
            ]
        };

        var service = new OllamaService(
            client,
            modelSettings);

        var result = await service.IsReadyAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsReadyAsync_ReturnsFalse_WhenMetadataModelIsMissing()
    {
        var modelSettings = CreateModelSettings();

        var client = new FakeOllamaClient
        {
            IsAvailable = true,
            Models =
            [
                new OllamaModel { Name = modelSettings.VisionModel }
            ]
        };

        var service = new OllamaService(
            client,
            modelSettings);

        var result = await service.IsReadyAsync();

        Assert.False(result);
    }

    private static OllamaModelSettings CreateModelSettings()
    {
        return new OllamaModelSettings
        {
            VisionModel = "test-vision-model",
            MetadataModel = "test-metadata-model"
        };
    }

    private sealed class FakeOllamaClient : IOllamaClient
    {
        public bool IsAvailable { get; init; }

        public IReadOnlyList<OllamaModel> Models { get; init; } = [];

        public Task<bool> IsAvailableAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsAvailable);
        }

        public Task<IReadOnlyList<OllamaModel>> GetModelsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Models);
        }

        public Task<string> GenerateAsync(
            string model,
            string prompt,
            string imageBase64,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(string.Empty);
        }
    }
}
