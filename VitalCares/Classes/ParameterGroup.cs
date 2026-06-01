using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalCares.Classes
{
    public class ParameterGroup : List<HistoryItem>
    {
        public string Name { get; private set; }

        public ParameterGroup(string name, List<HistoryItem> items) : base(items)
        {
            Name = name;
        }
    }
}
