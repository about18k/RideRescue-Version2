namespace road_rescue.Mechanic_UI;

public partial class trackingP : ContentPage
{
    public trackingP()
    {
        InitializeComponent();
    }

    private async void OnLiveTrackingClicked(object sender, EventArgs e)
    {
        // This is a placeholder for the actual live tracking logic.
        // You can later replace this with a map page navigation or location API.
        await DisplayAlert("Live Tracking", "Live tracking will be implemented here.", "OK");

        // Example: Navigate to another page if needed
        // await Navigation.PushAsync(new MapPage());
    }
}
