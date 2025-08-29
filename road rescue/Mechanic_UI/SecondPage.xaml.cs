using Microsoft.Maui.Controls;

namespace road_rescue
{

	public partial class SecondPage : ContentPage
	{
		public SecondPage()
		{
			InitializeComponent();
		}


        // Navigation Methods

        private async void OnRequestTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RequestPage());
        }

        private async void OnTrackingTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TrackingPage());
        }

        private async void OnMessagesTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new messagesM());
        }

        private async void OnPaymentsTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PaymentPage());
        }

        private async void OnCompleteTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CompletePage());
        }

        private async void OnNavHomeClicked(object sender, EventArgs e)
        {
            // This could refresh or just stay on MainPage
            await DisplayAlert("Home", "You're already on the Home Page, my Lord Stayve.", "OK");
        }

        private async void OnUserIconTapped(object sender, TappedEventArgs e)
        {
            // Optional: You can show a loading spinner or change the icon's color here if needed

            // 1.5-second delay (1500 milliseconds)
            await Task.Delay(1500);

            // Navigate to MechanicProfilePage after the delay
            await Navigation.PushAsync(new MechanicProfile());
        }
    }
}