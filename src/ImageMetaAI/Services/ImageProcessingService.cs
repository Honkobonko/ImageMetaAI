using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public class ImageProcessingService : IImageProcessingService
{
    private readonly IImageAnalyzer _imageAnalyzer;
    private readonly IMetadataGenerator _metadataGenerator;

    public ImageProcessingService(
        IImageAnalyzer imageAnalyzer,
        IMetadataGenerator metadataGenerator)
    {
        _imageAnalyzer = imageAnalyzer;
        _metadataGenerator = metadataGenerator;
    }

    public async Task ProcessAsync(
        ImageFile imageFile,
        Action<ImageFile>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        imageFile.Status = ImageStatus.Processing;
        progressCallback?.Invoke(imageFile);

        try
        {
            imageFile.VisionDescription =
                await _imageAnalyzer.AnalyzeAsync(
                    imageFile.FilePath,
                    cancellationToken);

            progressCallback?.Invoke(imageFile);

            imageFile.Metadata =
                await _metadataGenerator.GenerateAsync(
                    imageFile.VisionDescription,
                    cancellationToken);

            imageFile.Status = ImageStatus.Review;
            progressCallback?.Invoke(imageFile);
        }
        catch
        {
            imageFile.Status = ImageStatus.Error;
            progressCallback?.Invoke(imageFile);
            throw;
        }
    }
}
