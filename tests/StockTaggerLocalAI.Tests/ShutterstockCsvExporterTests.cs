using StockTaggerLocalAI.Models;
using StockTaggerLocalAI.Services;

namespace StockTaggerLocalAI.Tests.Services;

public class ShutterstockCsvExporterTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly ShutterstockCsvExporter _exporter = new();

    public ShutterstockCsvExporterTests()
    {
        _tempFolder = Path.Combine(
            Path.GetTempPath(),
            $"StockTaggerLocalAI_Tests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(_tempFolder);
    }

    [Fact]
    public async Task ExportAsync_WritesCsvHeaderAndMetadata()
    {
        var image = CreateImage(
            "photo_001.jpg",
            "Lavender field in Provence during summer",
            ["lavender", "Provence", "flowers"],
            "Nature",
            "Parks/Outdoor");

        var filePath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        Assert.True(File.Exists(filePath));
        Assert.EndsWith(".csv", filePath);

        var lines = await File.ReadAllLinesAsync(filePath);

        Assert.Equal(
            "Filename,Description,Keywords,Categories",
            lines[0]);

        Assert.Contains("\"photo_001.jpg\"", lines[1]);
        Assert.Contains(
            "\"Lavender field in Provence during summer\"",
            lines[1]);
        Assert.Contains(
            "\"lavender, Provence, flowers\"",
            lines[1]);
        Assert.Contains(
            "\"Nature, Parks/Outdoor\"",
            lines[1]);
    }

    [Fact]
    public async Task ExportAsync_EscapesCommasAndQuotes()
    {
        var image = CreateImage(
            "photo_002.jpg",
            "Flowers, \"beautiful\" and bright",
            ["flowers", "bright, colorful"],
            "Nature");

        var filePath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        var csv = await File.ReadAllTextAsync(filePath);

        Assert.Contains(
            "\"Flowers, \"\"beautiful\"\" and bright\"",
            csv);

        Assert.Contains(
            "\"flowers, bright, colorful\"",
            csv);
    }

    [Fact]
    public async Task ExportAsync_ExportsOnlyImagesWithCompleteMetadata()
    {
        var validImage = CreateImage(
            "valid.jpg",
            "A valid stock photo description",
            ["nature", "outdoors"],
            "Nature");

        var missingTitle = CreateImage(
            "missing_title.jpg",
            "",
            ["nature"],
            "Nature");

        var missingKeywords = CreateImage(
            "missing_keywords.jpg",
            "A photo without keywords",
            [],
            "Nature");

        var missingCategory = CreateImage(
            "missing_category.jpg",
            "A photo without category",
            ["nature"],
            "");

        var filePath = await _exporter.ExportAsync(
            _tempFolder,
            [
                validImage,
                missingTitle,
                missingKeywords,
                missingCategory
            ]);

        var lines = await File.ReadAllLinesAsync(filePath);

        Assert.Equal(2, lines.Length);
        Assert.Contains("\"valid.jpg\"", lines[1]);
        Assert.DoesNotContain("missing_title.jpg", lines[1]);
        Assert.DoesNotContain("missing_keywords.jpg", lines[1]);
        Assert.DoesNotContain("missing_category.jpg", lines[1]);
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenNoImagesAreExportable()
    {
        var image = CreateImage(
            "incomplete.jpg",
            "",
            [],
            "Nature");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _exporter.ExportAsync(
                _tempFolder,
                [image]));
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenFolderDoesNotExist()
    {
        var missingFolder = Path.Combine(
            _tempFolder,
            "missing");

        var exception =
            await Assert.ThrowsAsync<DirectoryNotFoundException>(
                () => _exporter.ExportAsync(
                    missingFolder,
                    []));

        Assert.Contains(
            "Export folder does not exist",
            exception.Message);
    }

    [Fact]
    public async Task ExportAsync_CreatesTimestampedFilename()
    {
        var image = CreateImage(
            "photo.jpg",
            "A clear description of a stock photo",
            ["photo", "stock"],
            "Objects");

        var filePath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        var fileName = Path.GetFileName(filePath);

        Assert.Matches(
            @"^shutterstock_metadata_\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}(?:_\d+)?\.csv$",
            fileName);
    }

    [Fact]
    public async Task ExportAsync_DoesNotOverwriteExistingExport()
    {
        var image = CreateImage(
            "photo.jpg",
            "A clear description of a stock photo",
            ["photo", "stock"],
            "Objects");

        var firstPath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        var originalContent = await File.ReadAllTextAsync(firstPath);

        var secondPath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        Assert.NotEqual(firstPath, secondPath);
        Assert.True(File.Exists(firstPath));
        Assert.True(File.Exists(secondPath));

        Assert.Equal(
            originalContent,
            await File.ReadAllTextAsync(firstPath));
    }

    private static ImageFile CreateImage(
        string fileName,
        string title,
        IReadOnlyList<string> keywords,
        string category,
        string? secondaryCategory = null)
    {
        return new ImageFile(Path.Combine(
            Path.GetTempPath(),
            fileName))
        {
            ShutterstockMetadata = new ImageMetadata
            {
                Title = title,
                Keywords = keywords,
                Category = category,
                SecondaryCategory = secondaryCategory
            }
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
        {
            Directory.Delete(_tempFolder, recursive: true);
        }
    }
}
