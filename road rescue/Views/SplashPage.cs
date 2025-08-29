using Microsoft.Maui.Controls;

namespace road_rescue
{
    public class SplashPage : ContentPage
    {
        public SplashPage()
        {
            BackgroundColor = Colors.White;
            Content = new Grid
            {
                Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = true,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    }
                }
            };
        }
    }
}
