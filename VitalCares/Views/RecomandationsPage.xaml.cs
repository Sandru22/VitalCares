using System.Collections.ObjectModel;

namespace VitalCares.Views;

public partial class RecommendationsPage : ContentPage
{
    public ObservableCollection<DoctorRecommendation> Recommendations { get; set; }

    public RecommendationsPage()
    {
        InitializeComponent();

        // Date Hardcoded
        Recommendations = new ObservableCollection<DoctorRecommendation>
        {
            new DoctorRecommendation
            {
                Title = "Hidratare Intensă",
                Description = "Consumați cel puțin 2.5 litri de apă pe zi pentru a ajuta la reglarea tensiunii.",
                Category = "Stil de viață",
                Date = "15 Mai 2024",
                Color = "#2196F3" // Albastru
            },
            new DoctorRecommendation
            {
                Title = "Reducere Sare",
                Description = "Evitați alimentele procesate și sarea în exces. Maxim 5g pe zi.",
                Category = "Dietă",
                Date = "14 Mai 2024",
                Color = "#FF9800" // Portocaliu
            },
            new DoctorRecommendation
            {
                Title = "Exerciții Cardio Ușoare",
                Description = "Mers pe jos în ritm alert timp de 20 de minute, de 3 ori pe săptămână.",
                Category = "Activitate",
                Date = "12 Mai 2024",
                Color = "#4CAF50" // Verde
            },
            new DoctorRecommendation
            {
                Title = "Monitorizare Puls",
                Description = "Dacă pulsul depășește 100 BPM în stare de repaus, contactați medicul.",
                Category = "Atenție",
                Date = "10 Mai 2024",
                Color = "#F44336" // Roșu
            }
        };

        recomList.ItemsSource = Recommendations;
    }
}

public class DoctorRecommendation
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Date { get; set; }
    public string Category { get; set; } // Ex: Medicamente, Dietă, Activitate
    public string Color { get; set; }    // Pentru a colora marginea în funcție de tip
}