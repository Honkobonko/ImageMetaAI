using System.IO;

namespace ImageMetaAI.Models;

public class ImageFile
{
    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public string Extension => Path.GetExtension(FilePath);

    public ImageStatus Status { get; set; } = ImageStatus.Open;

    public string? VisionDescription { get; set; }

    public ImageMetadata? Metadata { get; set; }

    public ImageFile(string filePath)
    {
        FilePath = filePath;
    }
}