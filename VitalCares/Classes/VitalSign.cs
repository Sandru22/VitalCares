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
        public double Puls { get; set; }
        public double Temperatura { get; set; }
        public double Umiditate { get; set; }
        public string ECG_Data { get; set; } // String pentru datele brute ECG
        public bool IsAlarm { get; set; }
    }
}
