namespace road_rescue.Driver_UI;

public partial class policeStation : ContentPage
{
	public policeStation()
	{
		InitializeComponent();
	}

    private void OnLocationClicked(object sender, EventArgs e)
    {

    }

    private void OnBackClicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

    private void OnChipClicked(object sender, EventArgs e)
    {

    }

    private void OnViewMapClicked(object sender, EventArgs e)
    {

    }

    private void OnCallClicked(object sender, EventArgs e)
    {

    }

    private void OnFavoriteClicked(object sender, EventArgs e)
    {

    }

    private async void OnMoreInfoClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MapPage());
    }

    private async void OnMessageClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ChatPage());
    }
}