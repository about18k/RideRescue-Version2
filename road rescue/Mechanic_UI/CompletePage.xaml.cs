using Microsoft.Maui.Controls;

namespace road_rescue
{
    public partial class CompletePage : ContentPage
    {
        private bool _isDrawerOpen = false;

        public CompletePage()
        {
            InitializeComponent();
        }

        private async void ToggleDrawer()
        {
            if (_isDrawerOpen)
            {
                // Close drawer with animation
                await DrawerMenu.TranslateTo(-280, 0, 300, Easing.CubicOut);
                DrawerOverlay.IsVisible = false;
            }
            else
            {
                // Open drawer with animation
                await DrawerMenu.TranslateTo(0, 0, 300, Easing.CubicIn);
                DrawerOverlay.IsVisible = true;
            }
            _isDrawerOpen = !_isDrawerOpen;
        }

        private void OnMenuClicked(object sender, EventArgs e)
        {
            ToggleDrawer();
        }

        private void OnOverlayTapped(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
                ToggleDrawer();
        }

        private void OnNotificationsClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
                ToggleDrawer();
            // TODO: Add navigation or logic
        }

        private void OnRequestClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
                ToggleDrawer();
            // TODO: Navigate to request page
        }

        private async void OnMessagesClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
                ToggleDrawer();
            await Navigation.PushAsync(new messagesM());
        }

        private void OnCompleteClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
                ToggleDrawer();
            // Already on Completed page
        }

        private async void OnImageClicked(object sender, EventArgs e)
        {
            // Optionally show the full image or open a gallery view
            await DisplayAlert("Image", "Open full image preview here.", "OK");
            // Or navigate to a full-screen image view page if you wish
        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            ToggleDrawer();
        }

        private async void OnProfileTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new MechanicProfile());

        }

        private async void OnLogoutTapped(object sender, TappedEventArgs e)
        {
            try
            {
                await road_rescue.Services.AuthService.LogoutAsync();

                // Hard-reset the root to Login (no back navigation possible)
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Application.Current.MainPage = new NavigationPage(new logInPage());
                    // or: Application.Current.MainPage = new logInPage();
                });
            }
            catch (Exception ex)
            {
                // Optional: log or show a friendly message
                await DisplayAlert("Logout", $"Something went wrong: {ex.Message}", "OK");
            }

        }
    }
}
