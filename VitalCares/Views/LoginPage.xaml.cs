using System.Text;
using System.Text.Json;

namespace VitalCares.Views;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private const string ApiUrl = "https://api.newsflowapi.uk/login.php";

    public LoginPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // 1. Validare locală elementară
        if (string.IsNullOrWhiteSpace(txtEmail.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            await DisplayAlert("Eroare", "Te rog introdu email-ul și parola.", "OK");
            return;
        }

        // Pregătim datele pentru trimitere (exact numele cheilor din PHP)
        var loginData = new
        {
            email = txtEmail.Text.Trim(),
            parola = txtPassword.Text
        };

        try
        {
            // Dezactivăm butonul temporar ca să nu dea click de mai multe ori
            var button = (Button)sender;
            button.IsEnabled = false;

            string json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 2. Apelul HTTP către serverul Ubuntu
            var response = await _httpClient.PostAsync(ApiUrl, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            button.IsEnabled = true; // Reactivăm butonul

            if (response.IsSuccessStatusCode)
            {
                // Parsăm JSON-ul primit de la PHP
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.GetProperty("success").GetBoolean())
                {
                    // Extragem id_pacient din răspuns
                    int idPacient = 1; // Fallback implicit

                    if (root.TryGetProperty("id_pacient", out var idElement) && idElement.ValueKind != JsonValueKind.Null)
                    {
                        idPacient = idElement.GetInt32();
                    }

                    int idUtilizator = root.GetProperty("id_utilizator").GetInt32();
                    string rol = root.GetProperty("rol").GetString();

                    // 3. SALVĂM DATELE ȘI STATUSUL LOGĂRII
                    Preferences.Default.Set("CurrentPatientID", idPacient);
                    Preferences.Default.Set("CurrentUserID", idUtilizator);
                    Preferences.Default.Set("UserRole", rol);

                    // Salvăm dacă utilizatorul a bifat căsuța "Rămâi conectat"
                    Preferences.Default.Set("IsLoggedIn", chkRememberMe.IsChecked);

                    // 4. Navigăm către structura Shell TabBar
                    await Shell.Current.GoToAsync("//ConnectionTest");
                }
            }
            else
            {
                // Tratare erori controlate (400, 401, 500)
                string errMsg = "Eroare la autentificare.";
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("message", out var msgElement))
                        errMsg = msgElement.GetString();
                }
                catch { }

                await DisplayAlert("Eșec Conectare", errMsg, "OK");
            }
        }
        catch (Exception ex)
        {
            // Tratare erori de rețea sau tunel oprit
            await DisplayAlert("Eroare Critică", $"Nu s-a putut face conexiunea cu serverul: {ex.Message}", "OK");
        }
    }

    private async void OnGoToRegisterClicked(object sender, EventArgs e)
    {
        // Navigăm către pagina de Register (XAML-ul modificat cu cele 14 câmpuri)
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}