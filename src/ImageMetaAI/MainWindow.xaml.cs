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

        _imageProcessingService = new ImageProcessingService(
            imageAnalyzer,
            metadataGenerator);

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        OllamaStatusText.Text = "Checking Ollama...";

        var isReady = await _ollamaService.IsReadyAsync();

        OllamaStatusText.Text = isReady
            ? "Ollama ready"
            : "Ollama is not ready";
    }

    private void SelectFolder_Click(
        object sender,
        RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select the folder containing your images."
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var folderPath = dialog.SelectedPath;

        var images = _imageScanner.ScanFolder(folderPath);

        FolderPathText.Text = folderPath;
        ImageCountText.Text = $"{images.Count} images found";

        ImageList.ItemsSource = images;

        if (images.Count > 0)
        {
            ImageList.SelectedIndex = 0;
        }
    }

    private void ImageList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (ImageList.SelectedItem is not ImageFile imageFile)
        {
            ImagePreview.Source = null;
            VisionDescriptionText.Clear();
            TitleText.Clear();
            KeywordsText.Clear();
            PrimaryCategoryComboBox.SelectedItem = null;
            SecondaryCategoryComboBox.SelectedItem = string.Empty;

            return;
        }

        var bitmap = new BitmapImage();

        bitmap.BeginInit();
        bitmap.UriSource = new Uri(imageFile.FilePath);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();

        ImagePreview.Source = bitmap;

        UpdateMetadataFields(imageFile);
    }

    private async void AnalyzeImage_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ImageList.SelectedItem is not ImageFile imageFile)
        {
            System.Windows.MessageBox.Show(
                "Please select an image first.",
                "ImageMetaAI");

            return;
        }

        try
        {
            OllamaStatusText.Text = "Processing image...";

            await _imageProcessingService.ProcessAsync(
                imageFile,
                updatedImage =>
                {
                    UpdateMetadataFields(updatedImage);

                    ImageList.Items.Refresh();
                });

            OllamaStatusText.Text = "Processing complete.";

            ImageList.Items.Refresh();
        }
        catch (Exception ex)
        {
            OllamaStatusText.Text = "Image processing failed.";

            ImageList.Items.Refresh();

            System.Windows.MessageBox.Show(
                ex.Message,
                "ImageMetaAI");
        }
    }

    private void UpdateMetadataFields(
        ImageFile imageFile)
    {
        VisionDescriptionText.Text =
            imageFile.VisionDescription
            ?? "No vision description available.";

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

    private void PrimaryCategoryComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (ImageList.SelectedItem is not ImageFile imageFile ||
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
        if (ImageList.SelectedItem is not ImageFile imageFile ||
            imageFile.Metadata is null)
        {
            return;
        }

        imageFile.Metadata.SecondaryCategory =
            SecondaryCategoryComboBox.SelectedItem as string;
    }
}
