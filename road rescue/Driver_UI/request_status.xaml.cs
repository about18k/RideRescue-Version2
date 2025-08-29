using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

namespace road_rescue;

public partial class request_status : ContentPage
{
	public request_status()
	{
		InitializeComponent();
	}

    private void OnBackButtonClicked(object sender, EventArgs e)
    {
		Navigation.PopAsync();
    }

    private void OnViewMechanicClicked(object sender, EventArgs e)
    {
        var popup = new MechanicInfoPopup();
        this.ShowPopup(popup);
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}