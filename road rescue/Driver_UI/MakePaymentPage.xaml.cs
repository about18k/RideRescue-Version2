namespace road_rescue;

public partial class MakePaymentPage : ContentPage
{
	public MakePaymentPage()
	{
		InitializeComponent();
	}

    private  void OnBackButtonClicked(object sender, EventArgs e)
    {
		Navigation.PopAsync();
    }

    private void OnCashTapped(object sender, TappedEventArgs e)
    {

    }

    private void OnGcashTapped(object sender, TappedEventArgs e)
    {

    }
}