// Platforms/Android/MainActivity.cs
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace road_rescue;

[Activity(Label = "road_rescue", Theme = "@style/Maui.SplashTheme", MainLauncher = true,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                                ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { Android.Content.Intent.ActionView },
    Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
    DataScheme = "roadrescue.staging",
    DataHost = "email-callback"
)]
public class MainActivity : MauiAppCompatActivity { }
