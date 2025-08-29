using System.Collections.ObjectModel;
using System.ComponentModel;

namespace road_rescue.ViewModels
{
    // Using a record for immutability
    public record OnboardingModel(string Image, string Heading, string? Description);

    public class OnboardingVm : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<OnboardingModel> OnboardingSteps { get; set; } = new();

        public OnboardingVm()
        {
            //Onboarding Data
            OnboardingSteps.Add(new OnboardingModel("girl.jpg", "Welcome to RoadRescue", "Your ultimate roadside assistance app! for flat tires, engine trouble, and more."));
            OnboardingSteps.Add(new OnboardingModel("girl2.jpg", "Automated Alerts", "Automatically notify emergency contacts when you're in trouble."));
            OnboardingSteps.Add(new OnboardingModel("guy1.jpg", "Let's Get Started!", "Tap below to start using RoadRescue and stay safe on the road!"));
        }

        private bool isLastStep;
        public bool IsLastStep
        {
            get => isLastStep;
            set
            {
                if (isLastStep != value)
                {
                    isLastStep = value;
                    OnPropertyChanged(nameof(IsLastStep));
                    OnPropertyChanged(nameof(IsNotLastStep));
                }
            }
        }

        public bool IsNotLastStep => !isLastStep;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
