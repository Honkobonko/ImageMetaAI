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
        var selectedPlatforms = GetSelectedPlatforms(platforms);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            imageFile.Status = ImageStatus.Analyzing;
            progressCallback?.Invoke(imageFile);

            await AnalyzeVisionAsync(
                imageFile,
                recognizeLocations,
                cancellationToken,
                progressCallback);

            cancellationToken.ThrowIfCancellationRequested();

            imageFile.Status = ImageStatus.AnalysisCompleted;
            progressCallback?.Invoke(imageFile);

            imageFile.Status = ImageStatus.GeneratingMetadata;
            progressCallback?.Invoke(imageFile);

            await GenerateMetadataAsync(
                imageFile,
                selectedPlatforms,
                cancellationToken,
                progressCallback);

            imageFile.Status = ImageStatus.MetadataCompleted;
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

    public async Task ProcessBatchAsync(
        IReadOnlyList<ImageFile> images,
        Action<ImageFile>? imageUpdated = null,
        Action<ProcessingPhase, int, int, string>? progressCallback = null,
        CancellationToken cancellationToken = default,
        bool recognizeLocations = false,
        IReadOnlyCollection<MetadataPlatform>? platforms = null,
        Action<ImageFile, Exception>? errorCallback = null)
    {
        if (images.Count == 0)
        {
            return;
        }

        var selectedPlatforms = GetSelectedPlatforms(platforms);

        // Images that have a usable vision description and can proceed
        // to metadata generation.
        var visionReady = new HashSet<ImageFile>();

        // Phase 1: Complete vision analysis for all images first.
        var visionCompleted = 0;
        var total = images.Count;

        foreach (var imageFile in images)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                imageFile.Status = ImageStatus.Analyzing;
                imageUpdated?.Invoke(imageFile);

                var alreadyHasDescription =
                    !string.IsNullOrWhiteSpace(imageFile.VisionDescription);

                if (alreadyHasDescription)
                {
                    progressCallback?.Invoke(
                        ProcessingPhase.Vision,
                        visionCompleted,
                        total,
                        $"Using existing vision description: {imageFile.FileName}");
                }
                else
                {
                    progressCallback?.Invoke(
                        ProcessingPhase.Vision,
                        visionCompleted,
                        total,
                        $"Analyzing image: {imageFile.FileName}");
                }

                await AnalyzeVisionAsync(
                    imageFile,
                    recognizeLocations,
                    cancellationToken,
                    imageUpdated);

                cancellationToken.ThrowIfCancellationRequested();

                // Mark this image as analyzed immediately.
                imageFile.Status = ImageStatus.AnalysisCompleted;
                imageUpdated?.Invoke(imageFile);

                visionReady.Add(imageFile);
            }
            catch (OperationCanceledException)
            {
                imageFile.Status = ImageStatus.Cancelled;
                imageUpdated?.Invoke(imageFile);
                throw;
            }
            catch (Exception ex)
            {
                imageFile.Status = ImageStatus.Error;
                imageUpdated?.Invoke(imageFile);

                System.Diagnostics.Debug.WriteLine(
                    $"Vision analysis failed for {imageFile.FileName}: {ex}");
            }
            finally
            {
                visionCompleted++;

                progressCallback?.Invoke(
                    ProcessingPhase.Vision,
                    visionCompleted,
                    total,
                    $"Vision analysis: {visionCompleted} / {total}");
            }
        }

        // Phase 2: Generate metadata only after phase 1 has finished.
        var metadataCompleted = 0;

        foreach (var imageFile in images)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!visionReady.Contains(imageFile))
            {
                metadataCompleted++;

                progressCallback?.Invoke(
                    ProcessingPhase.Metadata,
                    metadataCompleted,
                    total,
                    $"Skipping metadata; vision analysis failed: {imageFile.FileName}");

                continue;
            }

            try
            {
                imageFile.Status = ImageStatus.GeneratingMetadata;
                imageUpdated?.Invoke(imageFile);

                progressCallback?.Invoke(
                    ProcessingPhase.Metadata,
                    metadataCompleted,
                    total,
                    $"Generating metadata: {imageFile.FileName}");

                await GenerateMetadataAsync(
                    imageFile,
                    selectedPlatforms,
                    cancellationToken,
                    imageUpdated);

                cancellationToken.ThrowIfCancellationRequested();

                // Mark this image as fully processed.
                imageFile.Status = ImageStatus.MetadataCompleted;
                imageUpdated?.Invoke(imageFile);
            }
            catch (OperationCanceledException)
            {
                imageFile.Status = ImageStatus.Cancelled;
                imageUpdated?.Invoke(imageFile);
                throw;
            }
            catch (Exception ex)
            {
                 imageFile.Status = ImageStatus.Error;
                    imageUpdated?.Invoke(imageFile);

                    System.Diagnostics.Debug.WriteLine(
                        $"Metadata generation failed for {imageFile.FileName}:\n{ex}");

                    errorCallback?.Invoke(imageFile, ex);
            }
            finally
            {
                metadataCompleted++;

                progressCallback?.Invoke(
                    ProcessingPhase.Metadata,
                    metadataCompleted,
                    total,
                    $"Metadata generation: {metadataCompleted} / {total}");
            }
        }
    }

    private static MetadataPlatform[] GetSelectedPlatforms(
        IReadOnlyCollection<MetadataPlatform>? platforms)
    {
        var selectedPlatforms = platforms?.Distinct().ToArray()
            ?? [MetadataPlatform.Shutterstock];

        if (selectedPlatforms.Length == 0)
        {
            throw new ArgumentException(
                "Select at least one metadata platform.",
                nameof(platforms));
        }

        return selectedPlatforms;
    }

    private async Task AnalyzeVisionAsync(
        ImageFile imageFile,
        bool recognizeLocations,
        CancellationToken cancellationToken,
        Action<ImageFile>? progressCallback)
    {
        // Reuse the description if it already exists.
        if (string.IsNullOrWhiteSpace(imageFile.VisionDescription))
        {
            imageFile.VisionDescription =
                await _imageAnalyzer.AnalyzeAsync(
                    imageFile.FilePath,
                    cancellationToken);

            progressCallback?.Invoke(imageFile);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Resolve GPS coordinates only if a location has not already
        // been supplied or manually edited.
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

                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(location) &&
                    !imageFile.IsLocationManuallyEdited &&
                    string.IsNullOrWhiteSpace(imageFile.ResolvedLocation))
                {
                    imageFile.ResolvedLocation = location;
                    imageFile.LocationStatus = LocationLookupStatus.Found;
                    progressCallback?.Invoke(imageFile);
                }
                else if (string.IsNullOrWhiteSpace(location))
                {
                    imageFile.LocationStatus = LocationLookupStatus.NotFound;
                    progressCallback?.Invoke(imageFile);
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

                progressCallback?.Invoke(imageFile);
            }
        }
        else if (recognizeLocations &&
                 imageFile.GpsCoordinates is null)
        {
            imageFile.LocationStatus = LocationLookupStatus.NoGpsData;
            progressCallback?.Invoke(imageFile);
        }
    }

    private async Task GenerateMetadataAsync(
        ImageFile imageFile,
        IReadOnlyCollection<MetadataPlatform> platforms,
        CancellationToken cancellationToken,
        Action<ImageFile>? progressCallback)
    {
        if (string.IsNullOrWhiteSpace(imageFile.VisionDescription))
        {
            throw new InvalidOperationException(
                $"No vision description is available for {imageFile.FileName}.");
        }

        // Build the context once so the location is appended only once.
        var additionalInformation = imageFile.AdditionalInformation;

        if (!string.IsNullOrWhiteSpace(imageFile.ResolvedLocation))
        {
            var locationLine = $"Location: {imageFile.ResolvedLocation}";

            if (string.IsNullOrWhiteSpace(additionalInformation))
            {
                additionalInformation = locationLine;
            }
            else if (!additionalInformation.Contains(
                         locationLine,
                         StringComparison.OrdinalIgnoreCase))
            {
                additionalInformation =
                    $"{additionalInformation}\n{locationLine}";
            }
        }

        foreach (var platform in platforms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = await _metadataGenerator.GenerateAsync(
                imageFile.VisionDescription,
                platform,
                cancellationToken,
                additionalInformation);

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
    }
}
