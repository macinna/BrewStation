using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Configuration;

namespace BrewStation
{
    class TinyOSRelayController : RelayController  
    {
        //module ID provided by relay board vendor
        private const int RELAY_MODULE_ID = 15;
        private string portName = null;
        private object lockObj = new Object();

        public override bool Initialize()
        {
            System.Configuration.AppSettingsReader reader = new AppSettingsReader();
            object port = reader.GetValue("RelayControllerCOMPort", typeof(String));
            string comPort =  port == null ? string.Empty : (string) port;

            if(!String.IsNullOrEmpty(comPort))
            {
                //let's ensure we can connect to the device on the specified COM port
                comPort = "COM" + comPort;
                if (CanConnectToModule(new SerialPort(comPort)))
                {
                    this.portName = comPort;
                    return true;
                }
            }

            //either the port wasn't provided, or we didn't find the module on that port
            //look through all ports
            foreach (string name in SerialPort.GetPortNames())
            {
                SerialPort sp = new SerialPort(name);
                if(CanConnectToModule(sp))
                {
                    this.portName = name;
                    return true;
                }
            }
            
            return false;
        }

        private bool CanConnectToModule(SerialPort sp)
        {
            int moduleId = -1;

            try
            {
                sp.ReadTimeout = 500;
                sp.WriteTimeout = 500;

                sp.Open();
                sp.Write(new char[] { 'Z' }, 0, 1);
                moduleId = sp.ReadByte();

                if (moduleId == RELAY_MODULE_ID)
                {
                    //found module.  set all relays to OPEN
                    sp.Write(new char[] { 'n' }, 0, 1);
                    return true;
                }

            }
            catch (Exception e)
            {
                //log error here
            }
            finally
            {
                sp.Close();
            }

            return false;
        }


        
        public override bool OpenRelay(Relays relay)
        {
            return ModifyRelay(relay, false);
        }

        public override bool CloseRelay(Relays relay)
        {
            return ModifyRelay(relay, true);
        }

        public override bool OpenAllRelays()
        {
            return ModifyRelay(Relays.All, true);
        }

        private bool ModifyRelay(Relays relay, bool openRelay)
        {
            if (String.IsNullOrEmpty(this.portName))
                return false;

            lock(this.lockObj)
            {

                //relays and responsibility
                //1:  HLT Burner
                //2:  MT Burner
                //3:  Left Pump
                //4:  Right Pump

                //default is to open all relays
                char charToSend = 'n';
                
                switch(relay)
                {
                    case Relays.HotLiquorTankBurner:
                        charToSend = openRelay ? 'e' : 'o';
                        break;
                    case Relays.MashTunBurner:
                        charToSend = openRelay ? 'f' : 'p';
                        break;
                    case Relays.Pump1:
                        charToSend = openRelay ? 'g' : 'q';
                        break;
                    case Relays.Pump2:
                        charToSend = openRelay ? 'h' : 'r';
                        break;
                    case Relays.All:
                        charToSend = openRelay ? 'n' : 'd';
                        break;

                }
                                
                SerialPort sp = new SerialPort(this.portName);
                bool success = false;

                try
                {
                    sp.Open();
                    sp.Write(new char[] { charToSend }, 0, 1);

                    success = true;
                }
                catch( Exception e)
                {
                    //log error here
                    success = false;
                }
                finally
                {
                    sp.Close();
                }

                return success;
            }

        }

    }
}
