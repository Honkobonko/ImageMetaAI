using StockTaggerLocalAI.Models;

namespace StockTaggerLocalAI.Services;

public interface IImageProcessingService
{
    Task ProcessAsync(
        ImageFile imageFile,
        Action<ImageFile>? progressCallback = null,
        CancellationToken cancellationToken = default,
        bool recognizeLocations = false,
        IReadOnlyCollection<MetadataPlatform>? platforms = null);

    Task ProcessBatchAsync(
        IReadOnlyList<ImageFile> images,
        Action<ImageFile>? imageUpdated = null,
        Action<ProcessingPhase, int, int, string>? progressCallback = null,
        CancellationToken cancellationToken = default,
        bool recognizeLocations = false,
        IReadOnlyCollection<MetadataPlatform>? platforms = null,
        Action<ImageFile, Exception>? errorCallback = null);
}
