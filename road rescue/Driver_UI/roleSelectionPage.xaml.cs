namespace road_rescue;

public partial class roleSelectionPage : ContentPage
{
	public roleSelectionPage()
	{
		InitializeComponent();
	}

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void logIn_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new logInPage());
    }

    private void DriverButton_Clicked(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new signUpPage());
    }

    private void MechanicButton_Clicked(object sender, TappedEventArgs e)
    {

    }
}