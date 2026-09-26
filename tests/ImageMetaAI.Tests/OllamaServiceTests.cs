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

        var service = new OllamaService(client);

        var result = await service.IsReadyAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsReadyAsync_ReturnsTrue_WhenRequiredModelsAreInstalled()
    {
        var client = new FakeOllamaClient
        {
            IsAvailable = true,
            Models =
            [
                new OllamaModel { Name = "qwen2.5vl:7b" },
                new OllamaModel { Name = "gemma4:26b" }
            ]
        };

        var service = new OllamaService(client);

        var result = await service.IsReadyAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsReadyAsync_ReturnsFalse_WhenVisionModelIsMissing()
    {
        var client = new FakeOllamaClient
        {
            IsAvailable = true,
            Models =
            [
                new OllamaModel { Name = "gemma4:26b" }
            ]
        };

        var service = new OllamaService(client);

        var result = await service.IsReadyAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsReadyAsync_ReturnsFalse_WhenMetadataModelIsMissing()
    {
        var client = new FakeOllamaClient
        {
            IsAvailable = true,
            Models =
            [
                new OllamaModel { Name = "qwen2.5vl:7b" }
            ]
        };

        var service = new OllamaService(client);

        var result = await service.IsReadyAsync();

        Assert.False(result);
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
