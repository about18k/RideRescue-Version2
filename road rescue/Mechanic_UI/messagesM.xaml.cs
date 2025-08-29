using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using road_rescue.Data; // Ensure MessageModel is inside this namespace

namespace road_rescue
{
    public partial class messagesM : ContentPage
    {
        private bool _isDrawerOpen = false;
        public ObservableCollection<MessageModel> Messages { get; set; }

        public messagesM()
        {
            InitializeComponent();

            Messages = new ObservableCollection<MessageModel>
            {
                new MessageModel
                {
                    ProfileImage = "profile1.png",
                    SenderName = "Tire Pro Service",
                    MessagePreview = "We’re on the way to your location.",
                    TimeSent = "2:15 PM",
                    TapCommand = new Command(() => OnMessageTapped("Tire Pro Service"))
                },
                new MessageModel
                {
                    ProfileImage = "profile2.png",
                    SenderName = "Road Rescue Support",
                    MessagePreview = "Please share your current location.",
                    TimeSent = "1:30 PM",
                    TapCommand = new Command(() => OnMessageTapped("Road Rescue Support"))
                },
                new MessageModel
                {
                    ProfileImage = "claison.jpg",
                    SenderName = "Claison Mar Famor",
                    MessagePreview = "Palihug ko'g check sa ako guba nga ligid.",
                    TimeSent = "12:20 PM",
                    TapCommand = new Command(() => OnMessageTapped("Claison Mar Famor"))
                },
                new MessageModel
                {
                    ProfileImage = "michael.jpg",
                    SenderName = "Michael Saragena",
                    MessagePreview = "Boss, pila bayad sa towing padulong Argao?",
                    TimeSent = "11:15 AM",
                    TapCommand = new Command(() => OnMessageTapped("Michael Saragena"))
                },
                new MessageModel
                {
                    ProfileImage = "gino.jpg",
                    SenderName = "Gino Gabrielle",
                    MessagePreview = "My engine is overheating again 😩",
                    TimeSent = "10:40 AM",
                    TapCommand = new Command(() => OnMessageTapped("Gino Gabrielle"))
                },
                new MessageModel
                {
                    ProfileImage = "ken.png",
                    SenderName = "Khen Rocaberte",
                    MessagePreview = "Naay available mechanic ron?",
                    TimeSent = "Yesterday",
                    TapCommand = new Command(() => OnMessageTapped("Khen Rocaberte"))
                },
                new MessageModel
                {
                    ProfileImage = "kim.jpg",
                    SenderName = "Kimberly Faith Ytac",
                    MessagePreview = "Hi sir, flat tire ko near Dalaguete.",
                    TimeSent = "Yesterday",
                    TapCommand = new Command(() => OnMessageTapped("Kimberly Faith Ytac"))
                },
                new MessageModel
                {
                    ProfileImage = "jiesmera.jpg",
                    SenderName = "Jiesmera Omboy",
                    MessagePreview = "Good morning, available ba change oil today?",
                    TimeSent = "2 days ago",
                    TapCommand = new Command(() => OnMessageTapped("Jiesmera Omboy"))
                },
                new MessageModel
                {
                    ProfileImage = "jovanie.jpg",
                    SenderName = "Jovanie Felamin",
                    MessagePreview = "Boss naa kay spare tire?",
                    TimeSent = "3 days ago",
                    TapCommand = new Command(() => OnMessageTapped("Jovanie Felamin"))
                }
            };

            MessagesList.ItemsSource = Messages;
        }

        private async void ToggleDrawer()
        {
            if (_isDrawerOpen)
            {
                // Close drawer with animation
                await DrawerMenu.TranslateTo(-280, 0, 300, Easing.CubicOut);
                DrawerOverlay.IsVisible = false;
            }
            else
            {
                // Open drawer with animation
                await DrawerMenu.TranslateTo(0, 0, 300, Easing.CubicIn);
                DrawerOverlay.IsVisible = true;
            }
            _isDrawerOpen = !_isDrawerOpen;
        }

        private void OnMenuClicked(object sender, EventArgs e)
        {
            ToggleDrawer();
        }

        private void OnOverlayTapped(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
                ToggleDrawer();
        }

        private async void OnNotificationsClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
                ToggleDrawer();

            // TODO: Navigate to Notifications page if implemented
            // await Navigation.PushAsync(new NotificationsPage());
        }

        private async void OnRequestClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
                ToggleDrawer();

            // Example: navigate to RequestPage if needed
             await Navigation.PushAsync(new RequestPage());
        }

        private void OnMessagesClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
                ToggleDrawer();
            // Already on Messages page
        }

        private async void OnCompleteClicked(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
                ToggleDrawer();

            // Example: navigate to CompletePage if needed
            await Navigation.PushAsync(new CompletePage());
        }

        private async void OnMessageTapped(string sender)
        {
            // Optionally show chat prompt
            // await DisplayAlert("Chat", $"Opening chat with {sender}", "OK");

            await Navigation.PushAsync(new ChatPage());
        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            ToggleDrawer();
        }

        private async void OnProfileTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new MechanicProfile());
        }

        private async void OnLogoutTapped(object sender, TappedEventArgs e)
        {
            try
            {
                await road_rescue.Services.AuthService.LogoutAsync();

                // Hard-reset the root to Login (no back navigation possible)
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Application.Current.MainPage = new NavigationPage(new logInPage());
                    // or: Application.Current.MainPage = new logInPage();
                });
            }
            catch (Exception ex)
            {
                // Optional: log or show a friendly message
                await DisplayAlert("Logout", $"Something went wrong: {ex.Message}", "OK");
            }

        }
    }
}
