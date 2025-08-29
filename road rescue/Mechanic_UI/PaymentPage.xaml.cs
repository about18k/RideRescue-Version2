namespace road_rescue;

public partial class PaymentPage : ContentPage
{
    private bool _isMenuOpen = false;

    public PaymentPage()
    {
        InitializeComponent();
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        if (_isMenuOpen)
        {
            await DrawerMenu.TranslateTo(-240, 0, 250, Easing.SinIn);
            _isMenuOpen = false;
        }
        else
        {
            await DrawerMenu.TranslateTo(0, 0, 250, Easing.SinOut);
            _isMenuOpen = true;
        }
    }

    private async void OnPaidClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Status Updated", "Marked as Paid 🟢", "OK");
    }

    private async void OnPendingClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Status Updated", "Marked as Pending 🟡", "OK");
    }

    private async void OnMoreClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Options", "Cancel", null, "Delete", "Archive");
        if (action == "Delete")
        {
            await DisplayAlert("Deleted", "Record has been deleted 🗑️", "OK");
        }
        else if (action == "Archive")
        {
            await DisplayAlert("Archived", "Record archived 🗂️", "OK");
        }
    }

    private async void OnProfileTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new MechanicProfile());
    }

    private async void Button_Clicked(object sender, EventArgs e)
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
