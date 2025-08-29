using System.Text.RegularExpressions;

namespace road_rescue;

public partial class MechanicSetupPage : ContentPage
{
    private List<string> customServices = new();

    public MechanicSetupPage()
    {
        InitializeComponent();
        ContactEntry.Text = "+63 ";
    }

    private void ValidateInputs(object? sender, TextChangedEventArgs e)
    {
        string contactText = ContactEntry.Text ?? "";
        bool isPhoneValid = Regex.IsMatch(contactText, @"^\+63\s\d{10}$");

        bool isNameValid = !string.IsNullOrWhiteSpace(NameEntry.Text);
        bool isUsernameValid = Regex.IsMatch(UsernameEntry.Text ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        bool isPasswordValid = (PasswordEntry.Text?.Length ?? 0) >= 6;
        bool isConfirmPasswordMatch = PasswordEntry.Text == ConfirmPasswordEntry.Text;

        bool isShopNameValid = !string.IsNullOrWhiteSpace(ShopNameEntry.Text);
        bool isLocationValid = !string.IsNullOrWhiteSpace(ShopLocationEntry.Text);

        ContactEntry.BackgroundColor = isPhoneValid ? Colors.Transparent : Colors.DarkRed;
        NameEntry.BackgroundColor = isNameValid ? Colors.Transparent : Colors.DarkRed;
        UsernameEntry.BackgroundColor = isUsernameValid ? Colors.Transparent : Colors.DarkRed;
        PasswordEntry.BackgroundColor = isPasswordValid ? Colors.Transparent : Colors.DarkRed;
        ConfirmPasswordEntry.BackgroundColor = isConfirmPasswordMatch ? Colors.Transparent : Colors.DarkRed;
        ShopNameEntry.BackgroundColor = isShopNameValid ? Colors.Transparent : Colors.DarkRed;
        ShopLocationEntry.BackgroundColor = isLocationValid ? Colors.Transparent : Colors.DarkRed;
    }

    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        string contactText = ContactEntry.Text ?? "";
        bool isPhoneValid = Regex.IsMatch(contactText, @"^\+63\s\d{10}$");
        bool isUsernameValid = Regex.IsMatch(UsernameEntry.Text ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        bool isPasswordValid = (PasswordEntry.Text?.Length ?? 0) >= 6;
        bool isConfirmPasswordMatch = PasswordEntry.Text == ConfirmPasswordEntry.Text;

        if (!isPhoneValid ||
            !isUsernameValid ||
            !isPasswordValid ||
            !isConfirmPasswordMatch ||
            string.IsNullOrWhiteSpace(NameEntry.Text) ||
            string.IsNullOrWhiteSpace(ShopNameEntry.Text) ||
            string.IsNullOrWhiteSpace(ShopLocationEntry.Text))
        {
            await DisplayAlert("Error", "Please fill all fields correctly.", "OK");
            return; // Stop execution if form is invalid
        }

        string startDay = StartDayPicker.SelectedItem?.ToString() ?? "N/A";
        string endDay = EndDayPicker.SelectedItem?.ToString() ?? "N/A";
        TimeSpan startTime = StartTimePicker.Time;
        TimeSpan endTime = EndTimePicker.Time;

        List<string> services = new();
        if (TireCheckbox.IsChecked) services.Add("Tire Replacement");
        if (EngineCheckbox.IsChecked) services.Add("Engine Repair");
        if (BatteryCheckbox.IsChecked) services.Add("Battery Replacement");
        if (TowingCheckbox.IsChecked) services.Add("Towing Service");

        services.AddRange(customServices);
        string servicesList = services.Count > 0 ? string.Join(", ", services) : "None";

        // Display success alert
        await DisplayAlert("Success",
            $"Account Created!\nName: {NameEntry.Text}\nUsername: {UsernameEntry.Text}\nContact: {ContactEntry.Text}\nShop: {ShopNameEntry.Text}\nLocation: {ShopLocationEntry.Text}\nDays: {startDay} to {endDay}\nTime: {startTime} - {endTime}\nServices: {servicesList}",
            "OK");

        // After a short delay, navigate to the next page
        await Task.Delay(1000); // Wait for the user to see the success message

        // Navigate to SecondPage after the success message
        await Navigation.PushAsync(new SecondPage());
    }


    private void OnServiceEntered(object sender, EventArgs e)
    {
        string service = (OtherServiceEntry.Text ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(service) && !customServices.Contains(service))
        {
            customServices.Add(service);
            AddServiceTag(service);
            OtherServiceEntry.Text = string.Empty;
        }
    }

    private void AddServiceTag(string service)
    {
        var tagFrame = new Frame
        {
            Padding = new Thickness(10, 5),
            BackgroundColor = Colors.White,
            CornerRadius = 15,
            Margin = new Thickness(5),
            HasShadow = false
        };

        var tagLayout = new HorizontalStackLayout
        {
            Spacing = 5
        };

        var tagLabel = new Label
        {
            Text = service,
            TextColor = Colors.Black,
            VerticalOptions = LayoutOptions.Center
        };

        var removeButton = new Button
        {
            Text = "❌",
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.Red,
            FontSize = 14,
            Padding = new Thickness(0),
            WidthRequest = 25,
            HeightRequest = 25,
            VerticalOptions = LayoutOptions.Center
        };

        removeButton.Clicked += (s, e) =>
        {
            ServiceTagsLayout.Children.Remove(tagFrame);
            customServices.Remove(service);
        };

        tagLayout.Children.Add(tagLabel);
        tagLayout.Children.Add(removeButton);
        tagFrame.Content = tagLayout;

        ServiceTagsLayout.Children.Add(tagFrame);
    }
}
