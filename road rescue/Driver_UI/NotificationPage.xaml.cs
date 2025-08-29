namespace road_rescue;

public partial class NotificationPage : ContentPage
{
	public NotificationPage()
	{
		InitializeComponent();
	}

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}