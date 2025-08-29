using CommunityToolkit.Maui.Views;
using road_rescue.Driver_UI;

namespace road_rescue;

public partial class OtherServicesPopup : Popup
{
    public OtherServicesPopup()
    {
        InitializeComponent();
    }

    private void OnHospitalClicked(object sender, EventArgs e)
    {

    }

    private void OnPoliceClicked(object sender, EventArgs e)
    {

    }

    private void OnFireStationClicked(object sender, EventArgs e)
    {

    }

    private void OnRescueClicked(object sender, EventArgs e)
    {

    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }

    private async void OnHospitalLabelTapped(object sender, TappedEventArgs e)
    {
        Close(); // Close popup first

        // If your MainPage is a NavigationPage or has a NavigationPage somewhere in hierarchy:
        if (Application.Current.MainPage is NavigationPage navPage)
        {
            await navPage.PushAsync(new HospitalPage());
        }
        else if (Application.Current.MainPage is Shell shell)
        {
            // If MainPage is Shell, try to get Navigation from Shell's CurrentPage
            var currentPage = shell.CurrentPage;
            if (currentPage != null)
                await currentPage.Navigation.PushAsync(new HospitalPage());
        }
        else
        {
            // fallback: just set MainPage to new NavigationPage with MapPage (rarely desired)
            Application.Current.MainPage = new NavigationPage(new HospitalPage());
        }
    }

    private async void OnHospitalTapped(object sender, TappedEventArgs e)
    {
        Close(); // Close popup first

        // If your MainPage is a NavigationPage or has a NavigationPage somewhere in hierarchy:
        if (Application.Current.MainPage is NavigationPage navPage)
        {
            await navPage.PushAsync(new HospitalPage());
        }
        else if (Application.Current.MainPage is Shell shell)
        {
            // If MainPage is Shell, try to get Navigation from Shell's CurrentPage
            var currentPage = shell.CurrentPage;
            if (currentPage != null)
                await currentPage.Navigation.PushAsync(new HospitalPage());
        }
        else
        {
            // fallback: just set MainPage to new NavigationPage with MapPage (rarely desired)
            Application.Current.MainPage = new NavigationPage(new HospitalPage());
        }
    }

    private async void OnRescueTapped(object sender, TappedEventArgs e)
    {
        Close(); // Close popup first

        // If your MainPage is a NavigationPage or has a NavigationPage somewhere in hierarchy:
        if (Application.Current.MainPage is NavigationPage navPage)
        {
            await navPage.PushAsync(new HospitalPage());
        }
        else if (Application.Current.MainPage is Shell shell)
        {
            // If MainPage is Shell, try to get Navigation from Shell's CurrentPage
            var currentPage = shell.CurrentPage;
            if (currentPage != null)
                await currentPage.Navigation.PushAsync(new MDRRMO());
        }
        else
        {
            // fallback: just set MainPage to new NavigationPage with MapPage (rarely desired)
            Application.Current.MainPage = new NavigationPage(new MDRRMO());
        }
    }

    private async void OnPoliceTapped(object sender, TappedEventArgs e)
    {
        Close(); // Close popup first

        // If your MainPage is a NavigationPage or has a NavigationPage somewhere in hierarchy:
        if (Application.Current.MainPage is NavigationPage navPage)
        {
            await navPage.PushAsync(new policeStation());
        }
        else if (Application.Current.MainPage is Shell shell)
        {
            // If MainPage is Shell, try to get Navigation from Shell's CurrentPage
            var currentPage = shell.CurrentPage;
            if (currentPage != null)
                await currentPage.Navigation.PushAsync(new policeStation());
        }
        else
        {
            // fallback: just set MainPage to new NavigationPage with MapPage (rarely desired)
            Application.Current.MainPage = new NavigationPage(new policeStation());
        }
    }

    private async void OnFireTapped(object sender, TappedEventArgs e)
    {
        Close(); // Close popup first

        // If your MainPage is a NavigationPage or has a NavigationPage somewhere in hierarchy:
        if (Application.Current.MainPage is NavigationPage navPage)
        {
            await navPage.PushAsync(new fireStation());
        }
        else if (Application.Current.MainPage is Shell shell)
        {
            // If MainPage is Shell, try to get Navigation from Shell's CurrentPage
            var currentPage = shell.CurrentPage;
            if (currentPage != null)
                await currentPage.Navigation.PushAsync(new fireStation());
        }
        else
        {
            // fallback: just set MainPage to new NavigationPage with MapPage (rarely desired)
            Application.Current.MainPage = new NavigationPage(new fireStation());
        }
    }
}