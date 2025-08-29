using System;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;   // Geolocation
using Microsoft.Maui.Devices;            // DeviceInfo
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace road_rescue
{
    public partial class RequestPage : ContentPage
    {
        private bool _isDrawerOpen = false;

        // Current mechanic location (set on page appear)
        private Location? _myLocation;

        public RequestPage()
        {
            InitializeComponent();
            // Resolve location in OnAppearing
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
                    : "Location unavailable (distance/directions may be limited)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Geolocation failed: {ex.Message}");
                _myLocation = null; // continue without distance
            }
        }

        private async void LoadEmergencyRequests()
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client!;

                Console.WriteLine("Attempting to fetch emergencies...");

                var emergencies = await supabase
                    .From<EmergencyRow>()
                    .Select("*")
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                Console.WriteLine($"Query completed. Found {emergencies.Models?.Count ?? 0} emergencies");

                EmergencyCardsContainer.Children.Clear();

                if (emergencies.Models?.Any() != true)
                {
                    EmergencyCardsContainer.Children.Add(new Label
                    {
                        Text = "No emergency requests found in database",
                        TextColor = Colors.Gray,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 20)
                    });
                    return;
                }

                foreach (var emergency in emergencies.Models)
                {
                    Console.WriteLine($"Processing emergency: {emergency.EmergencyId}, Status: {emergency.EmergencyStatus}");

                    try
                    {
                        var userResult = await supabase
                            .From<AppUserRow>()
                            .Where(x => x.UserId == emergency.UserId)
                            .Single();

                        var card = CreateEmergencyCard(emergency, userResult);
                        EmergencyCardsContainer.Children.Add(card);
                    }
                    catch (Exception userEx)
                    {
                        Console.WriteLine($"User lookup failed for emergency {emergency.EmergencyId}: {userEx.Message}");
                        var card = CreateEmergencyCard(emergency, null);
                        EmergencyCardsContainer.Children.Add(card);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading emergencies: {ex}");
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

        private Frame CreateEmergencyCard(EmergencyRow emergency, AppUserRow? user)
        {
            // Parse attachments
            List<string> attachments = new();
            try
            {
                if (!string.IsNullOrEmpty(emergency.Attachments))
                    attachments = JsonSerializer.Deserialize<List<string>>(emergency.Attachments) ?? new List<string>();
            }
            catch
            {
                attachments = new List<string>();
            }

            var hasAttachments = attachments.Any();

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

            // Emergency ID for debugging
            //layout.Children.Add(new Label
            //{
            //    Text = $"Emergency ID: {emergency.EmergencyId}",
            //    FontSize = 12,
            //    TextColor = Colors.Gray
            //});

            // Driver Name
            layout.Children.Add(new Label
            {
                Text = $"Name: {user?.FullName ?? "Unknown Driver"}",
                FontSize = 14,
                TextColor = Color.FromArgb("#111827")
            });

            // Distance (if we have current location)
            if (_myLocation != null)
            {
                var km = HaversineKm(
                    _myLocation.Latitude,
                    _myLocation.Longitude,
                    emergency.Latitude,
                    emergency.Longitude
                );

                layout.Children.Add(new Label
                {
                    Text = $"Distance: {FormatDistance(km)}",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#111827")
                });
            }

            // Vehicle Type
            layout.Children.Add(new Label
            {
                Text = $"Vehicle Type: {emergency.VehicleType}",
                FontSize = 14,
                TextColor = Color.FromArgb("#111827")
            });

            // Breakdown Cause
            if (!string.IsNullOrEmpty(emergency.BreakdownCause))
            {
                layout.Children.Add(new Label
                {
                    Text = $"Breakdown Cause: {emergency.BreakdownCause}",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#111827")
                });
            }

            // Location Coordinates
            layout.Children.Add(new Label
            {
                Text = $"Location: ({emergency.Latitude:0.0000}° N, {emergency.Longitude:0.0000}° E)",
                FontSize = 14,
                TextColor = Color.FromArgb("#111827")
            });

            // Attachments: open in-app image modal (swipe + pinch), not Chrome
            if (hasAttachments)
            {
                var attachmentLabel = new Label
                {
                    Text = attachments.Count == 1 ? "View image" : $"View {attachments.Count} images",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#3B82F6")
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    await ShowImageModalAsync(attachments, 0);
                };
                attachmentLabel.GestureRecognizers.Add(tapGesture);

                layout.Children.Add(attachmentLabel);
            }
            else
            {
                layout.Children.Add(new Label
                {
                    Text = "No images attached",
                    FontSize = 14,
                    TextColor = Colors.Gray
                });
            }

            // Date and Time
            layout.Children.Add(new Label
            {
                Text = $"Date and Time: {emergency.CreatedAt:yyyy-MM-dd hh:mm tt}",
                FontSize = 14,
                TextColor = Color.FromArgb("#111827")
            });

            // Emergency Status with color coding
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

            // Buttons (only show for waiting status)
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
                viewLocationButton.Clicked += async (s, e) =>
                {
                    await OpenInGoogleMapsAsync(emergency);
                };

                var declineButton = new Button
                {
                    Text = "Decline",
                    BackgroundColor = Color.FromArgb("#EF4444"),
                    TextColor = Colors.White,
                    CornerRadius = 20,
                    Padding = new Thickness(10, 5),
                    WidthRequest = 90
                };
                declineButton.Clicked += async (s, e) => { await HandleDecline(emergency); };

                var acceptButton = new Button
                {
                    Text = "Accept",
                    BackgroundColor = Color.FromArgb("#10B981"),
                    TextColor = Colors.White,
                    CornerRadius = 20,
                    Padding = new Thickness(10, 5),
                    WidthRequest = 90
                };
                acceptButton.Clicked += async (s, e) => { await HandleAccept(emergency); };

                buttonLayout.Children.Add(viewLocationButton);
                buttonLayout.Children.Add(declineButton);
                buttonLayout.Children.Add(acceptButton);

                layout.Children.Add(buttonLayout);
            }

            card.Content = layout;
            return card;
        }

        // === In-app image modal (swipe + pinch) ===
        private async Task ShowImageModalAsync(IList<string> urls, int startIndex = 0)
        {
            // Build sources from URLs (uses ImageSource.FromUri for remote images)
            var sources = urls
                .Where(u => Uri.TryCreate(u, UriKind.Absolute, out _))
                .Select(u => ImageSource.FromUri(new Uri(u))) // docs: FromUri loads from URI
                .ToList();

            if (sources.Count == 0)
            {
                await DisplayAlert("Images", "No viewable images.", "OK");
                return;
            }

            // Modal page layout
            var overlay = new Grid
            {
                BackgroundColor = Color.FromRgba(0, 0, 0, 0.9),
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Star }
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star }
                },
                Padding = 0
            };

            // Close button
            var closeBtn = new ImageButton
            {
                Source = "x_icon.png", // add an 'X' asset; or replace with text button
                BackgroundColor = Colors.Transparent,
                WidthRequest = 36,
                HeightRequest = 36,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(12, 20, 12, 12)
            };
            closeBtn.Clicked += async (_, __) => await Navigation.PopModalAsync();

            // Carousel with pinch-zoomable images
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

                    var image = new Image
                    {
                        Aspect = Aspect.AspectFit
                    };
                    image.SetBinding(Image.SourceProperty, ".");

                    // Pinch-to-zoom (per MAUI pinch docs)
                    var pinch = new PinchGestureRecognizer();
                    double currentScale = 1;
                    double startScale = 1;
                    double xOffset = 0;
                    double yOffset = 0;

                    pinch.PinchUpdated += (s, e) =>
                    {
                        if (e.Status == GestureStatus.Started)
                        {
                            startScale = image.Scale;
                            image.AnchorX = 0;
                            image.AnchorY = 0;
                        }
                        else if (e.Status == GestureStatus.Running)
                        {
                            currentScale = Math.Max(1, startScale * e.Scale); // clamp min 1x
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
                            // store translation offsets
                            xOffset = image.TranslationX;
                            yOffset = image.TranslationY;
                        }
                    };

                    // Double-tap to reset zoom
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

            // Start at tapped image
            carousel.Position = Math.Min(Math.Max(0, startIndex), sources.Count - 1);

            // Tap outside to close
            var backdropTap = new TapGestureRecognizer();
            backdropTap.Tapped += async (_, __) => await Navigation.PopModalAsync();
            overlay.GestureRecognizers.Add(backdropTap);

            overlay.Children.Add(carousel);
            overlay.Children.Add(closeBtn);

            var modal = new ContentPage
            {
                BackgroundColor = Colors.Transparent,
                Content = overlay
            };

            await Navigation.PushModalAsync(modal);
        }

        // === Open Google Maps with directions if possible ===
        private async Task OpenInGoogleMapsAsync(EmergencyRow emergency)
        {
            string Dest(double x) => x.ToString(CultureInfo.InvariantCulture);
            var destLat = Dest(emergency.Latitude);
            var destLon = Dest(emergency.Longitude);
            var dest = $"{destLat},{destLon}";

            if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                string iosUrl = _myLocation != null
                    ? $"comgooglemaps://?saddr={Dest(_myLocation.Latitude)},{Dest(_myLocation.Longitude)}&daddr={dest}&directionsmode=driving"
                    : $"comgooglemaps://?daddr={dest}&directionsmode=driving";

                if (await Launcher.CanOpenAsync(iosUrl))
                {
                    await Launcher.OpenAsync(iosUrl);
                    return;
                }
            }

            if (DeviceInfo.Platform == DevicePlatform.Android && _myLocation != null)
            {
                var androidNav = $"google.navigation:q={dest}&mode=d";
                if (await Launcher.CanOpenAsync(androidNav))
                {
                    await Launcher.OpenAsync(androidNav);
                    return;
                }
            }

            string url = _myLocation != null
                ? $"https://www.google.com/maps/dir/?api=1&origin={Dest(_myLocation.Latitude)},{Dest(_myLocation.Longitude)}&destination={dest}&travelmode=driving"
                : $"https://www.google.com/maps/search/?api=1&query={dest}";

            await Launcher.OpenAsync(new Uri(url));
        }

        private async Task HandleAccept(EmergencyRow emergency)
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client!;

                emergency.EmergencyStatus = "in_process";
                emergency.AcceptedAt = DateTimeOffset.UtcNow;

                await supabase.From<EmergencyRow>().Update(emergency);
                await DisplayAlert("Success", "Emergency request accepted!", "OK");
                LoadEmergencyRequests();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to accept request: {ex.Message}", "OK");
            }
        }

        private async Task HandleDecline(EmergencyRow emergency)
        {
            try
            {
                await SupabaseService.InitializeAsync();
                var supabase = SupabaseService.Client!;

                emergency.EmergencyStatus = "canceled";
                emergency.CanceledAt = DateTimeOffset.UtcNow;
                emergency.CanceledReason = "Declined by mechanic";

                await supabase.From<EmergencyRow>().Update(emergency);
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

        private void OnOverlayTapped(object sender, EventArgs e)
        {
            if (_isDrawerOpen) ToggleDrawer();
        }

        private void OnNotificationsClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen) ToggleDrawer();
        }

        private void OnRequestClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen) ToggleDrawer();
        }

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

        // Great-circle distance in kilometers (mean Earth radius ≈ 6371 km)
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
    }

    // ===== Model classes (ensure columns match DB) =====
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

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }

    [Table("app_user")]
    public class AppUserRow : BaseModel
    {
        [PrimaryKey("user_id", false)]
        public Guid UserId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; } = "";

        [Column("email")]
        public string Email { get; set; } = "";

        [Column("role")]
        public string Role { get; set; } = "";
    }
}
