using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using VitalCares.Classes;

namespace VitalCares.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // Buffer pentru stocarea măsurătorilor de la 10s până la calculul mediei de 30s
        private List<VitalSign> _vitalsBuffer = new List<VitalSign>();

        // Proprietăți pentru afișarea LIVE pe ecran
        private double _currentPuls;
        public double CurrentPuls
        {
            get => _currentPuls;
            set => SetProperty(ref _currentPuls, value);
        }

        // --- LOGICA DE PROCESARE DATE ---

        public void OnDataReceivedFromBluetooth(VitalSign newData)
        {
            // 1. Actualizează imediat interfața grafică (Real-time)
            CurrentPuls = newData.Puls; 
            // 2. Verifică condiția de alarmă (Trimitere ASINCRONĂ)
            // Dacă valoarea este în afara limitelor, trimitem imediat la Cloud 
            if (CheckAlarmConditions(newData))
            {
                SendAsyncAlarmToCloud(newData); 
            }

            // 3. Adaugă în buffer pentru calculul mediei 
            _vitalsBuffer.Add(newData);

            // 4. Când avem 3 măsurători (adică au trecut 30 de secunde) 
            if (_vitalsBuffer.Count >= 3)
            {
                var averageData = CalculateAverage(_vitalsBuffer);
                SendToCloud(averageData); 
                _vitalsBuffer.Clear();
            }
        }

        private bool CheckAlarmConditions(VitalSign data)
        {
            // Aici compari cu pragurile setate de medic în fișa pacientului [cite: 36, 46]
            return data.Puls > 100 || data.Puls < 50;
        }

        private VitalSign CalculateAverage(List<VitalSign> dataList)
        {
            // Calculează media măsurătorilor la 30 de secunde 
            return new VitalSign
            {
                Timestamp = DateTime.Now,
                Puls = dataList.Average(v => v.Puls),
                Temperatura = dataList.Average(v => v.Temperatura)
            };
        }

        private async void SendToCloud(VitalSign data)
        {
            // Aici vei apela CloudService-ul mai târziu
            System.Diagnostics.Debug.WriteLine("Trimitere date periodice la Cloud...");
        }

        private async void SendAsyncAlarmToCloud(VitalSign data)
        {
            // Trimitere asincronă imediată conform specificațiilor
            System.Diagnostics.Debug.WriteLine("ALARMĂ! Trimitere imediată la Cloud!"); 
        }
    }
}
