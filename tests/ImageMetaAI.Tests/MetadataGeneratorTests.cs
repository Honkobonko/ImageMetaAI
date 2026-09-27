using ImageMetaAI.Models;
using ImageMetaAI.Services;

namespace ImageMetaAI.Tests;

public class MetadataGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Throws_WhenVisionDescriptionIsEmpty()
    {
        var client = new FakeOllamaClient();

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        await Assert.ThrowsAsync<ArgumentException>(
            () => generator.GenerateAsync(string.Empty));
    }

    [Fact]
    public async Task GenerateAsync_Throws_WhenVisionDescriptionIsWhitespace()
    {
        var client = new FakeOllamaClient();

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        await Assert.ThrowsAsync<ArgumentException>(
            () => generator.GenerateAsync("   "));
    }

    [Fact]
    public async Task GenerateAsync_UsesConfiguredMetadataModel()
    {
        var client = new FakeOllamaClient();

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        await generator.GenerateAsync(
            "A mountain landscape.");

        Assert.Equal(
            "test-metadata-model",
            client.LastModel);
    }

    [Fact]
    public async Task GenerateAsync_IncludesVisionDescriptionInPrompt()
    {
        var client = new FakeOllamaClient();

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        const string visionDescription =
            "A mountain landscape with a lake.";

        await generator.GenerateAsync(
            visionDescription);

        Assert.NotNull(client.LastPrompt);
        Assert.Contains(
            visionDescription,
            client.LastPrompt);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsMetadataFromClient()
    {
        var expectedMetadata = new ImageMetadata
        {
            Title = "Mountain landscape with lake",
            Keywords =
            [
                "mountain",
                "landscape",
                "lake",
                "nature"
            ]
        };

        var client = new FakeOllamaClient
        {
            Metadata = expectedMetadata
        };

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        var result = await generator.GenerateAsync(
            "A mountain landscape with a lake.");

        Assert.Same(
            expectedMetadata,
            result);
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
        public string? LastModel { get; private set; }

        public string? LastPrompt { get; private set; }

        public ImageMetadata Metadata { get; init; } = new();

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

        public Task<string> GenerateVisionAsync(
            string model,
            string prompt,
            string imageBase64,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<ImageMetadata> GenerateMetadataAsync(
            string model,
            string prompt,
            CancellationToken cancellationToken = default)
        {
            LastModel = model;
            LastPrompt = prompt;

            return Task.FromResult(Metadata);
        }
    }
}
