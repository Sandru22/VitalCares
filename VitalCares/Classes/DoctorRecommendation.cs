using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalCares.Classes
{
    public class DoctorRecommendation
    {
        // Mapăm exact câmpurile venite din baza de date prin PHP
        public int id_recomandare { get; set; }
        public string tip_recomandare { get; set; } // Ex: Dietă, Stil de viață, Medicamente, Atenție
        public string indicatii { get; set; }

        // --- PROPRIETĂȚI CALCULATE PENTRU BINDING-UL DIN XAML ---

        public string Title => tip_recomandare;
        public string Description => indicatii;
        public string Category => tip_recomandare;

        // Generăm automat culorile din interfața ta în funcție de ce a scris medicul în bază
        public string Color
        {
            get
            {
                if (string.IsNullOrEmpty(tip_recomandare)) return "#999999";

                return tip_recomandare.ToLower() switch
                {
                    "dietă" or "dieta" => "#FF9800",          // Portocaliu
                    "stil de viață" or "stil de viata" => "#2196F3", // Albastru
                    "activitate" or "sport" => "#4CAF50",     // Verde
                    "atenție" or "atentie" or "critic" => "#F44336", // Roșu
                    _ => "#512BD4"                             // Mov implicit pentru restul (ex: Medicamente)
                };
            }
        }
    }
}
