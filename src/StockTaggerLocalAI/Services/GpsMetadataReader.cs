using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using StockTaggerLocalAI.Models;

namespace StockTaggerLocalAI.Services;

public class GpsMetadataReader
{
    public GpsCoordinates? Read(string filePath)
    {
        var directories = ImageMetadataReader.ReadMetadata(filePath);

        var gpsDirectory = directories
            .OfType<GpsDirectory>()
            .FirstOrDefault();

        var location = gpsDirectory?.GetGeoLocation();

        if (location is null)
        {
            return null;
        }

        return new GpsCoordinates(
            location.Value.Latitude,
            location.Value.Longitude);
    }
}
