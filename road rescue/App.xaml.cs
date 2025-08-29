using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using road_rescue.Models;
using Supabase.Postgrest;

namespace road_rescue
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new SplashPage();
            _ = BootstrapAsync();
        }

        private async Task BootstrapAsync()
        {
            try
            {
                await SupabaseService.InitializeAsync();

                var user = SupabaseService.Client!.Auth.CurrentUser; // null if no session
                if (user != null)
                {
                    var profile = await SupabaseService.GetOrCreateAppUserAsync(user.Id, user.Email);
                    await MainThread.InvokeOnMainThreadAsync(() => NavigateByRole(profile));
                    return;
                }
            }
            catch
            {
                // ignore
            }

            await MainThread.InvokeOnMainThreadAsync(() => Current.MainPage = new AuthShell());
        }

        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            // Handles email verification & magic links
            if (uri.Scheme != "roadrescue.staging") return;

            try
            {
                static IDictionary<string, string> ParseKv(string s) =>
                    s.Split('&', StringSplitOptions.RemoveEmptyEntries)
                     .Select(p => p.Split('=', 2))
                     .Where(kv => kv.Length == 2)
                     .ToDictionary(kv => Uri.UnescapeDataString(kv[0]),
                                   kv => Uri.UnescapeDataString(kv[1]),
                                   StringComparer.OrdinalIgnoreCase);

                var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(uri.Fragment))
                    foreach (var kv in ParseKv(uri.Fragment.TrimStart('#'))) bag[kv.Key] = kv.Value;
                if (!string.IsNullOrEmpty(uri.Query))
                    foreach (var kv in ParseKv(uri.Query.TrimStart('?'))) bag[kv.Key] = kv.Value;

                // email verification / magic links typically return access_token + refresh_token
                if (bag.TryGetValue("access_token", out var access) &&
                    bag.TryGetValue("refresh_token", out var refresh))
                {
                    await SupabaseService.InitializeAsync();
                    await SupabaseService.Client!.Auth.SetSession(access, refresh, forceAccessTokenRefresh: true);

                    // finalize any pending manual sign-up
                    await FinalizePendingSignupAsync();
                    return;
                }

                // fallthrough: if no tokens, ask to sign in
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Current.MainPage.DisplayAlert("Email verified",
                        "Please sign in to continue.", "OK"));
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Current.MainPage.DisplayAlert("Deep link error", ex.Message, "OK"));
            }
        }

        private async Task FinalizePendingSignupAsync()
        {
            try
            {
                var user = SupabaseService.Client!.Auth.CurrentUser;
                if (user == null) return;

                var json = await SecureStorage.GetAsync("pending_signup_data");
                if (string.IsNullOrWhiteSpace(json))
                {
                    // no pending payload, simply route by role
                    var existing = await SupabaseService.GetOrCreateAppUserAsync(user.Id, user.Email);
                    NavigateByRole(existing);
                    return;
                }

                var data = System.Text.Json.JsonSerializer.Deserialize<SignupData>(json)!;

                // ensure app_user exists/updated
                var profile = await EnsureAppUserRoleAsync(
                    requiredRole: data.Role ?? "Driver",
                    fullName: data.FullName ?? (user.Email ?? "User"),
                    email: data.Email ?? user.Email ?? "",
                    phone: data.Phone, address: data.Address,
                    googleSub: data.GoogleSub, photoUrl: data.PhotoUrl
                );

                if (string.Equals(data.Role, "Mechanic", StringComparison.OrdinalIgnoreCase) || data.IsMechanicFlow)
                {
                    // upsert mechanic_details
                    var mechResp = await SupabaseService.Client!
                        .From<MechanicDetails>()
                        .Select("*")
                        .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, profile.UserId.ToString())
                        .Get();

                    var mech = mechResp.Models.FirstOrDefault() ?? new MechanicDetails
                    {
                        MechanicId = Guid.NewGuid(),
                        UserId = profile.UserId
                    };

                    mech.Services = data.Services ?? mech.Services;
                    mech.CertificateUrl = data.CertificateUrl ?? mech.CertificateUrl; // already uploaded earlier if public bucket
                    mech.TimeOpen = data.TimeOpen ?? mech.TimeOpen;
                    mech.TimeClose = data.TimeClose ?? mech.TimeClose;
                    mech.Days = data.Days ?? mech.Days;
                    mech.IsVerified = false;
                    mech.UpdatedAt = DateTime.UtcNow;

                    if (mechResp.Models.Any())
                        await SupabaseService.Client.From<MechanicDetails>().Update(mech, new QueryOptions { Returning = QueryOptions.ReturnType.Minimal });
                    else
                        await SupabaseService.Client.From<MechanicDetails>().Insert(mech, new QueryOptions { Returning = QueryOptions.ReturnType.Minimal });

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                        await Current.MainPage.DisplayAlert("Submitted",
                            "Admin must verify your mechanic account. You can’t log in yet. Watch your email for updates.",
                            "OK"));

                    // important: sign out so they can't enter before verification
                    await SupabaseService.SignOutAsync();
                    await MainThread.InvokeOnMainThreadAsync(() => Current.MainPage = new AuthShell());
                }
                else
                {
                    // driver: auto-login to AppShell
                    await MainThread.InvokeOnMainThreadAsync(() => Application.Current.MainPage = new AppShell());
                }

                SecureStorage.Remove("pending_signup_data");
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Current.MainPage.DisplayAlert("Finalize error", ex.Message, "OK"));
            }
        }

        private async Task<AppUser> EnsureAppUserRoleAsync(
            string requiredRole,
            string fullName,
            string email,
            string? phone,
            string? address,
            string? googleSub,
            string? photoUrl)
        {
            await SupabaseService.InitializeAsync();

            // fetch by user_id
            var uidStr = SupabaseService.Client!.Auth.CurrentUser?.Id ?? "";
            var resp = await SupabaseService.Client
                .From<AppUser>().Select("*")
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, uidStr)
                .Get();

            var user = resp.Models.FirstOrDefault();

            if (user == null)
            {
                user = new AppUser
                {
                    UserId = Guid.Parse(uidStr),
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    Address = address,
                    GoogleSub = googleSub,
                    PhotoUrl = photoUrl,
                    Role = requiredRole
                };

                var ins = await SupabaseService.Client
                    .From<AppUser>()
                    .Insert(user, new QueryOptions { Returning = QueryOptions.ReturnType.Representation });

                user = ins.Models.First();
            }
            else
            {
                bool changed = false;
                if ((user.FullName ?? "") != fullName) { user.FullName = fullName; changed = true; }
                if (!string.Equals(user.Email ?? "", email ?? "", StringComparison.OrdinalIgnoreCase)) { user.Email = email; changed = true; }
                if ((user.Phone ?? "") != (phone ?? "")) { user.Phone = phone; changed = true; }
                if ((user.Address ?? "") != (address ?? "")) { user.Address = address; changed = true; }
                if ((user.PhotoUrl ?? "") != (photoUrl ?? "")) { user.PhotoUrl = photoUrl; changed = true; }
                if (string.IsNullOrEmpty(user.GoogleSub) && !string.IsNullOrEmpty(googleSub)) { user.GoogleSub = googleSub; changed = true; }
                if (!string.Equals(user.Role ?? "", requiredRole, StringComparison.OrdinalIgnoreCase)) { user.Role = requiredRole; changed = true; }

                if (changed)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    await SupabaseService.Client
                        .From<AppUser>()
                        .Update(user, new QueryOptions { Returning = QueryOptions.ReturnType.Minimal });
                }
            }

            return user;
        }

        private void NavigateByRole(AppUser appUser)
        {
            if ((appUser.Role ?? "Driver").Equals("Mechanic", StringComparison.OrdinalIgnoreCase))
                Application.Current.MainPage = new NavigationPage(new RequestPage());
            else
                Application.Current.MainPage = new AppShell();
        }
    }
}
