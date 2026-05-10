using System.Collections.ObjectModel;

namespace VitalCares.Views;

public partial class Calendar : ContentPage
{
    private List<ActivityItem> _allActivities;

    // Lista afișată pe ecran
    public ObservableCollection<ActivityItem> FilteredActivities { get; set; }

    public Calendar()
    {
        InitializeComponent();

        LoadData();

        FilteredActivities = new ObservableCollection<ActivityItem>();
        lstActivities.ItemsSource = FilteredActivities;

        // Filtrăm pentru data de azi la pornire
        FilterByDate(DateTime.Today);
    }

    private void LoadData()
    {
        // Aici poți adăuga activitățile pacientului
        _allActivities = new List<ActivityItem>
        {
            new ActivityItem { Name = "Luare Medicamente", Description = "Pastila de inimă (1 tb)", Time = "08:00", Date = DateTime.Today },
            new ActivityItem { Name = "Măsurare Puls", Description = "Folosește senzorul BLE", Time = "09:30", Date = DateTime.Today },
            new ActivityItem { Name = "Plimbare ușoară", Description = "Minim 15 minute", Time = "18:00", Date = DateTime.Today },
            new ActivityItem { Name = "Control Tensiune", Description = "Înainte de culcare", Time = "21:00", Date = DateTime.Today.AddDays(1) }
        };
    }

    private void OnDateSelected(object sender, DateChangedEventArgs e)
    {
        FilterByDate(e.NewDate);
        lblSelectedDate.Text = e.NewDate.Date == DateTime.Today ? "Activități pentru Azi" : $"Activități pt. {e.NewDate:dd/MM/yyyy}";
    }

    private void FilterByDate(DateTime targetDate)
    {
        FilteredActivities.Clear();
        var matches = _allActivities.Where(a => a.Date.Date == targetDate.Date).OrderBy(a => a.Time);

        foreach (var activity in matches)
        {
            FilteredActivities.Add(activity);
        }
    }
}

// Modelul simplu de date
public class ActivityItem
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Time { get; set; }
    public DateTime Date { get; set; }
}


