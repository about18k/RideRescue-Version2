using Microsoft.Maui.Controls;
using road_rescue.Driver_UI;
using Supabase.Postgrest.Attributes;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace road_rescue.Driver_UI
{
    public partial class gasStation : ContentPage
    {
        private readonly Supabase.Client _supabase;
        public Location CurrentLocation { get; set; }
        public ObservableCollection<PlaceModel> Places { get; set; } = new ObservableCollection<PlaceModel>();

        // Command for opening maps
        public ICommand OpenMapsCommand { get; }

        public gasStation(Location location, Supabase.Client supabase)
        {
            try
            {
                InitializeComponent();
                BindingContext = this;
                CurrentLocation = location;
                _supabase = supabase;

                OpenMapsCommand = new Command<string>(async (url) =>
                {
                    if (!string.IsNullOrEmpty(url))
                    {
                        try
                        {
                            await Launcher.OpenAsync(url);
                        }
                        catch (Exception ex)
                        {
                            await DisplayAlert("Error", $"Could not open maps: {ex.Message}", "OK");
                        }
                    }
                });

                LoadGasStations();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing GasStation: {ex}");
                DisplayAlert("Error", "Failed to load gas stations", "OK");
            }
        }

        private async void LoadGasStations()
        {
            try
            {
                Places.Clear();

                // 1. Fetch all gas stations
                var placesResponse = await _supabase
                    .From<PlaceModel>()
                    .Select("*")
                    .Where(x => x.category == "gas_station")
                    .Get();

                // 2. Fetch all ratings (single query is more efficient)
                var ratingsResponse = await _supabase
                    .From<RatingModel>()
                    .Select("*")
                    .Get();

                var ratingsLookup = ratingsResponse.Models
                    .GroupBy(r => r.place_id)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Average(r => r.stars)
                    );

                // 3. Calculate distance and assign ratings
                var sortedPlaces = placesResponse.Models
                    .Select(p => new
                    {
                        Place = p,
                        DistanceKm = CalculateDistance(
                            CurrentLocation.Latitude,
                            CurrentLocation.Longitude,
                            p.latitude,
                            p.longitude),
                        Rating = ratingsLookup.TryGetValue(p.place_id, out var avg)
                            ? avg
                            : 0 // Default to 0 if no ratings
                    })
                    .OrderBy(x => x.DistanceKm)
                    .ToList();

                foreach (var item in sortedPlaces)
                {
                    item.Place.Distance = $"{item.DistanceKm:0.1} km away";
                    item.Place.rating = item.Rating; // Assign the calculated average
                    Places.Add(item.Place);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            lat1 = ToRadians(lat1);
            lat2 = ToRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle) => Math.PI * angle / 180.0;

        private void OnBackClicked(object sender, EventArgs e) => Navigation.PopAsync();

        private async void OnMessageClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new ChatPage());

        private void OnLocationClicked(object sender, EventArgs e)
        {
            // Implement location functionality if needed
        }

        private void OnViewMapClicked(object sender, EventArgs e)
        {
            // Implement view map functionality if needed
        }

        private void OnCallClicked(object sender, EventArgs e)
        {
            // Implement call functionality if needed
        }

        private void OnFavoriteClicked(object sender, EventArgs e)
        {
            // Implement favorite functionality if needed
        }

        private async void OnMoreInfoClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MapPage());
        }

        private void OnChipClicked(object sender, EventArgs e)
        {
            // Implement chip functionality if needed
        }
    }
}
