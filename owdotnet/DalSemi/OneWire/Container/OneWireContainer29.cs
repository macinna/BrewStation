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

namespace DalSemi.OneWire.Container
{


	/// <summary>
	/// 1-Wire container for a Single Addressable Switch, DS2408.  This container
	//// encapsulates the functionality of the 1-Wire family type 0x29
	///
	/// Features
	/// - Eight channels of programmable I/O with open-drain outputs
	/// - Logic level sensing of the PIO pin can be sensed
	/// - Multiple DS2408's can be identified on a common 1-Wire bus and operated independently.
	/// - Supports 1-Wire Conditional Search command with response controlled by programmable PIO conditions
	/// - Supports Overdrive mode which boosts communication speed up to 142k bits per second.
	/// </summary>
	public class OneWireContainer29 : OneWireContainer, SwitchContainer
	{

		/// <summary>
		/// Gets the family code.
		/// </summary>
		/// <returns></returns>
		public static byte GetFamilyCode()
		{
			return 0x29;
		}


		//--------
		//-------- Variables
		//--------

		/// <summary>
		/// Status memory bank of the DS2408 for memory map registers
		/// </summary>
		private MemoryBankEEPROMstatus map;

		/// <summary>
		/// Status memory bank of the DS2408 for the conditional Search
		/// </summary>
		private MemoryBankEEPROMstatus search;

		/// <summary>
		/// Reset the activity latches
		/// </summary>
		public const byte RESET_ACTIVITY_LATCHES = 0xC3;

		private const byte CONDITIONAL_CHANNEL_SELECTION = 0;
		private const byte CONDITIONAL_CHANNEL_POLARITY = 1;
		private const byte CONTROL_STATUS_REGISTER = 2;



		/// <summary>
		/// Used for 0xFF array
		/// </summary>
		private byte[] FF = new byte[8];


		//--------
		//-------- Constructors
		//--------

		/// <summary>
		/// Initializes a new instance of the <see cref="OneWireContainer29"/> class.
		/// </summary>
		/// <param name="sourceAdapter">adapter object required to communicate with this device.</param>
		/// <param name="newAddress">address of this 1-Wire device</param>
		/// <see cref="OneWireContainer()"/>
		/// <see cref="DalSemi.OneWire.Utils.Address"/>
		public OneWireContainer29( PortAdapter sourceAdapter, byte[] newAddress )
			: base( sourceAdapter, newAddress )
		{
			Initmem();

			for( int i = 0; i < FF.Length; i++ )
				FF[i] = 0x0FF;
		}

		//--------
		//-------- Methods
		//--------


		/// <summary>
		/// Retrieves the Dallas Semiconductor part number of the 1-Wire device as a string.  For example 'Crypto iButton' or 'DS1992'.
		/// </summary>
		/// <returns>1-Wire device name</returns>
		public override string GetName()
		{
			return "DS2408";
		}


		/// <summary>
		/// Returns an a MemoryBankList of MemoryBanks.  Default is no memory banks.
		/// </summary>
		/// <returns>
		/// enumeration of memory banks to read and write memory on this iButton or 1-Wire device
		/// </returns>
		/// <see cref="MemoryBank"/>
		public override MemoryBankList GetMemoryBanks()
		{
			MemoryBankList bank_vector = new MemoryBankList();

			bank_vector.Add( map );
			bank_vector.Add( search );

			return bank_vector;
		}

		/// <summary>
		/// Retrieves the alternate Dallas Semiconductor part numbers or names.
		/// A 'family' of 1-Wire Network devices may have more than one part number depending on packaging.
		/// There can also be nicknames such as 'Crypto iButton'.
		/// </summary>
		/// <returns>1-Wire device alternate names</returns>
		public override string GetAlternateNames()
		{
			return "8-Channel Addressable Switch";
		}

		/// <summary>
		/// Retrieves a short description of the function of the 1-Wire device type.
		/// </summary>
		/// <returns>Device functional description</returns>
		public override string GetDescription()
		{
			return "1-Wire 8-Channel Addressable Switch";
		}

		//--------
		//-------- Switch Feature methods
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
		public int GetNumberChannels( byte[] state )
		{
			// check the 88h byte bits 6 and 7
			// 00 - 4 channels
			// 01 - 5 channels
			// 10 - 8 channels
			// 11 - 16 channes, which hasn't been implemented yet
			return 8;
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
		public Boolean IsHighSideSwitch()
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
		public Boolean HasActivitySensing()
		{
			return true;
		}

		/// <summary>
		/// Checks to see if the channels of this switch support
		/// level sensing.  If this method returns <c>true</c> then the method <c>GetLevel(int,byte[])</c> can be used.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if channels support level sensing; otherwise, <c>false</c>.
		/// </returns>
		/// <seealso cref="GetLevel(int,byte[])"/>
		public Boolean HasLevelSensing()
		{
			return true;
		}

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
		public Boolean HasSmartOn()
		{
			return false;
		}

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
		public Boolean OnlySingleChannelOn()
		{
			return false;
		}

		//--------
		//-------- Switch 'get' Methods
		//--------


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
		public Boolean GetLevel( int channel, byte[] state )
		{
			byte level = (byte)( 0x01 << channel );
			return ( ( state[0] & level ) == level );
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
		/// <seealso cref="SetLatchState(int,boolean,boolean,byte[])"/>
		public Boolean GetLatchState( int channel, byte[] state )
		{
			byte latch = (byte)( 0x01 << channel );
			return ( ( state[1] & latch ) == latch );
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
		public Boolean GetSensedActivity( int channel, byte[] state )
		{
			byte activity = (byte)( 0x01 << channel );
			return ( ( state[2] & activity ) == activity );
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
			adapter.SelectDevice( address );
			byte[] buffer = new byte[9];

			buffer[0] = RESET_ACTIVITY_LATCHES;
			Array.Copy( FF, 0, buffer, 1, 8 );

			adapter.DataBlock( buffer, 0, 9 );

			if( ( buffer[1] != 0xAA ) && ( buffer[1] != 0x55 ) )
				throw new OneWireException( "Sense Activity was not cleared." );
		}

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
		public void SetLatchState( int channel, Boolean latchState,
								   Boolean doSmart, byte[] state )
		{
			byte latch = (byte)( 0x01 << channel );

			if( latchState )
				state[1] = (byte)( state[1] | latch );
			else
				state[1] = (byte)( state[1] & ~latch );
		}

		/// <summary>
		/// Sets the state of the latch.
		/// </summary>
		/// <param name="set">The state</param>
		/// <param name="state">The state array read by ReadDevice()</param>
		public void SetLatchState( byte set, byte[] state )
		{
			state[1] = set;
		}


		/// <summary>
		/// Retrieves the 1-Wire device sensor state.  This state is
		/// returned as a byte array.  Pass this byte array to the 'Get'
		/// and 'Set' methods.  If the device state needs to be changed then call
		/// the 'WriteDevice' to finalize the changes.
		/// </summary>
		/// <returns>1-Wire device sensor state</returns>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error such as
		/// reading an incorrect CRC from a 1-Wire device.  This could be
		/// caused by a physical interruption in the 1-Wire Network due to
		/// shorts or a newly arriving 1-Wire device issuing a 'presence pulse'</exception>
		/// <exception cref="OneWireException">on a communication or setup error with the 1-Wire adapter</exception>
		public byte[] ReadDevice()
		{
			byte[] state = new byte[3];

			Array.Copy( FF, 0, state, 0, 3 );
			map.Read( 0, false, state, 0, 3 );

			return state;
		}

		/// <summary>
		/// Reads the register.
		/// </summary>
		/// <returns></returns>
		public byte[] ReadRegister()
		{
			byte[] register = new byte[3];

			search.Read( 0, false, register, 0, 3 );

			return register;
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
			map.Write( 1, state, 1, 1 );
		}


		/// <summary>
		/// Writes the register.
		/// </summary>
		/// <param name="register">The register.</param>
		public void WriteRegister( byte[] register )
		{
			search.Write( 0, register, 0, 3 );
		}


		/// <summary>
		/// Sets the reset mode.
		/// </summary>
		/// <param name="register">The register.</param>
		/// <param name="set">if set to <c>true</c> register mode is on</param>
		public void SetResetMode( byte[] register, Boolean set )
		{
			if( set && ( ( register[CONTROL_STATUS_REGISTER] & 0x04 ) == 0x04 ) ) {
				register[CONTROL_STATUS_REGISTER] = (byte)( register[CONTROL_STATUS_REGISTER] & 0xFB );
			}
			else if( ( !set ) && ( ( register[CONTROL_STATUS_REGISTER] & 0x04 ) == 0x00 ) ) {
				register[CONTROL_STATUS_REGISTER] = (byte)( register[CONTROL_STATUS_REGISTER] | 0x04 );
			}
		}


		/// <summary>
		/// Retrieves the state of the VCC pin.  If the pin is powered 'true' is
		/// returned else 'false' is returned if the pin is grounded.
		/// </summary>
		/// <param name="register">The register.</param>
		/// <returns></returns>
		public Boolean GetVCC( byte[] register )
		{
			if( ( register[CONTROL_STATUS_REGISTER] & 0x80 ) == 0x80 )
				return true;

			return false;
		}


		/// <summary>
		/// Clears the power on reset.
		/// </summary>
		/// <param name="register">The register.</param>
		public void ClearPowerOnReset( byte[] register )
		{
			if( ( register[CONTROL_STATUS_REGISTER] & 0x08 ) == 0x08 ) {
				register[CONTROL_STATUS_REGISTER] = (byte)( register[CONTROL_STATUS_REGISTER] & 0xF7 );
			}
		}

		/// <summary>
		/// Gets the power on reset latch state.
		/// </summary>
		/// <param name="register">The register.</param>
		public bool GetPowerOnReset( byte[] register )
		{
			return ( register[CONTROL_STATUS_REGISTER] & 0x08 ) == 0x08;
		}

		/// <summary>
		/// Sets the RSTZ pin mode.
		/// </summary>
		/// <param name="strobe">if set to <c>true</c> [strobe].</param>
		/// <param name="register">The register.</param>
		public void SetROS( bool strobe, byte[] register )
		{
			// RSTZ pin is byte three in the register
			if( strobe ) {
				register[CONTROL_STATUS_REGISTER] |= 0x04;
			}
			else {
				register[CONTROL_STATUS_REGISTER] &= 0x8D;
			}
		}


		/// <summary>
		/// Gets the RSTZ pin mode. 
		/// </summary>
		/// <param name="register">The register.</param>
		/// <returns>true if the pin is set to strobe</returns>
		public bool GetROS( byte[] register )
		{
			// RSTZ pin is byte three in the register
			return ( register[CONTROL_STATUS_REGISTER] & 0x04 ) == 0x04;
		}


		/// <summary>
		/// Checks if the 'or' Condition Search is set and if not sets it.
		/// </summary>
		/// <param name="register">The register array</param>
		public void ORConditionalSearch( byte[] register )
		{
			if( ( register[CONTROL_STATUS_REGISTER] & 0x02 ) == 0x02 ) {
				register[CONTROL_STATUS_REGISTER] = (byte)( register[CONTROL_STATUS_REGISTER] & 0xFD );
			}
		}


		/// <summary>
		/// Checks if the 'and' Conditional Search is set and if not sets it.
		/// </summary>
		/// <param name="register">The register array</param>
		public void ANDConditionalSearch( byte[] register )
		{
			if( ( register[CONTROL_STATUS_REGISTER] & 0x02 ) != 0x02 ) {
				register[CONTROL_STATUS_REGISTER] = (byte)( register[CONTROL_STATUS_REGISTER] | 0x02 );
			}
		}


		/// <summary>
		/// Checks if the 'PIO' Conditional Search is set for input and if not sets it.
		/// </summary>
		/// <param name="register">Current register for conditional Search, which is returned from ReadRegister()</param>
		/// 
		public void PIOConditionalSearch( byte[] register )
		{
			if( ( register[CONTROL_STATUS_REGISTER] & 0x01 ) == 0x01 ) {
				register[CONTROL_STATUS_REGISTER] = (byte)( register[CONTROL_STATUS_REGISTER] & 0xFE );
			}
		}


		/// <summary>
		/// Checks if the activity latches are set for Conditional Search and if not sets it.
		/// </summary>
		/// <param name="register">Current register for conditional Search, which is returned from ReadRegister()</param>
		public void ActivityConditionalSearch( byte[] register )
		{
			if( ( register[CONTROL_STATUS_REGISTER] & 0x01 ) != 0x01 ) {
				register[CONTROL_STATUS_REGISTER] = (byte)( register[CONTROL_STATUS_REGISTER] | 0x01 );
			}
		}

		/// <summary>
		/// Sets the channel passed to the proper state depending on the set parameter for
		/// responding to the Conditional Search.
		/// </summary>
		/// <param name="channel">channel to set</param>
		/// <param name="set">whether to turn the channel on/off for Conditional Search</param>
		/// <param name="register">Current register for conditional Search, which is returned from ReadRegister()</param>
		public void SetChannelMask( int channel, Boolean set, byte[] register )
		{
			byte mask = (byte)( 0x01 << channel );

			if( set )
				register[CONDITIONAL_CHANNEL_SELECTION] = (byte)( register[CONDITIONAL_CHANNEL_SELECTION] | mask );
			else
				register[CONDITIONAL_CHANNEL_SELECTION] = (byte)( register[CONDITIONAL_CHANNEL_SELECTION] & ~mask );
		}

		/// <summary>
		/// Sets the channel passed to the proper state depending on the set parameter for
		/// the correct polarity in the Conditional Search.
		/// </summary>
		/// <param name="channel">current channel to set</param>
		/// <param name="set">whether to turn the channel on/off for polarity Conditional Search</param>
		/// <param name="register">current register for conditional Search, which is returned from ReadRegister()</param>
		public void SetChannelPolarity( int channel, Boolean set, byte[] register )
		{
			byte polarity = (byte)( 0x01 << channel );

			if( set )
				register[CONDITIONAL_CHANNEL_POLARITY] = (byte)( register[CONDITIONAL_CHANNEL_POLARITY] | polarity );
			else
				register[CONDITIONAL_CHANNEL_POLARITY] = (byte)( register[CONDITIONAL_CHANNEL_POLARITY] & ~polarity );
		}



		/// <summary>
		/// Retrieves the information if the channel is masked for the Conditional Search.
		/// </summary>
		/// <param name="channel">current channel to set</param>
		/// <param name="register">current register for conditional Search, which is returned from ReadRegister()</param>
		/// <returns>true if the channel is masked and false other wise</returns>
		public Boolean GetChannelMask( int channel, byte[] register )
		{
			byte mask = (byte)( 0x01 << channel );

			return ( ( register[CONDITIONAL_CHANNEL_SELECTION] & mask ) == mask );
		}



		/// <summary>
		/// Retrieves the polarity of the channel for the Conditional Search.
		/// </summary>
		/// <param name="channel">The channel to set.</param>
		/// <param name="register">urrent register for conditional Search, which is returned from ReadRegister()</param>
		/// <returns>true if the channel polarity is correct</returns>
		public Boolean GetChannelPolarity( int channel, byte[] register )
		{
			byte polarity = (byte)( 0x01 << channel );

			return ( ( register[CONDITIONAL_CHANNEL_POLARITY] & polarity ) == polarity );
		}

		/// <summary>
		/// Initialize the memory banks and data associated with each.
		/// </summary>
		private void Initmem()
		{
			// Memory map registers
			map = new MemoryBankEEPROMstatus( this );
			map.bankDescription = "Memory mapped register of pin logic state, port output latch logic state and activity latch logic state.";
			map.startPhysicalAddress = 0x88;
			map.size = 3; // 0x88 - 0x8A
			map.readOnly = true;

			// Conditional Search
			search = new MemoryBankEEPROMstatus( this );
			search.bankDescription = "Conditional Search bit mask, polarity bit mask and control register.";
			search.startPhysicalAddress = 0x8B;
			search.size = 3; // 0x8B - 0x8D
			search.readWrite = true;
		}
	}
}
