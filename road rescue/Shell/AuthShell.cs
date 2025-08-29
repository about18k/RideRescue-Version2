using Microsoft.Maui.Controls;

namespace road_rescue
{
    // Logged-out shell
    public class AuthShell : Shell
    {
        public AuthShell()
        {
            // Root login page
            Items.Add(new ShellContent
            {
                Content = new logInPage(),
                Route = "login",
                //Title = "Sign in"
            });

            // Additional routes used while logged out
            Routing.RegisterRoute(nameof(signUpPage), typeof(signUpPage));
        }
    }

    // Driver shell (has a real Flyout)
    public class DriverShell : Shell
    {
        public DriverShell()
        {
            FlyoutBehavior = FlyoutBehavior.Flyout;

            var home = new FlyoutItem
            {
                Title = "Home",
                Route = "home",
                Items =
                {
                    new ShellContent
                    {
                        Title = "Home",
                        Route = "landing",
                        Content = new landingpage()
                    }
                }
            };

            Items.Add(home);

            // Register any pages you'll navigate to via GoToAsync (optional)
            Routing.RegisterRoute(nameof(EmergencyRequestPage), typeof(EmergencyRequestPage));
            Routing.RegisterRoute(nameof(Vulcanizing), typeof(Vulcanizing));
            //Routing.RegisterRoute(nameof(repairShop), typeof(repairShop));
            //Routing.RegisterRoute(nameof(gasStation), typeof(gasStation));
        }
    }

    // Mechanic shell
    public class MechanicShell : Shell
    {
        public MechanicShell()
        {
            FlyoutBehavior = FlyoutBehavior.Flyout;

            var requests = new FlyoutItem
            {
                Title = "Requests",
                Route = "requests",
                Items =
                {
                    new ShellContent
                    {
                        Title = "Requests",
                        Route = "requests-root",
                        Content = new RequestPage()
                    }
                }
            };

            Items.Add(requests);
        }
    }
}
