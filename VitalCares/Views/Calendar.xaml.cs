using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VitalCares.Classes;

namespace VitalCares.Views;


public partial class Calendar : ContentPage
{
    private readonly HttpClient _httpClient = new HttpClient();
    private bool _isBusy = false; // Previne buclele infinite la actualizarea CheckBox-urilor

    public ObservableCollection<ActivityItem> Activities { get; set; } = new ObservableCollection<ActivityItem>();

    public Calendar()
    {
        InitializeComponent();
        lstActivities.ItemsSource = Activities;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await IncarcaActivitateDataAsync(datePicker.Date);
    }

    private async Task IncarcaActivitateDataAsync(DateTime dataSelectata)
    {
        _isBusy = true; // Blocăm temporar evenimentul CheckedChanged în timpul încărcării datelor
        try
        {
            int idPacient = Preferences.Default.Get("CurrentPatientID", 1);
            string dataFormatata = dataSelectata.ToString("yyyy-MM-dd");

            string url = $"https://api.newsflowapi.uk/get_activitati.php?id_pacient={idPacient}&data={dataFormatata}";

            var dateServer = await _httpClient.GetFromJsonAsync<List<ActivityItem>>(url);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Activities.Clear();
                if (dateServer != null)
                {
                    foreach (var act in dateServer)
                    {
                        Activities.Add(act);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Calendar] Eroare incarcare: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
            MainThread.BeginInvokeOnMainThread(() => { refreshView.IsRefreshing = false; });
        }
    }

    private async void OnDateSelected(object sender, DateChangedEventArgs e)
    {
        await IncarcaActivitateDataAsync(e.NewDate);
        lblSelectedDate.Text = e.NewDate.Date == DateTime.Today ? "Activități pentru Azi" : $"Activități pt. {e.NewDate:dd/MM/yyyy}";
    }

    // Această metodă rulează de fiecare dată când utilizatorul bifează sau debifează o casetă
    private async void OnActivityCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isBusy) return; // Dacă lista se încarcă acum, ignorăm evenimentul automat

        var checkBox = (CheckBox)sender;
        var activitate = (ActivityItem)checkBox.BindingContext;

        if (activitate == null) return;

        // Actualizăm valoarea locală
        activitate.este_finalizata = e.Value;

        // Trimitem starea nouă către serverul PHP
        try
        {
            string urlUpdate = "https://api.newsflowapi.uk/update_status_activitate.php";
            var corpCerere = new
            {
                id_activitate = activitate.id_activitate,
                este_finalizata = activitate.este_finalizata
            };

            string json = JsonSerializer.Serialize(corpCerere);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var raspuns = await _httpClient.PostAsync(urlUpdate, content);
            if (!raspuns.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("[Calendar] Serverul a respins actualizarea statusului.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Calendar] Eroare la salvarea statusului: {ex.Message}");
        }
    }

    private async void OnRefreshRequested(object sender, EventArgs e)
    {
        await IncarcaActivitateDataAsync(datePicker.Date);
    }
}



