namespace road_rescue;

public partial class CertificatePreviewPage : ContentPage
{
    public CertificatePreviewPage(string filePath)
    {
        InitializeComponent();

        previewImage.Source = ImageSource.FromFile(filePath);
    }
}
