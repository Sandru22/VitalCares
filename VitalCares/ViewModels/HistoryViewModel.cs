using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Shapes;

namespace VitalCares.ViewModels
{
    public class HistoryViewModel : BaseViewModel
    {
        private PointCollection _pulsPoints;
        public PointCollection PulsPoints
        {
            get => _pulsPoints;
            set => SetProperty(ref _pulsPoints, value);
        }

        public HistoryViewModel()
        {
            Title = "Istoric Măsurători";
            GenerateManualGraph();
        }

        private void GenerateManualGraph()
        {
            // Coordonatele X,Y (X = timpul, Y = valoarea pulsului inversată)
            // Notă: În coordonate de ecran, 0,0 este colțul stânga-sus.
            // Pentru un puls de 80bpm, dacă graficul are înălțime 200, 
            // punctul va fi la Y = 200 - 80 = 120.

            var points = new PointCollection();
            points.Add(new Point(0, 150));
            points.Add(new Point(50, 120));
            points.Add(new Point(100, 140));
            points.Add(new Point(150, 100));
            points.Add(new Point(200, 130));
            points.Add(new Point(250, 110));
            points.Add(new Point(300, 150));

            PulsPoints = points;
        }
    }
}
