using System.Collections.ObjectModel;
using System.Net.Http.Json;
using VitalCares.Classes;

namespace VitalCares.Views;

public partial class RecommendationsPage : ContentPage
{
    private readonly HttpClient _httpClient = new HttpClient();

    // URL dinamic care citește id-ul pacientului curent autentificat
    private string ApiRecommendationsUrl
    {
        get
        {
            int idPacient = Preferences.Default.Get("CurrentPatientID", 1);
            return $"https://api.newsflowapi.uk/get_recomandari.php?id_pacient={idPacient}";
        }
    }

    public ObservableCollection<DoctorRecommendation> Recommendations { get; set; } = new ObservableCollection<DoctorRecommendation>();

    public RecommendationsPage()
    {
        InitializeComponent();
        recomList.ItemsSource = Recommendations;
    }

    // Se execută automat când utilizatorul apasă pe tab-ul de Recomandări
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await IncarcaRecomandariServerAsync();
    }

    private async Task IncarcaRecomandariServerAsync()
    {
        try
        {
            // Descărcăm datele proaspete de pe serverul Ubuntu
            var dateServer = await _httpClient.GetFromJsonAsync<List<DoctorRecommendation>>(ApiRecommendationsUrl);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Recommendations.Clear();
                if (dateServer != null)
                {
                    foreach (var item in dateServer)
                    {
                        Recommendations.Add(item);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Recomandari] Eroare la descarcare: {ex.Message}");
        }
    }
}
