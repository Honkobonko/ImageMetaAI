using System.Net.Http;
using System.Windows;
using System.Windows.Media.Imaging;
using ImageMetaAI.Models;
using ImageMetaAI.Services;

namespace ImageMetaAI;

public partial class MainWindow : Window
{
    private readonly ImageScanner _imageScanner = new();
    private readonly IOllamaService _ollamaService;
    private readonly IImageAnalyzer _imageAnalyzer;

    public MainWindow()
    {
        InitializeComponent();

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var ollamaClient = new OllamaClient(httpClient);

        var modelSettings = new OllamaModelSettings();

        _ollamaService = new OllamaService(
            ollamaClient,
            modelSettings);

        _imageAnalyzer = new ImageAnalyzer(
            ollamaClient,
            modelSettings);

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        OllamaStatusText.Text = "Checking Ollama...";

        var isReady = await _ollamaService.IsReadyAsync();

        OllamaStatusText.Text = isReady
            ? "Ollama ready"
            : "Ollama is not ready";
    }

    private void SelectFolder_Click(object sender, RoutedEventArgs e)
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
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ImageList.SelectedItem is not ImageFile imageFile)
        {
            ImagePreview.Source = null;
            VisionDescriptionText.Clear();
            return;
        }

        var bitmap = new BitmapImage();

        bitmap.BeginInit();
        bitmap.UriSource = new Uri(imageFile.FilePath);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();

        ImagePreview.Source = bitmap;
        VisionDescriptionText.Text =
            imageFile.VisionDescription ?? "No vision description available.";
    }

    private async void AnalyzeImage_Click(object sender, RoutedEventArgs e)
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
            OllamaStatusText.Text = "Analyzing image...";            
            imageFile.Status = ImageStatus.Processing;
            ImageList.Items.Refresh();

            var result = await _imageAnalyzer.AnalyzeAsync(imageFile.FilePath);

            imageFile.VisionDescription = result;

            VisionDescriptionText.Text = result;

            OllamaStatusText.Text = "Analysis complete.";  
            imageFile.Status = ImageStatus.Review;
            ImageList.Items.Refresh();         
        }
        catch (Exception ex)
        {
            OllamaStatusText.Text = "Image analysis failed.";

            imageFile.Status = ImageStatus.Error;
            ImageList.Items.Refresh();

            System.Windows.MessageBox.Show(
                ex.Message,
                "ImageMetaAI");
        }
    }
}
