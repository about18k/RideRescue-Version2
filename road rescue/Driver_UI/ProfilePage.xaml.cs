using Microsoft.Maui.Controls;
using road_rescue.Services;
using Supabase;
using System;

namespace road_rescue
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage()
        {
            InitializeComponent();
            LoadUserData();
        }

        private void OnBackButtonClicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        private async void LoadUserData()
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client!;
                var authUser = supabase.Auth.CurrentUser;

                if (authUser != null)
                {
                    var appUser = await SupabaseService.GetOrCreateAppUserAsync(authUser.Id);

                    FullNameLabel.Text = appUser?.FullName ?? "No Name";
                    EmailLabel.Text = authUser.Email ?? "No Email";

                    if (!string.IsNullOrEmpty(appUser?.PhotoUrl))
                    {
                        // If it's a web URL
                        if (appUser.PhotoUrl.StartsWith("http"))
                            ProfileImage.Source = ImageSource.FromUri(new Uri(appUser.PhotoUrl));
                        else
                            // If it's a local file path
                            ProfileImage.Source = ImageSource.FromFile(appUser.PhotoUrl);
                    }
                    else
                    {
                        ProfileImage.Source = "derek.jpg"; // fallback default
                    }
                }
                else
                {
                    await DisplayAlert("Error", "No logged-in user found. Please log in again.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load profile: {ex.Message}", "OK");
            }
        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            Shell.Current.FlyoutIsPresented = true;
        }

        private async void OnChangeProfilePictureTapped(object sender, TappedEventArgs e)
        {
            try
            {
                // 1) Pick a photo
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return;

                // 2) Copy to local cache
                var ext = Path.GetExtension(photo.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var localPath = Path.Combine(FileSystem.CacheDirectory, fileName);

                using (var src = await photo.OpenReadAsync())
                using (var dst = File.Open(localPath, FileMode.Create, FileAccess.Write))
                    await src.CopyToAsync(dst);

                // 3) Show immediately
                ProfileImage.Source = ImageSource.FromFile(localPath);

                // 4) Init Supabase + upload to Storage
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client!;
                var authUser = supabase.Auth.CurrentUser;
                if (authUser == null)
                {
                    await DisplayAlert("Error", "No logged-in user found.", "OK");
                    return;
                }

                // store under a user-scoped folder to avoid name collisions/CDN staleness
                var objectPath = $"{authUser.Id}/{fileName}";

                // set a reasonable Content-Type
                string contentType = ext.ToLowerInvariant() switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };

                // NOTE: Upload signature: Upload(localFilePath, destinationPath, options)
                await supabase.Storage
                    .From("profile_photos")
                    .Upload(localPath, objectPath, new Supabase.Storage.FileOptions
                    {
                        Upsert = true,
                        ContentType = contentType,
                    }); // :contentReference[oaicite:0]{index=0}

                // 5) Public URL (bucket must be Public; otherwise use a signed URL)
                var publicUrl = supabase.Storage.From("profile_photos").GetPublicUrl(objectPath);
                // If your bucket is NOT public, use:
                // var publicUrl = await supabase.Storage.From("profile_photos").CreateSignedUrl(objectPath, 60 * 60); // 1 hour
                // :contentReference[oaicite:1]{index=1}

                // 6) UPDATE app_user.photo_url using Where -> Set -> Update
                // If your model maps user_id as Guid/UUID:
                var userUuid = Guid.Parse(authUser.Id);

                await supabase
                    .From<Models.AppUser>()
                    .Where(u => u.UserId == userUuid)
                    .Set(u => u.PhotoUrl, publicUrl)
                    .Update(); // :contentReference[oaicite:2]{index=2}

                await DisplayAlert("Success", "Profile picture updated!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to change profile picture: {ex.Message}", "OK");
            }
        }
    }
}
