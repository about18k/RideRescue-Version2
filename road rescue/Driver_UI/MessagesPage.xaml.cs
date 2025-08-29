using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace road_rescue
{
    public partial class MessagesPage : ContentPage
    {
        public ObservableCollection<MessageModel> Messages { get; set; }

        public MessagesPage()
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
                }
            };

            MessagesList.ItemsSource = Messages;
        }

        private async void OnMessageTapped(string sender)
        {
            await DisplayAlert("Chat", $"Opening chat with {sender}", "OK");
            await Navigation.PushAsync(new ChatPage());
        }

        private void OnAddImageClicked(object sender, EventArgs e)
        {

        }

        private void OnSendClicked(object sender, EventArgs e)
        {

        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            Shell.Current.FlyoutIsPresented = true;
        }
    }

    public class MessageModel
    {
        public string ProfileImage { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string MessagePreview { get; set; } = string.Empty;
        public string TimeSent { get; set; } = string.Empty;
        public ICommand TapCommand { get; set; } = null!;
    }
}
