using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalCares.Classes
{
    public class ActivityItem
    {
        public int id_activitate { get; set; }
        public string nume_activitate { get; set; }
        public string descriere { get; set; }
        public string ora_programata { get; set; }
        public bool este_finalizata { get; set; }
    }
}
