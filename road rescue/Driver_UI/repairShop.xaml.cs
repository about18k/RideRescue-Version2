using Microsoft.Maui.Controls;
using Supabase.Postgrest.Attributes;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace road_rescue.Driver_UI
{
    public partial class repairShop : ContentPage
    {
        private readonly Supabase.Client _supabase;
        public Location CurrentLocation { get; set; }
        public ObservableCollection<PlaceModel> Places { get; set; } = new ObservableCollection<PlaceModel>();

        // Command for opening maps
        public ICommand OpenMapsCommand { get; }

        public repairShop(Location location, Supabase.Client supabase)
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

                LoadRepairShops();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing repairShop: {ex}");
                DisplayAlert("Error", "Failed to load repair shops", "OK");
            }
        }


        private async void LoadRepairShops()
        {
            try
            {
                Places.Clear();


                var placesResponse = await _supabase
                    .From<PlaceModel>()
                    .Select("*")
                    .Where(x => x.category == "repair_shop")
                    .Get();


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
                            : 0
                    })
                    .OrderBy(x => x.DistanceKm)
                    .ToList();

                foreach (var item in sortedPlaces)
                {
                    item.Place.Distance = $"{item.DistanceKm:0.1} km away";
                    item.Place.rating = item.Rating;
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

        }

        //private void OnBackClicked(object sender, EventArgs e)
        //{
        //    Navigation.PopAsync();
        //}



        private void OnViewMapClicked(object sender, EventArgs e)
        {

        }

        private void OnCallClicked(object sender, EventArgs e)
        {

        }

        private void OnFavoriteClicked(object sender, EventArgs e)
        {

        }

        private async void OnMoreInfoClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MapPage());
        }

        //private async void OnMessageClicked(object sender, EventArgs e)
        //{
        //    await Navigation.PushAsync(new ChatPage());
        //}

        private void Button_Clicked(object sender, EventArgs e)
        {

        }
    }
    public class RatingToStarsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double rating)
            {
                int fullStars = (int)Math.Floor(rating);
                bool halfStar = (rating - fullStars >= 0.5);
                string stars = new string('⭐', fullStars);
                if (halfStar) stars += "⭐";
                return stars.PadRight(5, '☆');
            }
            return "☆☆☆☆☆";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    [Table("places")]
    public class PlaceModel : Supabase.Postgrest.Models.BaseModel
    {
        [PrimaryKey("place_id")]
        public string place_id { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string address { get; set; }
        public string plus_code { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string maps_link { get; set; }
        public string Distance { get; set; }
        public double rating { get; set; }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }

    [Table("ratings")]
    public class RatingModel : Supabase.Postgrest.Models.BaseModel
    {
        [PrimaryKey("rating_id")]
        public string rating_id { get; set; }
        public string driver_id { get; set; }
        public string place_id { get; set; }
        public double stars { get; set; }
        public string comment { get; set; }
    }
}
