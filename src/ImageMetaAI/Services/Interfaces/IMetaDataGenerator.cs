using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public interface IMetadataGenerator
{
    Task<ImageMetadata> GenerateAsync(
        string visionDescription,
        MetadataPlatform platform,
        CancellationToken cancellationToken = default,
        string? additionalInformation = null);
}
