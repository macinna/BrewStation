// TODO: AdapterDetected

/*---------------------------------------------------------------------------
 * Copyright (C) 2004 Dallas Semiconductor Corporation, All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY,  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL DALLAS SEMICONDUCTOR BE LIABLE FOR ANY CLAIM, DAMAGES
 * OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * Except as contained in this notice, the name of Dallas Semiconductor
 * shall not be used except as stated in the Dallas Semiconductor
 * Branding Policy.
 *---------------------------------------------------------------------------
 */

// This file is distributed as part of the open source OWdotNET project.
// Project pages: https://sourceforge.net/projects/owdotnet
// Web Site:      http://owdotnet.sourceforge.net/

using System;
using DalSemi;
using DalSemi.Utils;
using DalSemi.Serial;

namespace DalSemi.OneWire.Adapter
{
    internal class SerialAdapter : PortAdapter // USerialAdapter
    {

        #region Ralph Maas / RM

        //--------
        //-------- Adapter detection 
        //--------

        /**
         * Detect adapter presence on the selected port.
         *
         * @return  <code>true</code> if the adapter is confirmed to be connected to
         * the selected port, <code>false</code> if the adapter is not connected.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public override Boolean AdapterDetected
        {
            get
            {
                return true; // TODO
            }
        }

        //--------
        //-------- Adapter detection
        //--------

        /**
         * Detect adapter presence on the selected port.
         *
         * @return  <code>true</code> if the adapter is confirmed to be connected to
         * the selected port, <code>false</code> if the adapter is not connected.
         *
         * @throws OneWireIOException
         * @throws OneWireException
        public boolean adapterDetected ()
           throws OneWireIOException, OneWireException
        {
           boolean rt;

           try
           {

              // acquire exclusive use of the port
              beginLocalExclusive();
              uAdapterPresent();

              rt = uVerify();
           }
           catch (OneWireException e)
           {
              rt = false;
           }
           finally
           {

              // release local exclusive use of port
              endLocalExclusive();
           }

           return rt;
        }
         */

        #endregion



        #region private fields
        //private bool doDebugMessages = false;

        private BaudRates maxBaud = BaudRates.CBR_115200;

        private SerialPort serial;

        private bool adapterPresent;

        /// <summary>Flag to indicate more than expected byte received in a transaction </summary>
        private bool extraBytesReceived;

        /// <summary>U Adapter packet builder</summary>
        private UPacketBuilder uBuild;

        /// <summary>State of the OneWire</summary>
        private OneWireState owState;

        /// <summary>U Adapter state</summary>
        private UAdapterState uState;

        /// <summary>Input buffer to hold received data</summary>
        private System.Collections.ArrayList inBuffer;

        /// <summary>
        /// Serial Port Settings object
        /// </summary>
        private DetailedPortSettings dps;
        #endregion

        #region Constructors and Destructors

        public SerialAdapter()
        {
            dps = new DetailedPortSettings();
            dps.BasicSettings.BaudRate = BaudRates.CBR_9600;
            dps.BasicSettings.Parity = Parity.none;
            dps.BasicSettings.StopBits = StopBits.one;
            dps.BasicSettings.ByteSize = 8;
            dps.AbortOnError = false;
            dps.RTSControl = RTSControlFlows.enable;
            dps.DTRControl = DTRControlFlows.enable;
            dps.DSRSensitive = false;
            dps.EOFChar = (char)0;
            dps.EVTChar = (char)0;
            dps.ErrorChar = (char)0;
            dps.DiscardNulls = false;// enable null stripping (????)
            dps.InX = false;
            dps.OutX = false;
            dps.TxContinueOnXOff = true;
            dps.XoffChar = (char)1;
            dps.XonChar = (char)0;
            dps.OutCTS = false;
            dps.OutDSR = false;
            owState = new OneWireState();
            uState = new UAdapterState(owState);
            uBuild = new UPacketBuilder(uState);
            inBuffer = new System.Collections.ArrayList(10);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (serial != null)
                {
                    serial.Dispose();
                    serial = null;
                }
            }
        }

        public override System.Collections.IList PortNames
        {
            get
            {
                return SerialPort.PortNames;
            }
        }
        public override string AdapterName
        {
            get
            {
                return "DS9097U";
            }
        }
        public override string PortName
        {
            get
            {
                return serial.PortName;
            }
        }
        public override string PortTypeDescription
        {
            get
            {
                return serial.PortName + " (.NET)";
            }
        }

        public override bool OpenPort(String PortName)
        {
            if (serial == null)
            {
                dps.BasicSettings.BaudRate = BaudRates.CBR_9600;
                Debug.WriteLine("DEBUG: Opening Port: " + PortName);
                serial = new SerialPort(PortName, dps, 1020, 1020);
                if (serial.Open())
                {
                    Debug.WriteLine("DEBUG: Open Succeeded");
                    if (uAdapterPresent())
                        return true;
                    Debug.WriteLine("DEBUG: Adapter not present");
                    serial.Close();
                }
                serial = null;
            }
            return false;
        }
        #endregion

        #region Data I/O
        public override OWResetResult Reset()
        {
            try
            {
                // acquire exclusive use of the port
                BeginExclusive(true);

                // make sure adapter is present
                if (uAdapterPresent())
                {
                    // check for pending power conditions
                    if (owState.oneWireLevel != OWLevel.LEVEL_NORMAL)
                        SetPowerNormal();

                    // flush out the com buffer
                    //serial.Flush();

                    // build a message to read the baud rate from the U brick
                    uBuild.Restart();

                    int reset_offset = uBuild.OneWireReset();

                    // send and receive
                    byte[] result_array = uTransaction(uBuild);

                    // check the result
                    if (result_array.Length == (reset_offset + 1))
                        return uBuild.InterpretOneWireReset(result_array[reset_offset]);
                    else
                        throw new AdapterException("USerialAdapter-reset: no return byte form 1-Wire reset");
                }
                else
                    throw new AdapterException("Error communicating with adapter");
            }
            catch (System.IO.IOException ioe)
            {
                throw new AdapterException(ioe);
            }
            finally
            {
                // release local exclusive use of port
                EndExclusive();
            }
        }


        /// <summary> Sends a bit to the 1-Wire Network.
        /// 
        /// </summary>
        /// <param name="bitValue"> the bit value to send to the 1-Wire Network.
        /// </param>
        public override void PutBit(bool bitValue)
        {
            try
            {
                // acquire exclusive use of the port
                BeginExclusive(true);

                // make sure adapter is present
                if (uAdapterPresent())
                {
                    // check for pending power conditions
                    if (owState.oneWireLevel != OWLevel.LEVEL_NORMAL)
                        SetPowerNormal();

                    // flush out the com buffer
                    //serial.Flush();

                    // build a message to send bit to the U brick
                    uBuild.Restart();

                    int bit_offset = uBuild.DataBit(bitValue, owState.levelChangeOnNextBit);

                    // check if just started power delivery
                    if (owState.levelChangeOnNextBit)
                    {
                        // clear the primed condition
                        owState.levelChangeOnNextBit = false;

                        // set new level state
                        owState.oneWireLevel = OWLevel.LEVEL_POWER_DELIVERY;
                    }

                    // send and receive
                    byte[] result_array = uTransaction(uBuild);

                    // check for echo
                    if (bitValue != uBuild.InterpretOneWireBit((byte)result_array[bit_offset]))
                        throw new AdapterException("1-Wire communication error, echo was incorrect");
                }
                else
                    throw new AdapterException("Error communicating with adapter");
            }
            catch (System.IO.IOException ioe)
            {
                throw new AdapterException(ioe);
            }
            finally
            {

                // release local exclusive use of port
                EndExclusive();
            }
        }

        /// <summary> Sends a byte to the 1-Wire Network.
        /// 
        /// </summary>
        /// <param name="byteValue"> the byte value to send to the 1-Wire Network.
        /// </param>
        public override void PutByte(int byteValue)
        {
            byte[] temp_block = new byte[1];

            temp_block[0] = (byte)byteValue;

            DataBlock(temp_block, 0, 1);

            // check to make sure echo was what was sent
            if (temp_block[0] != (byte)byteValue)
                throw new AdapterException("Error short on 1-Wire during putByte");
        }

        /// <summary> Gets a bit from the 1-Wire Network.
        /// 
        /// </summary>
        /// <returns>  the bit value recieved from the the 1-Wire Network.
        /// </returns>
        public override bool GetBit()
        {
            try
            {

                // acquire exclusive use of the port
                BeginExclusive(true);

                // make sure adapter is present
                if (uAdapterPresent())
                {

                    // check for pending power conditions
                    if (owState.oneWireLevel != OWLevel.LEVEL_NORMAL)
                        SetPowerNormal();

                    // flush out the com buffer
                    //serial.Flush();

                    // build a message to send bit to the U brick
                    uBuild.Restart();

                    int bit_offset = uBuild.DataBit(true, owState.levelChangeOnNextBit);

                    // check if just started power delivery
                    if (owState.levelChangeOnNextBit)
                    {

                        // clear the primed condition
                        owState.levelChangeOnNextBit = false;

                        // set new level state
                        owState.oneWireLevel = OWLevel.LEVEL_POWER_DELIVERY;
                    }

                    // send and receive
                    byte[] result_array = uTransaction(uBuild);

                    // check the result
                    if (result_array.Length == (bit_offset + 1))
                        return uBuild.InterpretOneWireBit((byte)result_array[bit_offset]);
                    else
                        return false;
                }
                else
                    throw new AdapterException("Error communicating with adapter");
            }
            catch (System.IO.IOException ioe)
            {
                throw new AdapterException(ioe);
            }
            finally
            {
                // release local exclusive use of port
                EndExclusive();
            }
        }

        /// <summary> Gets a byte from the 1-Wire Network.
        /// 
        /// </summary>
        /// <returns>  the byte value received from the the 1-Wire Network.
        /// </returns>
        public override byte GetByte()
        {
            byte[] temp_block = GetBlock(1);
            return temp_block[0];
        }

        /// <summary> Get a block of data from the 1-Wire Network.
        /// 
        /// </summary>
        /// <param name="len"> length of data bytes to receive
        /// </param>
        /// <returns>  the data received from the 1-Wire Network.
        /// </returns>
        public override byte[] GetBlock(int len)
        {
            byte[] temp_block = new byte[len];

            GetBlock(temp_block, 0, len);

            return temp_block;
        }

        /// <summary> Get a block of data from the 1-Wire Network and write it into
        /// the provided array.
        /// 
        /// </summary>
        /// <param name="arr">    array in which to write the received bytes
        /// </param>
        /// <param name="len">    length of data bytes to receive
        /// </param>
        public override void GetBlock(byte[] arr, int len)
        {
            GetBlock(arr, 0, len);
        }


        /// <summary> Get a block of data from the 1-Wire Network and write it into
        /// the provided array.
        /// 
        /// </summary>
        /// <param name="arr">    array in which to write the received bytes
        /// </param>
        /// <param name="off">    offset into the array to start
        /// </param>
        /// <param name="len">    length of data bytes to receive
        /// </param>
        public override void GetBlock(byte[] arr, int off, int len)
        {
            // set block to read 0xFF
            for (int i = off; i < len; i++)
                arr[i] = 0xFF;

            DataBlock(arr, off, len);
        }


        /// <summary> Sends a block of data and returns the data received in the same array.
        /// This method is used when sending a block that contains reads and writes.
        /// The 'read' portions of the data block need to be pre-loaded with 0xFF's.
        /// It starts sending data from the index at offset 'off' for length 'len'.
        /// 
        /// </summary>
        /// <param name="buffer"> array of data to transfer to and from the 1-Wire Network.
        /// </param>
        /// <param name="off">       offset into the array of data to start
        /// </param>
        /// <param name="len">       length of data to send / receive starting at 'off'
        /// </param>
        public override void DataBlock(byte[] buffer, int off, int len)
        {
            int data_offset;
            byte[] ret_data;

            // acquire exclusive use of the port
            BeginExclusive(true);
            try
            {
                // make sure adapter is present
                if (uAdapterPresent())
                {

                    // check for pending power conditions
                    if (owState.oneWireLevel != OWLevel.LEVEL_NORMAL)
                        SetPowerNormal();

                    // set the correct baud rate to stream this operation
                    SetStreamingSpeed(UPacketBuilder.OPERATION_BYTE);

                    // flush out the com buffer
                    //serial.Flush();

                    // build a message to write/read data bytes to the U brick
                    uBuild.Restart();

                    // check for primed byte
                    if ((len == 1) && owState.levelChangeOnNextByte)
                    {
                        data_offset = uBuild.PrimedDataByte(buffer[off]);
                        owState.levelChangeOnNextByte = false;

                        // send and receive
                        ret_data = uTransaction(uBuild);

                        // set new level state
                        owState.oneWireLevel = OWLevel.LEVEL_POWER_DELIVERY;

                        // extract the result byte
                        buffer[off] = uBuild.InterpretPrimedByte(ret_data, data_offset);
                    }
                    else
                    {
                        data_offset = uBuild.DataBytes(buffer, off, len);

                        // send and receive
                        ret_data = uTransaction(uBuild);

                        // extract the result byte(s)
                        uBuild.InterpretDataBytes(ret_data, data_offset, buffer, off, len);
                    }
                }
                else
                    throw new AdapterException("Error communicating with adapter");
            }
            catch (System.IO.IOException ioe)
            {
                throw new AdapterException(ioe);
            }
            finally
            {

                // release local exclusive use of port
                EndExclusive();
            }
        }
        #endregion

        #region Communication Speed
        /// <summary>OWSpeed representing current speed of communication on 1-Wire network
        /// </summary>
        public override OWSpeed Speed
        {
            set
            {
                // acquire exclusive use of the port
                BeginExclusive(true);
                try
                {
                    // change 1-Wire speed
                    owState.oneWireSpeed = value;

                    // set adapter to communicate at this new speed (regular == flex for now)
                    if (value == OWSpeed.SPEED_OVERDRIVE)
                        uState.uSpeedMode = UAdapterState.USPEED_OVERDRIVE;
                    else
                        uState.uSpeedMode = UAdapterState.USPEED_FLEX;
                }
                finally
                {
                    // release local exclusive use of port
                    EndExclusive();
                }
            }
            get
            {
                return owState.oneWireSpeed;
            }
        }
        #endregion

        #region Power Delivery
        /// <summary> Sets the duration to supply power to the 1-Wire Network.
        /// This method takes a time parameter that indicates the program
        /// pulse length when the method startPowerDelivery().<p>
        /// 
        /// Note: to avoid getting an exception,
        /// use the canDeliverPower() and canDeliverSmartPower()
        /// method to check it's availability. <p>
        /// 
        /// </summary>
        /// <param name="">timeFactor
        /// <ul>
        /// <li>   0 (DELIVERY_HALF_SECOND) provide power for 1/2 second.
        /// <li>   1 (DELIVERY_ONE_SECOND) provide power for 1 second.
        /// <li>   2 (DELIVERY_TWO_SECONDS) provide power for 2 seconds.
        /// <li>   3 (DELIVERY_FOUR_SECONDS) provide power for 4 seconds.
        /// <li>   4 (DELIVERY_SMART_DONE) provide power until the
        /// the device is no longer drawing significant power.
        /// <li>   5 (DELIVERY_INFINITE) provide power until the
        /// setBusNormal() method is called.
        /// </ul>
        /// </param>
        public override void SetPowerDuration(OWPowerTime powerDur)
        {
            if (powerDur != OWPowerTime.DELIVERY_INFINITE)
                throw new AdapterException("USerialAdapter-setPowerDuration, does not support this duration, infinite only");
            else
                owState.levelTimeFactor = OWPowerTime.DELIVERY_INFINITE;
        }

        /// <summary> Sets the 1-Wire Network voltage to supply power to an iButton device.
        /// This method takes a time parameter that indicates whether the
        /// power delivery should be done immediately, or after certain
        /// conditions have been met. <p>
        /// 
        /// Note: to avoid getting an exception,
        /// use the canDeliverPower() and canDeliverSmartPower()
        /// method to check it's availability. <p>
        /// 
        /// </summary>
        /// <param name="">changeCondition
        /// <ul>
        /// <li>   0 (CONDITION_NOW) operation should occur immediately.
        /// <li>   1 (CONDITION_AFTER_BIT) operation should be pending
        /// execution immediately after the next bit is sent.
        /// <li>   2 (CONDITION_AFTER_BYTE) operation should be pending
        /// execution immediately after next byte is sent.
        /// </ul>
        /// 
        /// </param>
        /// <returns> <code>true</code> if the voltage change was successful,
        /// <code>false</code> otherwise.
        /// 
        /// @throws AdapterException on a 1-Wire communication error
        /// @throws AdapterException on a setup error with the 1-Wire adapter
        /// </returns>
        public override bool StartPowerDelivery(OWPowerStart changeCondition)
        {
            // acquire exclusive use of the port
            BeginExclusive(true);
            try
            {
                if (changeCondition == OWPowerStart.CONDITION_AFTER_BIT)
                {
                    owState.levelChangeOnNextBit = true;
                    owState.primedLevelValue = OWLevel.LEVEL_POWER_DELIVERY;
                }
                else if (changeCondition == OWPowerStart.CONDITION_AFTER_BYTE)
                {
                    owState.levelChangeOnNextByte = true;
                    owState.primedLevelValue = OWLevel.LEVEL_POWER_DELIVERY;
                }
                else if (changeCondition == OWPowerStart.CONDITION_NOW)
                {

                    // make sure adapter is present
                    if (uAdapterPresent())
                    {

                        // check for pending power conditions
                        if (owState.oneWireLevel != OWLevel.LEVEL_NORMAL)
                            SetPowerNormal();

                        // flush out the com buffer
                        //serial.Flush();

                        // build a message to read the baud rate from the U brick
                        uBuild.Restart();

                        // set the SPUD time value
                        int set_SPUD_offset = uBuild.SetParameter(
                           ProgramPulseTime5.TIME5V_infinite);

                        // add the command to begin the pulse
                        uBuild.SendCommand(UPacketBuilder.FUNCTION_5VPULSE_NOW, false);

                        // send and receive
                        byte[] result_array = uTransaction(uBuild);

                        // check the result
                        if (result_array.Length == (set_SPUD_offset + 1))
                        {
                            owState.oneWireLevel = OWLevel.LEVEL_POWER_DELIVERY;

                            return true;
                        }
                    }
                    else
                        throw new AdapterException("Error communicating with adapter");
                }
                else
                    throw new AdapterException("Invalid power delivery condition");

                return false;
            }
            catch (System.IO.IOException ioe)
            {
                throw new AdapterException(ioe);
            }
            finally
            {
                // release local exclusive use of port
                EndExclusive();
            }
        }

        /// <summary> Sets the duration for providing a program pulse on the
        /// 1-Wire Network.
        /// This method takes a time parameter that indicates the program
        /// pulse length when the method startProgramPulse().<p>
        /// 
        /// Note: to avoid getting an exception,
        /// use the canDeliverPower() method to check it's
        /// availability. <p>
        /// 
        /// </summary>
        /// <param name="">timeFactor
        /// <ul>
        /// <li>   6 (DELIVERY_EPROM) provide program pulse for 480 microseconds
        /// <li>   5 (DELIVERY_INFINITE) provide power until the
        /// setBusNormal() method is called.
        /// </ul>
        /// </param>
        public override void SetProgramPulseDuration(OWPowerTime pulseDur)
        {
            if (pulseDur != OWPowerTime.DELIVERY_EPROM)
                throw new AdapterException("Only support EPROM length program pulse duration");
        }


        /// <summary> Sets the 1-Wire Network voltage to eprom programming level.
        /// This method takes a time parameter that indicates whether the
        /// power delivery should be done immediately, or after certain
        /// conditions have been met. <p>
        /// 
        /// Note: to avoid getting an exception,
        /// use the canProgram() method to check it's
        /// availability. <p>
        /// 
        /// </summary>
        /// <param name="">changeCondition
        /// <ul>
        /// <li>   0 (CONDITION_NOW) operation should occur immediately.
        /// <li>   1 (CONDITION_AFTER_BIT) operation should be pending
        /// execution immediately after the next bit is sent.
        /// <li>   2 (CONDITION_AFTER_BYTE) operation should be pending
        /// execution immediately after next byte is sent.
        /// </ul>
        /// 
        /// </param>
        /// <returns> <code>true</code> if the voltage change was successful,
        /// <code>false</code> otherwise.
        /// 
        /// @throws AdapterException on a 1-Wire communication error
        /// @throws AdapterException on a setup error with the 1-Wire adapter
        /// or the adapter does not support this operation
        /// </returns>
        public override bool StartProgramPulse(OWPowerStart changeCondition)
        {
            // check if adapter supports program
            if (!uState.programVoltageAvailable)
                throw new AdapterException(
                   "SerialAdapter: startProgramPulse, program voltage not available");

            // check if correct change condition
            if (changeCondition != OWPowerStart.CONDITION_NOW)
                throw new AdapterException(
                   "SerialAdapter: startProgramPulse, CONDITION_NOW only currently supported");

            // acquire exclusive use of the port
            BeginExclusive(true);
            try
            {
                // build a message to read the baud rate from the U brick
                uBuild.Restart();

                //int set_SPUD_offset =
                uBuild.SetParameter(ProgramPulseTime12.TIME12V_512us);

                // add the command to begin the pulse
                //int pulse_offset =
                uBuild.SendCommand(UPacketBuilder.FUNCTION_12VPULSE_NOW, true);

                // send the command
                //char[] result_array =
                uTransaction(uBuild);

                // check the result ??
                return true;
            }
            finally
            {

                // release local exclusive use of port
                EndExclusive();
            }
        }

        /// <summary> Sets the 1-Wire Network voltage to 0 volts.  This method is used
        /// rob all 1-Wire Network devices of parasite power delivery to force
        /// them into a hard reset.
        /// </summary>
        public override void StartBreak()
        {
            // acquire exclusive use of the port
            BeginExclusive(true);
            try
            {
                // power down the 2480 (dropping the 1-Wire)
                serial.DTREnable = false;
                serial.RTSEnable = false;

                // wait for power to drop
                sleep(200);

                // set the level state
                owState.oneWireLevel = OWLevel.LEVEL_BREAK;
            }
            finally
            {
                // release local exclusive use of port
                EndExclusive();
            }
        }

        /// <summary> Sets the 1-Wire Network voltage to normal level.  This method is used
        /// to disable 1-Wire conditions created by startPowerDelivery and
        /// startProgramPulse.  This method will automatically be called if
        /// a communication method is called while an outstanding power
        /// command is taking place.
        /// 
        /// @throws AdapterException on a 1-Wire communication error
        /// @throws AdapterException on a setup error with the 1-Wire adapter
        /// or the adapter does not support this operation
        /// </summary>
        public override void SetPowerNormal()
        {
            // acquire exclusive use of the port
            BeginExclusive(true);
            try
            {
                if (owState.oneWireLevel == OWLevel.LEVEL_POWER_DELIVERY)
                {

                    // make sure adapter is present
                    if (uAdapterPresent())
                    {

                        // flush out the com buffer
                        //serial.Flush();

                        // build a message to read the baud rate from the U brick
                        uBuild.Restart();

                        //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                        // shughes - 8-28-2003
                        // Fixed the Set Power Level Normal problem where adapter
                        // is left in a bad state.  Removed bad fix: extra getBit()
                        // SEE BELOW!
                        // stop pulse command
                        uBuild.SendCommand(UPacketBuilder.FUNCTION_STOP_PULSE, true);

                        // start pulse with no prime
                        uBuild.SendCommand(UPacketBuilder.FUNCTION_5VPULSE_NOW, false);
                        //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

                        // add the command to stop the pulse
                        int pulse_response_offset = uBuild.SendCommand(
                           UPacketBuilder.FUNCTION_STOP_PULSE, true);

                        // send and receive
                        byte[] result_array = uTransaction(uBuild);

                        // check the result
                        if (result_array.Length == (pulse_response_offset + 1))
                        {
                            owState.oneWireLevel = OWLevel.LEVEL_NORMAL;

                            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                            // shughes - 8-28-2003
                            // This is a bad "fix", it was needed when we were causing
                            // a bad condition.  Instead of fixing it here, we should
                            // fix it where we were causing it..  Which we did!
                            // SEE ABOVE!
                            //getBit();
                            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                        }
                        else
                            throw new AdapterException(
                               "Did not get a response back from stop power delivery");
                    }
                }
                else if (owState.oneWireLevel == OWLevel.LEVEL_BREAK)
                {

                    // restore power
                    serial.DTREnable = true;
                    serial.RTSEnable = true;

                    // wait for power to come up
                    sleep(300);

                    // set the level state
                    owState.oneWireLevel = OWLevel.LEVEL_NORMAL;

                    // set the DS2480 to the correct mode and verify
                    adapterPresent = false;

                    if (!uAdapterPresent())
                        throw new AdapterException(
                           "Did not get a response back from adapter after break");
                }
            }
            catch (System.IO.IOException ioe)
            {
                throw new AdapterException(ioe);
            }
            finally
            {

                // release local exclusive use of port
                EndExclusive();
            }
        }
        #endregion

        #region Adapter Features
        /// <summary> Returns whether adapter can physically support overdrive mode.
        /// 
        /// </summary>
        /// <returns>  <code>true</code> if this port adapter can do OverDrive,
        /// <code>false</code> otherwise.
        /// 
        /// @throws AdapterException on a 1-Wire communication error with the adapter
        /// @throws AdapterException on a setup error with the 1-Wire
        /// adapter
        /// </returns>
        public override bool CanOverdrive
        {
            get
            {
                return true;
            }
        }

        /// <summary> Returns whether the adapter can physically support hyperdrive mode.
        /// 
        /// </summary>
        /// <returns>  <code>true</code> if this port adapter can do HyperDrive,
        /// <code>false</code> otherwise.
        /// 
        /// @throws AdapterException on a 1-Wire communication error with the adapter
        /// @throws AdapterException on a setup error with the 1-Wire
        /// adapter
        /// </returns>
        public override bool CanHyperdrive
        {
            get
            {
                return false;
            }
        }

        /// <summary> Returns whether the adapter can physically support flex speed mode.
        /// 
        /// </summary>
        /// <returns>  <code>true</code> if this port adapter can do flex speed,
        /// <code>false</code> otherwise.
        /// 
        /// @throws AdapterException on a 1-Wire communication error with the adapter
        /// @throws AdapterException on a setup error with the 1-Wire
        /// adapter
        /// </returns>
        public override bool CanFlex
        {
            get
            {
                return true;
            }
        }

        /// <summary> Returns whether adapter can physically support 12 volt power mode.
        /// 
        /// </summary>
        /// <returns>  <code>true</code> if this port adapter can do Program voltage,
        /// <code>false</code> otherwise.
        /// 
        /// @throws AdapterException on a 1-Wire communication error with the adapter
        /// @throws AdapterException on a setup error with the 1-Wire
        /// adapter
        /// </returns>
        public override bool CanProgram
        {
            get
            {
                // acquire exclusive use of the port
                BeginExclusive(true);
                try
                {
                    // only check if the port is aquired
                    if (uAdapterPresent())
                    {

                        // perform a reset to read the program available flag
                        if (uState.revision == 0)
                            Reset();

                        // return the flag
                        return uState.programVoltageAvailable;
                    }
                    else
                        throw new AdapterException("SerialAdapter-canProgram, adapter not present");
                }
                finally
                {

                    // release local exclusive use of port
                    EndExclusive();
                }
            }
        }

        /// <summary> Returns whether the adapter can physically support strong 5 volt power
        /// mode.
        /// 
        /// </summary>
        /// <returns>  <code>true</code> if this port adapter can do strong 5 volt
        /// mode, <code>false</code> otherwise.
        /// 
        /// @throws AdapterException on a 1-Wire communication error with the adapter
        /// @throws AdapterException on a setup error with the 1-Wire
        /// adapter
        /// </returns>
        public override bool CanDeliverPower
        {
            get
            {
                return true;
            }
        }

        /// <summary> Returns whether the adapter can physically support "smart" strong 5
        /// volt power mode.  "smart" power delivery is the ability to deliver
        /// power until it is no longer needed.  The current drop it detected
        /// and power delivery is stopped.
        /// </summary>
        /// <returns>  <code>true</code> if this port adapter can do "smart" strong
        /// 5 volt mode, <code>false</code> otherwise.
        /// </returns>
        public override bool CanDeliverSmartPower
        {
            get
            {
                return false;
            }
        }

        /// <summary> Returns whether adapter can physically support 0 volt 'break' mode.
        /// </summary>
        /// <returns>  <code>true</code> if this port adapter can do break,
        /// <code>false</code> otherwise.
        /// </returns>
        public override bool CanBreak
        {
            get
            {
                return true;
            }
        }
        #endregion

        #region Searching
        /// <summary> Returns <code>true</code> if the first iButton or 1-Wire device
        /// is found on the 1-Wire Network.
        /// If no devices are found, then <code>false</code> will be returned.
        /// </summary>
        /// <returns>  <code>true</code> if an iButton or 1-Wire device is found.
        /// </returns>
        public override bool GetFirstDevice(byte[] address, int offset)
        {
            // reset the current Search
            owState.searchLastDiscrepancy = 0;
            owState.searchFamilyLastDiscrepancy = 0;
            owState.searchLastDevice = false;

            // Search for the first device using next
            return GetNextDevice(address, offset);
        }

        /// <summary> Returns <code>true</code> if the next iButton or 1-Wire device
        /// is found. The previous 1-Wire device found is used
        /// as a starting point in the Search.  If no more devices are found
        /// then <code>false</code> will be returned.
        /// </summary>
        /// <returns>  <code>true</code> if an iButton or 1-Wire device is found.
        /// </returns>
        public override bool GetNextDevice(byte[] address, int offset)
        {
            bool search_result;

            // acquire exclusive use of the port
            BeginExclusive(true);
            try
            {
                // check for previous last device
                if (owState.searchLastDevice)
                {
                    owState.searchLastDiscrepancy = 0;
                    owState.searchFamilyLastDiscrepancy = 0;
                    owState.searchLastDevice = false;

                    return false;
                }

                // check for 'first' and only 1 target
                if ((owState.searchLastDiscrepancy == 0)
                   && (owState.searchLastDevice == false)
                   && (owState.searchIncludeFamilies.Length == 1))
                {

                    // set the Search to find the 1 target first
                    owState.searchLastDiscrepancy = 64;

                    // create an id to set
                    byte[] new_id = new byte[8];

                    // set the family code
                    new_id[0] = (byte)owState.searchIncludeFamilies[0];

                    // clear the rest
                    for (int i = 1; i < 8; i++)
                        new_id[i] = 0;

                    // set this new ID
                    Array.Copy(new_id, 0, owState.ID, 0, 8);
                }

                // loop until the correct type is found or no more devices
                do
                {

                    // perform a Search and keep the result
                    search_result = Search(owState);

                    if (search_result)
                    {
                        for (int i = 0; i < 8; i++)
                            address[i + offset] = (byte)owState.ID[i];

                        // check if not in exclude list
                        bool is_excluded = false;

                        for (int i = 0; i < owState.searchExcludeFamilies.Length; i++)
                        {
                            if (owState.ID[0] == owState.searchExcludeFamilies[i])
                            {
                                is_excluded = true;

                                break;
                            }
                        }

                        // if not in exclude list then check for include list
                        if (!is_excluded)
                        {

                            // loop through the include list
                            bool is_included = false;

                            for (int i = 0; i < owState.searchIncludeFamilies.Length; i++)
                            {
                                if (owState.ID[0] == owState.searchIncludeFamilies[i])
                                {
                                    is_included = true;

                                    break;
                                }
                            }

                            // check if include list or there is no include list
                            if (is_included || (owState.searchIncludeFamilies.Length == 0))
                                return true;
                        }
                    }

                    // skip the current type if not last device
                    if (!owState.searchLastDevice && (owState.searchFamilyLastDiscrepancy != 0))
                    {
                        owState.searchLastDiscrepancy = owState.searchFamilyLastDiscrepancy;
                        owState.searchFamilyLastDiscrepancy = 0;
                        owState.searchLastDevice = false;
                    }
                    // end of Search so reset and return
                    else
                    {
                        owState.searchLastDiscrepancy = 0;
                        owState.searchFamilyLastDiscrepancy = 0;
                        owState.searchLastDevice = false;
                        search_result = false;
                    }
                }
                while (search_result);

                // device not found
                return false;
            }
            finally
            {

                // release local exclusive use of port
                EndExclusive();
            }
        }
        /// <summary> Verifies that the iButton or 1-Wire device specified is present on
        /// the 1-Wire Network. This does not affect the 'current' device
        /// state information used in searches (findNextDevice...).
        /// 
        /// </summary>
        /// <param name="address"> device address to verify is present
        /// 
        /// </param>
        /// <returns>  <code>true</code> if device is present else
        /// <code>false</code>.
        /// 
        /// @throws AdapterException on a 1-Wire communication error
        /// @throws AdapterException on a setup error with the 1-Wire adapter
        /// 
        /// </returns>
        /// <seealso cref="Address">
        /// </seealso>
        public override bool IsPresent(byte[] address, int offset)
        {
            // acquire exclusive use of the port
            BeginExclusive(true);
            try
            {
                // make sure adapter is present
                if (uAdapterPresent())
                {

                    // check for pending power conditions
                    if (owState.oneWireLevel != OWLevel.LEVEL_NORMAL)
                        SetPowerNormal();

                    // if in overdrive, then use the block method in super
                    if (owState.oneWireSpeed == OWSpeed.SPEED_OVERDRIVE)
                        return BlockIsPresent(address, offset, false);

                    // create a private OneWireState
                    OneWireState onewire_state = new OneWireState();

                    // set the ID to the ID of the iButton passes to this method
                    Array.Copy(address, offset, onewire_state.ID, 0, 8);

                    // set the state to find the specified device
                    onewire_state.searchLastDiscrepancy = 64;
                    onewire_state.searchFamilyLastDiscrepancy = 0;
                    onewire_state.searchLastDevice = false;
                    onewire_state.searchOnlyAlarmingButtons = false;

                    // perform a Search
                    if (Search(onewire_state))
                    {

                        // compare the found device with the desired device
                        for (int i = 0; i < 8; i++)
                            if (address[i + offset] != onewire_state.ID[i])
                                return false;

                        // must be the correct device
                        return true;
                    }

                    // failed to find device
                    return false;
                }
                else
                    throw new AdapterException("Error communicating with adapter");
            }
            finally
            {

                // release local exclusive use of port
                EndExclusive();
            }
        }

        /// <summary> Verifies that the iButton or 1-Wire device specified is present
        /// on the 1-Wire Network and in an alarm state. This does not
        /// affect the 'current' device state information used in searches
        /// (findNextDevice...).
        /// 
        /// </summary>
        /// <param name="address"> device address to verify is present and alarming
        /// 
        /// </param>
        /// <returns>  <code>true</code> if device is present and alarming else
        /// <code>false</code>.
        /// 
        /// @throws AdapterException on a 1-Wire communication error
        /// @throws AdapterException on a setup error with the 1-Wire adapter
        /// 
        /// </returns>
        /// <seealso cref="Address">
        /// </seealso>
        public override bool IsAlarming(byte[] address, int offset)
        {
            // acquire exclusive use of the port
            BeginExclusive(true);
            try
            {
                // make sure adapter is present
                if (uAdapterPresent())
                {

                    // check for pending power conditions
                    if (owState.oneWireLevel != OWLevel.LEVEL_NORMAL)
                        SetPowerNormal();

                    // if in overdrive, then use the block method in super
                    if (owState.oneWireSpeed == OWSpeed.SPEED_OVERDRIVE)
                        return BlockIsPresent(address, offset, true);

                    // create a private OneWireState
                    OneWireState onewire_state = new OneWireState();

                    // set the ID to the ID of the iButton passes to this method
                    Array.Copy(address, offset, onewire_state.ID, 0, 8);

                    // set the state to find the specified device (alarming)
                    onewire_state.searchLastDiscrepancy = 64;
                    onewire_state.searchFamilyLastDiscrepancy = 0;
                    onewire_state.searchLastDevice = false;
                    onewire_state.searchOnlyAlarmingButtons = true;

                    // perform a Search
                    if (Search(onewire_state))
                    {

                        // compare the found device with the desired device
                        for (int i = 0; i < 8; i++)
                            if (address[i + offset] != onewire_state.ID[i])
                                return false;

                        // must be the correct device
                        return true;
                    }

                    // failed to find any alarming device
                    return false;
                }
                else
                    throw new AdapterException("Error communicating with adapter");
            }
            finally
            {

                // release local exclusive use of port
                EndExclusive();
            }
        }

        /// <summary> Set the 1-Wire Network Search to find only iButtons and 1-Wire
        /// devices that are in an 'Alarm' state that signals a need for
        /// attention.  Not all iButton types
        /// have this feature.  Some that do: DS1994, DS1920, DS2407.
        /// This selective searching can be canceled with the
        /// 'setSearchAllDevices()' method.
        /// </summary>
        public override void SetSearchOnlyAlarmingDevices()
        {
            owState.searchOnlyAlarmingButtons = true;
        }

        /// <summary> Set the 1-Wire Network Search to not perform a 1-Wire
        /// reset before a Search.  This feature is chiefly used with
        /// the DS2409 1-Wire coupler.
        /// The normal reset before each Search can be restored with the
        /// 'setSearchAllDevices()' method.
        /// </summary>
        public override void SetNoResetSearch()
        {
            owState.skipResetOnSearch = true;
        }

        /// <summary> Set the 1-Wire Network Search to find all iButtons and 1-Wire
        /// devices whether they are in an 'Alarm' state or not and
        /// restores the default setting of providing a 1-Wire reset
        /// command before each Search. (see setNoResetSearch() method).
        /// </summary>
        public override void SetSearchAllDevices()
        {
            owState.searchOnlyAlarmingButtons = false;
            owState.skipResetOnSearch = false;
        }

        /// <summary> Removes any selectivity during a Search for iButtons or 1-Wire devices
        /// by family type.  The unique address for each iButton and 1-Wire device
        /// contains a family descriptor that indicates the capabilities of the
        /// device.
        /// </summary>
        public override void TargetAllFamilies()
        {

            // clear the include and exclude family Search lists
            owState.searchIncludeFamilies = new byte[0];
            owState.searchExcludeFamilies = new byte[0];
        }

        /// <summary> Takes an integer to selectively Search for this desired family type.
        /// If this method is used, then no devices of other families will be
        /// found by getFirstButton() & getNextButton().
        /// </summary>
        /// <param name="family">  the code of the family type to target for searches
        /// </param>
        public override void TargetFamily(int familyID)
        {
            // replace include family array with 1 element array
            owState.searchIncludeFamilies = new byte[1];
            owState.searchIncludeFamilies[0] = (byte)familyID;
        }

        /// <summary> Takes an array of bytes to use for selectively searching for acceptable
        /// family codes.  If used, only devices with family codes in this array
        /// will be found by any of the Search methods.
        /// </summary>
        /// <param name="family"> array of the family types to target for searches
        /// </param>
        public override void TargetFamily(byte[] familyID)
        {
            // replace include family array with new array
            owState.searchIncludeFamilies = new byte[familyID.Length];

            Array.Copy(familyID, 0, owState.searchIncludeFamilies, 0, familyID.Length);
        }

        /// <summary> Takes an integer family code to avoid when searching for iButtons.
        /// or 1-Wire devices.
        /// If this method is used, then no devices of this family will be
        /// found by any of the Search methods.
        /// </summary>
        /// <param name="family">  the code of the family type NOT to target in searches
        /// </param>
        public override void ExcludeFamily(int familyID)
        {
            // replace exclude family array with 1 element array
            owState.searchExcludeFamilies = new byte[1];
            owState.searchExcludeFamilies[0] = (byte)familyID;
        }

        /// <summary> Takes an array of bytes containing family codes to avoid when finding
        /// iButtons or 1-Wire devices.  If used, then no devices with family
        /// codes in this array will be found by any of the Search methods.
        /// </summary>
        /// <param name="family"> array of family cods NOT to target for searches
        /// </param>
        public override void ExcludeFamily(byte[] familyID)
        {
            // replace exclude family array with new array
            owState.searchExcludeFamilies = new byte[familyID.Length];

            Array.Copy(familyID, 0, owState.searchExcludeFamilies, 0, familyID.Length);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>Normal Search, all devices participate </summary>
        private const byte NORMAL_SEARCH_CMD = (byte)(0xF0);

        /// <summary>Conditional Search, only 'alarming' devices participate </summary>
        private const byte ALARM_SEARCH_CMD = (byte)(0xEC);

        /// <summary> Perform a 'strongAccess' with the provided 1-Wire address.
        /// 1-Wire Network has already been reset and the 'Search'
        /// command sent before this is called.
        /// 
        /// </summary>
        /// <param name="address"> device address to do strongAccess on
        /// </param>
        /// <param name="alarmOnly"> verify device is present and alarming if true
        /// 
        /// </param>
        /// <returns>  true if device participated and was present
        /// in the strongAccess Search
        /// </returns>
        private bool BlockIsPresent(byte[] address, int offset, bool alarmOnly)
        {
            byte[] send_packet = new byte[25];
            int i;

            // reset the 1-Wire
            Reset();

            // send Search command
            if (alarmOnly)
                send_packet[0] = ALARM_SEARCH_CMD;
            //PutByte(ALARM_SEARCH_CMD);
            else
                send_packet[0] = NORMAL_SEARCH_CMD;
            //PutByte(NORMAL_SEARCH_CMD);

            // set all bits at first
            for (i = 1; i < 25; i++)
                send_packet[i] = (byte)0xFF;

            // now set or clear apropriate bits for Search
            // TODO
            for (i = 0; i < 64; i++)
                ArrayWriteBit(
                   ArrayReadBit(i, address, offset), (i + 1) * 3 - 1, send_packet, 1);

            // send to 1-Wire Net
            DataBlock(send_packet, 0, 25);

            // check the results of last 8 triplets (should be no conflicts)
            int cnt = 56, goodbits = 0, tst, s;

            for (i = 168; i < 192; i += 3)
            {
                tst = (ArrayReadBit(i, send_packet, 1) << 1)
                   | ArrayReadBit(i + 1, send_packet, 1);
                s = ArrayReadBit(cnt++, address, offset);

                if (tst == 0x03)
                // no device on line
                {
                    goodbits = 0; // number of good bits set to zero

                    break; // quit
                }

                if (((s == 0x01) && (tst == 0x02)) || ((s == 0x00) && (tst == 0x01)))
                    // correct bit
                    goodbits++; // count as a good bit
            }

            // check too see if there were enough good bits to be successful
            return (goodbits >= 8);
        }

        /// <summary> Writes the bit state in a byte array.
        /// </summary>
        /// <param name="state">new state of the bit 1, 0
        /// </param>
        /// <param name="index">bit index into byte array
        /// </param>
        /// <param name="buf">byte array to manipulate
        /// </param>
        /// <param name="offset">offset into byte array to read from
        /// </param>
        private void ArrayWriteBit(int state, int index,
           byte[] buf, int offset)
        {
            int nbyt = index >> 3;
            int nbit = index - (nbyt << 3);

            if (state == 1)
                buf[nbyt] |= (byte)(0x01 << nbit);
            else
                buf[nbyt] &= (byte)~(0x01 << nbit);
        }

        /// <summary> Reads a bit state in a byte array.
        /// </summary>
        /// <param name="index">bit index into byte array
        /// </param>
        /// <param name="buf">byte array to read from
        /// </param>
        /// <param name="offset">offset into byte array to read from
        /// </param>
        /// <returns> bit state 1 or 0
        /// </returns>
        private int ArrayReadBit(int index,
           byte[] buf, int offset)
        {
            int nbyt = index >> 3;
            int nbit = index - (nbyt << 3);

            return ((buf[nbyt] >> nbit) & 0x01);
        }

        /// <summary>Peform a Search using the oneWire state provided
        /// </summary>
        /// <param name="mState"> current OneWire state used to do the Search
        /// </param>
        private bool Search(OneWireState mState)
        {
            int reset_offset = 0;

            // make sure adapter is present
            if (uAdapterPresent())
            {

                // check for pending power conditions
                if (owState.oneWireLevel != OWLevel.LEVEL_NORMAL)
                    SetPowerNormal();

                // set the correct baud rate to stream this operation
                SetStreamingSpeed(UPacketBuilder.OPERATION_SEARCH);

                // reset the packet
                uBuild.Restart();

                // add a reset/ Search command
                if (!mState.skipResetOnSearch)
                    reset_offset = uBuild.OneWireReset();

                if (mState.searchOnlyAlarmingButtons)
                    uBuild.DataByte((byte)ALARM_SEARCH_CMD);
                else
                    uBuild.DataByte((byte)NORMAL_SEARCH_CMD);

                // add Search sequence based on mState
                int search_offset = uBuild.Search(mState);

                // send/receive the Search
                byte[] result_array = uTransaction(uBuild);

                // interpret Search result and return
                if (!mState.skipResetOnSearch)
                {
                    if (OWResetResult.RESET_NOPRESENCE ==
                       uBuild.InterpretOneWireReset(result_array[reset_offset]))
                    {
                        return false;
                    }
                }

                return uBuild.InterpretSearch(mState, result_array, search_offset);
            }
            else
                throw new AdapterException("Error communicating with adapter");
        }


        /// <summary> set the correct baud rate to stream this operation</summary>
        private void SetStreamingSpeed(int desiredOperation)
        {
            // get the desired baud rate for this operation
            BaudRates baud = UPacketBuilder.GetDesiredBaud(
               desiredOperation, owState.oneWireSpeed, maxBaud);

            // check if already at the correct speed
            if (baud == serial.BaudRate)
                return;

            //         if (doDebugMessages)
            //            System.Console.Out.WriteLine("Changing baud rate from " + serial.BaudRate + " to " + baud);

            // convert this baud to 'u' baud
            AdapterBaud ubaud;

            switch (baud)
            {
                case BaudRates.CBR_115200:
                    ubaud = AdapterBaud.BAUD_115200;
                    break;

                case BaudRates.CBR_57600:
                    ubaud = AdapterBaud.BAUD_57600;
                    break;

                case BaudRates.CBR_19200:
                    ubaud = AdapterBaud.BAUD_19200;
                    break;

                case BaudRates.CBR_9600:
                default:
                    ubaud = AdapterBaud.BAUD_9600;
                    break;
            }

            // see if this is a new baud
            if (ubaud == uState.ubaud)
                return;

            // default, loose communication with adapter
            adapterPresent = false;

            // build a message to read the baud rate from the U brick
            uBuild.Restart();

            int baud_offset = uBuild.SetParameter(ubaud);

            try
            {
                // send command, no response at this baud rate
                //serial.Flush();

                System.Collections.IEnumerator pkts = uBuild.Packets;
                pkts.MoveNext();
                RawSendPacket pkt = (RawSendPacket)pkts.Current;
                byte[] temp_buf = new byte[pkt.dataList.Count];
                pkt.dataList.CopyTo(temp_buf);

                serial.Write(temp_buf);

                // delay to let things settle
                sleep(5);
                serial.Flush();

                // set the baud rate
                sleep(5); //solaris hack!!!
                serial.BaudRate = baud;
            }
            catch (System.IO.IOException ioe)
            {
                throw new AdapterException(ioe);
            }

            uState.ubaud = ubaud;

            // delay to let things settle
            sleep(5);

            // verify adapter is at new baud rate
            uBuild.Restart();

            baud_offset = uBuild.GetParameter(Parameter.PARAMETER_BAUDRATE);

            // set the DS2480 communication speed for subsequent blocks
            uBuild.SetSpeed();

            try
            {

                // send and receive
                //serial.Flush();

                byte[] result_array = uTransaction(uBuild);

                // check the result
                if (result_array.Length == 1)
                {
                    if (((result_array[baud_offset] & 0xF1) == 0) && ((result_array[baud_offset] & 0x0E) == (byte)uState.ubaud))
                    {
                        //                  if (doDebugMessages)
                        //                     System.Console.Out.WriteLine("Success, baud changed and DS2480 is there");

                        // adapter still with us
                        adapterPresent = true;

                        // flush any garbage characters
                        sleep(150);
                        serial.Flush();

                        return;
                    }
                }
            }
            catch (System.IO.IOException)
            {
                //            if (doDebugMessages)
                //            {
                //               System.Console.Error.WriteLine("SerialAdapter-setStreamingSpeed: " + ioe);
                //            }
            }
            catch (AdapterException)
            {
                //            if (doDebugMessages)
                //            {
                //               System.Console.Error.WriteLine("SerialAdapter-setStreamingSpeed: " + ae);
                //            }
            }

            //         if (doDebugMessages)
            //            System.Console.Out.WriteLine("Failed to change baud of DS2480");
        }


        /// <summary> Verify that the DS2480 based adapter is present on the open port.
        /// 
        /// </summary>
        /// <returns> 'true' if adapter present
        /// 
        /// @throws AdapterException - if port not selected
        /// </returns>
        private bool uAdapterPresent()
        {
            bool rt = true;

            // check if adapter has already be verified to be present
            if (!adapterPresent)
            {

                // do a master reset
                uMasterReset();

                // attempt to verify
                if (!uVerify())
                {

                    // do a master reset and try again
                    uMasterReset();

                    if (!uVerify())
                    {

                        // do a power reset and try again
                        uPowerReset();

                        if (!uVerify())
                            rt = false;
                    }
                }
            }

            adapterPresent = rt;

            //         if (doDebugMessages)
            //            System.Console.Error.WriteLine("DEBUG: AdapterPresent result: " + rt);

            return rt;
        }

        /// <summary> Do a master reset on the DS2480.  This reduces the baud rate to
        /// 9600 and peforms a break.  A single timing byte is then sent.
        /// </summary>
        private void uMasterReset()
        {
            //         if (doDebugMessages)
            //            System.Console.Error.WriteLine("DEBUG: uMasterReset");

            // try to aquire the port
            try
            {

                // set the baud rate
                serial.BaudRate = BaudRates.CBR_9600;

                uState.ubaud = AdapterBaud.BAUD_9600;

                // put back to standard speed
                owState.oneWireSpeed = OWSpeed.SPEED_REGULAR;
                uState.uSpeedMode = UAdapterState.USPEED_FLEX;
                uState.ubaud = AdapterBaud.BAUD_9600;

                // send a break to reset DS2480
                serial.SendBreak(20);
                sleep(5);

                // send the timing byte
                //serial.Flush();
                serial.Write(UPacketBuilder.FUNCTION_RESET);
                serial.Flush();
            }
            catch (System.IO.IOException)
            {
                //            if (doDebugMessages)
                //            {
                //               System.Console.Error.WriteLine("USerialAdapter-uMasterReset: " + e);
                //            }
            }
        }

        /// <summary>  Do a power reset on the DS2480.  This reduces the baud rate to
        /// 9600 and powers down the DS2480.  A single timing byte is then sent.
        /// </summary>
        private void uPowerReset()
        {
            //         if (doDebugMessages)
            //            System.Console.Error.WriteLine("DEBUG: uPowerReset");

            // try to aquire the port
            try
            {

                // set the baud rate
                serial.BaudRate = BaudRates.CBR_9600;

                uState.ubaud = AdapterBaud.BAUD_9600;

                // put back to standard speed
                owState.oneWireSpeed = OWSpeed.SPEED_REGULAR;
                uState.uSpeedMode = UAdapterState.USPEED_FLEX;
                uState.ubaud = AdapterBaud.BAUD_9600;

                // power down DS2480
                serial.DTREnable = false;
                serial.RTSEnable = false;
                sleep(300);
                serial.DTREnable = true;
                serial.RTSEnable = true;
                sleep(1);

                // send the timing byte
                //serial.Flush();
                serial.Write(UPacketBuilder.FUNCTION_RESET);
                serial.Flush();
            }
            catch (System.IO.IOException)
            {
                //            if (doDebugMessages)
                //            {
                //               System.Console.Error.WriteLine("USerialAdapter-uPowerReset: " + e);
                //            }
            }
        }

        /// <summary>  Read and verify the baud rate with the DS2480 chip and perform a
        /// single bit MicroLAN operation.  This is used as a DS2480 detect.
        /// 
        /// </summary>
        /// <returns>  'true' if the correct baud rate and bit operation
        /// was read from the DS2480
        /// 
        /// @throws AdapterException on a 1-Wire communication error
        /// </returns>
        private bool uVerify()
        {
            try
            {
                //serial.Flush();

                // build a message to read the baud rate from the U brick
                uBuild.Restart();

                uBuild.SetParameter(
                   uState.uParameters[(int)owState.oneWireSpeed].pullDownSlewRate);
                uBuild.SetParameter(
                   uState.uParameters[(int)owState.oneWireSpeed].write1LowTime);
                uBuild.SetParameter(
                   uState.uParameters[(int)owState.oneWireSpeed].sampleOffsetTime);
                uBuild.SetParameter(
                   ProgramPulseTime5.TIME5V_infinite);
                int baud_offset = uBuild.GetParameter(Parameter.PARAMETER_BAUDRATE);
                int bit_offset = uBuild.DataBit(true, false);

                // send and receive
                byte[] result_array = uTransaction(uBuild);

                // check the result
                if (result_array.Length == (bit_offset + 1))
                {
                    if (((result_array[baud_offset] & 0xF1) == 0) && ((result_array[baud_offset] & 0x0E) == (byte)uState.ubaud) && ((result_array[bit_offset] & 0xF0) == 0x90) && ((result_array[bit_offset] & 0x0C) == uState.uSpeedMode))
                        return true;
                }
            }
            catch (System.IO.IOException)
            {
                //            if (doDebugMessages)
                //            {
                //               System.Console.Error.WriteLine("USerialAdapter-uVerify: " + ioe);
                //            }
            }
            catch (AdapterException)
            {
                //            if (doDebugMessages)
                //            {
                //               System.Console.Error.WriteLine("USerialAdapter-uVerify: " + e);
                //            }
            }

            return false;
        }

        /// <summary> Write the raw U packet and then read the result.
        /// 
        /// </summary>
        /// <param name="tempBuild"> the U Packet Build where the packet to send
        /// resides
        /// 
        /// </param>
        /// <returns>  the result array
        /// 
        /// @throws AdapterException on a 1-Wire communication error
        /// </returns>
        private byte[] uTransaction(UPacketBuilder tempBuild)
        {
            int offset;

            try
            {
                // clear the buffers
                //serial.Flush();
                inBuffer.Clear();

                // loop to send all of the packets
                for (System.Collections.IEnumerator packet_enum = tempBuild.Packets; packet_enum.MoveNext(); )
                {

                    // get the next packet
                    RawSendPacket pkt = (RawSendPacket)packet_enum.Current;

                    // bogus packet to indicate need to wait for long DS2480 alarm reset
                    if ((pkt.dataList.Count == 0) && (pkt.returnLength == 0))
                    {
                        sleep(6);
                        serial.Flush();

                        continue;
                    }

                    // get the data
                    byte[] temp_buf = new byte[pkt.dataList.Count];
                    pkt.dataList.CopyTo(temp_buf);

                    // remember number of bytes in input
                    offset = inBuffer.Count;

                    // send the packet
                    serial.Write(temp_buf);

                    // wait on returnLength bytes in inBound
                    inBuffer.AddRange(serial.Read(pkt.returnLength));
                }

                // read the return packet
                byte[] ret_buffer = new byte[inBuffer.Count];
                inBuffer.CopyTo(ret_buffer);

                // check for extra bytes in inBuffer
                extraBytesReceived = (inBuffer.Count > tempBuild.totalReturnLength);

                // clear the inbuffer
                inBuffer.Clear();

                return ret_buffer;
            }
            catch (System.IO.IOException e)
            {

                // need to check on adapter
                adapterPresent = false;

                // pass it on
                throw new AdapterException(e.Message);
            }
        }


        /// <summary> Sleep for the specified number of milliseconds</summary>
        private void sleep(int msTime)
        {

            // provided debug on standard out
            //         if (doDebugMessages)
            //            System.Console.Error.WriteLine("DEBUG: sleep(" + msTime + ")");

            System.Threading.Thread.Sleep(msTime);
        }
        #endregion
    }


    /// <summary>UPacketBuilder contains the methods to build a communication packet
    /// to the DS2480 based serial adapter.
    /// </summary>
    /// <version>0.00</version>
    /// <author>DS, SH</author>
    internal class UPacketBuilder
    {
        /// <summary> Retrieve enumeration of raw send packets
        /// 
        /// </summary>
        /// <returns>  the enumeration of packets
        /// </returns>
        public System.Collections.IEnumerator Packets
        {
            get
            {

                // put the last packet into the vector if it is non zero
                if (packet.dataList.Count > 0)
                    NewPacket();

                return packetsVector.GetEnumerator();
            }

        }

        //--------
        //-------- Finals
        //--------
        //-------- Misc

        /// <summary>Byte operation                                     </summary>
        public const int OPERATION_BYTE = 0;

        /// <summary>Byte operation                                     </summary>
        public const int OPERATION_SEARCH = 1;

        /// <summary>Max bytes to stream at once  </summary>
        public const byte MAX_BYTES_STREAMED = (byte)(64);

        //-------- DS9097U function commands

        /// <summary>DS9097U funciton command, single bit               </summary>
        public const byte FUNCTION_BIT = (byte)(0x81);

        /// <summary>DS9097U funciton command, turn Search mode on      </summary>
        public const byte FUNCTION_SEARCHON = (byte)(0xB1);

        /// <summary>DS9097U funciton command, turn Search mode off     </summary>
        public const byte FUNCTION_SEARCHOFF = (byte)(0xA1);

        /// <summary>DS9097U funciton command, OneWire reset            </summary>
        public const byte FUNCTION_RESET = (byte)(0xC1);

        /// <summary>DS9097U funciton command, 5V pulse imediate        </summary>
        public const byte FUNCTION_5VPULSE_NOW = (byte)(0xED);

        /// <summary>DS9097U funciton command, 12V pulse imediate        </summary>
        public const byte FUNCTION_12VPULSE_NOW = (byte)(0xFD);

        /// <summary>DS9097U funciton command, 5V pulse after next byte </summary>
        public const byte FUNCTION_5VPULSE_ARM = (byte)(0xEF);

        /// <summary>DS9097U funciton command to stop an ongoing pulse  </summary>
        public const byte FUNCTION_STOP_PULSE = (byte)(0xF1);

        //-------- DS9097U bit polarity settings for doing bit operations

        /// <summary>DS9097U bit polarity one for function FUNCTION_BIT   </summary>
        public const byte BIT_ONE = (byte)(0x10);

        /// <summary>DS9097U bit polarity zero  for function FUNCTION_BIT </summary>
        public const byte BIT_ZERO = (byte)(0x00);

        //-------- DS9097U 5V priming values 

        /// <summary>DS9097U 5V prime on for function FUNCTION_BIT    </summary>
        public const byte PRIME5V_TRUE = (byte)(0x02);

        /// <summary>DS9097U 5V prime off for function FUNCTION_BIT   </summary>
        public const byte PRIME5V_FALSE = (byte)(0x00);

        //-------- DS9097U command masks 

        /// <summary>DS9097U mask to read or write a configuration parameter   </summary>
        public const byte CONFIG_MASK = (byte)(0x01);

        /// <summary>DS9097U mask to read the OneWire reset response byte </summary>
        public const byte RESPONSE_RESET_MASK = (byte)(0x03);

        //-------- DS9097U reset results 

        /// <summary>DS9097U  OneWire reset result = shorted </summary>
        public const byte RESPONSE_RESET_SHORT = (byte)(0x00);

        /// <summary>DS9097U  OneWire reset result = presence </summary>
        public const byte RESPONSE_RESET_PRESENCE = (byte)(0x01);

        /// <summary>DS9097U  OneWire reset result = alarm </summary>
        public const byte RESPONSE_RESET_ALARM = (byte)(0x02);

        /// <summary>DS9097U  OneWire reset result = no presence </summary>
        public const byte RESPONSE_RESET_NOPRESENCE = (byte)(0x03);

        //-------- DS9097U bit interpretation 

        /// <summary>DS9097U mask to read bit operation result   </summary>
        public const byte RESPONSE_BIT_MASK = (byte)(0x03);

        /// <summary>DS9097U read bit operation 1 </summary>
        public const byte RESPONSE_BIT_ONE = (byte)(0x03);

        /// <summary>DS9097U read bit operation 0 </summary>
        public const byte RESPONSE_BIT_ZERO = (byte)(0x00);

        /// <summary>Enable/disable debug messages                   </summary>
        public static bool doDebugMessages = false;

        //--------
        //-------- Variables
        //--------

        /// <summary> The current state of the U brick, passed into constructor.</summary>
        private UAdapterState uState;

        /// <summary> The current current count for the number of return bytes from
        /// the packet being created.
        /// </summary>
        protected internal int totalReturnLength;

        /// <summary> Current raw send packet before it is added to the packetsVector</summary>
        protected internal RawSendPacket packet;

        /// <summary> Vector of raw send packets</summary>
        protected internal System.Collections.ArrayList packetsVector;

        /// <summary> Flag to send only 'bit' commands to the DS2480</summary>
        protected internal bool bitsOnly;

        //--------
        //-------- Constructors
        //--------

        /// <summary> Constructs a new u packet builder.
        /// 
        /// </summary>
        /// <param name="startUState">  the object that contains the U brick state
        /// which is reference when creating packets
        /// </param>
        public UPacketBuilder(UAdapterState startUState)
        {

            // get a reference to the U state
            uState = startUState;

            // create the buffer for the data
            packet = new RawSendPacket();

            // create the vector
            packetsVector = new System.Collections.ArrayList(10);

            // Restart the packet to initialize
            Restart();

            // Default on SunOS to bit-banging
            //UPGRADE_ISSUE: Method 'java.lang.System.getProperty' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javalangSystemgetProperty_javalangString"'
            //bitsOnly = (System_Renamed.getProperty("os.name").IndexOf("SunOS") != - 1);

            // check for a bits only property
            // TODO: set bit-banging property
            System.String bits = null;//OneWireAccessProvider.getProperty("onewire.serial.forcebitsonly");
            if ((System.Object)bits != null)
            {
                if (bits.IndexOf("true") != -1)
                    bitsOnly = true;
                else if (bits.IndexOf("false") != -1)
                    bitsOnly = false;
            }
        }

        //--------
        //-------- Packet handling Methods 
        //--------

        /// <summary> Reset the packet builder to start a new one.</summary>
        public virtual void Restart()
        {
            // clear the vector list of packets
            System.Collections.ArrayList temp_arraylist;
            temp_arraylist = packetsVector;
            temp_arraylist.RemoveRange(0, temp_arraylist.Count);

            // truncate the packet to 0 length
            packet.dataList.Clear();

            packet.returnLength = 0;

            // reset the return cound
            totalReturnLength = 0;
        }

        /// <summary> Take the current packet and place it into the vector.  This
        /// indicates a place where we need to wait for the results from
        /// DS9097U adapter.
        /// </summary>
        public virtual void NewPacket()
        {

            // add the packet
            packetsVector.Add(packet);

            // get a new packet
            packet = new RawSendPacket();
        }

        //--------
        //-------- 1-Wire Network operation append methods 
        //--------

        /// <summary>Add the command to reset the OneWire at the current speed.
        /// 
        /// </summary>
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int OneWireReset()
        {
            // set to command mode
            SetToCommandMode();

            // append the reset command at the current speed
            packet.dataList.Add((byte)(FUNCTION_RESET | uState.uSpeedMode));

            // count this as a return 
            totalReturnLength++;
            packet.returnLength++;

            // check if not streaming resets
            if (!uState.streamResets)
                NewPacket();

            // check for 2480 wait on extra bytes packet 
            if (uState.longAlarmCheck && ((uState.uSpeedMode == UAdapterState.USPEED_REGULAR) || (uState.uSpeedMode == UAdapterState.USPEED_FLEX)))
                NewPacket();

            return totalReturnLength - 1;
        }

        /// <summary> Append data bytes (read/write) to the packet.
        /// 
        /// </summary>
        /// <param name="dataBytesValue"> character array of data bytes
        /// 
        /// </param>
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int DataBytes(byte[] dataBytesValue)
        {
            byte byte_value;
            int i, j;

            // set to data mode
            if (!bitsOnly)
                SetToDataMode();

            //         // provide debug output
            //         if (doDebugMessages)
            //            System.Console.Error.WriteLine("DEBUG: UPacketbuilder-DataBytes[] length " + dataBytesValue.Length);
            //       
            // record the current count location 
            int ret_value = totalReturnLength;

            // check each byte to see if some need duplication
            for (i = 0; i < dataBytesValue.Length; i++)
            {
                // convert the rest to AdapterExceptions
                if (bitsOnly)
                {
                    // change byte to bits
                    byte_value = dataBytesValue[i];
                    for (j = 0; j < 8; j++)
                    {
                        DataBit(((byte_value & 0x01) == 0x01), false);
                        byte_value = (byte)(byte_value >> 1);
                    }
                }
                else
                {
                    // append the data
                    packet.dataList.Add(dataBytesValue[i]);

                    // provide debug output
                    //               if (doDebugMessages)
                    //                  System.Console.Error.WriteLine("DEBUG: UPacketbuilder-DataBytes[] byte[" + System.Convert.ToString((int) dataBytesValue[i] & 0x00FF, 16) + "]");

                    // check for duplicates needed for special characters  
                    if (((byte)(dataBytesValue[i] & 0x00FF) == UAdapterState.MODE_COMMAND) || (((byte)(dataBytesValue[i] & 0x00FF) == UAdapterState.MODE_SPECIAL) && (uState.revision == UAdapterState.CHIP_VERSION1)))
                    {
                        // duplicate this data byte
                        packet.dataList.Add(dataBytesValue[i]);
                    }

                    // add to the return number of bytes
                    totalReturnLength++;
                    packet.returnLength++;

                    // provide debug output
                    //               if (doDebugMessages)
                    //                  System.Console.Error.WriteLine("DEBUG: UPacketbuilder-DataBytes[] returnlength " + packet.returnLength + " bufferLength " + packet.dataList.Count);

                    // check for packet too large or not streaming bytes
                    if ((packet.dataList.Count > MAX_BYTES_STREAMED) || !uState.streamBytes)
                        NewPacket();
                }
            }

            return ret_value;
        }

        /// <summary> Append data bytes (read/write) to the packet.
        /// 
        /// </summary>
        /// <param name="dataBytesValue"> byte array of data bytes
        /// </param>
        /// <param name="off">  offset into the array of data to start
        /// </param>
        /// <param name="len">  length of data to send / receive starting at 'off'
        /// 
        /// </param>
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int DataBytes(byte[] dataBytesValue, int off, int len)
        {
            byte[] tmpBuf = new byte[len];
            Array.Copy(dataBytesValue, off, tmpBuf, 0, len);

            return DataBytes(tmpBuf);
        }

        /// <summary> Append a data byte (read/write) to the packet.
        /// 
        /// </summary>
        /// <param name="dataByteValue"> data byte to append
        /// 
        /// </param>
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int DataByte(byte dataByteValue)
        {

            // contruct a temporary array of characters of lenght 1
            // to use the DataBytes method
            byte[] tmpBuff = new byte[1];

            tmpBuff[0] = dataByteValue;

            // provide debug output
            //         if (doDebugMessages)
            //            System.Console.Error.WriteLine("DEBUG: UPacketbuilder-DataBytes [" + System.Convert.ToString((int) dataByteValue & 0x00FF, 16) + "]");

            return DataBytes(tmpBuff);
        }

        /// <summary> Append a data byte (read/write) to the packet.  Do a strong pullup
        /// when the byte is complete
        /// 
        /// </summary>
        /// <param name="dataByteValue"> data byte to append
        /// 
        /// </param>
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int PrimedDataByte(byte dataByteValue)
        {
            int offset, start_offset = 0;

            // create a primed data byte by using bits with last one primed
            for (int i = 0; i < 8; i++)
            {
                offset = DataBit(((dataByteValue & 0x01) == 0x01), (i == 7));
                dataByteValue = (byte)(dataByteValue >> 1);

                // record the starting offset
                if (i == 0)
                    start_offset = offset;
            }

            return start_offset;
        }

        /// <summary> Append a data bit (read/write) to the packet.
        /// 
        /// </summary>
        /// <param name="DataBit">  bit to append
        /// </param>
        /// <param name="strong5V"> true if want strong5V after bit
        /// 
        /// </param>
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int DataBit(bool dataBit, bool strong5V)
        {

            // set to command mode
            SetToCommandMode();

            // append the bit with polarity and strong5V options
            packet.dataList.Add((byte)(FUNCTION_BIT | uState.uSpeedMode | ((dataBit) ? BIT_ONE : BIT_ZERO) | ((strong5V) ? PRIME5V_TRUE : PRIME5V_FALSE)));

            // add to the return number of bytes
            totalReturnLength++;
            packet.returnLength++;

            // check for packet too large or not streaming bits
            if ((packet.dataList.Count > MAX_BYTES_STREAMED) || !uState.streamBits)
                NewPacket();

            return (totalReturnLength - 1);
        }

        /// <summary> Append a Search to the packet.  Assume that any reset and Search
        /// command have already been appended.  This will add only the Search
        /// itself.
        /// 
        /// </summary>
        /// <param name="mState">OneWire state
        /// 
        /// </param>
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int Search(OneWireState mState)
        {

            // set to command mode
            SetToCommandMode();

            // Search mode on
            packet.dataList.Add((byte)(FUNCTION_SEARCHON | uState.uSpeedMode));

            // set to data mode
            SetToDataMode();

            // create the Search sequence character array
            byte[] search_sequence = new byte[16];

            // get a copy of the current ID
            byte[] id = new byte[8];

            for (int i = 0; i < 8; i++)
                id[i] = (byte)(mState.ID[i] & 0xFF);

            // clear the string
            for (int i = 0; i < 16; i++)
                search_sequence[i] = (byte)(0);

            // provide debug output
            //         if (doDebugMessages)
            //            System.Console.Error.WriteLine("DEBUG: UPacketbuilder-Search [" + System.Convert.ToString((int) id.Length, 16) + "]");

            // only modify bits if not the first Search
            if (mState.searchLastDiscrepancy != 0xFF)
            {

                // set the bits in the added buffer
                for (int i = 0; i < 64; i++)
                {

                    // before last discrepancy (go direction based on ID)
                    if (i < (mState.searchLastDiscrepancy - 1))
                        BitWrite(search_sequence, (i * 2 + 1), BitRead(id, i));
                    // at last discrepancy (go 1's direction)
                    else if (i == (mState.searchLastDiscrepancy - 1))
                        BitWrite(search_sequence, (i * 2 + 1), true);

                    // after last discrepancy so leave zeros
                }
            }

            // remember this position
            int return_position = totalReturnLength;

            // add this sequence
            packet.dataList.AddRange(search_sequence);

            // set to command mode
            SetToCommandMode();

            // Search mode off
            packet.dataList.Add((byte)(FUNCTION_SEARCHOFF | uState.uSpeedMode));

            // add to the return number of bytes
            totalReturnLength += 16;
            packet.returnLength += 16;

            return return_position;
        }

        /// <summary> Append a Search off to set the current speed.</summary>
        public virtual void SetSpeed()
        {

            // set to command mode
            SetToCommandMode();

            // Search mode off and change speed
            packet.dataList.Add((byte)(FUNCTION_SEARCHOFF | uState.uSpeedMode));

            // no return byte
        }

        //--------
        //-------- U mode commands 
        //--------

        /// <summary> Set the U state to command mode.</summary>
        public virtual void SetToCommandMode()
        {
            if (!uState.inCommandMode)
            {

                // append the command to switch
                packet.dataList.Add((byte)UAdapterState.MODE_COMMAND);

                // switch the state
                uState.inCommandMode = true;
            }
        }

        /// <summary> Set the U state to data mode.</summary>
        public virtual void SetToDataMode()
        {
            if (uState.inCommandMode)
            {

                // append the command to switch
                packet.dataList.Add((byte)UAdapterState.MODE_DATA);

                // switch the state
                uState.inCommandMode = false;
            }
        }

        /// <summary> Append a get parameter to the packet.
        /// 
        /// </summary>
        /// <param name="parameter"> parameter to get
        /// 
        /// </param>
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int GetParameter(Parameter parameter)
        {

            // set to command mode
            SetToCommandMode();

            // append paramter get
            packet.dataList.Add((byte)(CONFIG_MASK | ((byte)parameter) >> 3));

            // add to the return number of bytes
            totalReturnLength++;
            packet.returnLength++;

            // check for packet too large
            if (packet.dataList.Count > MAX_BYTES_STREAMED)
                NewPacket();

            return (totalReturnLength - 1);
        }

        /// <summary> Append a set parameter to the packet.</summary>
        /// <param name="parameterValue"> parameter value
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int SetParameter(SlewRate parameterValue)
        {
            return SetParameter(
               Parameter.PARAMETER_SLEW, (byte)parameterValue);
        }
        /// <summary> Append a set parameter to the packet.</summary>
        /// <param name="parameterValue"> parameter value
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int SetParameter(ProgramPulseTime12 parameterValue)
        {
            return SetParameter(
               Parameter.PARAMETER_12VPULSE, (byte)parameterValue);
        }
        /// <summary> Append a set parameter to the packet.</summary>
        /// <param name="parameterValue"> parameter value
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int SetParameter(ProgramPulseTime5 parameterValue)
        {
            return SetParameter(
               Parameter.PARAMETER_5VPULSE, (byte)parameterValue);
        }
        /// <summary> Append a set parameter to the packet.</summary>
        /// <param name="parameterValue"> parameter value
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int SetParameter(WriteOneLowTime parameterValue)
        {
            return SetParameter(
               Parameter.PARAMETER_WRITE1LOW, (byte)parameterValue);
        }
        /// <summary> Append a set parameter to the packet.</summary>
        /// <param name="parameterValue"> parameter value
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int SetParameter(SampleOffsetTime parameterValue)
        {
            return SetParameter(
               Parameter.PARAMETER_SAMPLEOFFSET, (byte)parameterValue);
        }
        /// <summary> Append a set parameter to the packet.</summary>
        /// <param name="parameterValue"> parameter value
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        public virtual int SetParameter(AdapterBaud parameterValue)
        {
            return SetParameter(
               Parameter.PARAMETER_BAUDRATE, (byte)parameterValue);
        }
        /// <summary> Append a set parameter to the packet.</summary>
        /// <param name="parameter">      parameter to set
        /// </param>
        /// <param name="parameterValue"> parameter value
        /// 
        /// </param>
        /// <returns> the number offset in the return packet to get the
        /// result of this operation
        /// </returns>
        int SetParameter(Parameter parameter, byte parameterValue)
        {

            // set to command mode
            SetToCommandMode();

            // append the paramter set with value
            packet.dataList.Add((byte)((CONFIG_MASK | (byte)parameter) | parameterValue));

            // add to the return number of bytes
            totalReturnLength++;
            packet.returnLength++;

            // check for packet too large
            if (packet.dataList.Count > MAX_BYTES_STREAMED)
                NewPacket();

            return (totalReturnLength - 1);
        }

        /// <summary> Append a send command to the packet.  This command does not
        /// elicit a response byte.
        /// 
        /// </summary>
        /// <param name="command">      command to send
        /// </param>
        /// <param name="">expectResponse
        /// 
        /// </param>
        /// <returns> the number offset in the return packet to get the
        /// result of this operation (if there is one)
        /// </returns>
        public virtual int SendCommand(byte command, bool expectResponse)
        {

            // set to command mode
            SetToCommandMode();

            // append the paramter set with value
            packet.dataList.Add((byte)command);

            // check for response
            if (expectResponse)
            {

                // add to the return number of bytes
                totalReturnLength++;
                packet.returnLength++;
            }

            // check for packet too large
            if (packet.dataList.Count > MAX_BYTES_STREAMED)
                NewPacket();

            return (totalReturnLength - 1);
        }

        //--------
        //-------- 1-Wire Network result interpretation methods 
        //--------

        /// <summary> Interpret the block of bytes 
        /// 
        /// </summary>
        /// <param name="dataByteResponse"> 
        /// </param>
        /// <param name="">responseOffset
        /// </param>
        /// <param name="">result
        /// </param>
        /// <param name="">offset
        /// </param>
        /// <param name="">len
        /// </param>
        public virtual void InterpretDataBytes(byte[] dataByteResponse, int responseOffset, byte[] result, int offset, int len)
        {
            byte result_byte;
            int temp_offset, i, j;

            for (i = 0; i < len; i++)
            {
                // convert the rest to AdapterExceptions
                if (bitsOnly)
                {
                    temp_offset = responseOffset + 8 * i;

                    // provide debug output
                    //               if (doDebugMessages)
                    //                  System.Console.Error.WriteLine("DEBUG: UPacketbuilder-InterpretDataBytes[] responseOffset " + responseOffset + " offset " + offset + " lenbuf " + dataByteResponse.Length);

                    // loop through and interpret each bit
                    result_byte = 0;
                    for (j = 0; j < 8; j++)
                    {
                        result_byte = (byte)(result_byte >> 1);

                        if (InterpretOneWireBit(dataByteResponse[temp_offset + j]))
                            result_byte |= 0x80;
                    }

                    result[offset + i] = (byte)(result_byte & 0xFF);
                }
                else
                    result[offset + i] = (byte)dataByteResponse[responseOffset + i];
            }
        }

        /// <summary> Interpret the reset response byte from a U adapter
        /// 
        /// </summary>
        /// <param name="resetResponse"> reset response byte from U
        /// 
        /// </param>
        /// <returns> the number representing the result of a 1-Wire reset
        /// </returns>
        public virtual OWResetResult InterpretOneWireReset(byte resetResponse)
        {

            if ((resetResponse & 0xC0) == 0xC0)
            {

                // retrieve the chip version and program voltage state
                uState.revision = (byte)(UAdapterState.CHIP_VERSION_MASK & resetResponse);
                uState.programVoltageAvailable = ((UAdapterState.PROGRAM_VOLTAGE_MASK & resetResponse) != 0);

                // provide debug output
                //            if (doDebugMessages)
                //               System.Console.Error.WriteLine("DEBUG: UPacketbuilder-reset response " + System.Convert.ToString((int) resetResponse & 0x00FF, 16));

                // convert the response byte to the OneWire reset result
                switch (resetResponse & RESPONSE_RESET_MASK)
                {
                    case RESPONSE_RESET_SHORT:
                        return OWResetResult.RESET_SHORT;

                    case RESPONSE_RESET_PRESENCE:
                        if (uState.longAlarmCheck)
                        {

                            // check if can give up checking
                            if (uState.lastAlarmCount++ > UAdapterState.MAX_ALARM_COUNT)
                                uState.longAlarmCheck = false;
                        }

                        return OWResetResult.RESET_PRESENCE;

                    case RESPONSE_RESET_ALARM:
                        uState.longAlarmCheck = true;
                        uState.lastAlarmCount = 0;

                        return OWResetResult.RESET_ALARM;

                    default:
                        return OWResetResult.RESET_NOPRESENCE;
                    case RESPONSE_RESET_NOPRESENCE:
                        return OWResetResult.RESET_NOPRESENCE;
                }
            }
            else
                return OWResetResult.RESET_NOPRESENCE;
        }

        /// <summary> Interpret the bit response byte from a U adapter
        /// 
        /// </summary>
        /// <param name="bitResponse"> bit response byte from U
        /// 
        /// </param>
        /// <returns> boolean representing the result of a 1-Wire bit operation
        /// </returns>
        public virtual bool InterpretOneWireBit(byte bitResponse)
        {

            // interpret the bit
            if ((bitResponse & RESPONSE_BIT_MASK) == RESPONSE_BIT_ONE)
                return true;
            else
                return false;
        }

        /// <summary> Interpret the Search response and set the 1-Wire state accordingly.
        /// 
        /// </summary>
        /// <param name="bitResponse"> bit response byte from U
        /// 
        /// </param>
        /// <param name="">mState
        /// </param>
        /// <param name="">searchResponse
        /// </param>
        /// <param name="">responseOffset
        /// 
        /// </param>
        /// <returns> boolean return is true if a valid ID was found when
        /// interpreting the Search results
        /// </returns>
        public virtual bool InterpretSearch(OneWireState mState, byte[] searchResponse, int responseOffset)
        {
            byte[] temp_id = new byte[8];

            // change byte offset to bit offset
            int bit_offset = responseOffset * 8;

            // set the temp Last Descrep to none
            int temp_last_descrepancy = 0xFF;
            int temp_last_family_descrepancy = 0;

            // interpret the Search response sequence
            for (int i = 0; i < 64; i++)
            {

                // get the SerialNum bit
                BitWrite(temp_id, i, BitRead(searchResponse, (i * 2) + 1 + bit_offset));

                // check LastDiscrepancy
                if (BitRead(searchResponse, i * 2 + bit_offset) && !BitRead(searchResponse, i * 2 + 1 + bit_offset))
                {
                    temp_last_descrepancy = i + 1;

                    // check LastFamilyDiscrepancy
                    if (i < 8)
                        temp_last_family_descrepancy = i + 1;
                }
            }

            // check
            byte[] id = new byte[8];

            for (int i = 0; i < 8; i++)
                id[i] = (byte)temp_id[i];

            // check results
            if ((!IsValidRomID(id, 0)) || (temp_last_descrepancy == 63))
                return false;

            // check for lastone
            if ((temp_last_descrepancy == mState.searchLastDiscrepancy) || (temp_last_descrepancy == 0xFF))
                mState.searchLastDevice = true;

            // copy the ID number to the buffer
            for (int i = 0; i < 8; i++)
                mState.ID[i] = (byte)temp_id[i];

            // set the count
            mState.searchLastDiscrepancy = temp_last_descrepancy;
            mState.searchFamilyLastDiscrepancy = temp_last_family_descrepancy;

            return true;
        }

        private bool IsValidRomID(byte[] address, int offset)
        {
            return ((address[offset] != 0)
               && (CRC8.Compute(address, offset, 8, 0) == 0));
        }

        /// <summary> Interpret the data response byte from a primed byte operation
        /// 
        /// </summary>
        /// <param name="">primedDataResponse
        /// </param>
        /// <param name="">responseOffset
        /// 
        /// </param>
        /// <returns> the byte representing the result of a 1-Wire data byte
        /// </returns>
        public virtual byte InterpretPrimedByte(byte[] primedDataResponse, int responseOffset)
        {
            byte result_byte = 0;

            // loop through and interpret each bit
            for (int i = 0; i < 8; i++)
            {
                result_byte = (byte)(result_byte >> 1);

                if (InterpretOneWireBit(primedDataResponse[responseOffset + i]))
                    result_byte |= 0x80;
            }

            return (byte)(result_byte & 0xFF);
        }

        //--------
        //-------- Misc Utility methods 
        //--------

        /// <summary> Request the maximum rate to do an operation</summary>
        public static BaudRates GetDesiredBaud(int operation, OWSpeed owSpeed, BaudRates maxBaud)
        {
            BaudRates baud = BaudRates.CBR_9600;

            switch (operation)
            {
                case OPERATION_BYTE:
                    if (owSpeed == OWSpeed.SPEED_OVERDRIVE)
                        baud = BaudRates.CBR_115200;
                    else
                        baud = BaudRates.CBR_9600;
                    break;

                case OPERATION_SEARCH:
                    if (owSpeed == OWSpeed.SPEED_OVERDRIVE)
                        baud = BaudRates.CBR_57600;
                    else
                        baud = BaudRates.CBR_9600;
                    break;
            }

            if (baud > maxBaud)
                baud = maxBaud;

            return baud;
        }

        /// <summary> Bit utility to read a bit in the provided array of chars.
        /// 
        /// </summary>
        /// <param name="bitBuffer">array of chars where the bit to read is located
        /// </param>
        /// <param name="address">  bit location to read (LSBit of first Byte in bitBuffer
        /// is postion 0)
        /// 
        /// </param>
        /// <returns> the boolean value of the bit position
        /// </returns>
        public virtual bool BitRead(byte[] bitBuffer, int address)
        {
            int byte_number, bit_number;

            byte_number = (address / 8);
            bit_number = address - (byte_number * 8);

            return (((byte)((bitBuffer[byte_number] >> bit_number) & 0x01)) == 0x01);
        }

        /// <summary> Bit utility to write a bit in the provided array of chars.
        /// 
        /// </summary>
        /// <param name="bitBuffer">array of chars where the bit to write is located
        /// </param>
        /// <param name="address">  bit location to write (LSBit of first Byte in bitBuffer
        /// is postion 0)
        /// </param>
        /// <param name="newBitState">new bit state
        /// </param>
        public virtual void BitWrite(byte[] bitBuffer, int address, bool newBitState)
        {
            int byte_number, bit_number;

            byte_number = (address / 8);
            bit_number = address - (byte_number * 8);

            if (newBitState)
                bitBuffer[byte_number] |= (byte)(0x01 << bit_number);
            else
                bitBuffer[byte_number] &= (byte)(~(0x01 << bit_number));
        }
    }


    /// <summary>UParameterSettings contains the parameter settings state for one
    /// speed on the DS2480 based iButton COM port adapter.
    /// </summary>
    /// <version>0.00</version>
    /// <author>DS, SH</author>
    internal struct UParameterSettings
    {
        /// <summary> The pull down slew rate for this mode.</summary>
        public SlewRate pullDownSlewRate;

        /// <summary> 12 Volt programming pulse time expressed in micro-seconds.</summary>
        public ProgramPulseTime12 pulse12VoltTime;

        /// <summary> 5 Volt programming pulse time expressed in milli-seconds.</summary>
        public ProgramPulseTime5 pulse5VoltTime;

        /// <summary> Write 1 low time expressed in micro-seconds.</summary>
        public WriteOneLowTime write1LowTime;

        /// <summary> Data sample offset and write 0 recovery time expressed in 
        /// micro-seconds.</summary>
        public SampleOffsetTime sampleOffsetTime;

        /// <summary>Parameter Settings constructor.</summary>
        public UParameterSettings(SlewRate sr, ProgramPulseTime12 ppt12,
           ProgramPulseTime5 ppt5, WriteOneLowTime wolt, SampleOffsetTime sot)
        {
            pullDownSlewRate = sr;
            pulse12VoltTime = ppt12;
            pulse5VoltTime = ppt5;
            write1LowTime = wolt;
            sampleOffsetTime = sot;
        }
    }


    /// <summary>Parameter selection</summary>
    internal enum Parameter : byte
    {
        /// <summary>Parameter selection, pull-down slew rate            </summary>
        PARAMETER_SLEW = (byte)(0x10),
        /// <summary>Parameter selection, 12 volt pulse time             </summary>
        PARAMETER_12VPULSE = (byte)(0x20),
        /// <summary>Parameter selection, 5 volt pulse time              </summary>
        PARAMETER_5VPULSE = (byte)(0x30),
        /// <summary>Parameter selection, write 1 low time               </summary>
        PARAMETER_WRITE1LOW = (byte)(0x40),
        /// <summary>Parameter selection, sample offset                  </summary>
        PARAMETER_SAMPLEOFFSET = (byte)(0x50),
        /// <summary>Parameter selection, baud rate                      </summary>
        PARAMETER_BAUDRATE = (byte)(0x70)
    }


    /// <summary>Pull down slew rate times</summary>
    internal enum SlewRate : byte
    {
        /// <summary>Pull down slew rate, 15V/us                    </summary>
        SLEWRATE_15Vus = (byte)(0x00),
        /// <summary>Pull down slew rate, 2.2V/us                   </summary>
        SLEWRATE_2p2Vus = (byte)(0x02),
        /// <summary>Pull down slew rate, 1.65V/us                  </summary>
        SLEWRATE_1p65Vus = (byte)(0x04),
        /// <summary>Pull down slew rate, 1.37V/us                  </summary>
        SLEWRATE_1p37Vus = (byte)(0x06),
        /// <summary>Pull down slew rate, 1.1V/us                   </summary>
        SLEWRATE_1p1Vus = (byte)(0x08),
        /// <summary>Pull down slew rate, 0.83V/us                  </summary>
        SLEWRATE_0p83Vus = (byte)(0x0A),
        /// <summary>Pull down slew rate, 0.7V/us                   </summary>
        SLEWRATE_0p7Vus = (byte)(0x0C),
        /// <summary>Pull down slew rate, 0.55V/us                  </summary>
        SLEWRATE_0p55Vus = (byte)(0x0E),
    }


    /// <summary>12 Volt programming pulse times</summary> 
    internal enum ProgramPulseTime12 : byte
    {
        /// <summary>12 Volt programming pulse, time 32us           </summary>
        TIME12V_32us = (byte)(0x00),
        /// <summary>12 Volt programming pulse, time 64us           </summary>
        TIME12V_64us = (byte)(0x02),
        /// <summary>12 Volt programming pulse, time 128us          </summary>
        TIME12V_128us = (byte)(0x04),
        /// <summary>12 Volt programming pulse, time 256us          </summary>
        TIME12V_256us = (byte)(0x06),
        /// <summary>12 Volt programming pulse, time 512us          </summary>
        TIME12V_512us = (byte)(0x08),
        /// <summary>12 Volt programming pulse, time 1024us         </summary>
        TIME12V_1024us = (byte)(0x0A),
        /// <summary>12 Volt programming pulse, time 2048us         </summary>
        TIME12V_2048us = (byte)(0x0C),
        /// <summary>12 Volt programming pulse, time (infinite)     </summary>
        TIME12V_infinite = (byte)(0x0E)
    }


    /// <summary>5 Volt programming pulse times </summary>
    internal enum ProgramPulseTime5 : byte
    {
        /// <summary>5 Volt programming pulse, time 16.4ms        </summary>
        TIME5V_16p4ms = (byte)(0x00),
        /// <summary>5 Volt programming pulse, time 65.5ms        </summary>
        TIME5V_65p5ms = (byte)(0x02),
        /// <summary>5 Volt programming pulse, time 131ms         </summary>
        TIME5V_131ms = (byte)(0x04),
        /// <summary>5 Volt programming pulse, time 262ms         </summary>
        TIME5V_262ms = (byte)(0x06),
        /// <summary>5 Volt programming pulse, time 524ms         </summary>
        TIME5V_524ms = (byte)(0x08),
        /// <summary>5 Volt programming pulse, time 1.05s         </summary>
        TIME5V_1p05s = (byte)(0x0A),
        /// <summary>5 Volt programming pulse, time 2.10sms       </summary>
        TIME5V_2p10s = (byte)(0x0C),
        /// <summary>5 Volt programming pulse, dynamic current detect       </summary>
        TIME5V_dynamic = (byte)(0x0C),
        /// <summary>5 Volt programming pulse, time (infinite)    </summary>
        TIME5V_infinite = (byte)(0x0E)
    }


    /// <summary>Write 1 low time </summary>
    internal enum WriteOneLowTime : byte
    {
        /// <summary>Write 1 low time, 8us                        </summary>
        WRITE1TIME_8us = (byte)(0x00),
        /// <summary>Write 1 low time, 9us                        </summary>
        WRITE1TIME_9us = (byte)(0x02),
        /// <summary>Write 1 low time, 10us                       </summary>
        WRITE1TIME_10us = (byte)(0x04),
        /// <summary>Write 1 low time, 11us                       </summary>
        WRITE1TIME_11us = (byte)(0x06),
        /// <summary>Write 1 low time, 12us                       </summary>
        WRITE1TIME_12us = (byte)(0x08),
        /// <summary>Write 1 low time, 13us                       </summary>
        WRITE1TIME_13us = (byte)(0x0A),
        /// <summary>Write 1 low time, 14us                       </summary>
        WRITE1TIME_14us = (byte)(0x0C),
        /// <summary>Write 1 low time, 15us                       </summary>
        WRITE1TIME_15us = (byte)(0x0E)
    }


    /// <summary>Data sample offset and write 0 recovery times </summary>
    internal enum SampleOffsetTime : byte
    {
        /// <summary>Data sample offset and Write 0 recovery time, 4us   </summary>
        SAMPLEOFFSET_TIME_4us = (byte)(0x00),
        /// <summary>Data sample offset and Write 0 recovery time, 5us   </summary>
        SAMPLEOFFSET_TIME_5us = (byte)(0x02),
        /// <summary>Data sample offset and Write 0 recovery time, 6us   </summary>
        SAMPLEOFFSET_TIME_6us = (byte)(0x04),
        /// <summary>Data sample offset and Write 0 recovery time, 7us   </summary>
        SAMPLEOFFSET_TIME_7us = (byte)(0x06),
        /// <summary>Data sample offset and Write 0 recovery time, 8us   </summary>
        SAMPLEOFFSET_TIME_8us = (byte)(0x08),
        /// <summary>Data sample offset and Write 0 recovery time, 9us   </summary>
        SAMPLEOFFSET_TIME_9us = (byte)(0x0A),
        /// <summary>Data sample offset and Write 0 recovery time, 10us  </summary>
        SAMPLEOFFSET_TIME_10us = (byte)(0x0C),
        /// <summary>Data sample offset and Write 0 recovery time, 11us  </summary>
        SAMPLEOFFSET_TIME_11us = (byte)(0x0E)
    }


    /// <summary>DS9097U brick baud rates expressed for the DS2480 ichip</summary>  
    internal enum AdapterBaud : byte
    {
        /// <summary>DS9097U brick baud rate expressed for the DS2480 ichip, 9600 baud   </summary>
        BAUD_9600 = (byte)(0x00),
        /// <summary>DS9097U brick baud rate expressed for the DS2480 ichip, 19200 baud  </summary>
        BAUD_19200 = (byte)(0x02),
        /// <summary>DS9097U brick baud rate expressed for the DS2480 ichip, 57600 baud  </summary>
        BAUD_57600 = (byte)(0x04),
        /// <summary>DS9097U brick baud rate expressed for the DS2480 ichip, 115200 baud </summary>
        BAUD_115200 = (byte)(0x06)
    }


    /// <summary>UAdapterState contains the communication state of the DS2480
    /// based COM port adapter.
    /// //\\//\\ This class is very preliminary and not all
    /// functionality is complete or debugged.  This
    /// class is subject to change.                  //\\//\\
    /// </summary>
    /// <version>0.00</version>
    /// <author>DS, SH</author>
    internal class UAdapterState
    {

        //--------
        //-------- Finals
        //--------

        //------- DS9097U speed modes

        /// <summary>DS9097U speed mode, regular speed                         </summary>
        public const byte USPEED_REGULAR = (byte)(0x00);

        /// <summary>DS9097U speed mode, flexible speed for long lines         </summary>
        public const byte USPEED_FLEX = (byte)(0x04);

        /// <summary>DS9097U speed mode, overdrive speed                       </summary>
        public const byte USPEED_OVERDRIVE = (byte)(0x08);

        /// <summary>DS9097U speed mode, pulse, for program and power delivery </summary>
        public const byte USPEED_PULSE = (byte)(0x0C);

        //------- DS9097U modes

        /// <summary>DS9097U data mode                                  </summary>
        public const byte MODE_DATA = (byte)(0x00E1);

        /// <summary>DS9097U command mode                               </summary>
        public const byte MODE_COMMAND = (byte)(0x00E3);

        /// <summary>DS9097U pulse mode                                 </summary>
        public const byte MODE_STOP_PULSE = (byte)(0x00F1);

        /// <summary>DS9097U special mode (in revision 1 silicon only)  </summary>
        public const byte MODE_SPECIAL = (byte)(0x00F3);

        //------- DS9097U chip revisions and state

        /// <summary>DS9097U chip revision 1  </summary>
        public const byte CHIP_VERSION1 = (byte)(0x04);

        /// <summary>DS9097U chip revision mask  </summary>
        public const byte CHIP_VERSION_MASK = (byte)(0x1C);

        /// <summary>DS9097U program voltage available mask  </summary>
        public const byte PROGRAM_VOLTAGE_MASK = (byte)(0x20);

        /// <summary>Maximum number of alarms</summary>
        public const int MAX_ALARM_COUNT = 3000;

        //--------
        //-------- Variables
        //--------

        /// <summary> Parameter settings for the three logical modes</summary>
        public UParameterSettings[] uParameters;

        /// <summary> The OneWire State object reference</summary>
        public OneWireState oneWireState;

        /// <summary> Flag true if can stream bits</summary>
        public bool streamBits;

        /// <summary> Flag true if can stream bytes</summary>
        public bool streamBytes;

        /// <summary> Flag true if can stream Search</summary>
        public bool streamSearches;

        /// <summary> Flag true if can stream resets</summary>
        public bool streamResets;

        /// <summary> Current baud rate that we are communicating with the DS9097U
        /// expressed for the DS2480 ichip. <p>
        /// Valid values can be:
        /// <ul>
        /// <li> BAUD_9600
        /// <li> BAUD_19200
        /// <li> BAUD_57600
        /// <li> BAUD_115200
        /// </ul>
        /// </summary>
        public AdapterBaud ubaud;

        /// <summary> This is the current 'real' speed that the OneWire is operating at.
        /// This is used to represent the actual mode that the DS2480 is operting
        /// in.  For example the logical speed might be USPEED_REGULAR but for
        /// RF emission reasons we may put the actual DS2480 in OWSpeed.SPEED_FLEX. <p>
        /// The valid values for this are:
        /// <ul>
        /// <li> USPEED_REGULAR
        /// <li> USPEED_FLEX
        /// <li> USPEED_OVERDRIVE
        /// <li> USPEED_PULSE
        /// </ul>
        /// </summary>
        public byte uSpeedMode;

        /// <summary> This is the current state of the DS2480 adapter on program
        /// voltage availablity.  'true' if available.
        /// </summary>
        public bool programVoltageAvailable;

        /// <summary> True when DS2480 is currently in command mode.  False when it is in
        /// data mode.
        /// </summary>
        public bool inCommandMode;

        /// <summary> The DS2480 revision number.  The current value values are 1 and 2.</summary>
        public byte revision;

        /// <summary> Flag to indicate need to Search for long alarm check</summary>
        protected internal bool longAlarmCheck;

        /// <summary> Count of how many resets have been seen without Alarms</summary>
        protected internal int lastAlarmCount;

        //--------
        //-------- Constructors
        //--------

        /// <summary> Construct the state of the U brick with the defaults</summary>
        public UAdapterState(OneWireState newOneWireState)
        {

            // get a pointer to the OneWire state object
            oneWireState = newOneWireState;

            // set the defaults
            ubaud = AdapterBaud.BAUD_9600;
            uSpeedMode = USPEED_FLEX;
            revision = 0;
            inCommandMode = true;
            streamBits = true;
            streamBytes = true;
            streamSearches = true;
            streamResets = false;
            programVoltageAvailable = false;
            longAlarmCheck = false;
            lastAlarmCount = 0;

            // create the three speed logical parameter settings
            uParameters = new UParameterSettings[4];
            uParameters[0] = new UParameterSettings(
               SlewRate.SLEWRATE_1p37Vus, ProgramPulseTime12.TIME12V_infinite,
               ProgramPulseTime5.TIME5V_infinite, WriteOneLowTime.WRITE1TIME_10us,
               SampleOffsetTime.SAMPLEOFFSET_TIME_8us);
            uParameters[1] = new UParameterSettings(
               SlewRate.SLEWRATE_1p37Vus, ProgramPulseTime12.TIME12V_infinite,
               ProgramPulseTime5.TIME5V_infinite, WriteOneLowTime.WRITE1TIME_10us,
               SampleOffsetTime.SAMPLEOFFSET_TIME_8us);
            uParameters[2] = new UParameterSettings(
               SlewRate.SLEWRATE_1p37Vus, ProgramPulseTime12.TIME12V_infinite,
               ProgramPulseTime5.TIME5V_infinite, WriteOneLowTime.WRITE1TIME_10us,
               SampleOffsetTime.SAMPLEOFFSET_TIME_8us);
            uParameters[3] = new UParameterSettings(
               SlewRate.SLEWRATE_1p37Vus, ProgramPulseTime12.TIME12V_infinite,
               ProgramPulseTime5.TIME5V_infinite, WriteOneLowTime.WRITE1TIME_10us,
               SampleOffsetTime.SAMPLEOFFSET_TIME_8us);

            // adjust flex time 
            uParameters[(int)OWSpeed.SPEED_FLEX].pullDownSlewRate = SlewRate.SLEWRATE_0p83Vus;
            uParameters[(int)OWSpeed.SPEED_FLEX].write1LowTime = WriteOneLowTime.WRITE1TIME_12us;
            uParameters[(int)OWSpeed.SPEED_FLEX].sampleOffsetTime = SampleOffsetTime.SAMPLEOFFSET_TIME_10us;
        }
    }


    /// <summary>Raw Send Packet that contains a StingBuffer of bytes to send and
    /// an expected return length.
    /// </summary>
    /// <version>0.00</version>
    /// <author>DS, SH</author>
    internal class RawSendPacket
    {

        //--------
        //-------- Variables
        //--------

        /// <summary> StringBuffer of bytes to send</summary>
        //public System.Text.StringBuilder buffer;
        public System.Collections.ArrayList dataList;


        /// <summary> Expected length of return packet</summary>
        public int returnLength;

        //--------
        //-------- Constructors
        //--------

        /// <summary> Construct and initiailize the raw send packet</summary>
        public RawSendPacket()
        {
            dataList = new System.Collections.ArrayList();
            returnLength = 0;
        }
    }


    /// <summary> 1-Wire Network State contains the current 1-Wire Network state information
    /// </summary>
    /// <version>0.00</version>
    /// <author>DS, SH</author>
    internal class OneWireState
    {

        //--------
        //-------- Variables
        //--------

        /// <summary> This is the current logical speed that the 1-Wire Network is operating at. <p>
        /// </summary>
        public OWSpeed oneWireSpeed;

        /// <summary> This is the current logical 1-Wire Network pullup level.<p>
        /// </summary>
        public OWLevel oneWireLevel;

        /// <summary> True if programming voltage is available</summary>
        public bool canProgram;

        /// <summary> True if a level change is primed to occur on the next bit
        /// of communication.
        /// </summary>
        public bool levelChangeOnNextBit;

        /// <summary> True if a level change is primed to occur on the next byte
        /// of communication.
        /// </summary>
        public bool levelChangeOnNextByte;

        /// <summary> The new level value that is primed to change on the next bit
        /// or byte depending on the flags, levelChangeOnNextBit and
        /// levelChangeOnNextByte. <p>
        /// </summary>
        public OWLevel primedLevelValue;

        /// <summary> The amount of time that the 'level' value will be on for. <p>
        /// </summary>
        public OWPowerTime levelTimeFactor;

        /// <summary> Value of the last discrepancy during the last Search for an iButton.</summary>
        public int searchLastDiscrepancy;

        /// <summary> Value of the last discrepancy in the family code during the last
        /// Search for an iButton.
        /// </summary>
        public int searchFamilyLastDiscrepancy;

        /// <summary> Flag to indicate that the last device found is the last device in a
        /// Search sequence on the 1-Wire Network.
        /// </summary>
        public bool searchLastDevice;

        /// <summary> ID number of the current iButton found.</summary>
        public byte[] ID;

        /// <summary> Array of iButton families to include in any Search.</summary>
        public byte[] searchIncludeFamilies;

        /// <summary> Array of iButton families to exclude in any Search.</summary>
        public byte[] searchExcludeFamilies;

        /// <summary> Flag to indicate the conditional Search is to be performed so that
        /// only iButtons in an alarm state will be found.
        /// </summary>
        public bool searchOnlyAlarmingButtons;

        /// <summary> Flag to indicate next Search will not be preceeded by a 1-Wire reset</summary>
        public bool skipResetOnSearch;

        //--------
        //-------- Constructors
        //--------

        /// <summary> Construct the initial state of the 1-Wire Network.</summary>
        public OneWireState()
        {

            // speed, level
            oneWireSpeed = OWSpeed.SPEED_REGULAR;
            oneWireLevel = OWLevel.LEVEL_NORMAL;

            // level primed
            levelChangeOnNextBit = false;
            levelChangeOnNextByte = false;
            primedLevelValue = OWLevel.LEVEL_NORMAL;
            levelTimeFactor = OWPowerTime.DELIVERY_INFINITE;

            // adapter abilities
            canProgram = false;

            // Search options 
            searchIncludeFamilies = new byte[0];
            searchExcludeFamilies = new byte[0];
            searchOnlyAlarmingButtons = false;
            skipResetOnSearch = false;

            // new iButton object
            ID = new byte[8];

            // Search state
            searchLastDiscrepancy = 0;
            searchFamilyLastDiscrepancy = 0;
            searchLastDevice = false;
        }
    }
}
