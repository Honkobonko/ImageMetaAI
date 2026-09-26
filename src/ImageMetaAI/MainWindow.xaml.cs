using System.Windows;
using System.Windows.Media.Imaging;
using ImageMetaAI.Models;
using ImageMetaAI.Services;

namespace ImageMetaAI;

public partial class MainWindow : Window
{
    private readonly ImageScanner _imageScanner = new();

    public MainWindow()
    {
        InitializeComponent();
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
            return;
        }

        var bitmap = new BitmapImage();

        bitmap.BeginInit();
        bitmap.UriSource = new Uri(imageFile.FilePath);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();

        ImagePreview.Source = bitmap;
    }
}
