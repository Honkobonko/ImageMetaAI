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
        bool recognizeLocations = false,
        IReadOnlyCollection<MetadataPlatform>? platforms = null)
    {
        var selectedPlatforms = platforms?.Distinct().ToArray()
            ?? [MetadataPlatform.Shutterstock];

        if (selectedPlatforms.Length == 0)
        {
            throw new ArgumentException(
                "Select at least one metadata platform.",
                nameof(platforms));
        }

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

            // Resolve the GPS location only when no location has been entered.
            if (recognizeLocations &&
                imageFile.GpsCoordinates is not null &&
                !imageFile.IsLocationManuallyEdited &&
                string.IsNullOrWhiteSpace(imageFile.ResolvedLocation))
            {
                try
                {
                    var location = await _locationService.ResolveLocationAsync(
                        imageFile.GpsCoordinates,
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        // Do not replace a location that was edited while awaiting the lookup.
                        if (!imageFile.IsLocationManuallyEdited &&
                            string.IsNullOrWhiteSpace(imageFile.ResolvedLocation))
                        {
                            imageFile.ResolvedLocation = location;
                            imageFile.LocationStatus = LocationLookupStatus.Found;
                            progressCallback?.Invoke(imageFile);
                        }
                    }
                    else
                    {
                        imageFile.LocationStatus = LocationLookupStatus.NotFound;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    imageFile.LocationStatus = LocationLookupStatus.Failed;

                    System.Diagnostics.Debug.WriteLine(
                        $"Location lookup failed for {imageFile.FileName}: {ex.Message}");
                }
            }
            else if (recognizeLocations &&
                    imageFile.GpsCoordinates is null)
            {
                imageFile.LocationStatus = LocationLookupStatus.NoGpsData;
            }

            // Include the current location in metadata generation.
            if (!string.IsNullOrWhiteSpace(imageFile.ResolvedLocation))
            {
                additionalInformation = string.IsNullOrWhiteSpace(additionalInformation)
                    ? $"Location: {imageFile.ResolvedLocation}"
                    : $"{additionalInformation}\nLocation: {imageFile.ResolvedLocation}";
            }

            foreach (var platform in selectedPlatforms)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var metadataInformation = additionalInformation;

                if (!string.IsNullOrWhiteSpace(imageFile.ResolvedLocation))
                {
                    metadataInformation = string.IsNullOrWhiteSpace(metadataInformation)
                        ? $"Location: {imageFile.ResolvedLocation}"
                        : $"{metadataInformation}\nLocation: {imageFile.ResolvedLocation}";
                }

                var metadata = await _metadataGenerator.GenerateAsync(
                    imageFile.VisionDescription!,
                    platform,
                    cancellationToken,
                    metadataInformation);

                switch (platform)
                {
                    case MetadataPlatform.Shutterstock:
                        imageFile.ShutterstockMetadata = metadata;
                        break;

                    case MetadataPlatform.AdobeStock:
                        imageFile.AdobeStockMetadata = metadata;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(platforms),
                            platform,
                            "Unsupported metadata platform.");
                }

                progressCallback?.Invoke(imageFile);
            }

            imageFile.Status = ImageStatus.Review;
            progressCallback?.Invoke(imageFile);
        }
        catch (OperationCanceledException)
        {
            imageFile.Status = ImageStatus.Cancelled;
            progressCallback?.Invoke(imageFile);
            throw;
        }
        catch
        {
            imageFile.Status = ImageStatus.Error;
            progressCallback?.Invoke(imageFile);
            throw;
        }
    }
}
