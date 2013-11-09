/*---------------------------------------------------------------------------
 * Copyright (C) 2002-2003 Dallas Semiconductor Corporation, All Rights Reserved.
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
using System.Collections.Generic;
using System.Text;

using DalSemi.OneWire.Adapter; // PortAdapter
using DalSemi.Utils;
using System.Threading; // CRC8

namespace DalSemi.OneWire.Container
{
    /// <summary>
    /// 1-Wire container for temperature iButton which measures temperatures
    /// from -55C to +100C, DS1920 or DS18S20.  This container encapsulates the
    /// functionality of the iButton family type 10 (hex)>
    /// 
    /// Features
    /// Measures temperatures from -55C to +100C in typically 0.2 seconds
    /// Zero standby power
    /// 0.5C resolution, digital temperature reading in two’s complement
    /// Increased resolution through interpolation in internal counters
    /// 8-bit device-generated CRC for data integrity
    /// Special command set allows user to skip ROM section and do temperature
    /// measurements simultaneously for all devices on the bus
    /// 2 bytes of EEPROM to be used either as alarm triggers or user memory
    /// Alarm Search directly indicates which device senses alarming temperatures
    /// 
    /// Usage
    /// See the usage example in TemperatureContainer for temperature specific operations.
    /// 
    /// DataSheet
    /// http://pdfserv.maxim-ic.com/arpdf/DS1920.pdf
    /// http://pdfserv.maxim-ic.com/arpdf/DS18S20.pdf
    /// </summary>
    /// <seealso cref="DalSemi.OneWire.Container.TemperatureContainer"/>
    public class OneWireContainer10 : OneWireContainer, TemperatureContainer
    {

        private bool normalResolution = true;

        #region Static Const Fields


        /// <summary>
        /// default temperature resolution for this OneWireContainer10 device.
        /// </summary>
        public const double RESOLUTION_NORMAL = 0.5;

        /// <summary>
        /// Maximum temperature resolution for this OneWireContainer10 device. Use RESOLUTION_MAXIMUM in SetResolution() if higher resolution is desired.
        /// </summary>
        public const double RESOLUTION_MAXIMUM = 0.1;

        private const int RESOLUTION_STATE_INDEX = 4;

        /// <summary>
        /// DS1920 convert temperature command
        /// </summary>
        private const byte CONVERT_TEMPERATURE_COMMAND = 0x44;

        /// <summary>
        /// DS1920 read data from scratchpad command 
        /// </summary>
        private const byte READ_SCRATCHPAD_COMMAND = (byte)0xBE;

        /// <summary>
        /// DS1920 write data to scratchpad command
        /// </summary>
        private const byte WRITE_SCRATCHPAD_COMMAND = (byte)0x4E;

        /// <summary>
        /// DS1920 copy data from scratchpad to EEPROM command
        /// </summary>
        private const byte COPY_SCRATCHPAD_COMMAND = (byte)0x48;


        /// <summary>
        /// DS1920 recall EEPROM command 
        /// </summary>
        private const byte RECALL_EEPROM_COMMAND = (byte)0xB8;

        #endregion // Static Const Fields


        /// <summary>
        /// Initializes a new instance of the <see cref="OneWireContainer10"/> class.
        /// This is one of the methods to construct a <see cref="OneWireContainer10"/>. 
        /// The others are through creating a <see cref="OneWireContainer10"/> with different parameters types.
        /// </summary>
        /// <param name="sourceAdapter">adapter object required to communicate with this device.</param>
        /// <param name="newAddress">address of this 1-Wire device</param>
        /// <seealso cref="OneWireContainer()"/>
        /// <seealso cref="DalSemi.OneWire.Utils.Address"/>
        public OneWireContainer10(PortAdapter sourceAdapter, byte[] newAddress)
            : base(sourceAdapter, newAddress)
        {
        }


        #region Information methods

        /// <summary>
        /// Gets the family code.
        /// </summary>
        /// <returns></returns>
        public static byte GetFamilyCode()
        {
            return 0x10;
        }

        /// <summary>
        /// Retrieves the Dallas Semiconductor part number of the 1-Wire device as a string.  For example 'DS1920'.
        /// </summary>
        /// <returns>1-Wire device name</returns>
        public override string GetName()
        {
            return "DS1920";
        }

        /// <summary>
        /// Retrieves the alternate Dallas Semiconductor part numbers or names.
        /// A 'family' of 1-Wire Network devices may have more than one part number depending on packaging.
        /// There can also be nicknames such as 'Crypto iButton'.
        /// </summary>
        /// <returns>1-Wire device alternate names</returns>
        public override string GetAlternateNames()
        {
            return "DS18S20";
        }


        /// <summary>
        /// Retrieves a short description of the function of the 1-Wire device type.
        /// </summary>
        /// <returns>Device functional description</returns>
        public override string GetDescription()
        {
            return "Digital thermometer measures temperatures from "
                   + "-55C to 100C in typically 0.2 seconds.  +/- 0.5C "
                   + "Accuracy between 0C and 70C. 0.5C standard "
                   + "resolution, higher resolution through interpolation.  "
                   + "Contains high and low temperature set points for "
                   + "generation of alarm.";
        }

        #endregion // Information methods

        #region Temperature Feature methods


        /// <summary>
        /// Determines whether this temperature measuring device has high/low trip alarms.
        /// </summary>
        /// <returns>
        /// true if this TemperatureContainer has high/low trip alarms
        /// </returns>
        /// <seealso cref="GetTemperatureAlarm"/>
        /// <seealso cref="SetTemperatureAlarm"/>
        public bool HasTemperatureAlarms()
        {
            return true;
        }

        /// <summary>
        /// Determines whether this device has selectable temperature resolution
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this device has selectable temperature resolution; otherwise, <c>false</c>.
        /// </returns>
        /// <seealso cref="GetTemperatureResolution"/>
        /// <seealso cref="GetTemperatureResolutions"/>
        /// <seealso cref="SetTemperatureResolution"/>
        public bool HasSelectableTemperatureResolution()
        {
            return true;
        }

        /// <summary>
        /// Get an array of available temperature resolutions in Celsius.
        /// </summary>
        /// <returns>
        /// byte array of available temperature resolutions in Celsius with
        /// minimum resolution as the first element and maximum resolution as the last element
        /// </returns>
        /// <seealso cref="HasSelectableTemperatureResolution"/>
        /// <seealso cref="GetTemperatureResolution"/>
        /// <seealso cref="SetTemperatureResolution"/>
        public double[] GetTemperatureResolutions()
        {
            double[] resolutions = new double[2];

            resolutions[0] = RESOLUTION_NORMAL;
            resolutions[1] = RESOLUTION_MAXIMUM;

            return resolutions;
        }

        /// <summary>
        /// Gets the temperature alarm resolution in Celsius.
        /// </summary>
        /// <returns>
        /// Temperature alarm resolution in Celsius for this 1-wire device
        /// </returns>
        /// <seealso cref="HasTemperatureAlarms"/>
        /// <seealso cref="GetTemperatureAlarm"/>
        /// <seealso cref="SetTemperatureAlarm"/>
        public double GetTemperatureAlarmResolution()
        {
            return 1.0;
        }

        /// <summary>
        /// Gets the maximum temperature in Celsius.
        /// </summary>
        /// <returns>
        /// Maximum temperature in Celsius for this 1-wire device
        /// </returns>
        /// <seealso cref="GetMinTemperature"/>
        public double GetMaxTemperature()
        {
            return 100.0;
        }

        /// <summary>
        /// Gets the minimum temperature in Celsius.
        /// </summary>
        /// <returns>
        /// Minimum temperature in Celsius for this 1-wire device
        /// </returns>
        /// <seealso cref="MaxTemperature"/>
        public double GetMinTemperature()
        {
            return -55.0;
        }

        #region Temperature I/O Methods

        /// <summary>
        /// Performs a temperature conversion.
        /// </summary>
        /// <param name="state">byte array with device state information</param>
        /// <exception cref="OneWireIOException">
        /// On a 1-Wire communication error such as reading an incorrect CRC from this OneWireContainer10
        /// This could be caused by a physical interruption in the 1-Wire Network due to shorts or a newly arriving
        /// 1-Wire device issuing a 'presence pulse'.
        /// </exception>
        /// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter</exception>
        /// <seealso cref="GetTemperature"/>
        public void DoTemperatureConvert(byte[] state)
        {
            DoSpeed();

            // select the device
            if (adapter.SelectDevice(address))
            {

                // Setup Power Delivery
                adapter.SetPowerDuration(OWPowerTime.DELIVERY_INFINITE);
                adapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);

                // send the convert temperature command
                adapter.PutByte(CONVERT_TEMPERATURE_COMMAND);

                // delay for 750 ms
                System.Threading.Thread.Sleep(750);

                // Turn power back to normal.
                adapter.SetPowerNormal();

                // check to see if the temperature conversion is over
                if (adapter.GetByte() != 0xFF)
                    throw new OneWireIOException(
                       "OneWireContainer10-temperature conversion not complete");

                // read the result
                byte mode = state[RESOLUTION_STATE_INDEX];   //preserve the resolution in the state

                adapter.SelectDevice(address);
                ReadScratch(state);

                state[RESOLUTION_STATE_INDEX] = mode;
            }
            else

                // device must not have been present
                throw new OneWireIOException(
                   "OneWireContainer10-device not present");
        }

        #endregion Temperature I/O Methods

        #region Temperature 'Get' Methods

        /// <summary>
        /// Gets the temperature value in Celsius from the state data retrieved from the ReadDevice() method.
        /// </summary>
        /// <param name="state">byte array with device state information</param>
        /// <returns>temperature in Celsius from the last DoTemperatureConvert() call</returns>
        ///<exception cref="OneWireIOException">
        /// On a 1-Wire communication error such as reading an incorrect CRC from this OneWireContainer10
        /// This could be caused by a physical interruption in the 1-Wire Network due to shorts
        /// or a newly arriving 1-Wire device issuing a 'presence pulse'.
        /// </exception>
        /// <exception cref="DivideByZeroException">When the </exception>
        /// <seealso cref="DoTemperatureConvert"/>
        public double GetTemperature(byte[] state)
        {
            // On some parts, namely the 18S20, you can get invalid readings.
            // Basically, the detection is that all the upper 8 bits should
            // be the same by sign extension.  The error condition (DS18S20
            // returns 185.0+) violated that condition
            if (state[1] != 0x00 && state[1] != 0xFF)
            {
                throw new OneWireIOException("Invalid temperature data!");
            }

            // Build the 16 bit value
            short temp = (short)(state[0] | (state[1] << 8));

            if (state[RESOLUTION_STATE_INDEX] == 1)
            {
                temp = (short)(temp >> 1);   //Shift out the decimal bit, also takes care of the / 2.0

                double tmp = (double)temp;
                double countRemain = state[6];
                double countPerC = state[7];

                //just let the thing throw a divide by zero exception
                tmp = tmp - (double)0.25 + (countPerC - countRemain) / countPerC;

                return tmp;
            }
            else
            {
                // Do normal resolution
                return temp / 2.0;
            }
        }

        /// <summary>
        /// Gets the specified temperature alarm value in Celsius from the state data retrieved from the ReadDevice() method.
        /// </summary>
        /// <param name="alarmType">Type of the alarm. Valid types are: TemperatureContainerConsts.ALARM_HIGH or TemperatureContainerConsts.ALARM_LOW</param>
        /// <param name="state">byte array with device state information</param>
        /// <returns>temperature alarm trip values in Celsius for this OneWireContainer10</returns>
        /// <seealsoalso cref="HasTemperatureAlarms"/>
        /// <seealsoalso cref="SetTemperatureAlarm"/>
        public double GetTemperatureAlarm(int alarmType, byte[] state)
        {
            return (double)state[alarmType == TemperatureContainerConsts.ALARM_LOW ? 3
                                                                                   : 2];
        }

        /// <summary>
        /// Gets the current temperature resolution in Celsius from the state data retrieved from the ReadDevice() method.
        /// </summary>
        /// <param name="state">byte array with device state information</param>
        /// <returns>
        /// resolution in Celsius for this 1-wire device
        /// </returns>
        /// <seealso cref="HasSelectableTemperatureResolution"/>
        /// <seealso cref="getTemperatureResolutions"/>
        /// <seealso cref="setTemperatureResolution"/>
        public double GetTemperatureResolution(byte[] state)
        {
            if (state[RESOLUTION_STATE_INDEX] == 0)
                return RESOLUTION_NORMAL;

            return RESOLUTION_MAXIMUM;
        }

        #endregion // Temperature 'Get' Methods

        #region Temperature 'Set' Methods

        /// <summary>
        /// Sets the temperature alarm value in Celsius in the provided state data.
        /// Use method WriteDevice() to finalize the change to the device.
        /// </summary>
        /// <param name="alarmType">The alarm type to set. Valid types are: TemperatureContainerConsts.ALARM_HIGH or TemperatureContainerConsts.ALARM_LOW </param>
        /// <param name="alarmValue">alarm trip value in Celsius</param>
        /// <param name="state">byte array with device state information</param>
        /// <seealso cref="HasTemperatureAlarms"/>
        /// <seealso cref="GetTemperatureAlarm"/>
        public void SetTemperatureAlarm(int alarmType, double alarmValue, byte[] state)
        {
            if ((alarmType != TemperatureContainerConsts.ALARM_LOW) && (alarmType != TemperatureContainerConsts.ALARM_HIGH))
                throw new ArgumentOutOfRangeException("Invalid alarm type.");

            if (alarmValue > GetMaxTemperature() || alarmValue < GetMinTemperature())
                throw new ArgumentOutOfRangeException(
                  "Value for alarm not in accepted range.  Must be " + GetMinTemperature() + " C <-> " + GetMaxTemperature() + " C.");

            state[(alarmType == TemperatureContainerConsts.ALARM_LOW) ? 3
                                                                      : 2] = (byte)alarmValue;
        }

        /// <summary>
        /// Sets the current temperature resolution in Celsius in the provided state data.
        /// Use the method WriteDevice() with this data to finalize the change to the device.
        /// </summary>
        /// <param name="resolution">temperature resolution in Celsius. Valid values are OneWireContainer10.RESOLUTION_NORMAL and OneWireContainer10.RESOLUTION_MAXIMUM</param>
        /// <param name="state">byte array with device state information</param>
        /// <seealso cref="HasSelectableTemperatureResolution"/>
        /// <seealso cref="GetTemperatureResolution"/>
        /// <seealso cref="GetTemperatureResolutions"/>
        public void SetTemperatureResolution(double resolution, byte[] state)
        {
            lock (this)
            {
                if (resolution == RESOLUTION_NORMAL)
                    normalResolution = true;
                else
                    normalResolution = false;

                state[RESOLUTION_STATE_INDEX] = (byte)(normalResolution ? 0
                                                       : 1);
            }
        }

        #endregion // Temperature 'Set' Methods

        #endregion // Temperature Feature methods

        #region Read & Write data methods

        /// <summary>
        /// Retrieves the 1-Wire device sensor state.  This state is
        /// returned as a byte array.  Pass this byte array to the 'Get'
        /// and 'Set' methods.  If the device state needs to be changed then call
        /// the 'WriteDevice' to finalize the changes.
        /// 
        /// /// Device state looks like this:
        /// Bit     Meaning
        /// 0       temperature LSB
        /// 1       temperature MSB
        /// 2       trip high
        /// 3       trip low
        /// 4       reserved (put the resolution here, 0 for normal, 1 for max)
        /// 5       reserved
        /// 6       count remain
        /// 7       count per degree Celsius
        /// 8       an 8 bit CRC over the previous 8 bytes of data
        /// 
        /// </summary>
        /// <returns>1-Wire device sensor state</returns>
        /// <exception cref="OneWireIOException">
        /// On a 1-Wire communication error such as reading an incorrect CRC from this OneWireContainer10
        /// This could be caused by a physical interruption in the 1-Wire Network due to shorts or a newly arriving 1-Wire device issuing a
        /// 'presence pulse'.
        /// </exception>
        /// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter</exception>
        /// <seealso cref="WriteDevice"/>
        public byte[] ReadDevice()
        {

            byte[] data = new byte[8];

            DoSpeed();

            // select the device
            if (adapter.SelectDevice(address))
            {

                // construct a block to read the scratchpad
                byte[] buffer = new byte[10];

                // read scratchpad command
                buffer[0] = (byte)READ_SCRATCHPAD_COMMAND;

                // now add the read bytes for data bytes and crc8
                for (int i = 1; i < 10; i++)
                    buffer[i] = (byte)0x0FF;

                // send the block
                adapter.DataBlock(buffer, 0, buffer.Length);

                // see if crc is correct
                if (CRC8.Compute(buffer, 1, 9) == 0)
                    Array.Copy(buffer, 1, data, 0, 8);
                else
                    throw new OneWireIOException(
                       "OneWireContainer10-Error reading CRC8 from device.");
            }
            else
                throw new OneWireIOException(
                   "OneWireContainer10-Device not found on 1-Wire Network");

            //we are just reading normalResolution here, no need to synchronize
            data[4] = (byte)(normalResolution ? 0 : 1);

            return data;
        }

        /// <summary>
        /// Writes to this OneWireContainer10 the state
        /// information that have been changed by 'Set' methods.
        /// Only the state registers that changed are updated.  This is done
        /// by referencing a field information appended to the state data.
        /// </summary>
        /// <param name="state">1-Wire device sensor state</param>
        /// <exception cref="OneWireIOException">
        /// On a 1-Wire communication error such as reading an incorrect CRC from this OneWireContainer10
        /// This could be caused by a physical interruption in the 1-Wire Network due to shorts or a newly
        /// arriving 1-Wire device issuing a 'presence pulse'.
        /// </exception>
        /// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter</exception>
        /// <seealso cref="readDevice"/>
        public void WriteDevice(byte[] state)
        {
            DoSpeed();

            byte[] temp = new byte[2];

            temp[0] = state[2];
            temp[1] = state[3];

            // Write it to the Scratchpad.
            WriteScratchpad(temp);

            // Place in memory.
            CopyScratchpad();
        }

        #endregion // Read & Write data methods

        #region Private Methods

        /// <summary>
        /// Reads the 8 bytes from the scratchpad and verify CRC8 returned.
        /// </summary>
        /// <param name="data">buffer to store the scratchpad data</param>  
        /// <exception cref="OneWireIOException">On a 1-Wire communication error such as reading
        /// an incorrect CRC from this OneWireContainer10. This could be caused by a physical
        /// interruption in the 1-Wire Network due to shorts or a newly arriving 1-Wire device
        /// issuing a 'presence pulse'.</exception>
        /// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter</exception>
        private void ReadScratch(byte[] data)
        {

            // select the device
            if (adapter.SelectDevice(address))
            {

                // construct a block to read the scratchpad
                byte[] buffer = new byte[10];

                // read scratchpad command
                buffer[0] = (byte)READ_SCRATCHPAD_COMMAND;

                // now add the read bytes for data bytes and crc8
                for (int i = 1; i < 10; i++)
                    buffer[i] = (byte)0x0FF;

                // send the block
                adapter.DataBlock(buffer, 0, buffer.Length);

                // see if crc is correct
                if (CRC8.Compute(buffer, 1, 9) == 0)
                    Array.Copy(buffer, 1, data, 0, 8);
                else
                    throw new OneWireIOException(
                       "OneWireContainer10-Error reading CRC8 from device.");
            }
            else
                throw new OneWireIOException(
                   "OneWireContainer10-Device not found on 1-Wire Network");
        }

        /// <summary>
        /// Writes to the Scratchpad.
        /// </summary>
        /// <param name="data">
        /// This is the data to be written to the scratchpad.  Cannot
        /// be more than two bytes in size. First byte of data must be
        /// the temperature High Trip Point and second byte must be
        /// temperature Low Trip Point.
        /// </param>
        /// <exception cref="OneWireIOException">
        /// On a 1-Wire communication error such as reading an incorrect CRC from this 
        /// OneWireContainer10. This could be caused by a physical interruption in the 1-Wire
        /// Network due to shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
        /// </exception>
        /// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter</exception>
        /// <exception cref="IllegalArgumentException">When data length is not equal to 2</exception>

        private void WriteScratchpad(byte[] data)
        {
            // First do some error checking.
            if (data.Length != 2)
                throw new ArgumentOutOfRangeException(
                   "Bad data.  Data must consist of only TWO bytes.");

            byte[] buffer = new byte[8];

            // Prepare the data to be sent.           
            buffer[0] = WRITE_SCRATCHPAD_COMMAND;
            buffer[1] = data[0];
            buffer[2] = data[1];

            // Send the block of data to the DS1920.
            if (adapter.SelectDevice(address))
                adapter.DataBlock(buffer, 0, 3);
            else
                throw new OneWireIOException(
                   "OneWireContainer10 - Device not found");

            // Check data to ensure correctly recived.
            ReadScratch(buffer);

            // verify data
            if ((buffer[2] != data[0]) || (buffer[3] != data[1]))
                throw new OneWireIOException(
                   "OneWireContainer10 - data read back incorrect");
        }

        /// <summary>
        /// Copies the contents of the User bytes of the ScratchPad to the EEPROM.
        /// </summary>
        /// <exception cref="OneWireIOException">
        /// On a 1-Wire communication error such as reading an incorrect CRC from this OneWireContainer10
        /// This could be caused by a physical interruption in the 1-Wire Network due to shorts or a
        /// newly arriving 1-Wire device issuing a 'presence pulse'.
        /// </exception>
        /// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter</exception>
        private void CopyScratchpad()
        {

            // select the device
            if (adapter.SelectDevice(address))
            {

                // send the copy command
                adapter.PutByte(COPY_SCRATCHPAD_COMMAND);

                // Setup Power Delivery
                adapter.SetPowerDuration(OWPowerTime.DELIVERY_INFINITE);
                adapter.StartPowerDelivery(OWPowerStart.CONDITION_NOW);

                // delay for 10 ms
                try
                {
                    System.Threading.Thread.Sleep(10);
                }
                catch (ThreadInterruptedException)
                {
                    // Just drain it 
                }

                // Turn power back to normal.
                adapter.SetPowerNormal();
            }
            else
                throw new OneWireIOException(
                   "OneWireContainer10 - device not found");
        }

        #endregion // Private Methods
    }
}
