namespace road_rescue
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register pages you navigate to that should NOT appear in the flyout
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(logInPage), typeof(logInPage));
            Routing.RegisterRoute(nameof(signUpPage), typeof(signUpPage));

            // If you navigate to content pages that aren’t flyout items:
            Routing.RegisterRoute(nameof(EmergencyRequestPage), typeof(EmergencyRequestPage));
            Routing.RegisterRoute(nameof(Vulcanizing), typeof(Vulcanizing));
            //Routing.RegisterRoute(nameof(repairShop), typeof(repairShop));
            //Routing.RegisterRoute(nameof(gasStation), typeof(gasStation));
        }

        private async void Logout_Clicked(object sender, EventArgs e)
        {
            try
            {
                await road_rescue.Services.AuthService.LogoutAsync();
                // swap back to your logged-out shell/root
                Application.Current.MainPage = new AuthShell();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Logout", $"Something went wrong: {ex.Message}", "OK");
            }
        }
    }
}
