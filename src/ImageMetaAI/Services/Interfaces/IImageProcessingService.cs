using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public interface IImageProcessingService
{
    Task ProcessAsync(
        ImageFile imageFile,
        Action<ImageFile>? progressCallback = null,
        CancellationToken cancellationToken = default);
}
