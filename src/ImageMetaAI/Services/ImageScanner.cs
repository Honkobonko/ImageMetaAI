using System.IO;
using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public class ImageScanner
{
    private static readonly string[] SupportedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff",
        ".webp"
    ];

    public List<ImageFile> ScanFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException(
                $"Folder not found: {folderPath}");
        }

        return Directory
            .EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(IsSupportedImage)
            .Select(filePath => new ImageFile(filePath))
            .ToList();
    }

    private static bool IsSupportedImage(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        return SupportedExtensions.Contains(
            extension,
            StringComparer.OrdinalIgnoreCase);
    }
}