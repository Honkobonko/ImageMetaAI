using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public interface IMetadataGenerator
{
    Task<ImageMetadata> GenerateAsync(
        string visionDescription,
        CancellationToken cancellationToken = default);
}
