using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace road_rescue
{
    public partial class ReviewsPage : ContentPage
    {
        private View _mainContent;
        public ObservableCollection<PendingReviewModel> PendingReviews { get; set; }

        public ReviewsPage()
        {
            InitializeComponent();

            // Store the original content
            _mainContent = Content;

            PendingReviews = new ObservableCollection<PendingReviewModel>
            {
                new PendingReviewModel
                {
                    ProviderName = "Tire Pro Service",
                    ServiceRequested = "Tire Replacement",
                    Location = "123 Main St, City",
                    DateRequested = new DateTime(2025, 5, 10),
                    DateCompleted = new DateTime(2025, 5, 11),
                    ReviewCommand = new Command(() => ShowReviewPopup("Tire Pro Service"))
                },
                new PendingReviewModel
                {
                    ProviderName = "Quick Tow Inc.",
                    ServiceRequested = "Towing",
                    Location = "456 Elm St, City",
                    DateRequested = new DateTime(2025, 5, 9),
                    DateCompleted = new DateTime(2025, 5, 9),
                    ReviewCommand = new Command(() => ShowReviewPopup("Quick Tow Inc."))
                }
            };

            PendingReviewsList.ItemsSource = PendingReviews;
        }

        private async void ShowReviewPopup(string providerName)
        {
            string description = "";
            int rating = 0;
            string imagePath = "no_image.png"; // Default

            var popup = new Grid
            {
                BackgroundColor = Color.FromArgb("#80000000"),
                Padding = 30
            };

            var popupContent = new Border
            {
                BackgroundColor = Colors.White,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = 20,
                WidthRequest = 300,
                Content = new VerticalStackLayout
                {
                    Spacing = 15,
                    Children =
                    {
                        new Label { Text = $"Review for {providerName}", FontAttributes = FontAttributes.Bold, FontSize = 20, HorizontalOptions = LayoutOptions.Center },

                        new Label { Text = "Rate this provider:" },
                        new HorizontalStackLayout
                        {
                            Children =
                            {
                                new Button { Text = "⭐", Command = new Command(() => rating = 1) },
                                new Button { Text = "⭐⭐", Command = new Command(() => rating = 2) },
                                new Button { Text = "⭐⭐⭐", Command = new Command(() => rating = 3) },
                                new Button { Text = "⭐⭐⭐⭐", Command = new Command(() => rating = 4) },
                                new Button { Text = "⭐⭐⭐⭐⭐", Command = new Command(() => rating = 5) },
                            }
                        },

                        new Label { Text = "Write a comment:" },
                        new Editor { Placeholder = "Type your feedback here...", AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 100 },

                        new Button
                        {
                            BackgroundColor = Colors.Gray,
                            Text = "Upload Photo (optional)",
                            Command = new Command(() => imagePath = "uploaded_sample.jpg") // Simulated
                        },

                        new HorizontalStackLayout
                        {
                            Spacing = 10,
                            Children =
                            {
                                new Button
                                {
                                    Text = "Cancel",
                                    BackgroundColor = Colors.Gray,
                                    TextColor = Colors.White,
                                    Command = new Command(() => Content = _mainContent)
                                },
                                new Button
                                {
                                    Text = "Submit Review",
                                    BackgroundColor = Color.FromArgb("#3070f6"),
                                    TextColor = Colors.White,
                                    Command = new Command(async () =>
                                    {
                                        await DisplayAlert("Thank You", "Your review has been submitted.", "OK");
                                        Content = _mainContent;
                                        
                                        // Optional: Remove the reviewed item
                                        var itemToRemove = PendingReviews.FirstOrDefault(p => p.ProviderName == providerName);
                                        if (itemToRemove != null)
                                        {
                                            PendingReviews.Remove(itemToRemove);
                                        }
                                    })
                                }
                            }
                        }
                    }
                }
            };

            popup.Add(popupContent);
            Content = popup;
        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            Shell.Current.FlyoutIsPresented = true;
        }
    }

    public class PendingReviewModel
    {
        public string ProviderName { get; set; } = string.Empty;
        public string ServiceRequested { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime DateRequested { get; set; }
        public DateTime DateCompleted { get; set; }
        public ICommand ReviewCommand { get; set; } = null!;
    }
}