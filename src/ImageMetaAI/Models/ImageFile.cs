using System.IO;

namespace ImageMetaAI.Models;

public class ImageFile
{
    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public string Extension => Path.GetExtension(FilePath);

    public ImageStatus Status { get; set; } = ImageStatus.Open;

    public string? VisionDescription { get; set; }

    public string AdditionalInformation { get; set; } = string.Empty;

    public ImageMetadata? Metadata { get; set; }

    public GpsCoordinates? GpsCoordinates { get; set; }

    public bool HasGpsCoordinates => GpsCoordinates is not null;

    public string? ResolvedLocation { get; set; }

    public LocationLookupStatus LocationStatus { get; set; } =
        LocationLookupStatus.NotAttempted;

    public string DisplayStatus
    {
        get
        {
            if (Status == ImageStatus.Error)
                return "Error";

            if (Status == ImageStatus.Processing)
                return "Analyzing...";

            if (Status == ImageStatus.Open)
                return "Ready";

            if (Status == ImageStatus.Skipped)
                return "Skipped";

            if (Status != ImageStatus.Review &&
                Status != ImageStatus.Completed)
                return Status.ToString();

            return LocationStatus switch
            {
                LocationLookupStatus.NoGpsData =>
                    "Completed (no GPS data)",

                LocationLookupStatus.NotFound =>
                    "Completed (no location found)",

                LocationLookupStatus.Failed =>
                    "Completed (location lookup failed)",

                _ => "Completed"
            };
        }
    }

    public ImageFile(string filePath)
    {
        FilePath = filePath;
    }
}
