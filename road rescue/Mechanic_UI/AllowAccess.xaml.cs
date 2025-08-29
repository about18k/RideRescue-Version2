using Microsoft.Maui.Controls;

namespace road_rescue.Mechanic_UI
{
    public partial class AllowAccess : ContentPage
    {
        public AllowAccess()
        {
            InitializeComponent();
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnAllowAccessClicked(object sender, EventArgs e)
        {
            // Here you can add actual location permission logic if you want

            await DisplayAlert("Permission", "Location access granted (simulated).", "OK");
            await Navigation.PushAsync(new LocationPage());

        }

    }
}
