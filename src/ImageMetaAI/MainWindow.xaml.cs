using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ImageMetaAI.Models;
using ImageMetaAI.Services;

namespace ImageMetaAI;

public partial class MainWindow : Window
{
    private readonly ImageScanner _imageScanner = new();
    private readonly IOllamaService _ollamaService;
    private readonly IImageProcessingService _imageProcessingService;

    private readonly GpsMetadataReader _gpsMetadataReader = new();

    private bool _isUpdatingFields;
    private bool _isProcessing;

    public MainWindow()
    {
        InitializeComponent();

        PrimaryCategoryComboBox.ItemsSource =
            ShutterstockCategories.All;

        SecondaryCategoryComboBox.ItemsSource =
            new[] { string.Empty }
                .Concat(ShutterstockCategories.All)
                .ToList();

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var ollamaClient = new OllamaClient(httpClient);
        var modelSettings = new OllamaModelSettings();

        _ollamaService = new OllamaService(
            ollamaClient,
            modelSettings);

        var imageAnalyzer = new ImageAnalyzer(
            ollamaClient,
            modelSettings);

        var metadataGenerator = new MetadataGenerator(
            ollamaClient,
            modelSettings);

        var locationService = new NominatimLocationService();

        _imageProcessingService = new ImageProcessingService(
            imageAnalyzer,
            metadataGenerator,
            locationService);

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        OllamaStatusText.Text = "Checking Ollama...";

        try
        {
            var isReady = await _ollamaService.IsReadyAsync();

            OllamaStatusText.Text = isReady
                ? "Ollama ready"
                : "Ollama is not ready";
        }
        catch
        {
            OllamaStatusText.Text = "Could not connect to Ollama.";
        }
    }

    private void SelectFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_isProcessing)
        {
            return;
        }

        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select the folder containing your images."
        };

        if (dialog.ShowDialog() !=
            System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var folderPath = dialog.SelectedPath;
        var images = _imageScanner.ScanFolder(folderPath);

        FolderPathText.Text = folderPath;
        ImageCountText.Text = $"{images.Count} images found";

        foreach (var image in images)
        {
            try
            {
                image.GpsCoordinates =
                    _gpsMetadataReader.Read(image.FilePath);
            }
            catch (Exception ex)
            {
                // GPS metadata is optional. Keep the image available.
                System.Diagnostics.Debug.WriteLine(
                    $"Could not read GPS data from {image.FileName}: {ex.Message}");
            }
        }

        ImageList.ItemsSource = images;
        ExportCsvButton.IsEnabled = images.Count > 0;

        if (images.Count > 0)
        {
            ImageList.SelectedIndex = 0;
        }
        else
        {
            ClearMetadataFields();
        }

        UpdateImageCount();
        ProcessingProgressBar.Value = 0;
        ProcessingProgressText.Text = string.Empty;
    }

    private void ImageList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (ImageList.SelectedItem is not ImageFile imageFile)
        {
            ClearMetadataFields();
            return;
        }

        try
        {
            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imageFile.FilePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            ImagePreview.Source = bitmap;
        }
        catch
        {
            ImagePreview.Source = null;
        }

        UpdateMetadataFields(imageFile);
    }

    private void AdditionalInformationText_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile)
        {
            return;
        }

        imageFile.AdditionalInformation =
            AdditionalInformationText.Text;
    }

    private void PrimaryCategoryComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            imageFile.Metadata is null ||
            PrimaryCategoryComboBox.SelectedItem is not string category)
        {
            return;
        }

        imageFile.Metadata.Category = category;
    }

    private void SecondaryCategoryComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            imageFile.Metadata is null)
        {
            return;
        }

        imageFile.Metadata.SecondaryCategory =
            SecondaryCategoryComboBox.SelectedItem as string;
    }

    private async void AnalyzeAllNew_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isProcessing)
        {
            return;
        }

        var images = ImageList.Items
            .Cast<ImageFile>()
            .Where(image =>
                image.Status == ImageStatus.Open ||
                image.Status == ImageStatus.Error)
            .ToList();

        await ProcessImagesAsync(
            images,
            "No new images to analyze.");
    }

    private async void AnalyzeSelected_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isProcessing)
        {
            return;
        }

        var images = ImageList.SelectedItems
            .Cast<ImageFile>()
            .ToList();

        await ProcessImagesAsync(
            images,
            "Please select one or more images first.");
    }

    private async Task ProcessImagesAsync(
        IReadOnlyList<ImageFile> images,
        string emptyMessage)
    {
        if (images.Count == 0)
        {
            OllamaStatusText.Text = emptyMessage;
            return;
        }

        _isProcessing = true;

        var succeeded = 0;
        var failed = 0;

        ProcessingProgressBar.Minimum = 0;
        ProcessingProgressBar.Maximum = images.Count;
        ProcessingProgressBar.Value = 0;

        try
        {
            for (var index = 0; index < images.Count; index++)
            {
                var imageFile = images[index];

                OllamaStatusText.Text =
                    $"Processing {index + 1} of {images.Count}: " +
                    imageFile.FileName;

                ProcessingProgressText.Text =
                    $"{index + 1} / {images.Count}";

                try
                {
                    await _imageProcessingService.ProcessAsync(
                    imageFile,
                    updatedImage =>
                    {
                        if (ReferenceEquals(
                            ImageList.SelectedItem,
                            updatedImage))
                        {
                            UpdateMetadataFields(updatedImage);
                        }

                        ImageList.Items.Refresh();
                    },
                    recognizeLocations:
                        RecognizeLocationsCheckBox.IsChecked == true);

                    succeeded++;
                }
                catch (Exception ex)
                {
                    failed++;

                    // Keep processing the remaining images.
                    System.Diagnostics.Debug.WriteLine(
                        $"Failed to process {imageFile.FileName}: {ex.Message}");
                }

                ProcessingProgressBar.Value = index + 1;
                ImageList.Items.Refresh();
                UpdateImageCount();
            }
        }
        finally
        {
            _isProcessing = false;
        }

        OllamaStatusText.Text =
            $"Processing finished. Successful: {succeeded}, Failed: {failed}.";

        ProcessingProgressText.Text =
            $"Completed {images.Count} images.";
    }

    private void UpdateMetadataFields(ImageFile imageFile)
    {
        _isUpdatingFields = true;

        try
        {
            VisionDescriptionText.Text =
                imageFile.VisionDescription
                ?? "No vision description available.";

            AdditionalInformationText.Text =
                imageFile.AdditionalInformation;

            TitleText.Text =
                imageFile.Metadata?.Title
                ?? string.Empty;

            KeywordsText.Text =
                imageFile.Metadata is null
                    ? string.Empty
                    : string.Join(
                        ", ",
                        imageFile.Metadata.Keywords);

            PrimaryCategoryComboBox.SelectedItem =
                imageFile.Metadata?.Category;

            SecondaryCategoryComboBox.SelectedItem =
                imageFile.Metadata?.SecondaryCategory
                ?? string.Empty;
        }
        finally
        {
            _isUpdatingFields = false;
        }
    }

    private void ClearMetadataFields()
    {
        _isUpdatingFields = true;

        try
        {
            ImagePreview.Source = null;
            VisionDescriptionText.Clear();
            AdditionalInformationText.Clear();
            TitleText.Clear();
            KeywordsText.Clear();
            PrimaryCategoryComboBox.SelectedItem = null;
            SecondaryCategoryComboBox.SelectedItem = string.Empty;
        }
        finally
        {
            _isUpdatingFields = false;
        }
    }

    private void UpdateImageCount()
    {
        var total = ImageList.Items.Count;
        var completed = ImageList.Items
            .Cast<ImageFile>()
            .Count(image => image.Status == ImageStatus.Review);
        var failed = ImageList.Items
            .Cast<ImageFile>()
            .Count(image => image.Status == ImageStatus.Error);

        ImageCountText.Text =
            $"{total} images | {completed} reviewed | {failed} failed";
    }

    private readonly ShutterstockCsvExporter _csvExporter = new();

    private void TitleText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            imageFile.Metadata is null)
        {
            return;
        }

        imageFile.Metadata.Title = TitleText.Text;
    }

    private void KeywordsText_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            imageFile.Metadata is null)
        {
            return;
        }

        imageFile.Metadata.Keywords = KeywordsText.Text
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_isProcessing)
        {
            return;
        }

        var folderPath = FolderPathText.Text;

        if (string.IsNullOrWhiteSpace(folderPath) ||
            !Directory.Exists(folderPath))
        {
            System.Windows.MessageBox.Show(
                "Please select an image folder first.",
                "ImageMetaAI",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        ExportCsvButton.IsEnabled = false;
        ExportCsvButton.Content = "Exporting...";
        OllamaStatusText.Text = "Exporting Shutterstock CSV...";

        try
        {
            var images = ImageList.Items
                .Cast<ImageFile>()
                .ToList();

            var filePath = await _csvExporter.ExportAsync(
                folderPath,
                images);

            OllamaStatusText.Text = "CSV export completed.";

            System.Windows.MessageBox.Show(
                $"CSV export completed successfully:\n\n{filePath}",
                "ImageMetaAI",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            OllamaStatusText.Text = "CSV export failed.";

            System.Windows.MessageBox.Show(
                ex.Message,
                "CSV Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ExportCsvButton.Content = "Export Shutterstock CSV";
            ExportCsvButton.IsEnabled =
                Directory.Exists(FolderPathText.Text) &&
                !_isProcessing;
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var folderPath = FolderPathText.Text;

        if (string.IsNullOrWhiteSpace(folderPath) ||
            !Directory.Exists(folderPath))
        {
            System.Windows.MessageBox.Show(
                "Please select a valid image folder first.",
                "ImageMetaAI",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not open the folder:\n{ex.Message}",
                "ImageMetaAI",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
