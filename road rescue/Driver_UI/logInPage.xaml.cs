using Microsoft.Maui.Authentication;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using road_rescue.Models;
using road_rescue.Services;
using Supabase.Gotrue;
using Provider = Supabase.Gotrue.Constants.Provider;
using Supabase.Postgrest;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace road_rescue
{
    public partial class logInPage : ContentPage
    {
        // MUST match your Android intent-filter for WebAuthenticator
        // Platforms/Android/WebAuthenticationCallbackActivity.cs:
        //   DataScheme="roadrescue.staging", DataHost="oauth-callback"
        private const string AppCallback = "roadrescue.staging://oauth-callback";

        public logInPage() => InitializeComponent();

        // =========================
        // Manual EMAIL/PASSWORD LOGIN
        // =========================
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var email = emailEntry.Text?.Trim();
            var password = passwordEntry.Text?.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please fill in all fields.", "OK");
                return;
            }

            try
            {
                await SupabaseService.InitializeAsync();

                // GoTrue email/password login (C#) → returns Session if credentials are valid
                var session = await SupabaseService.Client!.Auth.SignIn(email!, password!); // docs: supabase.com/docs/reference/csharp/auth-signinwithpassword
                if (session?.User == null)
                {
                    await DisplayAlert("Login failed", "Invalid email or password.", "OK");
                    return;
                }

                // After sign-in we can safely query the user's own row (RLS)
                var appUser = await TryGetAppUserByUidAsync(session.User.Id);

                // 1) profile must exist
                if (appUser is null)
                {
                    await DisplayAlert("Account incomplete",
                        "This account doesn't have a profile yet. Please finish sign up first.",
                        "OK");
                    await SupabaseService.SignOutAsync();
                    return;
                }

                // 2) profile created with Google? block manual login
                if (!string.IsNullOrWhiteSpace(appUser.GoogleSub))
                {
                    await DisplayAlert("Use Google sign in",
                        "This email was registered with Google. Please sign in with Google instead.",
                        "OK");
                    await SupabaseService.SignOutAsync();
                    return;
                }

                await OnLoginSuccessAsync(session, appUser);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
            }
        }

        private void signup_Clicked(object sender, EventArgs e) =>
            Navigation.PushAsync(new signUpPage());

        // =========================
        // GOOGLE LOGIN via Supabase OAuth (PKCE)
        // =========================
        private async void Signinwithgoogle_Clicked(object sender, EventArgs e)
        {
            try
            {
                await SupabaseService.InitializeAsync();

                // Start PKCE OAuth with Supabase (returns provider auth state)
                var state = await SupabaseService.Client!.Auth.SignIn(
                    Provider.Google,
                    new SignInOptions
                    {
                        FlowType = Supabase.Gotrue.Constants.OAuthFlowType.PKCE,
                        RedirectTo = AppCallback,
                        Scopes = "openid email profile"
                    });

                // Hand off to MAUI WebAuthenticator (browser)
                var result = await WebAuthenticator.Default.AuthenticateAsync(
                    new WebAuthenticatorOptions
                    {
                        Url = state.Uri,
                        CallbackUrl = new Uri(AppCallback),
                        PrefersEphemeralWebBrowserSession = true
                    });

                // Google → Supabase redirected back with an Auth Code
                if (result?.Properties is null ||
                    !result.Properties.TryGetValue("code", out var code) ||
                    string.IsNullOrWhiteSpace(code))
                {
                    await DisplayAlert("Sign-in canceled", "No auth code returned.", "OK");
                    return;
                }

                // Exchange the code (+ PKCE verifier) for a Session (access+refresh tokens)
                var session = await SupabaseService.Client.Auth.ExchangeCodeForSession(state.PKCEVerifier, code);
                if (session?.User == null)
                {
                    await DisplayAlert("Error", "Failed to create Supabase session.", "OK");
                    return;
                }

                // Get profile & route
                var appUser = await TryGetAppUserByUidAsync(session.User.Id);
                if (appUser is null)
                {
                    // If you want to block here instead of auto-creating:
                    await DisplayAlert("No profile found",
                        "This Google account isn’t registered yet. Please sign up first.",
                        "OK");
                    await SupabaseService.SignOutAsync();
                    return;
                }

                await OnLoginSuccessAsync(session, appUser);
            }
            catch (TaskCanceledException) { /* user aborted */ }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Google sign-in failed: {ex.Message}", "OK");
            }
        }

        // =========================
        // Shared post-login path
        // =========================
        private async Task OnLoginSuccessAsync(Supabase.Gotrue.Session session, AppUser appUser)
        {
            PersistTokensOnAuthChanges(); // keep refresh/access tokens updated (auto-refresh)

            // Route by role:
            if ((appUser.Role ?? "Driver").Equals("Mechanic", StringComparison.OrdinalIgnoreCase))
                Application.Current.MainPage = new NavigationPage(new RequestPage());
            else
                Application.Current.MainPage = new AppShell();

            await Task.CompletedTask;
        }

        // Query the user's own app_user row (RLS must allow auth.uid() == user_id)
        private static async Task<AppUser?> TryGetAppUserByUidAsync(string uidStr)
        {
            var resp = await SupabaseService.Client!
                .From<AppUser>()
                .Select("*")
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, uidStr)
                .Get();

            return resp.Models.FirstOrDefault();
        }

        // Persist tokens & allow “non-expiring” sign-in via refresh token rotation
        // Access tokens are short-lived; refresh tokens do not expire (they rotate). We save them and Supabase refreshes automatically.
        // Persist tokens & allow “non-expiring” sign-in via refresh token rotation
        // Access tokens are short-lived; refresh tokens do not expire (they rotate). We save them and Supabase refreshes automatically.
        private static void PersistTokensOnAuthChanges()
        {
            SupabaseService.Client!.Auth.AddStateChangedListener(async (_, state) =>
            {
                var s = SupabaseService.Client.Auth.CurrentSession;
                if (s != null &&
                    (state == Supabase.Gotrue.Constants.AuthState.TokenRefreshed ||
                     state == Supabase.Gotrue.Constants.AuthState.SignedIn ||
                     state == Supabase.Gotrue.Constants.AuthState.UserUpdated))
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(s.AccessToken))
                            await SecureStorage.Default.SetAsync("sb_access", s.AccessToken);
                        if (!string.IsNullOrWhiteSpace(s.RefreshToken))
                            await SecureStorage.Default.SetAsync("sb_refresh", s.RefreshToken);
                    }
                    catch { /* ignore */ }
                }
                else if (state == Supabase.Gotrue.Constants.AuthState.SignedOut)
                {
                    SecureStorage.Default.Remove("sb_access");
                    SecureStorage.Default.Remove("sb_refresh");
                }
            });
        }
    }
}
