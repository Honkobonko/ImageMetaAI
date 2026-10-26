using StockTaggerLocalAI.Models;
using StockTaggerLocalAI.Services;

namespace StockTaggerLocalAI.Tests;

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
            () => generator.GenerateAsync(
                string.Empty,
                MetadataPlatform.Shutterstock));
    }

    [Fact]
    public async Task GenerateAsync_Throws_WhenVisionDescriptionIsWhitespace()
    {
        var client = new FakeOllamaClient();

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        await Assert.ThrowsAsync<ArgumentException>(
            () => generator.GenerateAsync(
                "   ",
                MetadataPlatform.Shutterstock));
    }

    [Fact]
    public async Task GenerateAsync_UsesConfiguredMetadataModel()
    {
        var client = new FakeOllamaClient();

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        await generator.GenerateAsync(
            "A mountain landscape.",
            MetadataPlatform.Shutterstock);

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
            visionDescription,
            MetadataPlatform.Shutterstock);

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
            "A mountain landscape with a lake.",
            MetadataPlatform.Shutterstock);

        Assert.Same(
            expectedMetadata,
            result);
    }

    [Fact]
    public async Task GenerateAsync_ReplacesImageDescriptionPlaceholder()
    {
        var client = new FakeOllamaClient();

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        const string description =
            "A mountain landscape with a lake.";

        await generator.GenerateAsync(
            description,
            MetadataPlatform.Shutterstock);

        Assert.Contains(description, client.LastPrompt);
        Assert.DoesNotContain(
            "{{IMAGE_DESCRIPTION}}",
            client.LastPrompt);
    }

    [Fact]
    public async Task GenerateAsync_IncludesAdditionalInformation()
    {
        var client = new FakeOllamaClient();

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        const string additionalInformation =
            "Location: Bavaria, Germany";

        await generator.GenerateAsync(
            "A mountain landscape.",
            MetadataPlatform.Shutterstock,
            additionalInformation: additionalInformation);

        Assert.Contains(
            additionalInformation,
            client.LastPrompt);

        Assert.DoesNotContain(
            "{{ADDITIONAL_INFORMATION}}",
            client.LastPrompt);
    }

    [Fact]
    public async Task GenerateAsync_UsesFallback_WhenAdditionalInformationIsEmpty()
    {
        var client = new FakeOllamaClient();

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        await generator.GenerateAsync(
            "A mountain landscape.",
            MetadataPlatform.Shutterstock);

        Assert.Contains(
            "None provided.",
            client.LastPrompt);

        Assert.DoesNotContain(
            "{{ADDITIONAL_INFORMATION}}",
            client.LastPrompt);
    }

    [Fact]
    public async Task GenerateAsync_UsesAdobeStockPrompt_WhenAdobeStockIsSelected()
    {
        var client = new FakeOllamaClient();

        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        await generator.GenerateAsync(
            "A mountain landscape.",
            MetadataPlatform.AdobeStock);

        Assert.NotNull(client.LastPrompt);

        // The Adobe prompt should not be the Shutterstock prompt.
        Assert.DoesNotContain(
            "Shutterstock",
            client.LastPrompt,
            StringComparison.OrdinalIgnoreCase);
    }

    
    [Fact]
    public async Task GenerateAsync_PassesShutterstockPlatformToClient()
    {
        var client = new FakeOllamaClient();
        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        await generator.GenerateAsync(
            "A mountain landscape",
            MetadataPlatform.Shutterstock);

        Assert.Equal(
            MetadataPlatform.Shutterstock,
            client.LastPlatform);
    }

    [Fact]
    public async Task GenerateAsync_PassesAdobeStockPlatformToClient()
    {
        var client = new FakeOllamaClient();
        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        await generator.GenerateAsync(
            "A mountain landscape",
            MetadataPlatform.AdobeStock);

        Assert.Equal(
            MetadataPlatform.AdobeStock,
            client.LastPlatform);
    }

    [Fact]
    public async Task GenerateAsync_ThrowsForUnsupportedPlatform()
    {
        var client = new FakeOllamaClient();
        var generator = new MetadataGenerator(
            client,
            CreateModelSettings());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => generator.GenerateAsync(
                "A mountain landscape",
                (MetadataPlatform)999));

        Assert.Null(client.LastPlatform);
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

        public MetadataPlatform? LastPlatform { get; private set; }

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
            MetadataPlatform platform,
            CancellationToken cancellationToken = default)
        {
            LastModel = model;
            LastPrompt = prompt;
            LastPlatform = platform;

            return Task.FromResult(Metadata);
        }
    }
}
