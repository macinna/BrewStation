using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrewStation
{
    public abstract class TemperatureReader
    {
        public abstract double GetCurrentTemperature(TemperatureProbes probe);

        public abstract bool Initialize();
    }
}
