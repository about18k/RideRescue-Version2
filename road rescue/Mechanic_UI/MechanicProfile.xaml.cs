namespace road_rescue
{

    public partial class MechanicProfile : ContentPage
    {
        public MechanicProfile()
        {
            InitializeComponent();
        }
        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();

        }
    }
}