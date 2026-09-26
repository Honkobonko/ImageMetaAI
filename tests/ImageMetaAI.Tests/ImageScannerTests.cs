using ImageMetaAI.Services;

namespace ImageMetaAI.Tests;

public class ImageScannerTests
{
    [Fact]
    public void ScanFolder_FindsSupportedImages()
    {
        var tempFolder = CreateTempFolder();

        try
        {
            File.WriteAllText(
                Path.Combine(tempFolder, "photo.jpg"), "");

            File.WriteAllText(
                Path.Combine(tempFolder, "image.png"), "");

            File.WriteAllText(
                Path.Combine(tempFolder, "document.txt"), "");

            var scanner = new ImageScanner();

            var result = scanner.ScanFolder(tempFolder);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.FileName == "photo.jpg");
            Assert.Contains(result, x => x.FileName == "image.png");
        }
        finally
        {
            Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public void ScanFolder_IgnoresUnsupportedFiles()
    {
        var tempFolder = CreateTempFolder();

        try
        {
            File.WriteAllText(
                Path.Combine(tempFolder, "photo.jpg"), "");

            File.WriteAllText(
                Path.Combine(tempFolder, "document.pdf"), "");

            File.WriteAllText(
                Path.Combine(tempFolder, "text.txt"), "");

            var scanner = new ImageScanner();

            var result = scanner.ScanFolder(tempFolder);

            Assert.Single(result);
            Assert.Equal("photo.jpg", result[0].FileName);
        }
        finally
        {
            Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public void ScanFolder_ThrowsWhenFolderDoesNotExist()
    {
        var scanner = new ImageScanner();

        var missingFolder = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Assert.Throws<DirectoryNotFoundException>(
            () => scanner.ScanFolder(missingFolder));
    }

    [Fact]
    public void ScanFolder_IsCaseInsensitiveForExtensions()
    {
        var tempFolder = CreateTempFolder();

        try
        {
            File.WriteAllText(
                Path.Combine(tempFolder, "photo.JPG"), "");

            File.WriteAllText(
                Path.Combine(tempFolder, "image.PnG"), "");

            var scanner = new ImageScanner();

            var result = scanner.ScanFolder(tempFolder);

            Assert.Equal(2, result.Count);
        }
        finally
        {
            Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public void ScanFolder_IgnoresImagesInSubfolders()
    {
        var tempFolder = CreateTempFolder();
        var subFolder = Path.Combine(tempFolder, "Subfolder");

        try
        {
            Directory.CreateDirectory(subFolder);

            File.WriteAllText(
                Path.Combine(tempFolder, "photo.jpg"), "");

            File.WriteAllText(
                Path.Combine(subFolder, "nested.jpg"), "");

            var scanner = new ImageScanner();

            var result = scanner.ScanFolder(tempFolder);

            Assert.Single(result);
            Assert.Equal("photo.jpg", result[0].FileName);
        }
        finally
        {
            Directory.Delete(tempFolder, true);
        }
    }

    private static string CreateTempFolder()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        return folder;
    }
}