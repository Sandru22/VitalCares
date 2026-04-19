using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalCares.Classes
{
    class Patient
    {

        public string Nume { get; set; }

        public string Prenume { get; set; }
        public int Varsta { get; set; }
        public string CNP { get; set; }
        public string Strada { get; set; }
        public string Numar { get; set; }
        public string Oras { get; set; }
        public string Judet { get; set; }

        public string NumarTelefon { get; set; }
        public string Email { get; set; }
        public string Profesie { get; set; }
        public string LocMunca { get; set; }

        public string IstoricMedical { get; set; } // Format text 
        public string Alergii { get; set; }
        public string ConsultatiiCardiologice { get; set; } // Format text 

        // --- PROPRIETĂȚI EXTRA PENTRU LOGICA APP ---

        public double PragPulsMaxim { get; set; }
        public double PragTempMaxima { get; set; }

        // ID-ul unic pentru Cloud/Baza de date
        public string Id => CNP;
    }
}
