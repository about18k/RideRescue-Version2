using CommunityToolkit.Maui.Views;

namespace road_rescue;

public partial class MechanicInfoPopup : Popup
{
    public MechanicInfoPopup()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }

    private async void OnNavigateToMessagesClicked(object sender, EventArgs e)
    {
        Close(); // Close the popup first
        await Shell.Current.GoToAsync("//MessagesPage");
    }

    private async void OnLocationTapped(object sender, TappedEventArgs e)
    {
        Close(); // Close popup first

        // If your MainPage is a NavigationPage or has a NavigationPage somewhere in hierarchy:
        if (Application.Current.MainPage is NavigationPage navPage)
        {
            await navPage.PushAsync(new MapPage());
        }
        else if (Application.Current.MainPage is Shell shell)
        {
            // If MainPage is Shell, try to get Navigation from Shell's CurrentPage
            var currentPage = shell.CurrentPage;
            if (currentPage != null)
                await currentPage.Navigation.PushAsync(new MapPage());
        }
        else
        {
            // fallback: just set MainPage to new NavigationPage with MapPage (rarely desired)
            Application.Current.MainPage = new NavigationPage(new MapPage());
        }
    }

}