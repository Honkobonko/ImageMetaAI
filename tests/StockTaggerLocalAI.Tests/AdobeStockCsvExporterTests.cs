using System.Text;
using StockTaggerLocalAI.Models;
using StockTaggerLocalAI.Services;

namespace StockTaggerLocalAI.Tests;

public class AdobeStockCsvExporterTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly AdobeStockCsvExporter _exporter = new();

    public AdobeStockCsvExporterTests()
    {
        _tempFolder = Path.Combine(
            Path.GetTempPath(),
            $"StockTaggerLocalAI_AdobeStockTests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(_tempFolder);
    }

    [Fact]
    public async Task ExportAsync_CreatesCsvWithExpectedHeader()
    {
        var image = CreateImageWithAdobeMetadata();

        var filePath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        Assert.True(File.Exists(filePath));
        Assert.EndsWith(".csv", filePath);

        var lines = await File.ReadAllLinesAsync(filePath);

        Assert.Equal(
            "Filename,Title,Keywords,Category,Releases",
            lines[0]);

        Assert.Equal(2, lines.Length);
    }

    [Fact]
    public async Task ExportAsync_UsesAdobeStockCategoryId()
    {
        var image = CreateImageWithAdobeMetadata(
            category: "Landscapes");

        var filePath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        var lines = await File.ReadAllLinesAsync(filePath);

        Assert.Contains(
            "\"11\"",
            lines[1]);
    }

    [Fact]
    public async Task ExportAsync_UsesAdobeStockMetadataOnly()
    {
        var adobeImage = CreateImageWithAdobeMetadata(
            filePath: "adobe.jpg");

        var shutterstockOnlyImage = new ImageFile(
            Path.Combine(_tempFolder, "shutterstock.jpg"))
        {
            ShutterstockMetadata = CreateMetadata()
        };

        var filePath = await _exporter.ExportAsync(
            _tempFolder,
            [adobeImage, shutterstockOnlyImage]);

        var lines = await File.ReadAllLinesAsync(filePath);

        Assert.Equal(2, lines.Length);
        Assert.Contains("\"adobe.jpg\"", lines[1]);
        Assert.DoesNotContain("shutterstock.jpg", lines[1]);
    }

    [Fact]
    public async Task ExportAsync_SkipsImagesWithIncompleteMetadata()
    {
        var validImage = CreateImageWithAdobeMetadata(
            filePath: "valid.jpg");

        var missingTitle = CreateImageWithAdobeMetadata(
            filePath: "missing-title.jpg",
            title: "");

        var missingKeywords = CreateImageWithAdobeMetadata(
            filePath: "missing-keywords.jpg",
            keywords: []);

        var missingCategory = CreateImageWithAdobeMetadata(
            filePath: "missing-category.jpg",
            category: "");

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
        Assert.DoesNotContain("missing-title.jpg", lines[1]);
        Assert.DoesNotContain("missing-keywords.jpg", lines[1]);
        Assert.DoesNotContain("missing-category.jpg", lines[1]);
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenNoCompleteMetadataExists()
    {
        var image = new ImageFile(
            Path.Combine(_tempFolder, "empty.jpg"));

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
            "does-not-exist");

        var image = CreateImageWithAdobeMetadata();

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _exporter.ExportAsync(
                missingFolder,
                [image]));
    }

    [Fact]
    public async Task ExportAsync_EscapesQuotesAndCommas()
    {
        var image = CreateImageWithAdobeMetadata(
            title: "A \"quiet\" lake, at dawn",
            keywords: ["lake", "dawn"]);

        var filePath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        var lines = await File.ReadAllLinesAsync(filePath);

        Assert.Contains(
            "\"A \"\"quiet\"\" lake, at dawn\"",
            lines[1]);

        Assert.Contains(
            "\"lake, dawn\"",
            lines[1]);
    }

    [Fact]
    public async Task ExportAsync_CreatesUniqueFileNameWhenFileAlreadyExists()
    {
        var image = CreateImageWithAdobeMetadata();

        var firstPath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        var secondPath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        Assert.NotEqual(firstPath, secondPath);
        Assert.True(File.Exists(firstPath));
        Assert.True(File.Exists(secondPath));
    }

    [Fact]
    public async Task ExportAsync_WritesUtf8WithoutBom()
    {
        var image = CreateImageWithAdobeMetadata(
            title: "München bei Sonnenaufgang");

        var filePath = await _exporter.ExportAsync(
            _tempFolder,
            [image]);

        var bytes = await File.ReadAllBytesAsync(filePath);

        Assert.False(
            bytes.Length >= 3 &&
            bytes[0] == 0xEF &&
            bytes[1] == 0xBB &&
            bytes[2] == 0xBF);

        var content = await File.ReadAllTextAsync(
            filePath,
            Encoding.UTF8);

        Assert.Contains("München bei Sonnenaufgang", content);
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenCancelled()
    {
        var image = CreateImageWithAdobeMetadata();

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _exporter.ExportAsync(
                _tempFolder,
                [image],
                cancellationTokenSource.Token));

        Assert.Empty(Directory.GetFiles(_tempFolder));
    }

    [Fact]
    public async Task ExportAsync_ThrowsForUnsupportedCategory()
    {
        var image = CreateImageWithAdobeMetadata(
            category: "Not an Adobe Stock category");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _exporter.ExportAsync(
                _tempFolder,
                [image]));
    }

    [Fact]
    public async Task ExportAsync_ExportsMultipleImages()
    {
        var firstImage = CreateImageWithAdobeMetadata(
            filePath: "first.jpg",
            title: "First landscape");

        var secondImage = CreateImageWithAdobeMetadata(
            filePath: "second.jpg",
            title: "Second landscape");

        var filePath = await _exporter.ExportAsync(
            _tempFolder,
            [firstImage, secondImage]);

        var lines = await File.ReadAllLinesAsync(filePath);

        Assert.Equal(3, lines.Length);
        Assert.Contains("\"first.jpg\"", lines[1]);
        Assert.Contains("\"second.jpg\"", lines[2]);
    }

    private ImageFile CreateImageWithAdobeMetadata(
        string filePath = "landscape.jpg",
        string title = "Mountain landscape with lake",
        IReadOnlyList<string>? keywords = null,
        string category = "Landscapes")
    {
        return new ImageFile(
            Path.Combine(_tempFolder, filePath))
        {
            AdobeStockMetadata = new ImageMetadata
            {
                Title = title,
                Keywords = keywords ?? ["mountain", "landscape", "lake"],
                Category = category
            }
        };
    }

    private static ImageMetadata CreateMetadata()
    {
        return new ImageMetadata
        {
            Title = "Shutterstock title",
            Keywords = ["nature"],
            Category = "Nature"
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
