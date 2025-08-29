using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace road_rescue
{
    public partial class PaymentsPage : ContentPage
    {
        public ObservableCollection<PaymentModel> Payments { get; set; }
        public ObservableCollection<TransactionModel> Transactions { get; set; }

        public PaymentsPage()
        {
            InitializeComponent();

            // Sample Payment History
            Payments = new ObservableCollection<PaymentModel>
            {
                new PaymentModel
                {
                    Provider = "Tire Pro Service",
                    Amount = "₱500.00",
                    DateTime = "May 12, 2025 - 3:45 PM",
                    Image = "profile1.png"
                },
                new PaymentModel
                {
                    Provider = "Road Rescue Support",
                    Amount = "₱700.00",
                    DateTime = "May 10, 2025 - 1:30 PM",
                    Image = "profile2.png"
                }
            };

            // Sample Transaction List
            Transactions = new ObservableCollection<TransactionModel>
            {
                new TransactionModel
                {
                    Description = "Payment to Juan Dela Cruz",
                    Amount = "₱1,200.00",
                    DateTime = "May 15, 2025 - 4:10 PM",
                    Image = "profile3.png"
                },
                new TransactionModel
                {
                    Description = "Order #1234",
                    Amount = "₱950.00",
                    DateTime = "May 14, 2025 - 2:20 PM",
                    Image = "profile4.png"
                }
            };

            PaymentList.ItemsSource = Payments;
            TransactionList.ItemsSource = Transactions;
        }

        private async void OnTransactionTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MakePaymentPage());
        }
    }

    public class PaymentModel
    {
        public string Provider { get; set; }
        public string Amount { get; set; }
        public string DateTime { get; set; }
        public string Image { get; set; }
    }

    public class TransactionModel
    {
        public string Description { get; set; }
        public string Amount { get; set; }
        public string DateTime { get; set; }
        public string Image { get; set; }
    }
}
