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

namespace DalSemi.OneWire.Container
{
    /// <summary>
    /// Constant holder for TemperatureContainer 
    /// (cannot declare fields in a c#-interface)
    /// </summary>
    public class TemperatureContainerConsts
    {
        /// <summary>
        /// High temperature alarm
        /// </summary>
        public const int ALARM_HIGH = 1;

        /// <summary>
        /// Low temperature alarm
        /// </summary>
        public const int ALARM_LOW = 0;
    }

    /// <summary>
    /// 1-Wire temperature interface class for basic temperature measuring
    /// operations. This class should be implemented for each temperature
    /// type 1-Wire device.
    ///
    ///
    /// The TemperatureContainer methods can be organized into the following categories:
    /// 
    /// Information
    /// 
    /// getMaxTemperature
    /// getMinTemperature
    /// getTemperature
    /// getTemperatureAlarm
    /// getTemperatureAlarmResolution
    /// getTemperatureResolution
    /// getTemperatureResolutions
    /// hasSelectableTemperatureResolution
    /// hasTemperatureAlarms
    /// 
    /// Options
    /// 
    /// doTemperatureConvert
    /// setTemperatureAlarm
    /// setTemperatureResolution
    ///
    /// I/O
    /// readDevice
    /// writeDevice
    ///
    /// Usage
    ///
    /// Example 1
    /// Display some features of TemperatureContainer instance 'tc':
    /// 
    ///   // Read High and Low Alarms
    ///   if (!tc.hasTemperatureAlarms()) {
    ///      Console.WriteLine("Temperature alarms not supported");
    ///   }
    ///   else
    ///   {
    ///      byte[] state     = tc.readDevice();
    ///      double alarmLow  = tc.getTemperatureAlarm(TemperatureContainer.ALARM_LOW, state);
    ///      double alarmHigh = tc.getTemperatureAlarm(TemperatureContainer.ALARM_HIGH, state);
    ///      Console.WriteLine("Alarm: High = " + alarmHigh + ", Low = " + alarmLow);
    ///   }
    ///
    /// Example 2
    /// Gets temperature reading from a TemperatureContainer instance 'tc':
    ///   double lastTemperature;
    ///
    ///   // get the current resolution and other settings of the device (done only once)
    ///   byte[] state = tc.readDevice();
    ///
    ///   do // loop to read the temp
    ///   {
    ///      // perform a temperature conversion
    ///      tc.doTemperatureConvert(state);
    ///
    ///      // read the result of the conversion
    ///      state = tc.readDevice();
    ///
    ///      // extract the result out of state
    ///      lastTemperature = tc.getTemperature(state);
    ///      ...
    ///
    ///   } while (!done);
    ///
    /// The reason the conversion and the reading are separated
    /// is that one may want to do a conversion without reading
    /// the result.  One could take advantage of the alarm features
    /// of a device by setting a threshold and doing conversions
    /// until the device is alarming.  For example:
    ///
    ///   // get the current resolution of the device
    ///   byte [] state = tc.readDevice();
    ///
    ///   // set the trips
    ///   tc.setTemperatureAlarm(TemperatureContainer.ALARM_HIGH, 50, state);
    ///   tc.setTemperatureAlarm(TemperatureContainer.ALARM_LOW, 20, state);
    ///   tc.writeDevice(state);
    ///
    ///   do // loop on conversions until an alarm occurs
    ///   {
    ///      tc.doTemperatureConvert(state);
    ///   } while (!tc.isAlarming());
    ///
    /// Example 3
    /// Sets the temperature resolution of a TemperatureContainer instance 'tc':
    ///
    ///   byte[] state = tc.readDevice();
    ///   if (tc.hasSelectableTemperatureResolution())
    ///   {
    ///      double[] resolution = tc.getTemperatureResolutions();
    ///      tc.setTemperatureResolution(resolution [resolution.length - 1], state);
    ///      tc.writeDevice(state);
    ///   }
    /// </summary>
    /// <see cref="DalSemi.OneWire.Container.OneWireContainer10"/> 
    /// <see cref="DalSemi.OneWire.Container.OneWireContainer21"/>
    /// <see cref="DalSemi.OneWire.Container.OneWireContainer26"/>
    /// <see cref="DalSemi.OneWire.Container.OneWireContainer28"/>
    /// <see cref="DalSemi.OneWire.Container.OneWireContainer30"/>
    public interface TemperatureContainer : IOneWireSensor
    {
        #region Temperature Feature methods

        /// <summary>
        /// Determines whether this temperature measuring device has high/low trip alarms.
        /// </summary>
        /// <returns>
        /// true if this TemperatureContainer has high/low trip alarms
        /// </returns>
        /// <see cref="GetTemperatureAlarm"/>
        /// <see cref="GetTemperatureAlarm"/>
        bool HasTemperatureAlarms();

        /// <summary>
        /// Determines whether this device has selectable temperature resolution
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this device has selectable temperature resolution; otherwise, <c>false</c>.
        /// </returns>
        /// <see cref="GetTemperatureResolution"/>
        /// <see cref="GetTemperatureResolutions"/>
        /// <see cref="SetTemperatureResolution"/>
        bool HasSelectableTemperatureResolution();


        /// <summary>
        ///  Get an array of available temperature resolutions in Celsius.
        /// </summary>
        /// <returns>byte array of available temperature resolutions in Celsius with
        /// minimum resolution as the first element and maximum resolution as the last element
        /// </returns>
        /// <see cref="HasSelectableTemperatureResolution"/>
        /// <see cref="GetTemperatureResolution"/>
        /// <see cref="SetTemperatureResolution"/>
        double[] GetTemperatureResolutions();


        /// <summary>
        /// Gets the temperature alarm resolution in Celsius.
        /// </summary>
        /// <returns>Temperature alarm resolution in Celsius for this 1-wire device</returns>
        /// <exception cref="OneWireException">Device does not support temperature alarms</exception>
        /// <see cref="HasTemperatureAlarms"/>
        /// <see cref="GetTemperatureAlarm"/>
        /// <see cref="SetTemperatureAlarm"/>
        double GetTemperatureAlarmResolution();


        /// <summary>
        /// Gets the maximum temperature in Celsius.
        /// </summary>
        /// <returns>Maximum temperature in Celsius for this 1-wire device</returns>
        double GetMaxTemperature();


        /// <summary>
        ///  Gets the minimum temperature in Celsius.
        /// </summary>
        /// <returns>Minimum temperature in Celsius for this 1-wire device</returns>
        double GetMinTemperature();

        #endregion // Temperature Feature methods

        #region Temperature I/O Methods

        /// <summary>
        /// Performs a temperature conversion.
        /// </summary>
        /// <param name="state">byte array with device state information</param>
        /// <exception cref="OneWireException">Part could not be found [ fatal ]</exception>
        /// <exception cref="OneWireIOException">Data wasn'thread transferred properly [ recoverable ]</exception>
        void DoTemperatureConvert(byte[] state);

        #endregion // Temperature I/O Methods

        #region Temperature 'Get' Methods

        /// <summary>
        /// Gets the temperature value in Celsius from the state data retrieved from the EeadDevice() method.
        /// </summary>
        /// <param name="state">byte array with device state information</param>
        /// <returns></returns>
        /// <exception cref="OneWireIOException">In the case of invalid temperature data</exception>
        double GetTemperature(byte[] state);

        /// <summary>
        /// Gets the specified temperature alarm value in Celsius from the state data retrieved from the ReadDevice() method.
        /// </summary>
        /// <param name="alarmType">Type of the alarm. Valid types are: TemperatureContainerConsts.ALARM_HIGH or TemperatureContainerConsts.ALARM_LOW</param>
        /// <param name="state">byte array with device state information</param>
        /// <returns></returns>
        /// <exception cref="OneWireException">Device does not support temperature alarms</exception>
        /// <see cref="HasTemperatureAlarms"/>
        /// <see cref="SetTemperatureAlarm"/>
        double GetTemperatureAlarm(int alarmType, byte[] state);


        /// <summary>
        /// Gets the current temperature resolution in Celsius from the state data retrieved from the ReadDevice() method.
        /// </summary>
        /// <param name="state">byte array with device state information</param>
        /// <returns>resolution in Celsius for this 1-wire device</returns>
        /// <see cref="HasSelectableTemperatureResolution"/>
        /// <see cref="GetTemperatureResolutions"/>
        /// <see cref="SetTemperatureResolution"/>
        double GetTemperatureResolution(byte[] state);

        #endregion // Temperature 'Get' Methods

        #region Temperature 'Set' Methods

        /// <summary>
        /// Sets the temperature alarm value in Celsius in the provided state data.
        /// Use the method WriteDevice() with this data to finalize the change to the device.
        /// </summary>
        /// <param name="alarmType">Type of the alarm. Valid types are: TemperatureContainerConsts.ALARM_HIGH or TemperatureContainerConsts.ALARM_LOW</param>
        /// <param name="alarmValue">alarm trip value in Celsius</param>
        /// <param name="state">byte array with device state information</param>
        /// <see cref="HasTemperatureAlarms"/>
        /// <see cref="GetTemperatureAlarm"/>
        void SetTemperatureAlarm(int alarmType, double alarmValue, byte[] state);


        /// <summary>
        /// Sets the current temperature resolution in Celsius in the provided state data. 
        /// Use the method WriteDevice() with this data to finalize the change to the device.
        /// </summary>
        /// <param name="resolution">temperature resolution in Celsius</param>
        /// <param name="state">byte array with device state information</param>
        /// <exception cref="OneWireException">Device does not support selectable temperature resolution</exception>
        /// <see cref="HasSelectableTemperatureResolution"/>
        /// <see cref="GetTemperatureResolution"/>
        /// <see cref="GetTemperatureResolutions"/>
        void SetTemperatureResolution(double resolution, byte[] state);

        #endregion // Temperature 'set' Methods
    }
}
