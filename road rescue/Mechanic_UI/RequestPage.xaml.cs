using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;   // Geolocation
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Models;

namespace road_rescue
{
    public partial class RequestPage : ContentPage
    {
        private bool _isDrawerOpen = false;
        private Location? _myLocation; // current mechanic location

        // 👉 Change this if your bucket name is different
        private const string EmergencyBucket = "emergency";

        public RequestPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await EnsureLocationAsync();
            LoadEmergencyRequests();
        }

        private async Task EnsureLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                _myLocation = await Geolocation.Default.GetLocationAsync(request)
                               ?? await Geolocation.Default.GetLastKnownLocationAsync();

                Console.WriteLine(_myLocation != null
                    ? $"Got location: lat={_myLocation.Latitude}, lon={_myLocation.Longitude}"
                    : "Location unavailable (distance may be hidden)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Geolocation failed: {ex.Message}");
                _myLocation = null;
            }
        }

        private async void LoadEmergencyRequests()
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client!;

                // Build RPC args only if we have a location
                Dictionary<string, object>? args = null;
                if (_myLocation != null)
                {
                    args = new Dictionary<string, object>
                    {
                        ["_lat"] = _myLocation.Latitude,
                        ["_lon"] = _myLocation.Longitude
                    };
                }

                // Call the v2 RPC which returns driver_name + distance_km + accepted_by, etc.
                var rpcResp = await supabase.Rpc("visible_emergencies_v2", args);

                var emergencies = JsonSerializer.Deserialize<List<EmergencyDto>>(
                    rpcResp.Content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<EmergencyDto>();

                EmergencyCardsContainer.Children.Clear();

                if (emergencies.Count == 0)
                {
                    EmergencyCardsContainer.Children.Add(new Label
                    {
                        Text = "No emergency requests right now",
                        TextColor = Colors.Gray,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 20)
                    });
                    return;
                }

                foreach (var emergency in emergencies)
                {
                    EmergencyCardsContainer.Children.Add(CreateEmergencyCard(emergency));
                }
            }
            catch (PostgrestException pgx)
            {
                await DisplayAlert("Error", $"RPC error: {pgx.Message}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load emergencies: {ex.Message}", "OK");
                EmergencyCardsContainer.Children.Clear();
                EmergencyCardsContainer.Children.Add(new Label
                {
                    Text = $"Error loading emergencies: {ex.Message}",
                    TextColor = Colors.Red,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 20)
                });
            }
        }

        private Frame CreateEmergencyCard(EmergencyDto emergency)
        {
            // Parse jsonb attachments robustly → List<string> (paths or urls)
            var attachments = ParseAttachmentPaths(emergency.Attachments);

            var card = new Frame
            {
                BackgroundColor = Color.FromArgb("#F9F9F9"),
                CornerRadius = 15,
                Padding = 15,
                HasShadow = true,
                BorderColor = Color.FromArgb("#E0E0E0"),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var layout = new VerticalStackLayout { Spacing = 8 };

            layout.Children.Add(new Label
            {
                Text = $"Name: {(!string.IsNullOrWhiteSpace(emergency.DriverName) ? emergency.DriverName : "Unknown Driver")}",
                FontSize = 14,
                TextColor = Color.FromArgb("#111827")
            });

            // Prefer server-side distance if present; otherwise compute client-side.
            if (emergency.DistanceKm.HasValue)
            {
                layout.Children.Add(new Label
                {
                    Text = $"Distance: {FormatDistance(emergency.DistanceKm.Value)}",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#111827")
                });
            }
            else if (_myLocation != null)
            {
                var km = HaversineKm(_myLocation.Latitude, _myLocation.Longitude,
                                     emergency.Latitude, emergency.Longitude);
                layout.Children.Add(new Label
                {
                    Text = $"Distance: {FormatDistance(km)}",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#111827")
                });
            }

            layout.Children.Add(new Label
            {
                Text = $"Vehicle Type: {(!string.IsNullOrWhiteSpace(emergency.VehicleType) ? emergency.VehicleType : "—")}",
                FontSize = 14,
                TextColor = Color.FromArgb("#111827")
            });

            if (!string.IsNullOrEmpty(emergency.BreakdownCause))
            {
                layout.Children.Add(new Label
                {
                    Text = $"Breakdown Cause: {emergency.BreakdownCause}",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#111827")
                });
            }

            layout.Children.Add(new Label
            {
                Text = $"Location: ({emergency.Latitude:0.0000}° N, {emergency.Longitude:0.0000}° E)",
                FontSize = 14,
                TextColor = Color.FromArgb("#111827")
            });

            if (attachments.Count > 0)
            {
                var attachmentLabel = new Label
                {
                    Text = attachments.Count == 1 ? "View image" : $"View {attachments.Count} images",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#3B82F6")
                };
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await ShowImageModalAsync(attachments, 0);
                attachmentLabel.GestureRecognizers.Add(tapGesture);
                layout.Children.Add(attachmentLabel);
            }
            else
            {
                // Fallback link: if RPC didn't include attachments, try fetching directly from the table
                var tryLoad = new Label
                {
                    Text = "Load images (if any)",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#6B7280")
                };
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await TryLoadAndShowImages(emergency.Id);
                tryLoad.GestureRecognizers.Add(tapGesture);
                layout.Children.Add(tryLoad);
            }

            layout.Children.Add(new Label
            {
                Text = $"Date and Time: {emergency.CreatedAt:yyyy-MM-dd hh:mm tt}",
                FontSize = 14,
                TextColor = Color.FromArgb("#111827")
            });

            var statusColor = emergency.EmergencyStatus?.ToLower() switch
            {
                "waiting" => Color.FromArgb("#FF9800"),
                "in_process" => Color.FromArgb("#2196F3"),
                "completed" => Color.FromArgb("#4CAF50"),
                "canceled" => Color.FromArgb("#EF4444"),
                _ => Color.FromArgb("#111827")
            };
            layout.Children.Add(new Label
            {
                Text = $"Status: {emergency.EmergencyStatus?.ToUpper() ?? "UNKNOWN"}",
                FontSize = 14,
                TextColor = statusColor
            });

            if (emergency.EmergencyStatus?.ToLower() == "waiting")
            {
                var buttonLayout = new HorizontalStackLayout
                {
                    Spacing = 15,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var viewLocationButton = new Button
                {
                    Text = "Location",
                    BackgroundColor = Color.FromArgb("#3B82F6"),
                    TextColor = Colors.White,
                    CornerRadius = 20,
                    Padding = new Thickness(10, 5),
                    WidthRequest = 90
                };
                viewLocationButton.Clicked += async (s, e) => await OpenInGoogleMapsAsync(emergency);

                var declineButton = new Button
                {
                    Text = "Decline",
                    BackgroundColor = Color.FromArgb("#EF4444"),
                    TextColor = Colors.White,
                    CornerRadius = 20,
                    Padding = new Thickness(10, 5),
                    WidthRequest = 90
                };
                declineButton.Clicked += async (s, e) => await HandleDecline(emergency);

                var acceptButton = new Button
                {
                    Text = "Accept",
                    BackgroundColor = Color.FromArgb("#10B981"),
                    TextColor = Colors.White,
                    CornerRadius = 20,
                    Padding = new Thickness(10, 5),
                    WidthRequest = 90
                };
                acceptButton.Clicked += async (s, e) => await HandleAccept(emergency);

                buttonLayout.Children.Add(viewLocationButton);
                buttonLayout.Children.Add(declineButton);
                buttonLayout.Children.Add(acceptButton);
                layout.Children.Add(buttonLayout);
            }

            card.Content = layout;
            return card;
        }

        // === Try loading attachments directly from the table if RPC omitted them ===
        private async Task TryLoadAndShowImages(long emergencyId)
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client!;
                var row = await supabase.From<EmergencyRow>()
                                        .Select("attachments,id") // minimal
                                        .Where(x => x.Id == emergencyId)
                                        .Single();

                // row.Attachments is a JSON string like '["a/b.jpg"]'
                var list = ParseAttachmentPaths(row.Attachments);
                if (list.Count == 0)
                {
                    await DisplayAlert("Images", "No images attached.", "OK");
                    return;
                }
                await ShowImageModalAsync(list, 0);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Images", $"Couldn't load images: {ex.Message}", "OK");
            }
        }

        // === In-app image modal (swipe + pinch) ===
        private async Task ShowImageModalAsync(IList<string> urlsOrPaths, int startIndex = 0)
        {
            // Resolve storage paths → absolute public URLs
            var resolved = await ResolveImageUrlsAsync(urlsOrPaths);

            var sources = resolved
                .Where(u => Uri.TryCreate(u, UriKind.Absolute, out _))
                .Select(u => ImageSource.FromUri(new Uri(u)))
                .ToList();

            if (sources.Count == 0)
            {
                await DisplayAlert("Images", "No viewable images.", "OK");
                return;
            }

            var overlay = new Grid
            {
                BackgroundColor = Color.FromRgba(0, 0, 0, 0.9),
                RowDefinitions = { new RowDefinition { Height = GridLength.Star } },
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } },
                Padding = 0
            };

            var closeBtn = new ImageButton
            {
                Source = "x_icon.png",
                BackgroundColor = Colors.Transparent,
                WidthRequest = 36,
                HeightRequest = 36,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(12, 20, 12, 12)
            };
            closeBtn.Clicked += async (_, __) => await Navigation.PopModalAsync();

            var carousel = new CarouselView
            {
                ItemsSource = sources,
                IsSwipeEnabled = true,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                ItemTemplate = new DataTemplate(() =>
                {
                    var container = new Grid
                    {
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill
                    };

                    var image = new Image { Aspect = Aspect.AspectFit };
                    image.SetBinding(Image.SourceProperty, ".");

                    var pinch = new PinchGestureRecognizer();
                    double currentScale = 1, startScale = 1, xOffset = 0, yOffset = 0;

                    pinch.PinchUpdated += (s, e) =>
                    {
                        if (e.Status == GestureStatus.Started)
                        {
                            startScale = image.Scale;
                            image.AnchorX = 0; image.AnchorY = 0;
                        }
                        else if (e.Status == GestureStatus.Running)
                        {
                            currentScale = Math.Max(1, startScale * e.Scale);
                            var renderedX = image.X + xOffset;
                            var deltaX = renderedX / Width;
                            var deltaWidth = Width / (image.Width * startScale);
                            var originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;

                            var renderedY = image.Y + yOffset;
                            var deltaY = renderedY / Height;
                            var deltaHeight = Height / (image.Height * startScale);
                            var originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

                            image.TranslationX = -originX * image.Width * (currentScale - 1);
                            image.TranslationY = -originY * image.Height * (currentScale - 1);
                            image.Scale = currentScale;
                        }
                        else if (e.Status == GestureStatus.Completed)
                        {
                            xOffset = image.TranslationX;
                            yOffset = image.TranslationY;
                        }
                    };

                    var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
                    doubleTap.Tapped += (s, e) =>
                    {
                        image.Scale = 1;
                        image.TranslationX = 0;
                        image.TranslationY = 0;
                    };

                    image.GestureRecognizers.Add(pinch);
                    image.GestureRecognizers.Add(doubleTap);

                    container.Children.Add(image);
                    return container;
                })
            };

            carousel.Position = Math.Min(Math.Max(0, startIndex), sources.Count - 1);

            var backdropTap = new TapGestureRecognizer();
            backdropTap.Tapped += async (_, __) => await Navigation.PopModalAsync();
            overlay.GestureRecognizers.Add(backdropTap);

            overlay.Children.Add(carousel);
            overlay.Children.Add(closeBtn);

            var modal = new ContentPage { BackgroundColor = Colors.Transparent, Content = overlay };
            await Navigation.PushModalAsync(modal);
        }

        // Resolve Supabase storage paths to absolute, public URLs.
        private async Task<List<string>> ResolveImageUrlsAsync(IList<string> pathsOrUrls)
        {
            var list = new List<string>(pathsOrUrls.Count);
            try
            {
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client;

                foreach (var p in pathsOrUrls)
                {
                    if (string.IsNullOrWhiteSpace(p)) continue;

                    // Already an absolute URL?
                    if (Uri.TryCreate(p, UriKind.Absolute, out _))
                    {
                        list.Add(p);
                        continue;
                    }

                    // Treat as storage object path within the bucket
                    try
                    {
                        var url = supabase?.Storage?.From(EmergencyBucket)?.GetPublicUrl(p);
                        if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out _))
                            list.Add(url!);
                    }
                    catch
                    {
                        // ignore bad paths
                    }
                }
            }
            catch
            {
                // ignore; return whatever we have
            }
            return list;
        }

        // === Open Google Maps with directions if possible ===
        private async Task OpenInGoogleMapsAsync(EmergencyDto emergency)
        {
            string Dest(double x) => x.ToString(CultureInfo.InvariantCulture);
            var dest = $"{Dest(emergency.Latitude)},{Dest(emergency.Longitude)}";

            if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                string iosUrl = _myLocation != null
                    ? $"comgooglemaps://?saddr={Dest(_myLocation.Latitude)},{Dest(_myLocation.Longitude)}&daddr={dest}&directionsmode=driving"
                    : $"comgooglemaps://?daddr={dest}&directionsmode=driving";

                if (await Launcher.CanOpenAsync(iosUrl)) { await Launcher.OpenAsync(iosUrl); return; }
            }

            if (DeviceInfo.Platform == DevicePlatform.Android && _myLocation != null)
            {
                var androidNav = $"google.navigation:q={dest}&mode=d";
                if (await Launcher.CanOpenAsync(androidNav)) { await Launcher.OpenAsync(androidNav); return; }
            }

            string url = _myLocation != null
                ? $"https://www.google.com/maps/dir/?api=1&origin={Dest(_myLocation.Latitude)},{Dest(_myLocation.Longitude)}&destination={dest}&travelmode=driving"
                : $"https://www.google.com/maps/search/?api=1&query={dest}";

            await Launcher.OpenAsync(new Uri(url));
        }

        private async Task HandleAccept(EmergencyDto emergency)
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client!;

                // Get current auth user id (string) → guid (if possible)
                Guid? acceptedBy = null;
                var authUserId = supabase.Auth.CurrentUser?.Id; // typically a UUID string
                if (!string.IsNullOrWhiteSpace(authUserId) && Guid.TryParse(authUserId, out var parsed))
                    acceptedBy = parsed;

                // Load the row to update (by PK id) then patch fields
                var row = await supabase.From<EmergencyRow>()
                                        .Where(x => x.Id == emergency.Id)
                                        .Single();

                row.EmergencyStatus = "in_process";
                row.AcceptedAt = DateTimeOffset.UtcNow;
                row.AcceptedBy = acceptedBy;

                await supabase.From<EmergencyRow>().Update(row);

                await DisplayAlert("Success", "Emergency request accepted!", "OK");
                LoadEmergencyRequests();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to accept request: {ex.Message}", "OK");
            }
        }

        private async Task HandleDecline(EmergencyDto emergency)
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client!;

                var row = await supabase.From<EmergencyRow>()
                                        .Where(x => x.Id == emergency.Id)
                                        .Single();

                row.EmergencyStatus = "canceled";
                row.CanceledAt = DateTimeOffset.UtcNow;
                row.CanceledReason = "Declined by mechanic";

                await supabase.From<EmergencyRow>().Update(row);

                await DisplayAlert("Success", "Emergency request declined!", "OK");
                LoadEmergencyRequests();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to decline request: {ex.Message}", "OK");
            }
        }

        private async void ToggleDrawer()
        {
            if (_isDrawerOpen)
            {
                await DrawerMenu.TranslateTo(-280, 0, 300, Easing.CubicOut);
                DrawerOverlay.IsVisible = false;
            }
            else
            {
                await DrawerMenu.TranslateTo(0, 0, 300, Easing.CubicIn);
                DrawerOverlay.IsVisible = true;
            }
            _isDrawerOpen = !_isDrawerOpen;
        }

        private void OnMenuClicked(object sender, EventArgs e) => ToggleDrawer();
        private void OnOverlayTapped(object sender, EventArgs e) { if (_isDrawerOpen) ToggleDrawer(); }
        private void OnNotificationsClicked(object sender, EventArgs e) { if (_isDrawerOpen) ToggleDrawer(); }
        private void OnRequestClicked(object sender, EventArgs e) { if (_isDrawerOpen) ToggleDrawer(); }

        private async void OnMessagesClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen) ToggleDrawer();
            await Navigation.PushAsync(new messagesM());
        }

        private async void OnCompleteClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen) ToggleDrawer();
            await Navigation.PushAsync(new CompletePage());
        }

        private async void OnViewLocationClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new road_rescue.Mechanic_UI.AllowAccess());
        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e) => ToggleDrawer();

        private async void OnProfileTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new MechanicProfile());
        }

        private async void OnLogoutTapped(object sender, TappedEventArgs e)
        {
            try
            {
                await road_rescue.Services.AuthService.LogoutAsync();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Application.Current.MainPage = new NavigationPage(new logInPage());
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Logout", $"Something went wrong: {ex.Message}", "OK");
            }
        }

        // === Distance helpers (Haversine) ===
        private static double ToRad(double deg) => Math.PI * deg / 180.0;

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static string FormatDistance(double km)
        {
            if (double.IsNaN(km) || double.IsInfinity(km)) return "—";
            if (km < 1) return $"{Math.Round(km * 1000):0} m away";
            if (km < 10) return $"{km:F1} km away";
            return $"{Math.Round(km):0} km away";
        }

        // === Robust attachment parsing helpers ===

        private static List<string> ParseAttachmentPaths(JsonElement? elNullable)
        {
            var list = new List<string>();
            if (!elNullable.HasValue) return list;
            return ParseAttachmentPaths(elNullable.Value);
        }

        private static List<string> ParseAttachmentPaths(string? jsonOrCsv)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(jsonOrCsv)) return list;

            var s = jsonOrCsv.Trim();
            try
            {
                if ((s.StartsWith("[") && s.EndsWith("]")) || (s.StartsWith("{") && s.EndsWith("}")))
                {
                    using var doc = JsonDocument.Parse(s);
                    list.AddRange(ParseAttachmentPaths(doc.RootElement));
                }
                else
                {
                    // comma/semicolon-separated
                    list.AddRange(s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(x => x.Trim()));
                }
            }
            catch
            {
                // if parsing fails, attempt simple split
                list.AddRange(s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(x => x.Trim()));
            }
            return list;
        }

        private static List<string> ParseAttachmentPaths(JsonElement el)
        {
            var list = new List<string>();
            try
            {
                switch (el.ValueKind)
                {
                    case JsonValueKind.Array:
                        foreach (var item in el.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                                list.Add(item.GetString()!);
                            else if (item.ValueKind == JsonValueKind.Object)
                            {
                                if (item.TryGetProperty("path", out var p) && p.ValueKind == JsonValueKind.String)
                                    list.Add(p.GetString()!);
                                else if (item.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String)
                                    list.Add(u.GetString()!);
                            }
                        }
                        break;

                    case JsonValueKind.String:
                        list.AddRange(ParseAttachmentPaths(el.GetString()));
                        break;

                    case JsonValueKind.Object:
                        if (el.TryGetProperty("path", out var pathProp) && pathProp.ValueKind == JsonValueKind.String)
                            list.Add(pathProp.GetString()!);
                        else if (el.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                            list.Add(urlProp.GetString()!);
                        break;
                }
            }
            catch { /* ignore */ }

            // de-dup + remove empties
            return list.Where(x => !string.IsNullOrWhiteSpace(x))
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList();
        }
    }

    // ===== RPC DTO (matches visible_emergencies_v2 RETURN TABLE) =====
    public class EmergencyDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }

        [JsonPropertyName("emergency_id")] public Guid EmergencyId { get; set; }
        [JsonPropertyName("user_id")] public Guid UserId { get; set; }

        [JsonPropertyName("vehicle_type")] public string VehicleType { get; set; } = "";
        [JsonPropertyName("breakdown_cause")] public string? BreakdownCause { get; set; }

        [JsonPropertyName("attachments")] public JsonElement? Attachments { get; set; } // jsonb or stringified json

        [JsonPropertyName("emergency_status")] public string EmergencyStatus { get; set; } = "waiting";

        [JsonPropertyName("latitude")] public double Latitude { get; set; }
        [JsonPropertyName("longitude")] public double Longitude { get; set; }

        [JsonPropertyName("accepted_at")] public DateTimeOffset? AcceptedAt { get; set; }
        [JsonPropertyName("completed_at")] public DateTimeOffset? CompletedAt { get; set; }
        [JsonPropertyName("canceled_at")] public DateTimeOffset? CanceledAt { get; set; }
        [JsonPropertyName("canceled_reason")] public string? CanceledReason { get; set; }

        [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("accepted_by")] public Guid? AcceptedBy { get; set; }

        [JsonPropertyName("distance_km")] public double? DistanceKm { get; set; }

        [JsonPropertyName("driver_name")] public string? DriverName { get; set; }
    }

    // ===== Table model for updates (PATCH via Update / read fallback) =====
    [Table("emergency")]
    public class EmergencyRow : BaseModel
    {
        [PrimaryKey("id", false)] public long Id { get; set; }
        [Column("emergency_id")] public Guid EmergencyId { get; set; }
        [Column("user_id")] public Guid UserId { get; set; }
        [Column("vehicle_type")] public string VehicleType { get; set; } = "";
        [Column("breakdown_cause")] public string? BreakdownCause { get; set; }
        [Column("attachments")] public string Attachments { get; set; } = "[]"; // stored as JSON string
        [Column("emergency_status")] public string EmergencyStatus { get; set; } = "waiting";
        [Column("latitude")] public double Latitude { get; set; }
        [Column("longitude")] public double Longitude { get; set; }
        [Column("accepted_at")] public DateTimeOffset? AcceptedAt { get; set; }
        [Column("completed_at")] public DateTimeOffset? CompletedAt { get; set; }
        [Column("canceled_at")] public DateTimeOffset? CanceledAt { get; set; }
        [Column("canceled_reason")] public string? CanceledReason { get; set; }
        [Column("created_at")] public DateTimeOffset CreatedAt { get; set; }
        [Column("accepted_by")] public Guid? AcceptedBy { get; set; }
    }
}
