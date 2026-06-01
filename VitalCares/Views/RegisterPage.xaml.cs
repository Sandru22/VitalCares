using System.Text.Json;
using System.Text;
using System.Net.Http.Json;

namespace VitalCares.Views;

public class MedicLookUp
{
    public int id_medic { get; set; }
    public string nume { get; set; }
    public string prenume { get; set; }
    public string specializare { get; set; }
    // Proprietate calculată pentru a fi afișată frumos în Picker
    public string NumeComplet => $"Dr. {nume} {prenume} ({specializare})";
}

public partial class RegisterPage : ContentPage
{
    private readonly HttpClient _httpClient = new HttpClient();
    private const string ApiRegisterUrl = "https://api.newsflowapi.uk/register.php";
    private const string ApiMediciUrl = "https://api.newsflowapi.uk/get_medici.php";

    public RegisterPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await IncarcaMediciiAsync();
    }

    private async Task IncarcaMediciiAsync()
    {
        try
        {
            var medici = await _httpClient.GetFromJsonAsync<List<MedicLookUp>>(ApiMediciUrl);
            if (medici != null)
            {
                pckMedic.ItemsSource = medici;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Eroare la incarcarea medicilor: {ex.Message}");
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Validare sumară (adaugă tu dacă vrei reguli stricte)
        if (string.IsNullOrWhiteSpace(txtEmail.Text) || string.IsNullOrWhiteSpace(txtPassword.Text) || string.IsNullOrWhiteSpace(txtCNP.Text))
        {
            await DisplayAlert("Eroare", "Email-ul, Parola și CNP-ul sunt obligatorii.", "OK");
            return;
        }

        // Extragem medicul selectat din Picker
        var medicSelectat = pckMedic.SelectedItem as MedicLookUp;
        int? idMedic = medicSelectat?.id_medic;

        var registerData = new
        {
            email = txtEmail.Text.Trim(),
            parola = txtPassword.Text,
            id_medic = idMedic, // Trimite null dacă nu a ales niciunul
            nume = txtNume.Text?.Trim() ?? "",
            prenume = txtPrenume.Text?.Trim() ?? "",
            varsta = string.IsNullOrWhiteSpace(txtVarsta.Text) ? 0 : int.Parse(txtVarsta.Text.Trim()),
            cnp = txtCNP.Text.Trim(),
            strada = txtStrada.Text?.Trim() ?? "",
            oras = txtOras.Text?.Trim() ?? "",
            judet = txtJudet.Text?.Trim() ?? "",
            telefon = txtTelefon.Text?.Trim() ?? "",
            profesie = txtProfesie.Text?.Trim() ?? "",
            loc_de_munca = txtLocMunca.Text?.Trim() ?? "",
            istoric_medical = txtIstoric.Text?.Trim() ?? "",
            alergii = txtAlergii.Text?.Trim() ?? ""
        };

        try
        {
            string json = JsonSerializer.Serialize(registerData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiRegisterUrl, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Succes", "Contul de pacient a fost înregistrat cu toate datele!", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await DisplayAlert("Eroare Server", responseBody, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", ex.Message, "OK");
        }
    }

    private async void OnLoginLabelTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}