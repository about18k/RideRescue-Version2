using System;
using Microsoft.Maui.Controls;

namespace road_rescue
{
    public partial class TrackingPage : ContentPage
    {
        public TrackingPage()
        {
            InitializeComponent();
        }

        private async void OnLiveTrackingClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Live Tracking", "Live tracking will be implemented here.", "OK");

            // Example for future use
            // await Navigation.PushAsync(new MapPage());
        }

        private async void OnBackTapped(object sender, EventArgs e)
        {
            // Navigate back to SecondPage
            await Navigation.PushAsync(new SecondPage());

            // Or just go back in navigation stack
            // await Navigation.PopAsync();
        }
    }
}
