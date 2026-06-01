using System.Net.Http.Json;
using SkiaSharp;
using ChartLib = Microcharts;
using MicroEntry = Microcharts.ChartEntry;
using Microsoft.Maui.ApplicationModel;
using VitalCares.Classes;

namespace VitalCares.Views;

// Adăugăm modelul local pentru a stoca dinamic răspunsul de praguri
public class DashboardThresholds
{
    public double max_puls { get; set; } = 93.0; // Valori de fallback în caz că API pică
    public double min_puls { get; set; } = 68.0;
    public double max_temp { get; set; } = 38.5;
}

public partial class DashboardPage : ContentPage
{
    private readonly HttpClient _httpClient = new HttpClient();
    private string ApiHistoryUrl => $"https://api.newsflowapi.uk/get_history.php?id_pacient={CurrentPatientId}";
    private string ApiPraguriUrl => $"https://api.newsflowapi.uk/get_praguri.php?id_pacient={CurrentPatientId}";

    private List<HistoryItem> _allRecords = new List<HistoryItem>();
    private DashboardThresholds _praguriPacient = new DashboardThresholds();

    private int CurrentPatientId => Preferences.Default.Get("CurrentPatientID", 1);

    public DashboardPage(ViewModels.MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await DescarcaDateServerAsync();
    }

    private async Task DescarcaDateServerAsync()
    {
        try
        {
            // 1. Descărcăm pragurile dinamice ale pacientului curent din API
            var praguriAlocate = await _httpClient.GetFromJsonAsync<DashboardThresholds>(ApiPraguriUrl);
            if (praguriAlocate != null)
            {
                _praguriPacient = praguriAlocate;
            }

            // 2. Descărcăm istoricul general
            var date = await _httpClient.GetFromJsonAsync<List<HistoryItem>>(ApiHistoryUrl);
            if (date != null)
            {
                _allRecords = date;
                GenereazaGraficePentruData(dpFiltruData.Date);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Eroare la descarcare date/praguri: {ex.Message}");
        }
    }

    private void GenereazaGraficePentruData(DateTime dataSelectata)
    {
        if (_allRecords == null || !_allRecords.Any()) return;

        var dateZi = _allRecords.Where(x =>
            DateTime.TryParse(x.moment_inregistrare, out DateTime dt) && dt.Date == dataSelectata.Date)
            .OrderBy(x => DateTime.Parse(x.moment_inregistrare))
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (!dateZi.Any())
                {
                    lblNoDataWarning.IsVisible = true;
                    panelPuls.IsVisible = panelTemp.IsVisible = panelHum.IsVisible = panelEcg.IsVisible = false;
                    return;
                }

                lblNoDataWarning.IsVisible = false;
                panelPuls.IsVisible = panelTemp.IsVisible = panelHum.IsVisible = panelEcg.IsVisible = true;

                // Calculul lățimii dinamice adaptat la noile categorii
                int numarPuncteGrafic = dateZi.Count(x => x.tip_parametru == "Puls");
                if (numarPuncteGrafic == 0)
                {
                    numarPuncteGrafic = dateZi.GroupBy(x => x.tip_parametru)
                                             .Select(g => g.Count())
                                             .DefaultIfEmpty(0)
                                             .Max();
                }

                double latimeCalculata = Math.Max(400, numarPuncteGrafic * 60);
                double latimeDinamica = Math.Min(3000, latimeCalculata);

                chartPuls.WidthRequest = latimeDinamica;
                chartTemp.WidthRequest = latimeDinamica;
                chartHum.WidthRequest = latimeDinamica;
                chartEcg.WidthRequest = Math.Max(600, dateZi.Count(x => x.tip_parametru == "ecg" || x.tip_parametru == "ECG") * 15); // Semnalul ECG are nevoie de o densitate mai strânsă

                var entriesPuls = new List<MicroEntry>();
                var entriesTemp = new List<MicroEntry>();
                var entriesHum = new List<MicroEntry>();
                var entriesEcg = new List<MicroEntry>();

                foreach (var item in dateZi)
                {
                    DateTime.TryParse(item.moment_inregistrare, out DateTime timpRecord);
                    string etichetaTimp = timpRecord.ToString("HH:mm");

                    // EVALUARE DINAMICĂ PULS (Folosește API-ul în loc de numere hardcodate)
                    if (item.tip_parametru == "Puls")
                    {
                        var culoarePunct = (item.valoare > _praguriPacient.max_puls || item.valoare < _praguriPacient.min_puls) ? SKColors.Red : SKColors.DarkSlateBlue;
                        entriesPuls.Add(new MicroEntry((float)item.valoare) { Label = etichetaTimp, ValueLabel = item.valoare.ToString(), Color = culoarePunct });
                    }
                    // EVALUARE DINAMICĂ TEMPERATURĂ (Folosește API-ul în loc de numere hardcodate)
                    else if (item.tip_parametru == "Temperatura")
                    {
                        var culoarePunct = (item.valoare > _praguriPacient.max_temp || item.valoare < 35.5) ? SKColors.Red : SKColors.DeepSkyBlue;
                        entriesTemp.Add(new MicroEntry((float)item.valoare) { Label = etichetaTimp, ValueLabel = $"{item.valoare}°C", Color = culoarePunct });
                    }
                    // PROCESARE UMIDITATE
                    else if (item.tip_parametru == "Umiditate")
                    {
                        entriesHum.Add(new MicroEntry((float)item.valoare) { Label = etichetaTimp, ValueLabel = $"{item.valoare}%", Color = SKColors.CornflowerBlue });
                    }
                    // PROCESARE ECG
                    else if (item.tip_parametru == "ecg" || item.tip_parametru == "ECG")
                    {
                        entriesEcg.Add(new MicroEntry((float)item.valoare) { Label = "", ValueLabel = null, Color = SKColors.SeaGreen });
                    }
                }

                // Sincronizare text valori medii / totale
                if (entriesPuls.Any()) lblPulsMediu.Text = $"Medie: {Math.Round((decimal)entriesPuls.Average(x => x.Value))} BPM";
                if (entriesTemp.Any()) lblTempMediu.Text = $"Medie: {Math.Round((decimal)entriesTemp.Average(x => x.Value), 1)}°C";
                if (entriesHum.Any()) lblHumMedie.Text = $"Medie: {Math.Round((decimal)entriesHum.Average(x => x.Value), 1)}%";
                lblEcgTotal.Text = $"Eșantioane: {entriesEcg.Count}";

                // Atribuire grafice
                if (entriesPuls.Any())
                    chartPuls.Chart = new ChartLib.LineChart { Entries = entriesPuls, LabelTextSize = 24, LineMode = ChartLib.LineMode.Spline, PointSize = 10 };

                if (entriesTemp.Any())
                    chartTemp.Chart = new ChartLib.LineChart { Entries = entriesTemp, LabelTextSize = 24, LineMode = ChartLib.LineMode.Straight, PointSize = 10 };

                if (entriesHum.Any())
                    chartHum.Chart = new ChartLib.LineChart { Entries = entriesHum, LabelTextSize = 24, LineMode = ChartLib.LineMode.Spline, PointSize = 8 };

                if (entriesEcg.Any())
                    chartEcg.Chart = new ChartLib.LineChart { Entries = entriesEcg, LabelTextSize = 20, LineMode = ChartLib.LineMode.Straight, PointSize = 0 }; // Fără puncte/buline pe linia ECG pentru aspect fluid
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard UI Error] {ex.Message}");
            }
        });
    }

    private void OnDateSelected(object sender, DateChangedEventArgs e)
    {
        GenereazaGraficePentruData(e.NewDate);
    }
}
