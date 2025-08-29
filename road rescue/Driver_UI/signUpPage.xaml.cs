using Microsoft.Maui.Authentication;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;
using road_rescue.Models;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Postgrest;
using Supabase.Postgrest.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
// ✅ Alias the enum instead of trying to 'using' a type as a namespace
using Provider = Supabase.Gotrue.Constants.Provider;

namespace road_rescue
{
    public partial class signUpPage : ContentPage
    {
        private string selectedCertificatePath = string.Empty;

        // === Google OAuth (Android) ===
        private const string GoogleAndroidClientId =
            "153424140230-0vf4eon1sgsob9842d1nkgt07q0np52r.apps.googleusercontent.com";

        private const string RedirectScheme =
            "com.googleusercontent.apps.153424140230-0vf4eon1sgsob9842d1nkgt07q0np52r";

        private string RedirectUri => $"{RedirectScheme}:/oauth2redirect";

        // Deep link used by Supabase email verification (must also be in Supabase "Redirect URLs")
        private const string EmailCallbackDeepLink = "roadrescue.staging://email-callback";

        // Supabase Storage
        private const string CertificateBucket = "certificates";

        // Hold Google profile for saving later
        private string? _googleSub = null;
        private string? _googlePhotoUrl = null;
        private bool _isGoogleMechanicFlow = false;

        // Animation
        private bool _isAnimating = false;

        public signUpPage()
        {
            InitializeComponent();

            // Default role to Driver (index 1: Mechanic=0, Driver=1)
            rolePicker.SelectedIndex = 1;
            UpdateRoleUI();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            StartLoadingAnimation();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopLoadingAnimation();
        }

        private void StartLoadingAnimation()
        {
            if (_isAnimating) return;

            _isAnimating = true;
            var animation = new Animation(v => LoadingImage.Rotation = v, 0, 360);
            animation.Commit(LoadingImage, "LoadingRotation", length: 1000,
                repeat: () => _isAnimating, easing: Easing.Linear);
        }

        private void StopLoadingAnimation()
        {
            _isAnimating = false;
            LoadingImage.AbortAnimation("LoadingRotation");
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.IsVisible = show;
            if (show) StartLoadingAnimation(); else StopLoadingAnimation();
        }

        private void login_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new logInPage());
        }

        private string CurrentRole =>
            rolePicker.SelectedItem?.ToString() ?? "Driver";

        private bool IsMechanic =>
            string.Equals(CurrentRole, "Mechanic", StringComparison.OrdinalIgnoreCase);

        private void OnRoleSelectedChanged(object? sender, EventArgs e)
        {
            _isGoogleMechanicFlow = false;
            UpdateRoleUI();
        }

        private void UpdateRoleUI()
        {
            mechanicFields.IsVisible = IsMechanic;

            // Default visibility (manual signup)
            PasswordRow.IsVisible = true;
            ConfirmPasswordRow.IsVisible = true;

            SignupButton.IsVisible = true;
            Signupwithgoogle.IsVisible = true;
            ContinueBtn.IsVisible = false;
            OrLabel.IsVisible = true;
            login.IsVisible = true;
        }

        // =========================
        // Manual Signup (email/password)
        // =========================
        private async void Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                ShowLoading(true);
                await SupabaseService.InitializeAsync();

                var fullname = fullnameEntry.Text?.Trim();
                var email = emailEntry.Text?.Trim();
                var password = passwordEntry.Text?.Trim();
                var confirmPassword = ConfirmpasswordEntry.Text?.Trim();
                var phone = phoneEntry.Text?.Trim();
                var address = AddressEntry.Text?.Trim();

                if (string.IsNullOrWhiteSpace(fullname) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    await DisplayAlert("Error", "Full name, Email, and Password are required.", "OK");
                    return;
                }

                if (password != confirmPassword)
                {
                    await DisplayAlert("Error", "Passwords do not match.", "OK");
                    return;
                }

                var signupData = new SignupData
                {
                    FullName = fullname!,
                    Email = email!,
                    Password = password!,
                    Phone = phone,
                    Address = address,
                    Role = CurrentRole,
                    IsMechanicFlow = IsMechanic,
                    GoogleSub = null,
                    PhotoUrl = null
                };

                if (IsMechanic)
                {
                    var servicesList = CollectServices();
                    signupData.Services = string.Join(",", servicesList);

                    var (openStr, closeStr) = GetOpenCloseTimes();
                    signupData.TimeOpen = openStr;
                    signupData.TimeClose = closeStr;
                    signupData.Days = GetSelectedDaysCsv();

                    try
                    {
                        signupData.CertificateUrl = await UploadCertificateIfAnyAsync();
                    }
                    catch { /* ignore */ }
                }

                // Save pending payload for your email-callback flow (if you have one)
                var signupDataJson = JsonSerializer.Serialize(signupData);
                await SecureStorage.SetAsync("pending_signup_data", signupDataJson);

                // Manual signup -> requires email confirmation
                var options = new SignUpOptions { RedirectTo = EmailCallbackDeepLink };
                var session = await SupabaseService.Client.Auth.SignUp(email!, password!, options);

                await DisplayAlert("Notice",
                    "Please confirm your email. After verification, you can log in.",
                    "OK");

                await Navigation.PushAsync(new logInPage());
            }
            catch (GotrueException gx)
            {
                await DisplayAlert("Signup failed", gx.Message, "OK");
            }
            catch (Exception ex)
            {
                await ShowPgError(ex, "Signup failed");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        // =========================
        // Upload certificate (image)
        // =========================
        private async void OnUploadCertificateClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select your certificate",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    selectedCertificatePath = result.FullPath;
                    certificateLabel.Text = System.IO.Path.GetFileName(selectedCertificatePath);
                    certificateLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"File selection failed: {ex.Message}", "OK");
            }
        }

        private async void OnViewCertificateTapped(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedCertificatePath)) return;

            var image = new Image
            {
                Source = ImageSource.FromFile(selectedCertificatePath),
                HeightRequest = 300,
                Aspect = Aspect.AspectFit
            };

            var closeButton = new Button
            {
                Text = "Close",
                BackgroundColor = Colors.Gray,
                TextColor = Colors.White
            };

            var popupContent = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 20,
                Children = { image, closeButton }
            };

            var popup = new ContentPage
            {
                Content = new Border
                {
                    Content = popupContent,
                    BackgroundColor = Colors.White,
                    Padding = 10,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                },
                BackgroundColor = new Color(0f, 0f, 0f, 0.5f)
            };

            closeButton.Clicked += (s, args) => Application.Current.MainPage.Navigation.PopModalAsync();
            await Navigation.PushModalAsync(popup);
        }

        // =========================
        // GOOGLE SIGN-IN (establishes Supabase session via ID token)
        // =========================
        // =========================
        // GOOGLE SIGN-UP / SIGN-IN (Supabase PKCE end-to-end)
        // =========================
        private async void Signupwithgoogle_Clicked(object sender, EventArgs e)
        {
            const string AppCallback = "roadrescue.staging://oauth-callback"; // must match your intent filter

            try
            {
                ShowLoading(true);
                await SupabaseService.InitializeAsync();

                // 1) Ask Supabase to start an OAuth (PKCE) sign-in with Google
                var state = await SupabaseService.Client!.Auth.SignIn(
                    Provider.Google,
                    new SignInOptions
                    {
                        FlowType = Supabase.Gotrue.Constants.OAuthFlowType.PKCE,
                        RedirectTo = AppCallback,
                        Scopes = "openid email profile"
                    });

                // 2) Launch the browser and wait for the callback
                var result = await WebAuthenticator.Default.AuthenticateAsync(
                    new WebAuthenticatorOptions
                    {
                        Url = state.Uri,                 // Supabase's authorize URL
                        CallbackUrl = new Uri(AppCallback),
                        PrefersEphemeralWebBrowserSession = true
                    });

                if (result?.Properties is null ||
                    !result.Properties.TryGetValue("code", out var code) ||
                    string.IsNullOrWhiteSpace(code))
                {
                    await DisplayAlert("Cancelled", "Google sign-in was cancelled.", "OK");
                    return;
                }

                // 3) Exchange the Auth Code + PKCE verifier with **Supabase** (not Google!)
                var session = await SupabaseService.Client.Auth.ExchangeCodeForSession(state.PKCEVerifier, code);
                if (session?.User == null)
                {
                    await DisplayAlert("Error", "Failed to create Supabase session.", "OK");
                    return;
                }



                // 4) Pre-fill fields from the Supabase user (OAuth metadata usually has name/email)
                fullnameEntry.Text = session.User?.Email ?? fullnameEntry.Text;
                emailEntry.Text = session.User?.Email ?? emailEntry.Text;

                // 5) Driver vs Mechanic routing/flow (same as you had)
                if (IsMechanic)
                {
                    await DisplayAlert(
                        "Almost there",
                        "Since you selected Mechanic, please fill the remaining details for verification.",
                        "OK");

                    _isGoogleMechanicFlow = true;
                    mechanicFields.IsVisible = true;

                    PasswordRow.IsVisible = false;
                    ConfirmPasswordRow.IsVisible = false;

                    SignupButton.IsVisible = false;
                    Signupwithgoogle.IsVisible = false;
                    login.IsVisible = false;
                    OrLabel.IsVisible = false;
                    ContinueBtn.IsVisible = true;
                }
                else
                {
                    await EnsureAppUserRowAsync(
                        requiredRole: "Driver",
                        fullName: fullnameEntry.Text ?? session.User.Email ?? "User",
                        email: emailEntry.Text ?? session.User.Email ?? "",
                        phone: null, address: null,
                        googleSub: session.User?.Id, // optional; or pull from UserMetadata if you store it
                        photoUrl: null
                    );

                    Application.Current.MainPage = new AppShell();
                }
            }
            catch (TaskCanceledException) { /* user backed out */ }
            catch (Exception ex)
            {
                await ShowPgError(ex, "Google sign-in failed");
            }
            finally
            {
                ShowLoading(false);
            }
        }


        // =========================
        // Continue (Mechanic + Google) => save app_user + mechanic_details (RLS-safe)
        // =========================
        private async void ContinueBtn_Clicked(object sender, EventArgs e)
        {
            try
            {
                ShowLoading(true);

                if (!_isGoogleMechanicFlow)
                {
                    await DisplayAlert("Notice", "Continue is only for Google + Mechanic flow.", "OK");
                    return;
                }

                // 1) Gather inputs
                string fullNameRaw = fullnameEntry.Text?.Trim() ?? "";
                string emailRaw = emailEntry.Text?.Trim() ?? "";
                string phoneRaw = phoneEntry.Text?.Trim();
                string? phoneSanitized = SanitizePhone(phoneRaw);
                string? addressRaw = AddressEntry.Text?.Trim();

                var servicesList = CollectServices();
                string servicesCsv = string.Join(",", servicesList);

                var (openStr, closeStr) = GetOpenCloseTimes();
                var daysCsv = GetSelectedDaysCsv();

                if (string.IsNullOrWhiteSpace(fullNameRaw) || string.IsNullOrWhiteSpace(emailRaw))
                {
                    await DisplayAlert("Missing Info", "Fullname and Email are required.", "OK");
                    return;
                }
                if (string.IsNullOrWhiteSpace(addressRaw))
                {
                    await DisplayAlert("Missing Info", "Please provide your Address.", "OK");
                    return;
                }
                if (servicesList.Count == 0)
                {
                    await DisplayAlert("Missing Services", "Please select at least one service you offer.", "OK");
                    return;
                }
                if (string.IsNullOrEmpty(daysCsv))
                {
                    await DisplayAlert("Missing Schedule", "Pick at least one shop day.", "OK");
                    return;
                }

                string? certUrl = null;
                try { certUrl = await UploadCertificateIfAnyAsync(); } catch { /* ignore */ }

                // 2) Ensure app_user row exists and role is Mechanic (RLS-safe)
                var appUser = await EnsureAppUserRowAsync(
                    requiredRole: "Mechanic",
                    fullName: fullNameRaw,
                    email: emailRaw,
                    phone: phoneSanitized,
                    address: addressRaw,
                    googleSub: _googleSub,
                    photoUrl: _googlePhotoUrl
                );

                // 3) Upsert mechanic_details referencing the same user_id (auth.uid())
                var mechResp = await SupabaseService.Client
                    .From<MechanicDetails>()
                    .Select("*")
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, appUser.UserId.ToString())
                    .Get();

                var mech = mechResp.Models.FirstOrDefault();

                if (mech == null)
                {
                    mech = new MechanicDetails
                    {
                        MechanicId = Guid.NewGuid(),
                        UserId = appUser.UserId,
                        Services = servicesCsv,
                        CertificateUrl = certUrl,
                        TimeOpen = openStr,
                        TimeClose = closeStr,
                        Days = daysCsv,
                        IsVerified = false
                    };

                    await SupabaseService.Client
                        .From<MechanicDetails>()
                        .Insert(mech, new QueryOptions { Returning = QueryOptions.ReturnType.Minimal });
                }
                else
                {
                    bool changed = false;

                    if ((mech.Services ?? "") != servicesCsv) { mech.Services = servicesCsv; changed = true; }
                    if ((mech.CertificateUrl ?? "") != (certUrl ?? "")) { mech.CertificateUrl = certUrl; changed = true; }
                    if ((mech.TimeOpen ?? "") != openStr) { mech.TimeOpen = openStr; changed = true; }
                    if ((mech.TimeClose ?? "") != closeStr) { mech.TimeClose = closeStr; changed = true; }
                    if ((mech.Days ?? "") != daysCsv) { mech.Days = daysCsv; changed = true; }

                    if (changed)
                    {
                        mech.UpdatedAt = DateTime.UtcNow;
                        await SupabaseService.Client
                            .From<MechanicDetails>()
                            .Update(mech, new QueryOptions { Returning = QueryOptions.ReturnType.Minimal });
                    }
                }

                await DisplayAlert("Submitted",
                    "We will send an update to your email once your mechanic account is verified.",
                    "OK");

                await Navigation.PushAsync(new logInPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to complete registration: {ex.Message}", "OK");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        // =========================
        // Helpers
        // =========================

        private static Guid RequireAuthUserId()
        {
            var uid = SupabaseService.Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrWhiteSpace(uid) || !Guid.TryParse(uid, out var g))
                throw new InvalidOperationException("No active Supabase session. Please log in again.");
            return g;
        }

        private async Task<AppUser> EnsureAppUserRowAsync(
            string requiredRole,
            string fullName,
            string email,
            string? phone,
            string? address,
            string? googleSub,
            string? photoUrl)
        {
            await SupabaseService.InitializeAsync();
            var userId = RequireAuthUserId();

            // Try to fetch existing by user_id
            var existingResp = await SupabaseService.Client
                .From<AppUser>()
                .Select("*")
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            var user = existingResp.Models.FirstOrDefault();

            if (user == null)
            {
                // Insert new row with user_id = auth.uid()
                user = new AppUser
                {
                    UserId = userId,
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    Address = address,
                    GoogleSub = googleSub,
                    Password = null, // Google users have no password
                    PhotoUrl = photoUrl,
                    Role = requiredRole
                };

                try
                {
                    await SupabaseService.Client
                        .From<AppUser>()
                        .Insert(user, new QueryOptions { Returning = QueryOptions.ReturnType.Representation });
                }
                catch (PostgrestException)
                {
                    // If a trigger already inserted it (race), try fetch again
                    var again = await SupabaseService.Client
                        .From<AppUser>()
                        .Select("*")
                        .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                        .Get();

                    user = again.Models.FirstOrDefault()
        ?? throw new InvalidOperationException("app_user row not found after insert retry.");

                }
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

        private static string? SanitizePhone(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var digits = new string(raw.Where(char.IsDigit).ToArray());
            return string.IsNullOrEmpty(digits) ? null : digits;
        }

        private async Task<AppUser?> FetchUserByEmailAsync(string email)
        {
            var resp = await SupabaseService.Client
                .From<AppUser>()
                .Select("*")
                .Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email)
                .Get();

            return resp.Models.FirstOrDefault();
        }

        private List<string> CollectServices()
        {
            var services = new List<string>();
            if (cbOilChange.IsChecked) services.Add("Oil Change");
            if (cbEngineTuneUp.IsChecked) services.Add("Engine Tune-up");
            if (cbBrakeRepair.IsChecked) services.Add("Brake Repair");
            if (cbTransmissionService.IsChecked) services.Add("Transmission Service");
            if (cbWheelAlignment.IsChecked) services.Add("Wheel Alignment");
            if (cbTireRotation.IsChecked) services.Add("Tire Rotation");
            if (cbBatteryReplacement.IsChecked) services.Add("Battery Replacement");
            if (cbElectricalRepair.IsChecked) services.Add("Electrical System Repair");
            if (cbSuspensionRepair.IsChecked) services.Add("Suspension Repair");
            if (cbACService.IsChecked) services.Add("Air Conditioning Service");
            if (cbExhaustRepair.IsChecked) services.Add("Exhaust System Repair");
            if (cbDiagnostics.IsChecked) services.Add("Diagnostic Services");
            if (cbWheelBalancing.IsChecked) services.Add("Wheel Balancing");
            if (cbRadiatorFlush.IsChecked) services.Add("Radiator Flush");
            if (cbFuelSystem.IsChecked) services.Add("Fuel System Cleaning");
            if (cbBeltsHoses.IsChecked) services.Add("Belt and Hose Replacement");
            if (cbHeadlightRestoration.IsChecked) services.Add("Headlight Restoration");
            if (cbWiperReplacement.IsChecked) services.Add("Windshield Wiper Replacement");
            if (cbWheelRepair.IsChecked) services.Add("Wheel Repair");
            if (cbVulcanizing.IsChecked) services.Add("Vulcanizing/Tire Patching");
            return services;
        }

        private (string openStr, string closeStr) GetOpenCloseTimes()
        {
            var openTs = openTimePicker?.Time ?? new TimeSpan(8, 0, 0);
            var closeTs = closeTimePicker?.Time ?? new TimeSpan(22, 0, 0);

            if (openTs >= closeTs)
                closeTs = openTs.Add(TimeSpan.FromHours(1));

            string open = $"{openTs.Hours:D2}:{openTs.Minutes:D2}";
            string close = $"{closeTs.Hours:D2}:{closeTs.Minutes:D2}";
            return (open, close);
        }

        private string GetSelectedDaysCsv()
        {
            var days = new List<string>();
            if (cbMonday.IsChecked) days.Add("mon");
            if (cbTuesday.IsChecked) days.Add("tue");
            if (cbWednesday.IsChecked) days.Add("wed");
            if (cbThursday.IsChecked) days.Add("thu");
            if (cbFriday.IsChecked) days.Add("fri");
            if (cbSaturday.IsChecked) days.Add("sat");
            if (cbSunday.IsChecked) days.Add("sun");
            return string.Join(",", days);
        }

        private async Task<string?> UploadCertificateIfAnyAsync()
        {
            if (string.IsNullOrEmpty(selectedCertificatePath))
                return null;

            await SupabaseService.InitializeAsync();

            var fileName = System.IO.Path.GetFileName(selectedCertificatePath);
            var safeEmail = (emailEntry.Text ?? "unknown").Replace("@", "_at_").Replace(".", "_");
            var objectPath = $"{safeEmail}/{DateTime.UtcNow:yyyyMMdd_HHmmss}_{fileName}";

            var storage = SupabaseService.Client.Storage;

            string contentType = fileName.ToLowerInvariant() switch
            {
                var f when f.EndsWith(".png") => "image/png",
                var f when f.EndsWith(".jpg") => "image/jpeg",
                var f when f.EndsWith(".jpeg") => "image/jpeg",
                var f when f.EndsWith(".webp") => "image/webp",
                _ => "application/octet-stream"
            };

            await storage.From(CertificateBucket).Upload(
                selectedCertificatePath,
                objectPath,
                new Supabase.Storage.FileOptions
                {
                    Upsert = true,
                    ContentType = contentType
                });

            return storage.From(CertificateBucket).GetPublicUrl(objectPath);
        }

        private async Task ShowPgError(Exception ex, string title)
        {
            if (ex is PostgrestException pg)
                await DisplayAlert("Error", $"{title}: {pg.Message}", "OK");
            else if (ex is GotrueException gx)
                await DisplayAlert("Auth Error", $"{title}: {gx.Message}", "OK");
            else
                await DisplayAlert("Error", $"{title}: {ex.Message}", "OK");
        }
    }

    // Holder for pending manual signup info
    public class SignupData
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string Role { get; set; } = "Driver";
        public bool IsMechanicFlow { get; set; }
        public string? GoogleSub { get; set; }
        public string? PhotoUrl { get; set; }
        public string? CertificateUrl { get; set; }
        public string? Services { get; set; }
        public string? TimeOpen { get; set; }
        public string? TimeClose { get; set; }
        public string? Days { get; set; }
    }
}
