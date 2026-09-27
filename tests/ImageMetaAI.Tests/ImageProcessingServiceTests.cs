using ImageMetaAI.Models;
using ImageMetaAI.Services;

namespace ImageMetaAI.Tests;

public class ImageProcessingServiceTests
{
    [Fact]
    public async Task ProcessAsync_SetsStatusToProcessing()
    {
        var analyzer = new FakeImageAnalyzer();
        var metadataGenerator = new FakeMetadataGenerator();

        var service = new ImageProcessingService(
            analyzer,
            metadataGenerator);

        var imageFile = CreateImageFile();

        await service.ProcessAsync(imageFile);

        Assert.Equal(
            ImageStatus.Review,
            imageFile.Status);
    }

    [Fact]
    public async Task ProcessAsync_SetsVisionDescription()
    {
        var analyzer = new FakeImageAnalyzer
        {
            Description = "A mountain landscape."
        };

        var metadataGenerator = new FakeMetadataGenerator();

        var service = new ImageProcessingService(
            analyzer,
            metadataGenerator);

        var imageFile = CreateImageFile();

        await service.ProcessAsync(imageFile);

        Assert.Equal(
            "A mountain landscape.",
            imageFile.VisionDescription);
    }

    [Fact]
    public async Task ProcessAsync_SetsMetadata()
    {
        var analyzer = new FakeImageAnalyzer
        {
            Description = "A mountain landscape."
        };

        var expectedMetadata = new ImageMetadata
        {
            Title = "Mountain landscape",
            Keywords =
            [
                "mountain",
                "landscape",
                "nature"
            ]
        };

        var metadataGenerator = new FakeMetadataGenerator
        {
            Metadata = expectedMetadata
        };

        var service = new ImageProcessingService(
            analyzer,
            metadataGenerator);

        var imageFile = CreateImageFile();

        await service.ProcessAsync(imageFile);

        Assert.Same(
            expectedMetadata,
            imageFile.Metadata);
    }

    [Fact]
    public async Task ProcessAsync_CallsProgressCallbackInExpectedOrder()
    {
        var analyzer = new FakeImageAnalyzer
        {
            Description = "A mountain landscape."
        };

        var metadataGenerator = new FakeMetadataGenerator();

        var service = new ImageProcessingService(
            analyzer,
            metadataGenerator);

        var imageFile = CreateImageFile();

        var statuses = new List<ImageStatus>();

        await service.ProcessAsync(
            imageFile,
            updatedImage =>
            {
                statuses.Add(updatedImage.Status);
            });

        Assert.Equal(
            [
                ImageStatus.Processing,
                ImageStatus.Processing,
                ImageStatus.Review
            ],
            statuses);
    }

    [Fact]
    public async Task ProcessAsync_SetsErrorStatus_WhenVisionAnalysisFails()
    {
        var analyzer = new FakeImageAnalyzer
        {
            Exception = new InvalidOperationException(
                "Vision analysis failed.")
        };

        var metadataGenerator = new FakeMetadataGenerator();

        var service = new ImageProcessingService(
            analyzer,
            metadataGenerator);

        var imageFile = CreateImageFile();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProcessAsync(imageFile));

        Assert.Equal(
            ImageStatus.Error,
            imageFile.Status);
    }

    [Fact]
    public async Task ProcessAsync_SetsErrorStatus_WhenMetadataGenerationFails()
    {
        var analyzer = new FakeImageAnalyzer
        {
            Description = "A mountain landscape."
        };

        var metadataGenerator = new FakeMetadataGenerator
        {
            Exception = new InvalidOperationException(
                "Metadata generation failed.")
        };

        var service = new ImageProcessingService(
            analyzer,
            metadataGenerator);

        var imageFile = CreateImageFile();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProcessAsync(imageFile));

        Assert.Equal(
            ImageStatus.Error,
            imageFile.Status);
    }

    [Fact]
    public async Task ProcessAsync_RethrowsVisionAnalysisException()
    {
        var expectedException = new InvalidOperationException(
            "Vision analysis failed.");

        var analyzer = new FakeImageAnalyzer
        {
            Exception = expectedException
        };

        var metadataGenerator = new FakeMetadataGenerator();

        var service = new ImageProcessingService(
            analyzer,
            metadataGenerator);

        var imageFile = CreateImageFile();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProcessAsync(imageFile));

        Assert.Same(
            expectedException,
            exception);
    }

    [Fact]
    public async Task ProcessAsync_RethrowsMetadataGenerationException()
    {
        var expectedException = new InvalidOperationException(
            "Metadata generation failed.");

        var analyzer = new FakeImageAnalyzer
        {
            Description = "A mountain landscape."
        };

        var metadataGenerator = new FakeMetadataGenerator
        {
            Exception = expectedException
        };

        var service = new ImageProcessingService(
            analyzer,
            metadataGenerator);

        var imageFile = CreateImageFile();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProcessAsync(imageFile));

        Assert.Same(
            expectedException,
            exception);
    }

    [Fact]
    public async Task ProcessAsync_PassesVisionDescriptionToMetadataGenerator()
    {
        var analyzer = new FakeImageAnalyzer
        {
            Description = "A mountain landscape with a lake."
        };

        var metadataGenerator = new FakeMetadataGenerator();

        var service = new ImageProcessingService(
            analyzer,
            metadataGenerator);

        var imageFile = CreateImageFile();

        await service.ProcessAsync(imageFile);

        Assert.Equal(
            "A mountain landscape with a lake.",
            metadataGenerator.LastVisionDescription);
    }

    private static ImageFile CreateImageFile()
    {
        return new ImageFile(
            @"C:\Test\image.jpg");
    }

    private sealed class FakeImageAnalyzer : IImageAnalyzer
    {
        public string Description { get; init; } =
            "Test image description.";

        public Exception? Exception { get; init; }

        public Task<string> AnalyzeAsync(
            string imagePath,
            CancellationToken cancellationToken = default)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(Description);
        }
    }

    private sealed class FakeMetadataGenerator : IMetadataGenerator
    {
        public ImageMetadata Metadata { get; init; } = new();

        public Exception? Exception { get; init; }

        public string? LastVisionDescription { get; private set; }

        public Task<ImageMetadata> GenerateAsync(
            string visionDescription,
            CancellationToken cancellationToken = default)
        {
            LastVisionDescription = visionDescription;

            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(Metadata);
        }
    }
}
