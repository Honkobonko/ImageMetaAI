using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private readonly ShutterstockCsvExporter _shutterstockCsvExporter = new();
    private readonly AdobeStockCsvExporter _adobeStockCsvExporter = new();

    private bool _isUpdatingFields;
    private bool _isProcessing;
    private CancellationTokenSource? _processingCancellationTokenSource;

    private const double DefaultFontSize = 16.0;
    private const double MinimumFontSize = 10.0;
    private const double MaximumFontSize = 22.0;
    private const double FontSizeStep = 1.0;

    private double _currentFontSize = DefaultFontSize;

    public MainWindow()
    {
        InitializeComponent();

        SetAppFontSize(DefaultFontSize);

        ShutterstockPrimaryCategoryComboBox.ItemsSource =
            ShutterstockCategories.All;

        ShutterstockSecondaryCategoryComboBox.ItemsSource =
            new[] { string.Empty }
                .Concat(ShutterstockCategories.All)
                .ToList();

        AdobeStockCategoryComboBox.ItemsSource =
            AdobeStockCategories.All
                .Select(category => category.Name)
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

    // Font size controls

    private void SetAppFontSize(double fontSize)
    {
        _currentFontSize = Math.Clamp(
            fontSize,
            MinimumFontSize,
            MaximumFontSize);

        FontSize = _currentFontSize;
    }

    private void ChangeAppFontSize(double delta)
    {
        SetAppFontSize(_currentFontSize + delta);
    }

    private void FontSizeIncrease_Click(
        object sender,
        RoutedEventArgs e)
    {
        ChangeAppFontSize(FontSizeStep);
    }

    private void FontSizeDecrease_Click(
        object sender,
        RoutedEventArgs e)
    {
        ChangeAppFontSize(-FontSizeStep);
    }

    private void FontSizeReset_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetAppFontSize(DefaultFontSize);
    }

    private void MainWindow_PreviewKeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) !=
            ModifierKeys.Control)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Add:
            case Key.OemPlus:
                ChangeAppFontSize(FontSizeStep);
                e.Handled = true;
                break;

            case Key.Subtract:
            case Key.OemMinus:
                ChangeAppFontSize(-FontSizeStep);
                e.Handled = true;
                break;

            case Key.D0:
            case Key.NumPad0:
                SetAppFontSize(DefaultFontSize);
                e.Handled = true;
                break;
        }
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

    private void SelectFolder_Click(
        object sender,
        RoutedEventArgs e)
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

        VisionProgressBar.Value = 0;
        VisionProgressText.Text = images.Count > 0
            ? $"0 / {images.Count}"
            : string.Empty;

        MetadataProgressBar.Value = 0;
        MetadataProgressText.Text = images.Count > 0
            ? "Waiting for vision analysis"
            : string.Empty;
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

    private void LocationText_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile)
        {
            return;
        }

        imageFile.ResolvedLocation = LocationText.Text;

        imageFile.IsLocationManuallyEdited =
            !string.IsNullOrWhiteSpace(LocationText.Text);

        UpdateLocationStatus(imageFile);
    }

    private void ShutterstockTitleText_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            imageFile.ShutterstockMetadata is not { } metadata)
        {
            return;
        }

        metadata.Title = ShutterstockTitleText.Text;
    }

    private void ShutterstockKeywordsText_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            imageFile.ShutterstockMetadata is not { } metadata)
        {
            return;
        }

        metadata.Keywords = ShutterstockKeywordsText.Text
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);
    }

    private void AdobeStockTitleText_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            imageFile.AdobeStockMetadata is not { } metadata)
        {
            return;
        }

        metadata.Title = AdobeStockTitleText.Text;
    }

    private void AdobeStockKeywordsText_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            imageFile.AdobeStockMetadata is not { } metadata)
        {
            return;
        }

        metadata.Keywords = AdobeStockKeywordsText.Text
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);
    }

    private void ShutterstockPrimaryCategoryComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            ShutterstockPrimaryCategoryComboBox.SelectedItem is not string category ||
            imageFile.ShutterstockMetadata is not { } metadata)
        {
            return;
        }

        metadata.Category = category;
    }

    private void ShutterstockSecondaryCategoryComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            imageFile.ShutterstockMetadata is not { } metadata)
        {
            return;
        }

        metadata.SecondaryCategory =
            ShutterstockSecondaryCategoryComboBox.SelectedItem as string;
    }

    private void AdobeStockCategoryComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isUpdatingFields ||
            ImageList.SelectedItem is not ImageFile imageFile ||
            AdobeStockCategoryComboBox.SelectedItem is not string categoryName ||
            imageFile.AdobeStockMetadata is not { } metadata)
        {
            return;
        }

        metadata.Category = categoryName;
    }

    private void UpdateLocationStatus(ImageFile imageFile)
    {
        if (imageFile.IsLocationManuallyEdited &&
            !string.IsNullOrWhiteSpace(imageFile.ResolvedLocation))
        {
            LocationStatusText.Text =
                "Manually edited. Clear the field to allow automatic GPS lookup during analysis.";
        }
        else if (!string.IsNullOrWhiteSpace(imageFile.ResolvedLocation))
        {
            LocationStatusText.Text =
                "Location detected or provided. You can edit it at any time.";
        }
        else if (imageFile.GpsCoordinates is null)
        {
            LocationStatusText.Text =
                "No location set. No GPS coordinates are available for automatic lookup.";
        }
        else
        {
            LocationStatusText.Text =
                "No location set. GPS lookup can fill this field during analysis if enabled.";
        }
    }

    private async void AnalyzeAll_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isProcessing)
        {
            return;
        }

        var images = ImageList.Items
            .Cast<ImageFile>()
            .ToList();

        await ProcessImagesAsync(
            images,
            "No images loaded.");
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

        var selectedPlatforms = new List<MetadataPlatform>();

        if (ShutterstockMetadataCheckBox.IsChecked == true)
        {
            selectedPlatforms.Add(MetadataPlatform.Shutterstock);
        }

        if (AdobeStockMetadataCheckBox.IsChecked == true)
        {
            selectedPlatforms.Add(MetadataPlatform.AdobeStock);
        }

        if (selectedPlatforms.Count == 0)
        {
            OllamaStatusText.Text =
                "Please select at least one metadata platform.";
            return;
        }

        _isProcessing = true;
        _processingCancellationTokenSource = new CancellationTokenSource();

        SetProcessingControlsEnabled(false);

        var cancelled = false;
        var batchFailed = false;

        // Track results only for this processing run.
        var succeededImages = new HashSet<ImageFile>();
        var failedImages = new HashSet<ImageFile>();

        var total = images.Count;

        VisionProgressBar.Minimum = 0;
        VisionProgressBar.Maximum = total;
        VisionProgressBar.Value = 0;
        VisionProgressText.Text = $"0 / {total}";

        MetadataProgressBar.Minimum = 0;
        MetadataProgressBar.Maximum = total;
        MetadataProgressBar.Value = 0;
        MetadataProgressText.Text = "Waiting for vision analysis";

        try
        {
            await _imageProcessingService.ProcessBatchAsync(
                images,
                imageUpdated: updatedImage =>
                {
                    if (updatedImage.Status == ImageStatus.Error)
                    {
                        failedImages.Add(updatedImage);
                        succeededImages.Remove(updatedImage);
                    }
                    else if (updatedImage.Status == ImageStatus.MetadataCompleted)
                    {
                        succeededImages.Add(updatedImage);
                        failedImages.Remove(updatedImage);
                    }

                    if (ReferenceEquals(
                        ImageList.SelectedItem,
                        updatedImage))
                    {
                        UpdateMetadataFields(updatedImage);
                    }

                    ImageList.Items.Refresh();
                    UpdateImageCount();
                },
                progressCallback: (phase, completed, phaseTotal, message) =>
                {
                    OllamaStatusText.Text = message;

                    if (phase == ProcessingPhase.Vision)
                    {
                        VisionProgressBar.Maximum = phaseTotal;
                        VisionProgressBar.Value = completed;
                        VisionProgressText.Text =
                            $"{completed} / {phaseTotal}";
                    }
                    else if (phase == ProcessingPhase.Metadata)
                    {
                        MetadataProgressBar.Maximum = phaseTotal;
                        MetadataProgressBar.Value = completed;
                        MetadataProgressText.Text =
                            $"{completed} / {phaseTotal}";
                    }
                },
                cancellationToken: _processingCancellationTokenSource.Token,
                recognizeLocations:
                    RecognizeLocationsCheckBox.IsChecked == true,
                platforms: selectedPlatforms,
                errorCallback: (imageFile, ex) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(
                            $"File: {imageFile.FileName}\n\n" +
                            $"Error type: {ex.GetType().Name}\n\n" +
                            $"Details:\n{ex}",
                            "ImageMetaAI - Processing Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                });
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch (Exception ex)
        {
            batchFailed = true;

            System.Diagnostics.Debug.WriteLine(
                $"Batch processing failed: {ex}");
        }
        finally
        {
            // Refresh final counts from the images processed in this run.
            foreach (var image in images)
            {
                if (image.Status == ImageStatus.MetadataCompleted)
                {
                    succeededImages.Add(image);
                    failedImages.Remove(image);
                }
                else if (image.Status == ImageStatus.Error)
                {
                    failedImages.Add(image);
                    succeededImages.Remove(image);
                }
            }

            _processingCancellationTokenSource.Dispose();
            _processingCancellationTokenSource = null;

            _isProcessing = false;
            SetProcessingControlsEnabled(true);

            ImageList.Items.Refresh();
            UpdateImageCount();
        }

        var succeeded = succeededImages.Count;
        var failed = failedImages.Count;

        if (cancelled)
        {
            OllamaStatusText.Text =
                $"Processing cancelled. Successful: {succeeded}, Failed: {failed}.";

            MetadataProgressText.Text =
                $"Cancelled at {MetadataProgressBar.Value:0} / {total}";
        }
        else if (batchFailed)
        {
            OllamaStatusText.Text =
                $"Batch processing failed. Successful: {succeeded}, Failed: {failed}.";
        }
        else
        {
            OllamaStatusText.Text =
                $"Processing finished. Successful: {succeeded}, Failed: {failed}.";

            VisionProgressText.Text = $"{total} / {total}";
            MetadataProgressText.Text = $"{total} / {total}";
        }
    }

    private void SetProcessingControlsEnabled(bool enabled)
    {
        SelectFolderButton.IsEnabled = enabled;
        AnalyzeAllButton.IsEnabled = enabled;
        AnalyzeSelectedButton.IsEnabled = enabled;
        CancelProcessingButton.IsEnabled = !enabled;

        ShutterstockMetadataCheckBox.IsEnabled = enabled;
        AdobeStockMetadataCheckBox.IsEnabled = enabled;
        RecognizeLocationsCheckBox.IsEnabled = enabled;

        ImageList.IsEnabled = true;

        LocationText.IsEnabled = enabled;
        AdditionalInformationText.IsEnabled = enabled;

        ShutterstockTitleText.IsEnabled = enabled;
        ShutterstockKeywordsText.IsEnabled = enabled;
        ShutterstockPrimaryCategoryComboBox.IsEnabled = enabled;
        ShutterstockSecondaryCategoryComboBox.IsEnabled = enabled;

        AdobeStockTitleText.IsEnabled = enabled;
        AdobeStockKeywordsText.IsEnabled = enabled;
        AdobeStockCategoryComboBox.IsEnabled = enabled;

        ExportCsvButton.IsEnabled =
            enabled && ImageList.Items.Count > 0;
    }

    private void CancelProcessing_Click(
        object sender,
        RoutedEventArgs e)
    {
        _processingCancellationTokenSource?.Cancel();

        CancelProcessingButton.IsEnabled = false;
        OllamaStatusText.Text = "Cancellation requested...";
    }

    private void UpdateMetadataFields(ImageFile imageFile)
    {
        _isUpdatingFields = true;

        try
        {
            VisionDescriptionText.Text =
                imageFile.VisionDescription
                ?? "No vision description available.";

            LocationText.Text =
                imageFile.ResolvedLocation ?? string.Empty;

            UpdateLocationStatus(imageFile);

            AdditionalInformationText.Text =
                imageFile.AdditionalInformation;

            var shutterstock = imageFile.ShutterstockMetadata;

            ShutterstockTitleText.Text =
                shutterstock?.Title ?? string.Empty;

            ShutterstockKeywordsText.Text = shutterstock is null
                ? string.Empty
                : string.Join(", ", shutterstock.Keywords);

            ShutterstockPrimaryCategoryComboBox.SelectedItem =
                shutterstock?.Category;

            ShutterstockSecondaryCategoryComboBox.SelectedItem =
                shutterstock?.SecondaryCategory ?? string.Empty;

            var adobeStock = imageFile.AdobeStockMetadata;

            AdobeStockTitleText.Text =
                adobeStock?.Title ?? string.Empty;

            AdobeStockKeywordsText.Text = adobeStock is null
                ? string.Empty
                : string.Join(", ", adobeStock.Keywords);

            var categoryName = adobeStock?.Category;

            AdobeStockCategoryComboBox.SelectedItem =
                AdobeStockCategories.All.Any(
                    category => category.Name == categoryName)
                    ? categoryName
                    : null;
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
            LocationText.Clear();
            LocationStatusText.Text =
                "Select an image to see its location status.";
            AdditionalInformationText.Clear();

            ShutterstockTitleText.Clear();
            ShutterstockKeywordsText.Clear();
            ShutterstockPrimaryCategoryComboBox.SelectedItem = null;
            ShutterstockSecondaryCategoryComboBox.SelectedItem = string.Empty;

            AdobeStockTitleText.Clear();
            AdobeStockKeywordsText.Clear();
            AdobeStockCategoryComboBox.SelectedItem = null;
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
            .Count(image => image.Status == ImageStatus.MetadataCompleted);

        var failed = ImageList.Items
            .Cast<ImageFile>()
            .Count(image => image.Status == ImageStatus.Error);

        ImageCountText.Text =
            $"{total} images | {completed} reviewed | {failed} failed";
    }

    private async void ExportCsv_Click(
        object sender,
        RoutedEventArgs e)
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

        var exportShutterstock =
            ShutterstockMetadataCheckBox.IsChecked == true;

        var exportAdobeStock =
            AdobeStockMetadataCheckBox.IsChecked == true;

        if (!exportShutterstock && !exportAdobeStock)
        {
            System.Windows.MessageBox.Show(
                "Please select at least one metadata platform to export.",
                "ImageMetaAI",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var images = ImageList.Items
            .Cast<ImageFile>()
            .ToList();

        ExportCsvButton.IsEnabled = false;
        ExportCsvButton.Content = "Exporting...";

        var exportedFiles = new List<string>();
        var errors = new List<string>();

        try
        {
            if (exportShutterstock)
            {
                OllamaStatusText.Text =
                    "Exporting Shutterstock CSV...";

                try
                {
                    var filePath =
                        await _shutterstockCsvExporter.ExportAsync(
                            folderPath,
                            images);

                    exportedFiles.Add(filePath);
                }
                catch (Exception ex)
                {
                    errors.Add($"Shutterstock: {ex.Message}");
                }
            }

            if (exportAdobeStock)
            {
                OllamaStatusText.Text =
                    "Exporting Adobe Stock CSV...";

                try
                {
                    var filePath =
                        await _adobeStockCsvExporter.ExportAsync(
                            folderPath,
                            images);

                    exportedFiles.Add(filePath);
                }
                catch (Exception ex)
                {
                    errors.Add($"Adobe Stock: {ex.Message}");
                }
            }

            if (errors.Count == 0)
            {
                OllamaStatusText.Text = "CSV export completed.";

                System.Windows.MessageBox.Show(
                    "CSV export completed successfully:\n\n" +
                    string.Join("\n", exportedFiles),
                    "ImageMetaAI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                OllamaStatusText.Text =
                    exportedFiles.Count > 0
                        ? "CSV export completed with errors."
                        : "CSV export failed.";

                var message = string.Empty;

                if (exportedFiles.Count > 0)
                {
                    message +=
                        "Successfully exported:\n" +
                        string.Join("\n", exportedFiles) +
                        "\n\n";
                }

                message +=
                    "Export errors:\n" +
                    string.Join("\n", errors);

                System.Windows.MessageBox.Show(
                    message,
                    "CSV Export Results",
                    MessageBoxButton.OK,
                    errors.Count == exportedFiles.Count
                        ? MessageBoxImage.Error
                        : MessageBoxImage.Warning);
            }
        }
        finally
        {
            ExportCsvButton.Content = "Export CSV";
            ExportCsvButton.IsEnabled =
                Directory.Exists(FolderPathText.Text) &&
                !_isProcessing;
        }
    }

    private void OpenFolder_Click(
        object sender,
        RoutedEventArgs e)
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
