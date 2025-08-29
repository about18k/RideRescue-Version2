using road_rescue.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace road_rescue
{
    public partial class MainPage : ContentPage
    {
        private readonly OnboardingVm _viewModel;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = new OnboardingVm();
            BindingContext = _viewModel;
            BindingContext = new OnboardingVm();
        }

        private void CarouselView_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            _viewModel.IsLastStep = e.CurrentPosition == (_viewModel.OnboardingSteps.Count - 1);



        }

        private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            Preferences.Set("HasSeenOnboarding", true);
            await Shell.Current.GoToAsync("logInPage"); 
        }

        private async void Button_Pressed(object sender, EventArgs e)
        {
            Preferences.Set("HasSeenOnboarding", true);
            await Shell.Current.GoToAsync("logInPage");
        }
    }
}
