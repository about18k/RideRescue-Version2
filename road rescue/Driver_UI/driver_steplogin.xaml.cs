using Microsoft.Maui.Controls.Shapes;

namespace road_rescue;

public partial class driver_steplogin : ContentPage
{
    string savedVehicleType, savedVehicleModel;
    string savedFullName, savedContact;
    string savedEmail, savedPassword;

    public driver_steplogin()
    {
        InitializeComponent();
        LoadStep1(); // Start with Step 1
    }

    void LoadStep1()
    {
        StepContainer.Children.Clear();

        var headerlabel = new Label
        {
            Text = "Tell Us about your vehicle",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 0, 0, 50)
        };

        var vehicleTypeLabel = new Label
        {
            Text = "What kind of vehicle do you own?",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        };
        var vehicleTypePicker = new Picker { Title = "Select Vehicle Type" };
        vehicleTypePicker.Items.Add("Car");
        vehicleTypePicker.Items.Add("Motorcycle");
        vehicleTypePicker.Items.Add("Truck");
        var vehicleTypeBorder = new Border
        {
            Stroke = Colors.Gray,
            StrokeThickness = 1,
            Padding = 5,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) }
        };
        vehicleTypeBorder.Content = vehicleTypePicker;

        var vehicleModelLabel = new Label
        {
            Text = "Vehicle Model and Year",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 10, 0, 5)
        };
        var vehicleModelEntry = new Entry { Placeholder = "e.g., Toyota Corolla 2022", BackgroundColor = Colors.Transparent };
        var vehicleModelBorder = new Border
        {
            Stroke = Colors.Gray,
            StrokeThickness = 1,
            Padding = 5,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) }
        };
        vehicleModelBorder.Content = vehicleModelEntry;

        var nextBtn = new Button
        {
            Text = "Next",
            BackgroundColor = Color.FromHex("#3070f6"),
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.End
        };
        nextBtn.Clicked += async (s, e) =>
        {
            if (vehicleTypePicker.SelectedIndex == -1 || string.IsNullOrWhiteSpace(vehicleModelEntry.Text))
            {
                await DisplayAlert("Missing Info", "Please select a vehicle type and enter your vehicle model.", "OK");
                return;
            }

            savedVehicleType = vehicleTypePicker.SelectedItem.ToString();
            savedVehicleModel = vehicleModelEntry.Text;

            LoadStep2();
        };

        StepContainer.Children.Add(headerlabel);
        StepContainer.Children.Add(vehicleTypeLabel);
        StepContainer.Children.Add(vehicleTypeBorder);
        StepContainer.Children.Add(vehicleModelLabel);
        StepContainer.Children.Add(vehicleModelBorder);
        StepContainer.Children.Add(nextBtn);
    }

    void LoadStep2()
    {
        StepContainer.Children.Clear();

        var headerlabel = new Label
        {
            Text = "Now, tell Us About Yourself",
            FontSize = 28,                   // changed to 28
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 0, 0, 50)  // changed to (0,0,0,50)
        };

        var fullNameLabel = new Label
        {
            Text = "Enter Your Full Name",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,  // made bold
            Margin = new Thickness(0, 0, 0, 5)
        };
        var fullNameEntry = new Entry { Placeholder = "John Doe", BackgroundColor = Colors.Transparent };
        var fullNameBorder = new Border
        {
            Stroke = Colors.Gray,
            StrokeThickness = 1,
            Padding = 5,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) }
        };
        fullNameBorder.Content = fullNameEntry;

        var contactLabel = new Label
        {
            Text = "Enter Your Contact Number",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,  // made bold
            Margin = new Thickness(0, 10, 0, 5)
        };
        var contactEntry = new Entry { Placeholder = "e.g., +1234567890", Keyboard = Keyboard.Telephone, BackgroundColor = Colors.Transparent };
        var contactBorder = new Border
        {
            Stroke = Colors.Gray,
            StrokeThickness = 1,
            Padding = 5,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) }
        };
        contactBorder.Content = contactEntry;

        var buttonLayout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(),
                new ColumnDefinition()
            },
            Margin = new Thickness(0, 20, 0, 0)
        };

        var backBtn = new Button
        {
            Text = "Back",
            BackgroundColor = Colors.Gray,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Start
        };
        backBtn.Clicked += (s, e) => LoadStep1();

        var nextBtn = new Button
        {
            Text = "Next",
            BackgroundColor = Color.FromHex("#3070f6"),
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.End
        };
        nextBtn.Clicked += async (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(fullNameEntry.Text) || string.IsNullOrWhiteSpace(contactEntry.Text))
            {
                await DisplayAlert("Missing Info", "Please enter your full name and contact number.", "OK");
                return;
            }

            savedFullName = fullNameEntry.Text;
            savedContact = contactEntry.Text;

            LoadStep3();
        };

        buttonLayout.Add(backBtn, 0, 0);
        buttonLayout.Add(nextBtn, 1, 0);

        StepContainer.Children.Add(headerlabel);
        StepContainer.Children.Add(fullNameLabel);
        StepContainer.Children.Add(fullNameBorder);
        StepContainer.Children.Add(contactLabel);
        StepContainer.Children.Add(contactBorder);
        StepContainer.Children.Add(buttonLayout);
    }

    void LoadStep3()
    {
        StepContainer.Children.Clear();

        var headerlabel = new Label
        {
            Text = "Create an account using email and password",
            FontSize = 28,                   // changed to 28
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 0, 0, 50)  // changed to (0,0,0,50)
        };

        var emailLabel = new Label
        {
            Text = "Enter Your Email",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,  // made bold
            Margin = new Thickness(0, 0, 0, 5)
        };
        var emailEntry = new Entry { Placeholder = "example@gmail.com", Keyboard = Keyboard.Email, BackgroundColor = Colors.Transparent };
        var emailBorder = new Border
        {
            Stroke = Colors.Gray,
            StrokeThickness = 1,
            Padding = 5,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) }
        };
        emailBorder.Content = emailEntry;

        var passwordLabel = new Label
        {
            Text = "Create a Password",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,  // made bold
            Margin = new Thickness(0, 10, 0, 5)
        };
        var passwordEntry = new Entry { Placeholder = "********", IsPassword = true, BackgroundColor = Colors.Transparent };
        var passwordBorder = new Border
        {
            Stroke = Colors.Gray,
            StrokeThickness = 1,
            Padding = 5,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) }
        };
        passwordBorder.Content = passwordEntry;

        var buttonLayout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(),
                new ColumnDefinition()
            },
            Margin = new Thickness(0, 20, 0, 0)
        };

        var backBtn = new Button
        {
            Text = "Back",
            BackgroundColor = Colors.Gray,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Start
        };
        backBtn.Clicked += (s, e) => LoadStep2();

        var signUpBtn = new Button
        {
            Text = "Sign Up",
            BackgroundColor = Color.FromHex("#3070f6"),
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.End
        };
        signUpBtn.Clicked += async (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(emailEntry.Text) || string.IsNullOrWhiteSpace(passwordEntry.Text))
            {
                await DisplayAlert("Missing Info", "Please enter your email and password.", "OK");
                return;
            }

            savedEmail = emailEntry.Text;
            savedPassword = passwordEntry.Text;

            await DisplayAlert("Success", "Account created successfully!", "OK");
            await Navigation.PushAsync(new logInPage());
        };

        buttonLayout.Add(backBtn, 0, 0);
        buttonLayout.Add(signUpBtn, 1, 0);

        StepContainer.Children.Add(headerlabel);
        StepContainer.Children.Add(emailLabel);
        StepContainer.Children.Add(emailBorder);
        StepContainer.Children.Add(passwordLabel);
        StepContainer.Children.Add(passwordBorder);
        StepContainer.Children.Add(buttonLayout);
    }
}
