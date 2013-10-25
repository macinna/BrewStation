using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibrary;

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

                switch (probe)
                {
                    case TemperatureProbes.HotLiquorTank:
                        tempDeviceIndex = 3;
                        break;
                    case TemperatureProbes.MashTun:
                        tempDeviceIndex = 4;
                        break;
                    case TemperatureProbes.BoilKettle:
                        tempDeviceIndex = 5;
                        break;
                }


                int counter = 0;
                int temp = 0;
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
                            temp = (int)(data.Data[3] + ((float)data.Data[4]) / 256);
                            temp = (int)(((data.Data[3] * 16) + (data.Data[4] / 16)) * 0.1125) + 32;
                        }
                    }
                    counter++;
                }

                return temp;
            }
        }
    }
}
