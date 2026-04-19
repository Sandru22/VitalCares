using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalCares.ViewModels
{
    public class MonitorViewModel : BaseViewModel
    {
        private double _currentPuls;

        public double CurrentPuls
        {
            get => _currentPuls;
            // Folosim SetProperty din BaseViewModel
            set => SetProperty(ref _currentPuls, value);
        }

        public MonitorViewModel()
        {
            Title = "Monitorizare Live";
            CurrentPuls = 0;
        }
    }
}
