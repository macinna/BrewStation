using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrewStation
{
    class TempControllerEventArgs
    {

        private int temp;
        private Relays relay;
        private TemperatureProbes tempProbe;

        public TempControllerEventArgs(int DesiredTemperature, Relays ControllingRelay, TemperatureProbes TempProbeToMonitor)
        {
            temp = DesiredTemperature;
            relay = ControllingRelay;
            tempProbe = TempProbeToMonitor;
        }

        public int DesiredTemperature
        {
            get { return temp; }
        }

        public Relays BurnerRelay
        {
            get { return relay; }
        }

        public TemperatureProbes TempProbe
        {
            get { return tempProbe; }
        }
    }
}
