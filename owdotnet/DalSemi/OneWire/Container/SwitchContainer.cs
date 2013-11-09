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
	/// Interface class for 1-Wire switch devices. This class should be implemented for each switch type 1-Wire device.
	/// Features
	/// Supports activity sensing and clearing on devices with activity sensing
	/// Supports level sensing on devices with level sensing
	/// Supports switches with 'smart on' capabilities for 1-Wire branch searches
	/// 
	/// Usage
	/// SwitchContainer extends OneWireSensor, so the general usage model applies to any SwitchContainer:
	/// ReadDevice()
	/// perform operations on the SwitchContainer
	/// writeDevice(byte[])
	/// 
	/// Consider this interaction with a SwitchContainer that toggles all of the switches on the device:
	/// // switchcontainer is a DalSemi.OneWire.Container.SwitchContainer
	/// byte[] state = switchcontainer.readDevice();
	/// int number_of_switches = switchcontainer.getNumberChannels(state);
	/// System.out.println("This device has "+number_of_switches+" switches");
	/// for (int i=0; i &lt; number_of_switches; i++)
	/// {              
	///     boolean switch_state = switchcontainer.getLatchState(i, state);
	///     Console.WriteLine("Switch "+i+" is "+(switch_state ? "on" : "off"));
	///     switchcontainer.setLatchState(i,!switch_state,false,state);
	/// }
	/// switchcontainer.writeDevice(state);
	/// </summary>
	/// <seealso cref="OneWireSensor"/>
	/// <seealso cref="ClockContainer"/>
	/// <seealso cref="TemperatureContainer"/>
	/// <seealso cref="PotentiometerContainer"/>
	/// <seealso cref="ADContainer"/>
	public interface SwitchContainer : IOneWireSensor
	{



		//--------
		//-------- Switch Feature methods
		//--------

		/// <summary>
		/// Checks to see if the channels of this switch are 'high side'
		/// switches.  This indicates that when 'on' or <c>true</c>, the switch output is
		/// connect to the 1-Wire data.  If this method returns  <c>false</c>
		/// then when the switch is 'on' or <c>true</c>, the switch is connected to ground.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if the switch is a 'high side' switch; otherwise, <c>false</c>.
		/// </returns>
		/// <seealso cref="GetLatchState(int,byte[])"/>
		bool IsHighSideSwitch();

		/// <summary>
		/// Checks to see if the channels of this switch support
		/// activity sensing.  If this method returns <c>true</c> then the method <c>GetSensedActivity(int,byte[])</c> can be used.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if channels support activity sensing; otherwise, <c>false</c>.
		/// </returns>
		/// <seealso cref="GetSensedActivity(int,byte[])"/>
		/// <seealso cref="ClearActivity()"/>
		bool HasActivitySensing();

		/// <summary>
		/// Checks to see if the channels of this switch support
		/// level sensing.  If this method returns <c>true</c> then the method <c>GetLevel(int,byte[])</c> can be used.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if channels support level sensing; otherwise, <c>false</c>.
		/// </returns>
		/// <seealso cref="GetLevel(int,byte[])"/>
		bool HasLevelSensing();

		/// <summary>
		/// Checks to see if the channels of this switch support
		/// 'smart on'. Smart on is the ability to turn on a channel
		/// such that only 1-Wire device on this channel are awake
		/// and ready to do an operation.  This greatly reduces
		/// the time to discover the device down a branch.
		/// If this method returns then the method <c>SetLatchState(int,boolean,boolean,byte[])</c> can be used with the doSmart parameter
		/// </summary>
		/// <returns>
		/// 	<c>true</c>  if channels support 'smart on'; otherwise, <c>false</c>.
		/// </returns>
		/// <seealso cref="SetLatchState(int,boolean,boolean,byte[])"/>
		bool HasSmartOn();

		/// <summary>
		/// Checks to see if the channels of this switch require that only one
		/// channel is on at any one time.  If this method returns true then the
		/// method SetLatchState(int,boolean,boolean,byte[]) will not only affect
		/// the state of the given channel but may affect the state of the other
		/// channels as well to insure that only one channel is on at a time.
		/// </summary>
		/// <returns>
		/// true if only one channel can be on at a time.
		/// </returns>
		/// <seealso cref="SetLatchState(int,boolean,boolean,byte[])"/>
		bool OnlySingleChannelOn();

		//--------
		//-------- Switch 'get' Methods
		//--------

		/// <summary>
		/// Gets the number of channels supported by this switch.
		/// Channel specific methods will use a channel number specified
		/// by an integer from [0 to <c>(GetNumberChannels(byte[])</c> - 1)].  Note that
		/// all devices of the same family will not necessarily have the same number of channels.
		/// The DS2406 comes in two packages--one that  has a single channel, and one that has two channels.
		/// </summary>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>the number of channels for this device</returns>
		int GetNumberChannels( byte[] state );

		/// <summary>
		/// Checks the sensed level on the indicated channel.
		/// To avoid an exception, verify that this switch has level sensing with the <c>HasLevelSensing()</c>
		/// Level sensing means that the device can sense the logic level on its PIO pin.
		/// </summary>
		/// <param name="channel">/// Channel to execute this operation, in the range
		/// [0 to (<c>GetNumberChannels(byte[])</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// true if level is 'high' and false if 'low'
		/// </returns>
		/// <seealso cref="DalSemi.OneWire.Container.OneWireSensor.ReadDevice()"/>
		/// <seealso cref="HasLevelSensing()"/>
		bool GetLevel( int channel, byte[] state );

		/// <summary>
		/// Checks the latch state of the indicated channel.
		/// </summary>
		/// <param name="channel">Channel to execute this operation, in the range [0 to (<c>getNumberChannels(byte[])</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>readDevice()</c></param>
		/// <returns>
		/// 	<c>true</c> if channel latch is 'on' or conducting and <c>false</c> if channel latch is 'off' and not conducting.
		/// Note that the actual output when the latch is 'on' is returned from the <c>isHighSideSwitch()</c> method.
		/// </returns>
		/// <seealso cref="DalSemi.OneWire.Container.OneWireSensor.ReadDevice()"/>
		/// <seealso cref="IsHighSideSwitch()"/>
		/// <seealso cref="SetLatchState(int,boolean,boolean,byte[])"/>
		bool GetLatchState( int channel, byte[] state );

		/// <summary>
		/// Checks if the indicated channel has experienced activity.
		/// This occurs when the level on the PIO pins changes.  To clear
		/// the activity that is reported, call <c>clearActivity()</c>
		/// To avoid an exception, verify that this device supports activity
		/// sensing by calling the method <c>hasActivitySensing()</c>
		/// </summary>
		/// <param name="channel">Channel to execute this operation, in the range [0 to (<c>GetNumberChannels(byte[])</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// 	<c>true</c> if activity was detected and <c>false</c> if no activity was detected
		/// </returns>
		/// <exception cref="OneWireException">If this device does not have activity sensing</exception>
		/// <seealso cref="HasActivitySensing()"/>
		/// <seealso cref="ClearActivity()"/>
		Boolean GetSensedActivity( int channel, byte[] state );

		//--------
		//-------- Switch 'set' Methods
		//--------

		/// <summary>
		/// Sets the latch state of the indicated channel.
		/// The method <c>WriteDevice()</c> must be called to finalize changes to the device.
		/// Note that multiple 'Set' methods can be called before one call to <c>WriteDevice()</c>
		/// </summary>
		/// <param name="channel">Channel to execute this operation, in the range [0 to (<c>GetNumberChannels(byte[])</c> - 1)]</param>
		/// <param name="latchState"><c>true</c> to set the channel latch 'on' (conducting) and <c>false</c> to set the channel latch 'off' (not conducting).
		/// Note that the actual output when the latch is 'on' is returned from the <c>IsHighSideSwitch()</c> method.</param>
		/// <param name="doSmart">If latchState is 'on'/<c>true</c> then doSmart indicates if a 'smart on' is to be done.
		/// To avoid an exception check the capabilities of this device using the <c>HasSmartOn()</c> method.</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <seealso cref="HasSmartOn()"/>
		/// <seealso cref="GetLatchState(int,byte[])"/>
		/// <seealso cref="Dalsemi.OneWire.Container.OneWireSensor.WriteDevice(byte[])"/>
		void SetLatchState( int channel, bool latchState, bool doSmart, byte[] state );

		/// <summary>
		/// Clears the activity latches the next time possible.  For
		/// example, on a DS2406/07, this happens the next time the
		/// status is read with <c>ReadDevice()</c>.
		/// The activity latches will only be cleared once.  With the
		/// DS2406/07, this means that only the first call to <c>ReadDevice()</c>
		/// will clear the activity latches.  Subsequent calls to <c>ReadDevice()</c>
		/// will leave the activity latch states intact, unless this method has been invoked
		/// since the last call to <c>ReadDevice()</c>
		/// </summary>
		/// <exception cref="OneWireException">if this device does not support activity sensing</exception>
		/// <seealso cref="DalSemi.Onewire.Container.OneWireSensor.ReadDevice()"/>
		/// <seealso cref="GetSensedActivity(int,byte[])"/>
		void ClearActivity();



	}
}
