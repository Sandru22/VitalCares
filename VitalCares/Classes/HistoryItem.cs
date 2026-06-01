using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalCares.Classes
{
    public class HistoryItem
    {
        public string tip_parametru { get; set; }
        public double valoare { get; set; }
        public string unitate_masurata { get; set; }
        public string moment_inregistrare { get; set; }

        // --- LOGICĂ PENTRU DETECTAREA VALORILOR ANORMALE ---
        public bool IsAlert => StatusText != "Normal";

        public string StatusText
        {
            get
            {
                if (tip_parametru == "Puls")
                {
                    if (valoare > 93.0) return "Puls prea mare (Tahiocardie)";
                    if (valoare < 68.0) return "Puls prea mic (Bradicardie)";
                }
                else if (tip_parametru == "SpO2")
                {
                    if (valoare < 95.0) return "Saturație oxigen critică!";
                }
                else if (tip_parametru == "Temperatura")
                {
                    if (valoare > 38.5) return "Febră ridicată";
                    if (valoare < 35.5) return "Hipotermie";
                }
                return "Normal";
            }
        }

        // Culoarea bulinei din stânga
        public Brush StatusColor => StatusText == "Normal" ? Brush.Green : Brush.Red;

        // Culoarea de fundal a cardului (Roșiatic palid dacă e alertă, alb dacă e normal)
        public Color CardBgColor => StatusText == "Normal" ? Colors.White : Color.FromRgba(255, 230, 230, 255);

        // Marginea cardului
        public Color CardBorderColor => StatusText == "Normal" ? Color.FromArgb("#E0E0E0") : Colors.Red;
    }
}
