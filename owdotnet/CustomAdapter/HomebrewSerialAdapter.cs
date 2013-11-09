// ---------------------------------------------------------------------------
// Copyright (C) 2008 Per Malmberg <www.pmalmberg.com>, All Rights Reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY,  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL DALLAS SEMICONDUCTOR BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
//---------------------------------------------------------------------------

// *** NO SUPPORT IS GIVEN FOR THIS ADAPTER ***

// This file is distributed as part of the open source OWdotNET project.
// Project pages: https://sourceforge.net/projects/owdotnet
// Web Site:      http://owdotnet.sourceforge.net/

using DalSemi.OneWire.Adapter;


namespace OWN.CustomAdapter
{
    /// <summary>
    /// A representation of a homebrew serial adapter (DS9097) that cannot
    /// deliver power. To be able to communicate with a limited set of the available devices,
    /// such as a DS18S20, we fake the power ability thus fooling the OneWireContainers
    /// that we can provide power.
    /// </summary>
    internal class HomebrewSerialAdapter : TMEXLibAdapter
    {
        #region Constructors and Destructors
        /// <summary>
        /// Constructs a HomebrewSerialAdapter
        /// </summary>
        /// <throws>ClassNotFoundException</throws>
        public HomebrewSerialAdapter()
        {
            // attempt to set the portType, will throw exception if does not exist
            if (!SetTMEXPortType(TMEXPortType.PassiveSerialPort))
            {
                throw new AdapterException("TMEX adapter type does not exist");
            }
        }

        /// <summary>
        /// Constructs an instance of the HomebrewSerialAdapter
        /// </summary>
        /// <param name="">newPortType</param>
        /// <throws>  ClassNotFoundException </throws>
        public HomebrewSerialAdapter(TMEXPortType newPortType)
        {
            // attempt to set the portType, will throw exception if does not exist
            if (newPortType != TMEXPortType.PassiveSerialPort)
            {
                throw new AdapterException("The HomebrewSerialAdapter can only fake a passive serial adapter type");
            }
            if (!SetTMEXPortType(TMEXPortType.PassiveSerialPort))
            {
                throw new AdapterException("TMEX adapter type does not exist");
            }
        }
        #endregion // Constructors and Destructors

        #region Power Delivery

        /// <summary>
        /// Fakes setting the 1-Wire Network voltage to supply power to an iButton device.
        /// </summary>
        public override bool StartPowerDelivery(OWPowerStart changeCondition)
        {
            return true;
        }


        /// <summary>
        /// Fakes setting the power back to normal 
        /// </summary>
        public override void SetPowerNormal()
        {
 
        }

        /// <summary>
        /// Fakes power capability
        /// </summary>
        /// <value></value>
        /// <returns>
        /// true always
        /// </returns>
        public override bool CanDeliverPower
        {
            get { return true; }
        }

        #endregion
    }


}
