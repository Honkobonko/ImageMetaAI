using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace StockTaggerLocalAI.Models;

public class ImageFile : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public string Extension => Path.GetExtension(FilePath);

    private ImageStatus _status = ImageStatus.Open;

    public ImageStatus Status
    {
        get => _status;
        set
        {
            if (_status == value)
                return;

            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayStatus));
        }
    }

    public string? VisionDescription { get; set; }

    public string AdditionalInformation { get; set; } = string.Empty;

    // Platform-specific metadata
    public ImageMetadata? ShutterstockMetadata { get; set; }

    public ImageMetadata? AdobeStockMetadata { get; set; }

    // Legacy property retained while existing code is migrated
    public ImageMetadata? Metadata { get; set; }

    public GpsCoordinates? GpsCoordinates { get; set; }

    public bool HasGpsCoordinates => GpsCoordinates is not null;

    public string? ResolvedLocation { get; set; }

    private LocationLookupStatus _locationStatus =
        LocationLookupStatus.NotAttempted;

    public LocationLookupStatus LocationStatus
    {
        get => _locationStatus;
        set
        {
            if (_locationStatus == value)
                return;

            _locationStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayStatus));
        }
    }

    public bool IsLocationManuallyEdited { get; set; }

    public string DisplayStatus => Status switch
    {
        ImageStatus.Open => "Ready",
        ImageStatus.Analyzing => "Analyzing...",
        ImageStatus.AnalysisCompleted => "Analysis completed",
        ImageStatus.GeneratingMetadata => "Generating metadata...",
        ImageStatus.MetadataCompleted => GetMetadataCompletedStatus(),
        ImageStatus.Error => "Error",
        ImageStatus.Cancelled => "Cancelled",
        ImageStatus.Skipped => "Skipped",
        _ => Status.ToString()
    };

    private string GetMetadataCompletedStatus()
    {
        return LocationStatus switch
        {
            LocationLookupStatus.NoGpsData =>
                "Metadata completed (no GPS data)",

            LocationLookupStatus.NotFound =>
                "Metadata completed (no location found)",

            LocationLookupStatus.Failed =>
                "Metadata completed (location lookup failed)",

            _ => "Metadata completed"
        };
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    public ImageFile(string filePath)
    {
        FilePath = filePath;
    }
}
