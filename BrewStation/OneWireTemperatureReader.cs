﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DalSemi.OneWire.Adapter;
using DalSemi.OneWire;
using DalSemi.OneWire.Container;


namespace BrewStation
{
    public class OneWireTemperatureReader : TemperatureReader
    {

        private const string HLT_TEMP_PROBE_ADDRESS = "6A000004EEE10828";
        private const string MT_TEMP_PROBE_ADDRESS = "C2000004EF544228";
        private const string BK_TEMP_PROBE_ADDRESS = "B1000004EE81A128";
        
        private object lockObj = new Object();
        private DeviceContainerList owContainers = null;

        public OneWireTemperatureReader()
        { }

        public override int GetCurrentTemperature(TemperatureProbes probe)
        {
            lock(this.lockObj)
            {
                if (this.owContainers == null)
                    throw new Exception("OneWireTEmperatureReader not initialized.");

                string probeAddr = null;
                switch(probe)
                {
                    case TemperatureProbes.BoilKettle:
                        probeAddr = BK_TEMP_PROBE_ADDRESS;
                        break;
                    case TemperatureProbes.HotLiquorTank:
                        probeAddr = HLT_TEMP_PROBE_ADDRESS;
                        break;
                    case TemperatureProbes.MashTun:
                        probeAddr = MT_TEMP_PROBE_ADDRESS;
                        break;
                }

                try
                {
                    foreach(OneWireContainer container in this.owContainers)
                    {
                        if(container.AddressAsString.Equals(probeAddr))
                        {
                            OneWireContainer28 tempSensor = container as OneWireContainer28;

                            byte[] state = tempSensor.ReadDevice();
                            tempSensor.DoTemperatureConvert(state);
                            tempSensor.SetTemperatureResolution(OneWireContainer28.RESOLUTION_9_BIT, state);

                            double tempC = tempSensor.GetTemperature(state);
                            double tempF = tempC * 9.0 / 5.0 + 32;

                            return (int)Math.Round(tempF, 0);

                        }
                    }
                }
                catch (Exception e)
                {

                }

                return 0;

            }
            
        }

        public override bool Initialize()
        {
            try
            {
                PortAdapter adapter = AccessProvider.GetAdapter("{DS9097U}", "COM9");
                this.owContainers = adapter.GetDeviceContainers();

                int adapterCount = 0;

                //look for each of our probes
                foreach (OneWireContainer container in this.owContainers)
                {
                    if (container.AddressAsString.Equals(BK_TEMP_PROBE_ADDRESS) || 
                        container.AddressAsString.Equals(HLT_TEMP_PROBE_ADDRESS) ||
                        container.AddressAsString.Equals(MT_TEMP_PROBE_ADDRESS))
                    {
                        adapterCount++;
                    }
                }
                //ensure all three were found
                if (adapterCount == 3)
                    return true;
            }
            catch(Exception e)
            {
                
            }


            return false;

        }
    }
}
