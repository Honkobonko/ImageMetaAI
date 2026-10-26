using System.Globalization;
using System.IO;
using System.Text;
using StockTaggerLocalAI.Models;

namespace StockTaggerLocalAI.Services;

public class ShutterstockCsvExporter
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

       var exportImages = images
        .Where(image =>
            image.ShutterstockMetadata is not null &&
            !string.IsNullOrWhiteSpace(image.ShutterstockMetadata.Title) &&
            image.ShutterstockMetadata.Keywords.Count > 0 &&
            !string.IsNullOrWhiteSpace(image.ShutterstockMetadata.Category))
        .ToList();

        if (exportImages.Count == 0)
        {
            throw new InvalidOperationException(
                "No images with complete metadata are available for export.");
        }

        var timestamp = DateTime.Now.ToString(
            "yyyy-MM-dd_HH-mm-ss",
            CultureInfo.InvariantCulture);

        var filePath = GetUniqueFilePath(
            folderPath,
            $"shutterstock_metadata_{timestamp}",
            ".csv");

        var csv = new StringBuilder();

        csv.AppendLine(
            "Filename,Description,Keywords,Categories");

        foreach (var image in exportImages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = image.ShutterstockMetadata!;

            var categories = new List<string>
            {
                metadata.Category
            };

            if (!string.IsNullOrWhiteSpace(metadata.SecondaryCategory) &&
                !string.Equals(
                    metadata.Category,
                    metadata.SecondaryCategory,
                    StringComparison.Ordinal))
            {
                categories.Add(metadata.SecondaryCategory);
            }

            var fields = new[]
            {
                image.FileName,
                metadata.Title,
                string.Join(", ", metadata.Keywords),
                string.Join(", ", categories)
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
