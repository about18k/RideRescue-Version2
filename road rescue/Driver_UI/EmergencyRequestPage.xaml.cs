// EmergencyRequestPage.xaml.cs
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;   // Geolocation
using Microsoft.Maui.Storage;            // MediaPicker, FileSystem
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using StorageFileOptions = Supabase.Storage.FileOptions;

namespace road_rescue
{
    public partial class EmergencyRequestPage : ContentPage
    {
        private string? _photoPath; // path to a cached copy inside app sandbox

        public EmergencyRequestPage() => InitializeComponent();

        private void OnBackButtonClicked(object sender, EventArgs e) => Navigation.PopAsync();

        // Capture a photo and copy it to app cache so no external storage permission is needed
        private static async Task<string?> CaptureToCacheAsync()
        {
            var file = await MediaPicker.CapturePhotoAsync();
            if (file == null) return null;

            var ext = Path.GetExtension(file.FileName);
            var cached = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}{ext}");

            using var src = await file.OpenReadAsync();
            using var dst = File.OpenWrite(cached);
            await src.CopyToAsync(dst);

            return cached;
        }

        private async void landmarkk_Clicked(object sender, EventArgs e)
        {
            try
            {
                var cam = await Permissions.RequestAsync<Permissions.Camera>();
                if (cam != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Denied", "Camera access is required.", "OK");
                    return;
                }

                _photoPath = await CaptureToCacheAsync();
                if (_photoPath == null) return;

                selectedImage.Source = ImageSource.FromFile(_photoPath);
                fileNameLabel.Text = Path.GetFileName(_photoPath);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // --- PostgREST model ---
        [Table("emergency")]
        public class EmergencyRow : BaseModel
        {
            [PrimaryKey("id", false)]
            public long Id { get; set; }

            [Column("emergency_id")]
            public Guid EmergencyId { get; set; }

            [Column("user_id")]
            public Guid UserId { get; set; }

            [Column("vehicle_type")]
            public string VehicleType { get; set; } = "";

            [Column("breakdown_cause")]
            public string? BreakdownCause { get; set; }

            [Column("attachments")]
            public string Attachments { get; set; } = "[]";

            [Column("emergency_status")]
            public string EmergencyStatus { get; set; } = "waiting";

            [Column("latitude")]
            public double Latitude { get; set; }

            [Column("longitude")]
            public double Longitude { get; set; }

            [Column("accepted_at")]
            public DateTimeOffset? AcceptedAt { get; set; }

            [Column("completed_at")]
            public DateTimeOffset? CompletedAt { get; set; }

            [Column("canceled_at")]
            public DateTimeOffset? CanceledAt { get; set; }

            [Column("canceled_reason")]
            public string? CanceledReason { get; set; }

            // REMOVED JsonIgnore attribute to allow created_at to be sent
            [Column("created_at")]
            public DateTimeOffset CreatedAt { get; set; }
        }

        // POST handler: confirm → get GPS → upload (optional) → insert row
        private async void postbutton_Clicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Post emergency?", "Do you want to post this emergency request now?", "Post", "Cancel");
            if (!confirm) return;

            try
            {
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client!;
                var authUser = supabase.Auth.CurrentUser;
                if (authUser == null)
                {
                    await DisplayAlert("Not logged in", "Please log in first.", "OK");
                    return;
                }

                // app_user.user_id should match auth UID
                var userId = Guid.Parse(authUser.Id);

                // Location
                var locPerm = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (locPerm != PermissionStatus.Granted)
                {
                    await DisplayAlert("Location needed", "We need your location to dispatch help.", "OK");
                    return;
                }

                Location? location = await Geolocation.Default.GetLastKnownLocationAsync();
                if (location == null)
                {
                    var req = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    location = await Geolocation.Default.GetLocationAsync(req);
                }
                if (location == null)
                {
                    await DisplayAlert("No location", "Could not determine your location.", "OK");
                    return;
                }

                // Optional: upload photo
                string[] urls = Array.Empty<string>();
                Guid emergencyId = Guid.NewGuid();

                if (!string.IsNullOrWhiteSpace(_photoPath))
                {
                    var objectPath =
                        $"{authUser.Id}/{emergencyId}/photo-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{Path.GetExtension(_photoPath)}";

                    var bucket = supabase.Storage.From("emergency-attachments"); // <-- exact bucket id (hyphen)

                    await bucket.Upload(
                        _photoPath,
                        objectPath,
                        new StorageFileOptions
                        {
                            Upsert = true,
                            ContentType = "image/jpeg",
                            CacheControl = "3600"
                        });

                    var publicUrl = bucket.GetPublicUrl(objectPath);
                    urls = new[] { publicUrl };
                }

                // Compose and insert
                var row = new EmergencyRow
                {
                    EmergencyId = emergencyId,
                    UserId = userId,
                    VehicleType = VehicleTypePicker.SelectedItem?.ToString() ?? "Unknown",
                    BreakdownCause = string.IsNullOrWhiteSpace(BreakdownCauseEditor.Text)
                        ? null
                        : BreakdownCauseEditor.Text.Trim(),
                    Attachments = JsonSerializer.Serialize(urls),
                    EmergencyStatus = "waiting",
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    // These will be null by default, matching your table structure
                    AcceptedAt = null,
                    CompletedAt = null,
                    CanceledAt = null,
                    CanceledReason = null,
                    // Set created_at to current UTC time
                    CreatedAt = DateTimeOffset.UtcNow
                };

                // Show the data being sent in an alert
                var dataPreview = $@"Data to be sent:
Emergency ID: {row.EmergencyId}
User ID: {row.UserId}
Vehicle Type: {row.VehicleType}
Breakdown Cause: {row.BreakdownCause ?? "None"}
Attachments: {row.Attachments}
Status: {row.EmergencyStatus}
Latitude: {row.Latitude}
Longitude: {row.Longitude}
Created At: {row.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC";

                var proceed = await DisplayAlert("Confirm Data", dataPreview, "Yes, Post It", "Cancel");
                if (!proceed) return;

                await supabase.From<EmergencyRow>().Insert(row);

                await DisplayAlert("Posted", "Your emergency request was created.", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                // Enhanced error message with more details
                var errorDetails = $@"Error: {ex.Message}

Stack Trace: {ex.StackTrace}";

                await DisplayAlert("Error", errorDetails, "OK");
            }
        }
    }
}