using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalCares.Classes
{
    public class VitalSign
    {
        public DateTime Timestamp { get; set; }

        // Parametrii fiziologici ceruți explicit (Sursa [cite: 9, 28])
        public double Puls { get; set; }
        public double Temperatura { get; set; }
        public double Umiditate { get; set; }
        // Datele ECG (reprezentate ca string sau listă de valori)
        public string ECGData { get; set; }

        // Date de la accelerometrul telefonului (Sursa )
        // Citite o dată pe secundă pentru corelare
        public double AccelerometruX { get; set; }
        public double AccelerometruY { get; set; }
        public double AccelerometruZ { get; set; }

        // Flag pentru a marca dacă acest pachet este o alarmă asincronă
        public bool EsteAlarma { get; set; }
        public string MesajAlarma { get; set; }
    }
}
