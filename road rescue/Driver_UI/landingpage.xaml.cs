using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using road_rescue.Driver_UI;

namespace road_rescue
{
    public partial class landingpage : ContentPage
    {
        public landingpage()
        {
            InitializeComponent();
        }

        private async void OnOtherHelpServicesTapped(object sender, TappedEventArgs e)
        {
            var popup = new OtherServicesPopup();
            await this.ShowPopupAsync(popup);
        }

        private async void OnVulcanizingShopsTapped(object sender, TappedEventArgs e)
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var location = await Geolocation.GetLocationAsync();
                // You can keep PushAsync, or switch to Shell routes if registered.
                await Navigation.PushAsync(new Vulcanizing(location, SupabaseService.Client));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void ProfileButton_Clicked(object sender, EventArgs e)
        {
            if (Application.Current?.MainPage is Shell shell)
                shell.FlyoutIsPresented = true; // open flyout safely
        }

        private async void sosbutton_Clicked(object sender, EventArgs e)
        {
            // If registered as a Shell route, do: await Shell.Current.GoToAsync(nameof(EmergencyRequestPage));
            await Navigation.PushAsync(new EmergencyRequestPage());
        }

        private async void ResetOnboarding_Clicked(object sender, EventArgs e)
        {
            Preferences.Set("IsFirstLaunch", true);
            // Replace the root with the logged-out shell
            Application.Current.MainPage = new AuthShell();
        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            if (Application.Current?.MainPage is Shell shell)
                shell.FlyoutIsPresented = true;
        }

        private async void OnRepairShopTapped(object sender, TappedEventArgs e)
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var location = await Geolocation.GetLocationAsync();
                await Navigation.PushAsync(new repairShop(location, SupabaseService.Client));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnGasStationTapped(object sender, TappedEventArgs e)
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var location = await Geolocation.GetLocationAsync();
                await Navigation.PushAsync(new gasStation(location, SupabaseService.Client));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
