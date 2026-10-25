using System.Globalization;
using System.IO;
using System.Text;
using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public class AdobeStockCsvExporter
{
    public async Task<string> ExportAsync(
        string folderPath,
        IEnumerable<ImageFile> images,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException(
                $"Export folder does not exist: {folderPath}");
        }

        ArgumentNullException.ThrowIfNull(images);

        var exportImages = images
            .Where(image =>
                image.AdobeStockMetadata is not null &&
                !string.IsNullOrWhiteSpace(
                    image.AdobeStockMetadata.Title) &&
                image.AdobeStockMetadata.Keywords is { Count: > 0 } &&
                !string.IsNullOrWhiteSpace(
                    image.AdobeStockMetadata.Category))
            .ToList();

        if (exportImages.Count == 0)
        {
            throw new InvalidOperationException(
                "No images with complete Adobe Stock metadata are available for export.");
        }

        var timestamp = DateTime.Now.ToString(
            "yyyy-MM-dd_HH-mm-ss",
            CultureInfo.InvariantCulture);

        var filePath = GetUniqueFilePath(
            folderPath,
            $"adobestock_metadata_{timestamp}",
            ".csv");

        var csv = new StringBuilder();

        csv.AppendLine(
            "Filename,Title,Keywords,Category,Releases");

        foreach (var image in exportImages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = image.AdobeStockMetadata!;

            var category = AdobeStockCategories.All
                .FirstOrDefault(item =>
                    string.Equals(
                        item.Name,
                        metadata.Category,
                        StringComparison.Ordinal));

            if (category == default)
            {
                throw new InvalidOperationException(
                    $"Unsupported Adobe Stock category: {metadata.Category}");
            }

            var fields = new[]
            {
                image.FileName,
                metadata.Title,
                string.Join(", ", metadata.Keywords),
                category.Id.ToString(CultureInfo.InvariantCulture),
                string.Empty
            };

            csv.AppendLine(
                string.Join(",", fields.Select(EscapeCsvField)));
        }

        await File.WriteAllTextAsync(
            filePath,
            csv.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);

        return filePath;
    }

    private static string GetUniqueFilePath(
        string folderPath,
        string fileNameWithoutExtension,
        string extension)
    {
        var filePath = Path.Combine(
            folderPath,
            fileNameWithoutExtension + extension);

        var counter = 1;

        while (File.Exists(filePath))
        {
            filePath = Path.Combine(
                folderPath,
                $"{fileNameWithoutExtension}_{counter:00}{extension}");

            counter++;
        }

        return filePath;
    }

    private static string EscapeCsvField(string value)
    {
        var escapedValue = value.Replace("\"", "\"\"");

        return $"\"{escapedValue}\"";
    }
}
