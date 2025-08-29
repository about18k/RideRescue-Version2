namespace road_rescue;

public partial class HospitalPage : ContentPage
{
	public HospitalPage()
	{
		InitializeComponent();
	}

    private async void OnMoreInfoClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MapPage());
    }

    private void OnChipClicked(object sender, EventArgs e)
    {

    }
}