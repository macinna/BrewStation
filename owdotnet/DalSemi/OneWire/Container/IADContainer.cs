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
	/// Constant container
	/// </summary>
	public class ADContainerConstants
	{
		/// <summary>
		/// Indicates the high AD alarm.
		/// </summary>
		public const int ALARM_HIGH = 1;

		/// <summary>
		/// Indicates the low AD alarm.
		/// </summary>
		public const int ALARM_LOW = 0;

	}

	/// <summary>
	/// Interface class for 1-Wire devices that perform analog measuring
	/// operations. This class should be implemented for each A/D type 1-Wire device.
	///
	/// Features
	/// Allows multi-channel voltage readings
	/// Supports A/D Alarm enabling on devices with A/D Alarms
	/// Supports selectable A/D ranges on devices with selectable ranges
	/// Supports selectable A/D resolutions on devices with selectable resolutions
	///
	/// Usage
	///
	/// <c>ADContainer</c> extends <c>OneWireSensor</c>, so the general usage model applies to any <c>ADContainer</c>:
	/// ReadDevice()
	/// Perform operations on the <c>ADContainer</c>
	/// WriteDevice( byte[] )
	///
	/// Consider this interaction with an <c>ADContainer</c> that reads from all of its
	/// A/D channels, then tries to set its high alarm on its first channel (channel 0):
	///
	/// <c>
	///     //adcontainer is a com.dalsemi.onewire.container.ADContainer
	///     byte[] state = adcontainer.ReadDevice();
	///     double[] voltages = new double[adcontainer.getNumberADChannels()];
	///     for (int i=0; i &lt; adcontainer.getNumberADChannels(); i++)
	///     {
	///          adcontainer.doADConvert(i, state);         
	///          voltages[i] = adc.getADVoltage(i, state);
	///     }
	///     if (adcontainer.HasADAlarms())
	///     {
	///          double highalarm = adcontainer.GetADAlarm(0, ADContainer.ALARM_HIGH, state);
	///          adcontainer.setADAlarm(0, ADContainer.ALARM_HIGH, highalarm + 1.0, state);
	///          adcontainer.writeDevice(state);
	///     }
	///
	/// </c>
	/// </summary>
	public interface IADContainer : IOneWireSensor
	{
		//--------
		//-------- A/D Feature methods
		//--------

		/// <summary>
		/// Gets the number A/D channels.
		/// Channel specific methods will use a channel number specified
		/// by an integer from [0 to (<c>GetNumberADChannels()</c> - 1)]
		/// </summary>
		/// <returns>the number of channels</returns>
		int GetNumberADChannels();

		/// <summary>
		/// Determines whether this A/D measuring device has high/low alarms.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if this device has high/low trip alarms
		/// </returns>
		bool HasADAlarms();

		/// <summary>
		/// Gets an array of available ranges for the specified A/D channel.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1]</param>
		/// <returns>array indicating the available ranges starting from the largest range to the smallest range</returns>
		double[] GetADRanges( int channel );

		/// <summary>
		/// Gets an array of available resolutions based on the specified range on the specified A/D channel.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1]</param>
		/// <param name="range">A/D range setting from the <c>GetADRanges(int)</c> method</param>
		/// <returns>array indicating the available resolutions on this <c>channel</c> for this <c>range</c></returns>
		double[] GetADResolutions( int channel, double range );

		/// <summary>
		/// Determines whether this A/D supports doing multiple voltage convesions at the same time
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if the device can do multi-channel voltage reads
		/// </returns>
		Boolean CanADMultiChannelRead();

		//--------
		//-------- A/D IO Methods
		//--------

		/// <summary>
		/// Performs a voltage conversion on one specified channel.
		/// Use the method <c>GetADVoltage(int,byte[])</c> to read the result
		/// of this conversion, using the same <c>channel</c> argument as this method uses.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error such as no 1-Wire device present.
		/// This could be caused by a physical interruption in the 1-Wire Network due to shorts or a newly
		/// arriving 1-Wire device issuing a 'presence pulse'. This is usually a recoverable error.</exception>
		/// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter. 
		/// This is usually a non-recoverable error.</exception>
		void DoADConvert( int channel, byte[] state );

		/// <summary>
		/// Performs voltage conversion on one or more specified channels.
		/// The method <c>GetADVoltage(byte[])</c> can be used to read the result
		/// of the conversion(s). This A/D must support multi-channel read,
		/// reported by <c>CanADMultiChannelRead()</c> if more than 1 channel is specified.
		/// </summary>
		/// <param name="doConvert">Array of size <c>GetNumberADChannels()</c> representing which channels should perform conversions</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error such as no 1-Wire device present.
		/// This could be caused by a physical interruption in the 1-Wire Network due to shorts or a newly arriving
		/// 1-Wire device issuing a 'presence pulse'. This is usually a recoverable error.</exception>
		/// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter.
		/// This is usually a non-recoverable error.</exception>
		void DoADConvert( bool[] doConvert, byte[] state );

		/// <summary>
		/// Reads the value of the voltages after a <c>DoADConvert(boolean[],byte[])</c>
		/// method call.  This A/D device must support multi-channel reading, reported by
		/// <c>CanADMultiChannelRead()</c> if more than 1 channel conversion was attempted
		/// by <c>DoADConvert()</c>
		/// </summary>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>array with the voltage values for all channels</returns>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error such as no 1-Wire device present. 
		/// This could be caused by a physical interruption in the 1-Wire Network due to shorts or a newly
		/// arriving 1-Wire device issuing a 'presence pulse'. This is usually a recoverable error.</exception>
		/// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter. 
		/// This is usually a non-recoverable error.</exception>
		double[] GetADVoltage( byte[] state );

		/// <summary>
		/// Reads the value of the voltages after a <c>DoADConvert(int,byte[])</c>
		/// method call.  If more than one channel has been read it is more efficient to use the 
		/// <c>GetADVoltage(byte[])</c> method that returns all channel voltage values.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>the voltage value for the specified channel</returns>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error such as no 1-Wire device present. 
		/// This could be caused by a physical interruption in the 1-Wire Network due to shorts or a newly
		/// arriving 1-Wire device issuing a 'presence pulse'. This is usually a recoverable error.</exception>
		/// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter. 
		/// This is usually a non-recoverable error.</exception>
		double GetADVoltage( int channel, byte[] state );

		//--------
		//-------- A/D 'get' Methods
		//--------

		/// <summary>
		/// Reads the value of the specified A/D alarm on the specified channel.
		/// Not all A/D devices have alarms.  Check to see if this device has
		/// alarms first by calling the <c>HasADAlarms()</c> method.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="alarmType">Type of the desired alarm, ADContainerConstants.ALARM_HIGH ir ADContainerConstants.ALARM_LOW</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>the alarm value in volts</returns>
		/// <exception cref="OneWireException">if this device does not have A/D alarms</exception>
		double GetADAlarm( int channel, int alarmType, byte[] state );

		/// <summary>
		/// Checks to see if the specified alarm on the specified channel is enabled.
		/// Not all A/D devices have alarms.  Check to see if this device has
		/// alarms first by calling the <c>HasADAlarms()</c> method.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="alarmType">Type of the desired alarm, ADContainerConstants.ALARM_HIGH ir ADContainerConstants.ALARM_LOW</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns></returns>
		/// <exception cref="OneWireException">if this device does not have A/D alarms</exception>
		bool GetADAlarmEnable( int channel, int alarmType, byte[] state );

		/// <summary>
		/// Checks the state of the specified alarm on the specified channel.
		/// Not all A/D devices have alarms.  Check to see if this device has
		/// alarms first by calling the <c>HasADAlarms()</c> method.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="alarmType">Type of the desired alarm, ADContainerConstants.ALARM_HIGH ir ADContainerConstants.ALARM_LOW</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// 	<c>true</c> if specified alarm occurred
		/// </returns>
		/// <exception cref="OneWireException">if this device does not have A/D alarms</exception>
		bool HasADAlarmed( int channel, int alarmType, byte[] state );

		/// <summary>
		/// Returns the currently selected resolution for the specified
		/// channel.  This device may not have selectable resolutions,
		/// though this method will return a valid value.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>The current resolution of <c>channel</c> in volts</returns>
		double GetADResolution( int channel, byte[] state );

		/// <summary>
		/// Returns the currently selected range for the specified
		/// channel.  This device may not have selectable ranges,
		/// though this method will return a valid value.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>the input voltage range</returns>
		double GetADRange( int channel, byte[] state );

		//--------
		//-------- A/D 'set' Methods
		//--------

		/// <summary>
		/// Sets the voltage value of the specified alarm on the specified channel.
		/// The method <c>WriteDevice()</c> must be called to finalize changes to the device.
		/// Note that multiple 'Set' methods can be called before one call to <c>WriteDevice()</c>.
		/// Also note that not all A/D devices have alarms. Check to see if this device has
		/// alarms first by calling the <c>HasADAlarms()</c> method.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="alarmType">Type of the desired alarm, ADContainerConstants.ALARM_HIGH ir ADContainerConstants.ALARM_LOW</param>
		/// <param name="alarm">New alarm value.</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <exception cref="OneWireException">If this device does not have A/D alarms</exception>
		void SetADAlarm( int channel, int alarmType, double alarm, byte[] state );

		/// <summary>
		/// Enables or disables the specified alarm on the specified channel.
		/// The method <c>WriteDevice()</c> must be called to finalize changes to the device.
		/// Note that multiple 'Set' methods can be called before one call to <c>WriteDevice()</c>.
		/// Also note that not all A/D devices have alarms. Check to see if this device has
		/// alarms first by calling the <c>HasADAlarms()</c> method.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="alarmType">Type of the desired alarm, ADContainerConstants.ALARM_HIGH ir ADContainerConstants.ALARM_LOW</param>
		/// <param name="alarmEnable"><c>true</c> to enable alarm, otherwise <c>false</c></param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <exception cref="OneWireException">If this device does not have A/D alarms</exception>
		void SetADAlarmEnable( int channel, int alarmType, bool alarmEnable, byte[] state );

		/// <summary>
		/// Sets the conversion resolution value for the specified channel.
		/// The method <c>WriteDevice()</c> must be called to finalize changes to the device.
		/// Note that multiple 'Set' methods can be called before one call to <c>WriteDevice()</c>.
		/// Also note that not all A/D devices have alarms. Check to see if this device has
		/// alarms first by calling the <c>HasADAlarms()</c> method.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="resolution">One of the resolutions returned by <c>GetADResolutions(int,double)</c></param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		void SetADResolution( int channel, double resolution, byte[] state );

		/// <summary>
		/// Sets the input range for the specified channel.
		/// Note that multiple 'Set' methods can be called before one call to <c>WriteDevice()</c>.
		/// Also note that not all A/D devices have alarms. Check to see if this device has
		/// alarms first by calling the <c>HasADAlarms()</c> method.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="range">One of the ranges returned by <c>GetADRanges(int)</c></param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <param name="channel">The channel.</param>
		void SetADRange( int channel, double range, byte[] state );
	}
}
