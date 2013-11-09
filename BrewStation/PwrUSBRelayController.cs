using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrewStation
{
    public class PwrUSBRelayController : RelayController
    {
        private Object thisLock = new Object();
        private static PwrUSBRelayController instance;

        public PwrUSBRelayController()
        {

        }


        public override bool OpenRelay(Relays relay)
        {
            return ModifyRelay(relay, true);
        }

        public override bool CloseRelay(Relays relay)
        {

            return ModifyRelay(relay, false);

        }

        public override bool OpenAllRelays()
        {
            return ModifyRelay(Relays.All, true)    ;
        }

        public override bool Initialize()
        {
            int model = 0;
            StringBuilder firmware = new StringBuilder(128);
            bool success = false;
            try
            {
                if(PwrUSBWrapper.InitPowerUSB(out model, firmware) == 2)
                {
                    success = true;
                }
            }
            catch
            {
                success = false;
            }

            return success;
        }

        private bool ModifyRelay(Relays relay, bool open)
        {
            lock (thisLock)
            {
                int model, pwrUsbConnected, pwrUsbDevice = 0;
                StringBuilder firmware = new StringBuilder(128);
                int stateP1, stateP2, stateP3 = 0;

                int newState = open ? 0 : 1;


                if (relay == Relays.HotLiquorTankBurner || relay == Relays.Pump1)
                    pwrUsbDevice = 0;
                else
                    pwrUsbDevice = 1;

                int numDevices = PwrUSBWrapper.InitPowerUSB(out model, firmware);

                int current = PwrUSBWrapper.SetCurrentPowerUSB(pwrUsbDevice);

                if (numDevices > 0)
                {

                    pwrUsbConnected = PwrUSBWrapper.CheckStatusPowerUSB();
                    PwrUSBWrapper.ReadPortStatePowerUSB(out stateP1, out stateP2, out stateP3);

                    switch (relay)
                    {
                        case Relays.HotLiquorTankBurner:
                            stateP2 = newState;
                            break;
                        case Relays.MashTunBurner:
                            stateP2 = newState;
                            break;
                        case Relays.Pump1:
                            stateP3 = newState;
                            break;
                        case Relays.Pump2:
                            stateP3 = newState;
                            break;
                    }

                    PwrUSBWrapper.SetPortPowerUSB(stateP1, stateP2, stateP3);
                    return true;
                }
                else
                    return false;

            }
        }

    }
}
