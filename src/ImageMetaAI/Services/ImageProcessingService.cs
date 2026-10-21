using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public class ImageProcessingService : IImageProcessingService
{
    private readonly IImageAnalyzer _imageAnalyzer;
    private readonly IMetadataGenerator _metadataGenerator;
    private readonly NominatimLocationService _locationService;

   public ImageProcessingService(
    IImageAnalyzer imageAnalyzer,
    IMetadataGenerator metadataGenerator,
    NominatimLocationService? locationService = null)
    {
        _imageAnalyzer = imageAnalyzer;
        _metadataGenerator = metadataGenerator;
        _locationService = locationService ?? new NominatimLocationService();
    }

    public async Task ProcessAsync(
        ImageFile imageFile,
        Action<ImageFile>? progressCallback = null,
        CancellationToken cancellationToken = default,
        bool recognizeLocations = false)
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

            var additionalInformation = imageFile.AdditionalInformation;

           if (recognizeLocations && imageFile.GpsCoordinates is not null && _locationService is not null)
            {
                try
                {
                    var location = await _locationService.ResolveLocationAsync(
                        imageFile.GpsCoordinates,
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        const string locationPrefix =
                            "GPS location identified by reverse geocoding: ";

                        // Remove a previous automatically added location.
                        var existingLines = imageFile.AdditionalInformation
                            .Split('\n')
                            .Where(line => !line.TrimStart().StartsWith(
                                locationPrefix,
                                StringComparison.OrdinalIgnoreCase));

                        var userInformation = string.Join(
                            "\n",
                            existingLines).Trim();

                        imageFile.AdditionalInformation =
                            string.IsNullOrWhiteSpace(userInformation)
                                ? $"{locationPrefix}{location}"
                                : $"{userInformation}\n{locationPrefix}{location}";

                        additionalInformation = imageFile.AdditionalInformation;
                        progressCallback?.Invoke(imageFile);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Location lookup is optional.
                    System.Diagnostics.Debug.WriteLine(
                        $"Location lookup failed for {imageFile.FileName}: {ex.Message}");
                }
            }

            imageFile.Metadata =
                await _metadataGenerator.GenerateAsync(
                    imageFile.VisionDescription,
                    cancellationToken,
                    additionalInformation);

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
