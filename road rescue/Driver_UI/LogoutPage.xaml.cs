using System;
using Microsoft.Maui.Controls;

namespace road_rescue
{
    public partial class LogoutPage : ContentPage
    {
        public LogoutPage()
        {
            InitializeComponent();
            ShowLogoutAlert();
        }

        private async void ShowLogoutAlert()
        {
            bool answer = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "Cancel");

            if (answer)
            {
                // You can add actual logout logic here later (like clearing session or navigating to login page)
                await DisplayAlert("Logged Out", "You have been logged out successfully.", "OK");
                // For now, go back to Home
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                // Go back if user cancels
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
    }
}
