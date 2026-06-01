using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace VitalCares
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(Views.LoginPage), typeof(Views.LoginPage));
            Routing.RegisterRoute(nameof(Views.RegisterPage), typeof(Views.RegisterPage));

            // REPARAT: Verificăm starea direct, sincron, la inițializarea componentei
            CheckLoginState();
        }

        private void CheckLoginState()
        {
            bool isLoggedIn = Preferences.Default.Get("IsLoggedIn", false);

            if (isLoggedIn)
            {
                // Dacă este logat, definim ruta către meniul principal
                CurrentItem = Items[2]; // Selectează direct al treilea element din XAML (MainFlyout)
            }
            // Dacă NU este logat, lăsăm comportamentul implicit (va încărca LoginPage de pe poziția 1)
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = false;

            Preferences.Default.Remove("IsLoggedIn");
            Preferences.Default.Remove("CurrentPatientID");
            Preferences.Default.Remove("CurrentUserID");
            Preferences.Default.Remove("UserRole");

            // Navigare curată înapoi la Login
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}