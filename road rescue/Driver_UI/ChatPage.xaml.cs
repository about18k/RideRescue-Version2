using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using System;
using System.Globalization;
using Microsoft.Maui.Storage;

namespace road_rescue
{
    public partial class ChatPage : ContentPage
    {
        public ObservableCollection<ChatMessage> Messages { get; set; } = new();

        public ChatPage()
        {
            InitializeComponent();
            BindingContext = this;

            Messages.Add(new ChatMessage { Text = "Hi, how can we help you?", IsSentByUser = false });
            Messages.Add(new ChatMessage { Text = "My tire blew up!", IsSentByUser = true });
            Messages.Add(new ChatMessage { Text = "Sending a mechanic to your location.", IsSentByUser = false });
        }

        private void OnSendClicked(object sender, EventArgs e)
        {
            string msg = MessageEntry.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(msg))
            {
                Messages.Add(new ChatMessage
                {
                    Text = msg,
                    IsSentByUser = true
                });
                MessageEntry.Text = string.Empty;
            }
        }

        private async void OnImageUploadClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Choose an image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    var filePath = result.FullPath;

                    Messages.Add(new ChatMessage
                    {
                        ImagePath = filePath,
                        IsSentByUser = true
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Image selection failed: " + ex.Message, "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnAttachmentClicked(object sender, EventArgs e)
        {

        }
    }

    public class ChatMessage
    {
        public string Text { get; set; } = "";
        public bool IsSentByUser { get; set; }
        public string ImagePath { get; set; } = null;
    }

    // Converter: null or empty string => false, otherwise true
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && !string.IsNullOrEmpty(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
