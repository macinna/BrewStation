/*---------------------------------------------------------------------------
 * Copyright (C) 1999,2000 Dallas Semiconductor Corporation, All Rights Reserved.
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

using DalSemi.OneWire.Adapter; // PortAdapter, OWSpeed

namespace DalSemi.OneWire.Container
{


	/// <summary>
	/// 1-Wire container for a Single Addressable Switch, DS2413.  This container
	/// encapsulates the functionality of the 1-Wire family type <B>3A</B> (hex)</P>
	/// </summary>
	public class OneWireContainer3A : OneWireContainer, SwitchContainer
	{
		/// <summary>
		/// Gets the family code.
		/// </summary>
		/// <returns></returns>
		public static byte GetFamilyCode()
		{
			return 0x3A;
		}


		/// <summary>
		/// PIO Access read command
		/// </summary>
		public const byte PIO_ACCESS_READ = 0xF5;


		/// <summary>
		/// PIO Access write command
		/// </summary>
		public const byte PIO_ACCESS_WRITE = 0x5A;

		private const int PIO_STATUS_READ_BYTE = 1;
		private const int PIO_STATUS_WRITE_BYTE = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="OneWireContainer3A"/> class.
		/// </summary>
		/// <param name="sourceAdapter">adapter object required to communicate with this device.</param>
		/// <param name="newAddress">address of this 1-Wire device</param>
		/// <see cref="OneWireContainer()"/>
		/// <see cref="DalSemi.OneWire.Utils.Address"/>
		public OneWireContainer3A( PortAdapter sourceAdapter, byte[] newAddress )
			: base( sourceAdapter, newAddress )
		{ }


		/// <summary>
		/// Retrieves the Dallas Semiconductor part number of the 1-Wire device as a string.  For example 'Crypto iButton' or 'DS1992'.
		/// </summary>
		/// <returns>1-Wire device name</returns>
		public override string GetName()
		{
			return "DS2413";
		}



		/// <summary>
		/// Retrieves the alternate Dallas Semiconductor part numbers or names.
		/// A 'family' of 1-Wire Network devices may have more than one part number depending on packaging.
		/// There can also be nicknames such as 'Crypto iButton'.
		/// </summary>
		/// <returns>1-Wire device alternate names</returns>
		public override string GetAlternateNames()
		{
			return "Dual Channel Switch";
		}


		/// <summary>
		/// Retrieves a short description of the function of the 1-Wire device type.
		/// </summary>
		/// <returns>Device functional description</returns>
		public override string GetDescription()
		{
			return "Dual Channel Addressable Switch";
		}


		/// <summary>
		/// Returns the maximum speed this iButton or 1-Wire device can communicate at.
		/// Override this method if derived iButton type can go faster than SPEED_REGULAR(0).
		/// </summary>
		/// <returns>The maxumin speed for the this device</returns>
		/// <see cref="DSPortAdapter.SetSpeed"/>
		public override OWSpeed GetMaxSpeed()
		{
			return OWSpeed.SPEED_OVERDRIVE;
		}


		/// <summary>
		/// Gets the number of channels supported by this switch.
		/// Channel specific methods will use a channel number specified
		/// by an integer from [0 to <c>(GetNumberChannels(byte[])</c> - 1)].  Note that
		/// all devices of the same family will not necessarily have the same number of channels.
		/// The DS2406 comes in two packages--one that  has a single channel, and one that has two channels.
		/// </summary>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>the number of channels for this device</returns>
		public int GetNumberChannels( byte[] state )
		{
			return 2;
		}


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
		public bool IsHighSideSwitch()
		{
			return false;
		}

		/// <summary>
		/// Checks to see if the channels of this switch support
		/// activity sensing.  If this method returns <c>true</c> then the method <c>GetSensedActivity(int,byte[])</c> can be used.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if channels support activity sensing; otherwise, <c>false</c>.
		/// </returns>
		/// <seealso cref="GetSensedActivity(int,byte[])"/>
		/// <seealso cref="ClearActivity()"/>
		public bool HasActivitySensing()
		{
			return false;
		}

		/// <summary>
		/// Checks to see if the channels of this switch support
		/// level sensing.  If this method returns <c>true</c> then the method <c>GetLevel(int,byte[])</c> can be used.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if channels support level sensing; otherwise, <c>false</c>.
		/// </returns>
		/// <seealso cref="GetLevel(int,byte[])"/>
		public bool HasLevelSensing()
		{
			return true;
		}

		/// <summary>
		/// Checks to see if the channels of this switch support
		/// 'smart on'. Smart on is the ability to turn on a channel
		/// such that only 1-Wire device on this channel are awake
		/// and ready to do an operation.  This greatly reduces
		/// the time to discover the device down a branch.
		/// If this method returns then the method <c>SetLatchState(int,bool,bool,byte[])</c> can be used with the doSmart parameter
		/// </summary>
		/// <returns>
		/// 	<c>true</c>  if channels support 'smart on'; otherwise, <c>false</c>.
		/// </returns>
		/// <seealso cref="SetLatchState(int,bool,bool,byte[])"/>
		public bool HasSmartOn()
		{
			return false;
		}

		/// <summary>
		/// Checks to see if the channels of this switch require that only one
		/// channel is on at any one time.  If this method returns true then the
		/// method SetLatchState(int,bool,bool,byte[]) will not only affect
		/// the state of the given channel but may affect the state of the other
		/// channels as well to insure that only one channel is on at a time.
		/// </summary>
		/// <returns>
		/// true if only one channel can be on at a time.
		/// </returns>
		/// <seealso cref="SetLatchState(int,bool,bool,byte[])"/>
		public bool OnlySingleChannelOn()
		{
			return false;
		}


		/// <summary>
		/// Checks the sensed level on the indicated channel.
		/// To avoid an exception, verify that this switch has level sensing with the <c>HasLevelSensing()</c>
		/// Level sensing means that the device can sense the logic level on its PIO pin.
		/// </summary>
		/// <param name="channel">Channel to execute this operation, in the range [0 to (<c>GetNumberChannels(byte[])</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// true if level is 'high' and false if 'low'
		/// </returns>
		/// <seealso cref="DalSemi.OneWire.Container.OneWireSensor.ReadDevice()"/>
		/// <seealso cref="HasLevelSensing()"/>
		public bool GetLevel( int channel, byte[] state )
		{
			if( channel < 0 || channel >= GetNumberChannels( state ) ) {
				throw new ArgumentOutOfRangeException( "channel: " + channel );
			}

			byte bit = 1;
			bit <<= channel * 2;
			return ( state[PIO_STATUS_READ_BYTE] & bit ) > 0;
		}


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
		/// <seealso cref="SetLatchState(int,bool,bool,byte[])"/>
		public bool GetLatchState( int channel, byte[] state )
		{
			if( channel < 0 || channel >= GetNumberChannels( state ) ) {
				throw new ArgumentOutOfRangeException( "channel: " + channel );
			}

			byte bit = 1;
			bit <<= ( channel * 2 );
			bit += 1;

			// A 1 on the latch means it is high and thus 'off'
			return ( state[PIO_STATUS_READ_BYTE] & bit ) == 0;
		}

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
		public bool GetSensedActivity( int channel, byte[] state )
		{
			return false;
		}

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
		public void ClearActivity()
		{
		}


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
		public void SetLatchState( int channel, bool latchState, bool doSmart, byte[] state )
		{
			if( channel < 0 || channel >= GetNumberChannels( state ) ) {
				throw new ArgumentOutOfRangeException( "channel: " + channel );
			}

			byte bit = 1;
			bit <<= channel;

			// Get the current states
			for( int i = 0; i < GetNumberChannels( state ); ++i ) {
				if( GetLatchState( i, state ) ) {
					// Sending 0 turns the output on
					state[PIO_STATUS_WRITE_BYTE] &= (byte)~bit;
				}
				else {
					// Sending 1 turns the ouput off
					state[PIO_STATUS_WRITE_BYTE] |= bit;
				}

			}

			// Update with desired state
			if( latchState ) {
				// Sending 0 turns the output on
				state[PIO_STATUS_WRITE_BYTE] &= (byte)~bit;
			}
			else {
				// Sending 1 turns the ouput off
				state[PIO_STATUS_WRITE_BYTE] |= bit;
			}

			// Always set the upper six bits
			state[0] |= 0xFC;
		}

		/// <summary>
		/// Retrieves the 1-Wire device sensor state.  This state is
		/// returned as a byte array.  Pass this byte array to the 'Get'
		/// and 'Set' methods.  If the device state needs to be changed then call
		/// the 'EriteDevice' to finalize the changes.
		/// </summary>
		/// <returns>1-Wire device sensor state</returns>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error such as
		/// reading an incorrect CRC from a 1-Wire device.  This could be
		/// caused by a physical interruption in the 1-Wire Network due to
		/// shorts or a newly arriving 1-Wire device issuing a 'presence pulse'</exception>
		/// <exception cref="OneWireException">on a communication or setup error with the 1-Wire adapter</exception>
		public byte[] ReadDevice()
		{
			byte[] buff = new byte[2];

			buff[0] = (byte)0xF5;  // PIO Access Read Command
			buff[1] = (byte)0xFF;  // Used to read the PIO Status Bit Assignment

			// select the device
			if( adapter.SelectDevice( address ) ) {
				adapter.DataBlock( buff, 0, 2 );
			}
			else
				throw new OneWireIOException( "Device select failed" );

			return buff;
		}

		/// <summary>
		/// Writes the 1-Wire device sensor state that
		/// have been changed by 'set' methods. Only the state registers that
		/// changed are updated.  This is done by referencing a field information
		/// appended to the state data.
		/// </summary>
		/// <param name="state">1-Wire device sensor state</param>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error such as
		/// reading an incorrect CRC from a 1-Wire device.  This could be
		/// caused by a physical interruption in the 1-Wire Network due to
		/// shorts or a newly arriving 1-Wire device issuing a 'presence pulse'</exception>
		/// <exception cref="OneWireException">on a communication or setup error with the 1-Wire adapter</exception>
		public void WriteDevice( byte[] state )
		{
			byte[] buff = new byte[5];

			buff[0] = (byte)0x5A;      // PIO Access Write Command
			buff[1] = (byte)state[PIO_STATUS_WRITE_BYTE];  // Channel write information
			buff[2] = (byte)~state[PIO_STATUS_WRITE_BYTE]; // Inverted write byte
			buff[3] = (byte)0xFF;      // Confirmation Byte
			buff[4] = (byte)0xFF;      // PIO Pin Status

			// select the device
			if( adapter.SelectDevice( address ) ) {
				adapter.DataBlock( buff, 0, 5 );
			}
			else {
				throw new OneWireIOException( "Device select failed" );
			}

			if( buff[3] != (byte)0xAA ) {
				throw new OneWireIOException( "Failure to change latch state." );
			}
		}
	}
}
