// Platforms/Android/WebAuthenticationCallbackActivity.cs
using Android.App;
using Android.Content.PM;
using Android.Content;            // for Intent constants if you want to shorten names
using Microsoft.Maui.Authentication;

namespace road_rescue;

// IMPORTANT: file must be under Platforms/Android, Build Action = Compile
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "roadrescue.staging",
    DataHost = "oauth-callback"      // <- must match your CallbackUrl host
                                     // (no DataPath because your URL has no path; if you add one later, include it)
)]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity { }
