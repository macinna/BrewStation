using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibrary;
using System.Configuration;

using System.Diagnostics;

namespace BrewStation
{
    class TemperatrureUtil
    {

        private static IEnumerable<HidLibrary.HidDevice> devices = null;

        public static int GetCurrentTemperature(TemperatureProbes probe)
        {
            if(devices == null)
            {
                devices = HidDevices.Enumerate(3141, 29697);
            }

            //if there aren't 6 devices (2 for each probe) then there is a problem


            lock (devices)
            {

                int tempDeviceIndex = 1;
                double scaleFactor = 1.0;
                double offset = 1.0;

                switch (probe)
                {
                    case TemperatureProbes.HotLiquorTank:
                        tempDeviceIndex = 3;
                        scaleFactor = Convert.ToDouble(ConfigurationSettings.AppSettings["HLTTempScaleFactor"]);
                        offset = Convert.ToDouble(ConfigurationSettings.AppSettings["HLTTempOffset"]);
                        break;
                    case TemperatureProbes.MashTun:
                        tempDeviceIndex = 4;
                        scaleFactor = Convert.ToDouble(ConfigurationSettings.AppSettings["MTTempScaleFactor"]);
                        offset = Convert.ToDouble(ConfigurationSettings.AppSettings["MTTempOffset"]);

                        break;
                    case TemperatureProbes.BoilKettle:
                        scaleFactor = Convert.ToDouble(ConfigurationSettings.AppSettings["BKTempScaleFactor"]);
                        offset = Convert.ToDouble(ConfigurationSettings.AppSettings["BKTempOffset"]);
                        
                        tempDeviceIndex = 5;
                        break;
                }


                int counter = 0;
                foreach (HidDevice device in devices)
                {
                    if (counter == tempDeviceIndex)
                    {

                        byte[] bytes = { 0, 1, 128, 51, 1, 0, 0, 0, 0 };

                        device.Write(bytes);
                        HidDeviceData data = device.Read();

                        //Debug.WriteLine(String.Format("{0}: {1}", counter, data.Data[3]));

                        if (data.Data[3] > 0)
                        {
                            double rawTemp = data.Data[3] + ((double)data.Data[4]) / 256;
                            double calibrated = rawTemp * scaleFactor + offset;
                            
                            Debug.WriteLine( String.Format("{0}:  {1:0.00}", probe, calibrated));
                            //temp = (int)(data.Data[3] + ((float)data.Data[4]) / 256);

                            double tempF = calibrated * 9.0 / 5.0 + 32;

                            

                            //return in degrees farenheit rounded to nearest int
                            return (int) Math.Round(tempF, 0);
                        }
                    }
                    counter++;
                }

            }

            return 0;
        }
    }
}
