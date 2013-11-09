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
using DalSemi.OneWire.Utils; // Bit
using DalSemi.Utils; // CRC16

namespace DalSemi.OneWire.Container
{



	/// <summary>
	/// 1-Wire container that encapsulates the functionality of the
	/// 1-Wire family type <b>20</b> (hex), Dallas Semiconductor part number: DS2450, 1-Wire Quad A/D Converter.
	/// Features
	///		Four high-impedance inputs
	///		Programmable input range (2.56V, 5.12V),
	///		resolution (1 to 16 bits) and alarm thresholds
	///		5V, single supply operation
	///		Very low power, 2.5 mW active, 25 &#181W idle
	///		Unused analog inputs can serve as open drain digital outputs for closed-loop control
	///		Operating temperature range from -40C to +85C
	///		
	/// Note
	/// When converting analog voltages to digital, the user of the device must
	/// gaurantee that the voltage seen by the channel of the quad A/D does not exceed
	/// the selected input range of the device.  If this happens, the device will default
	/// to reading 0 volts.  There is NO way to know if the device is reading a higher than
	/// specified voltage or NO voltage.
	/// </summary>
	public class OneWireContainer20 : OneWireContainer, IADContainer
	{

		public static byte GetFamilyCode()
		{
			return 0x20;
		}

		//--------
		//-------- Static Final Variables
		//--------

		/// <summary>
		/// Offset of BITMAP in array returned from read state
		/// </summary>
		public const int BITMAP_OFFSET = 24;

		/// <summary>
		/// Offset of ALARMS in array returned from read state
		/// </summary>
		public const int ALARM_OFFSET = 8;

		/// <summary>
		/// Offset of external power offset in array returned from read state
		/// </summary>
		public const int EXPOWER_OFFSET = 20;

		/// <summary>
		/// Channel A number
		/// </summary>
		public const int CHANNELA = 0;


		/// <summary>
		/// Channel B number
		/// </summary>
		public const int CHANNELB = 1;


		/// <summary>
		/// Channel C number
		/// </summary>
		public const int CHANNELC = 2;


		/// <summary>
		/// Channel D number
		/// </summary>
		public const int CHANNELD = 3;

		/// <summary>
		/// No preset value
		/// </summary>
		public const int NO_PRESET = 0;

		/// <summary>
		/// Preset value to zeros
		/// </summary>
		public const int PRESET_TO_ZEROS = 1;

		/// <summary>
		/// Preset value to ones
		/// </summary>
		public const int PRESET_TO_ONES = 2;

		/// <summary>
		/// Number of channels
		/// </summary>
		public const int NUM_CHANNELS = 4;

		/// <summary>
		/// DS2450 Convert command
		/// </summary>
		private const byte CONVERT_COMMAND = (byte)0x3C;

		//--------
		//-------- Variables
		//--------

		/// <summary>
		/// Voltage readout memory bank
		/// </summary>
		private MemoryBankAD readout;


		/// <summary>
		/// Control/Alarms/calibration memory banks vector
		/// </summary>
		private MemoryBankList regs;

		//--------
		//-------- Constructors
		//--------

		/// <summary>
		/// Initializes a new instance of the <see cref="OneWireContainer20"/> class.
		/// </summary>
		/// <param name="sourceAdapter">adapter object required to communicate with this device.</param>
		/// <param name="newAddress">address of this 1-Wire device</param>
		public OneWireContainer20( PortAdapter sourceAdapter, byte[] newAddress )
			: base( sourceAdapter, newAddress )
		{
			// initialize the memory banks
			InitMem();
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
			return "DS2450";
		}


		/// <summary>
		/// Retrieves the alternate Dallas Semiconductor part numbers or names.
		/// A 'family' of 1-Wire Network devices may have more than one part number depending on packaging.
		/// There can also be nicknames such as 'Crypto iButton'.
		/// </summary>
		/// <returns>1-Wire device alternate names</returns>
		public override string GetAlternateNames()
		{
			return "1-Wire Quad A/D Converter";
		}


		/// <summary>
		/// Retrieves a short description of the function of the 1-Wire device type.
		/// </summary>
		/// <returns>Device functional description</returns>
		public override string GetDescription()
		{
			return "Four high-impedance inputs for measurement of analog "
				   + "voltages.  User programable input range.  Very low "
				   + "power.  Built-in multidrop controller.  Channels "
				   + "not used as input can be configured as outputs "
				   + "through the use of open drain digital outputs. "
				   + "Capable of use of Overdrive for fast data transfer. "
				   + "Uses on-chip 16-bit CRC-generator to guarantee good data.";
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
		/// Returns an a MemoryBankList of MemoryBanks.  Default is no memory banks.
		/// </summary>
		/// <returns>
		/// enumeration of memory banks to read and write memory on this iButton or 1-Wire device
		/// </returns>
		/// <see cref="MemoryBank"/>
		public override MemoryBankList GetMemoryBanks()
		{
			MemoryBankList bank_vector = new MemoryBankList();

			// readout
			bank_vector.Add( readout );

			// control/alarms/calibration
			for( int i = 0; i < 3; i++ )
				bank_vector.Add( regs[i] );

			return bank_vector;
		}



		//--------
		//-------- A/D Feature methods
		//--------


		/// <summary>
		/// Gets the number A/D channels.
		/// Channel specific methods will use a channel number specified
		/// by an integer from [0 to (<c>GetNumberADChannels()</c> - 1)]
		/// </summary>
		/// <returns>the number of channels</returns>
		public int GetNumberADChannels()
		{
			return NUM_CHANNELS;
		}


		/// <summary>
		/// Determines whether this A/D measuring device has high/low alarms.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if this device has high/low trip alarms
		/// </returns>
		public bool HasADAlarms()
		{
			return true;
		}


		/// <summary>
		/// Gets an array of available ranges for the specified A/D channel.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1]</param>
		/// <returns>
		/// array indicating the available ranges starting from the largest range to the smallest range
		/// </returns>
		public double[] GetADRanges( int channel )
		{
			double[] ranges = new double[2];

			ranges[0] = 5.12;
			ranges[1] = 2.56;

			return ranges;
		}


		/// <summary>
		/// Gets an array of available resolutions based on the specified range on the specified A/D channel.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1]</param>
		/// <param name="range">A/D range setting from the <c>GetADRanges(int)</c> method</param>
		/// <returns>
		/// array indicating the available resolutions on this <c>channel</c> for this <c>range</c>
		/// </returns>
		public double[] GetADResolutions( int channel, double range )
		{
			double[] res = new double[16];

			for( int i = 0; i < 16; i++ )
				res[i] = range / (double)( 1 << ( i + 1 ) );

			return res;
		}


		/// <summary>
		/// Determines whether this A/D supports doing multiple voltage convesions at the same time
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if the device can do multi-channel voltage reads
		/// </returns>
		public bool CanADMultiChannelRead()
		{
			return true;
		}

		//--------
		//-------- A/D IO Methods
		//--------

		/// <summary>
		/// Retrieves the 1-Wire device sensor state.  This state is
		/// returned as a byte array.  Pass this byte array to the 'Get'
		/// and 'Set' methods.  If the device state needs to be changed then call
		/// the 'EriteDevice' to finalize the changes.
		/// </summary>
		/// <returns>1-Wire device sensor state</returns>
		public byte[] ReadDevice()
		{
			byte[] read_buf = new byte[27];
			MemoryBankAD mb;

			// read the banks, control/alarm/calibration
			for( int i = 0; i < 3; i++ ) {
				mb = (MemoryBankAD)regs[i];

				mb.ReadPageCRC( 0, ( i != 0 ), read_buf, i * 8 );
			}

			// zero out the bitmap
			read_buf[24] = 0;
			read_buf[25] = 0;
			read_buf[26] = 0;

			return read_buf;
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
			int start_offset, len, i, bank, index;
			bool got_block;
			MemoryBankAD mb;

			// Force a clear of the alarm flags
			for( i = 0; i < 4; i++ ) {

				// check if POR or alarm high/low flag present
				index = i * 2 + 1;

				if( ( state[index] & (byte)0xB0 ) != 0 ) {

					// clear the bits
					state[index] &= (byte)0x0F;

					// set to write in bitmap
					Bit.ArrayWriteBit( 1, index, BITMAP_OFFSET, state );
				}
			}

			// only allow physical address 0x1C to be written in calibration bank
			state[BITMAP_OFFSET + 2] = (byte)( state[BITMAP_OFFSET + 2] & 0x10 );

			// loop through the three memory banks collecting changes
			for( bank = 0; bank < 3; bank++ ) {
				start_offset = 0;
				len = 0;
				got_block = false;
				mb = (MemoryBankAD)regs[bank];

				// loop through each byte in the memory bank
				for( i = 0; i < 8; i++ ) {

					// check to see if this byte needs writing (skip control register for now)
					if( Bit.ArrayReadBit( bank * 8 + i, BITMAP_OFFSET, state ) == 1 ) {

						// check if already in a block
						if( got_block )
							len++;

							// new block
						else {
							got_block = true;
							start_offset = i;
							len = 1;
						}

						// check for last byte exception, write current block
						if( i == 7 )
							mb.Write( start_offset, state, bank * 8 + start_offset, len );
					}
					else if( got_block ) {

						// done with this block so write it
						mb.Write( start_offset, state, bank * 8 + start_offset, len );

						got_block = false;
					}
				}
			}

			// clear out the bitmap
			state[24] = 0;
			state[25] = 0;
			state[26] = 0;
		}


		/// <summary>
		/// Reads the value of the voltages after a <c>DoADConvert(boolean[],byte[])</c>
		/// method call.  This A/D device must support multi-channel reading, reported by
		/// <c>CanADMultiChannelRead()</c> if more than 1 channel conversion was attempted
		/// by <c>DoADConvert()</c>
		/// </summary>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// array with the voltage values for all channels
		/// </returns>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error such as no 1-Wire device present.
		/// This could be caused by a physical interruption in the 1-Wire Network due to shorts or a newly
		/// arriving 1-Wire device issuing a 'presence pulse'. This is usually a recoverable error.</exception>
		/// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter.
		/// This is usually a non-recoverable error.</exception>
		public double[] GetADVoltage( byte[] state )
		{
			byte[] read_buf = new byte[8];
			double[] ret_dbl = new double[4];

			// get readout page
			readout.ReadPageCRC( 0, false, read_buf, 0 );

			// convert to array of doubles
			for( int ch = 0; ch < 4; ch++ ) {
				ret_dbl[ch] = InterpretVoltage( DalSemi.OneWire.Utils.Convert.ToLong( read_buf, ch * 2, 2 ),
											   GetADRange( ch, state ) );
			}

			return ret_dbl;
		}


		/// <summary>
		/// Reads the value of the voltages after a <c>DoADConvert(int,byte[])</c>
		/// method call.  If more than one channel has been read it is more efficient to use the
		/// <c>GetADVoltage(byte[])</c> method that returns all channel voltage values.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// the voltage value for the specified channel
		/// </returns>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error such as no 1-Wire device present.
		/// This could be caused by a physical interruption in the 1-Wire Network due to shorts or a newly
		/// arriving 1-Wire device issuing a 'presence pulse'. This is usually a recoverable error.</exception>
		/// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter.
		/// This is usually a non-recoverable error.</exception>
		public double GetADVoltage( int channel, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			// get readout page
			byte[] read_buf = new byte[8];

			readout.ReadPageCRC( 0, false, read_buf, 0 );

			return InterpretVoltage( DalSemi.OneWire.Utils.Convert.ToLong( read_buf, channel * 2, 2 ),
									GetADRange( channel, state ) );
		}


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
		public void DoADConvert( int channel, byte[] state )
		{
			// call with set presets to 0
			DoADConvert( channel, PRESET_TO_ZEROS, state );
		}

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
		public void DoADConvert( bool[] doConvert, byte[] state )
		{

			// call with set presets to 0
			int[] presets = new int[4];

			for( int i = 0; i < 4; i++ )
				presets[i] = PRESET_TO_ZEROS;

			DoADConvert( doConvert, presets, state );
		}


		/// <summary>
		/// Does the AD convert.
		/// </summary>
		/// <param name="channel">The channel.</param>
		/// <param name="preset">The preset.</param>
		/// <param name="state">The state.</param>
		public void DoADConvert( int channel, int preset, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			// perform the conversion (do fixed max conversion time)
			DoADConvert( (byte)( 0x01 << channel ), (byte)( preset << channel ),
						1440, state );
		}


		/// <summary>
		/// Does the AD convert.
		/// </summary>
		/// <param name="doConvert">The do convert.</param>
		/// <param name="preset">The preset.</param>
		/// <param name="state">The state.</param>
		public void DoADConvert( bool[] doConvert, int[] preset, byte[] state )
		{
			byte input_select_mask = 0;
			byte read_out_control = 0;
			int time = 160;   // Time required in micro Seconds to covert.

			// calculate the input mask, readout control, and conversion time
			for( int ch = 3; ch >= 0; ch-- ) {

				// input select
				input_select_mask <<= 1;

				if( doConvert[ch] )
					input_select_mask |= 0x01;

				// readout control
				read_out_control <<= 2;

				if( preset[ch] == PRESET_TO_ZEROS )
					read_out_control |= 0x01;
				else if( preset[ch] == PRESET_TO_ONES )
					read_out_control |= 0x02;

				// conversion time
				time += ( 80 * (int)GetADResolution( ch, state ) ); // int ?
			}

			// do the conversion
			DoADConvert( input_select_mask, read_out_control, time, state );
		}

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
		public double GetADAlarm( int channel, int alarmType, byte[] state )
		{
			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			// extract alarm value and convert to voltage
			long temp_long =
			   (long)( state[ALARM_OFFSET + channel * 2 + alarmType] & 0xFF )
			   << 8;

			return InterpretVoltage( temp_long, GetADRange( channel, state ) );
		}

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
		public bool GetADAlarmEnable( int channel, int alarmType, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			return ( Bit.ArrayReadBit( 2 + alarmType, channel * 2 + 1, state ) == 1 );
		}

		/// <summary>
		/// Checks the state of the specified alarm on the specified channel.
		/// Not all A/D devices have alarms.  Check to see if this device has
		/// alarms first by calling the <c>HasADAlarms()</c> method.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="alarmType">Type of the desired alarm, ADContainerConstants.ALARM_HIGH ir ADContainerConstants.ALARM_LOW</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns><c>true</c> if specified alarm occurred</returns>
		/// <exception cref="OneWireException">if this device does not have A/D alarms</exception>
		public bool HasADAlarmed( int channel, int alarmType, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			return ( Bit.ArrayReadBit( 4 + alarmType, channel * 2 + 1, state ) == 1 );
		}

		/// <summary>
		/// Returns the currently selected resolution for the specified
		/// channel.  This device may not have selectable resolutions,
		/// though this method will return a valid value.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// The current resolution of <c>channel</c> in volts
		/// </returns>
		public double GetADResolution( int channel, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			int res = state[channel * 2] & 0x0F;

			// return resolution, if 0 then 16 bits
			if( res == 0 )
				res = 16;

			return GetADRange( channel, state ) / (double)( 1 << res );
		}

		/// <summary>
		/// Returns the currently selected range for the specified
		/// channel.  This device may not have selectable ranges,
		/// though this method will return a valid value.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>the input voltage range</returns>
		public double GetADRange( int channel, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			return ( Bit.ArrayReadBit( 0, channel * 2 + 1, state ) == 1 ) ? 5.12 : 2.56;
		}

		/// <summary>
		/// Determines whether the output is enabled for the specified channel.
		/// </summary>
		/// <param name="channel">The channel.</param>
		/// <param name="state">The state.</param>
		public bool IsOutputEnabled( int channel, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			return ( Bit.ArrayReadBit( 7, channel * 2, state ) == 1 );
		}


		/// <summary>
		/// Gets the state of the output.
		/// </summary>
		/// <param name="channel">The channel.</param>
		/// <param name="state">The state.</param>
		/// <returns></returns>
		public bool GetOutputState( int channel, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			return ( Bit.ArrayReadBit( 6, channel * 2, state ) == 1 );
		}

		/// <summary>
		/// Detects if this device has seen a Power-On-Reset (POR).  If this has
		/// occured it may be necessary to set the state of the device to the
		/// desired values. The register buffer is retrieved from the ReadDevice() method.
		/// </summary>
		/// <param name="state">The state.</param>
		/// <returns>
		/// true if the device has performed a power on reset cycle
		/// </returns>
		public bool GetDevicePOR( byte[] state )
		{
			return ( Bit.ArrayReadBit( 7, 1, state ) == 1 );
		}


		/// <summary>
		/// Clears or sets the device Power On Reset indicator.
		/// </summary>
		/// <param name="state">The state, retrieved from the ReadDevice() method</param>
		/// <param name="reset">if set to <c>true</c> the power on reset signal is cleared, otherwise it is set</param>
		public void SetDevicePOR( byte[] state, bool clear )
		{
			Bit.ArrayWriteBit( clear ? 0 : 1, 7, 1, state );
		}

		/// <summary>
		/// Extracts the state of the external power indicator from the provided
		/// register buffer.  Use 'SetPower' to set or clear the external power
		/// indicator flag.
		/// </summary>
		/// <param name="state">The state.</param>
		/// <returns>
		/// 	true if set to external power operation
		/// </returns>
		public bool IsPowerExternal( byte[] state )
		{
			return ( state[EXPOWER_OFFSET] != 0 );
		}

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
		public void SetADAlarm( int channel, int alarmType, double alarm,
								byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			int offset = ALARM_OFFSET + channel * 2 + alarmType;

			state[offset] =
			   (byte)( ( VoltageToInt( alarm, GetADRange( channel, state ) ) >> 8 )
						 & 0x00FF );

			// set bitmap field to indicate this register has changed
			Bit.ArrayWriteBit( 1, offset, BITMAP_OFFSET, state );
		}


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
		public void SetADAlarmEnable( int channel, int alarmType,
									  bool alarmEnable, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			// change alarm enable
			Bit.ArrayWriteBit( ( ( alarmEnable ) ? 1
											 : 0 ), 2 + alarmType, channel * 2 + 1,
												   state );

			// set bitmap field to indicate this register has changed
			Bit.ArrayWriteBit( 1, channel * 2 + 1, BITMAP_OFFSET, state );
		}

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
		public void SetADResolution( int channel, double resolution, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			// convert voltage resolution into bit resolution
			int div = (int)( GetADRange( channel, state ) / resolution );
			int res_bits = 0;

			do {
				div >>= 1;

				res_bits++;
			}
			while( div != 0 );

			res_bits -= 1;

			if( res_bits == 16 )
				res_bits = 0;

			// check for valid bit resolution
			if( ( res_bits < 0 ) || ( res_bits > 15 ) )
				throw new ArgumentOutOfRangeException( "Invalid resolution" );

			// clear out the resolution
			state[channel * 2] &= (byte)0xF0;

			// set the resolution
			state[channel * 2] |= (byte)( ( res_bits == 16 ) ? 0
															  : res_bits );

			// set bitmap field to indicate this register has changed
			Bit.ArrayWriteBit( 1, channel * 2, BITMAP_OFFSET, state );
		}

		/// <summary>
		/// Sets the input range for the specified channel.
		/// Note that multiple 'Set' methods can be called before one call to <c>WriteDevice()</c>.
		/// Also note that not all A/D devices have alarms. Check to see if this device has
		/// alarms first by calling the <c>HasADAlarms()</c> method.
		/// </summary>
		/// <param name="channel">Channel number in the range [0 to (<c>GetNumberADChannels()</c> - 1)]</param>
		/// <param name="range">One of the ranges returned by <c>GetADRanges(int)</c></param>
		/// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
		public void SetADRange( int channel, double range, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			// convert range into bit value
			int range_bit;

			if( ( range > 5.00 ) & ( range < 5.30 ) )
				range_bit = 1;
			else if( ( range > 2.40 ) & ( range < 2.70 ) )
				range_bit = 0;
			else
				throw new ArgumentOutOfRangeException( "Invalid range" );

			// change range bit
			Bit.ArrayWriteBit( range_bit, 0, channel * 2 + 1, state );

			// set bitmap field to indicate this register has changed
			Bit.ArrayWriteBit( 1, channel * 2 + 1, BITMAP_OFFSET, state );
		}

		/// <summary>
		/// Sets the output.
		/// </summary>
		/// <param name="channel">[0 to (GetNumberChannels() - 1)]</param>
		/// <param name="outputEnable">true if output is enabled</param>
		/// <param name="outputState">false if output is conducting to ground and true if not conducting.
		/// This parameter is not used if OutputEnable is false</param>
		/// <param name="state">The state.</param>
		public void SetOutput( int channel, bool outputEnable,
							   bool outputState, byte[] state )
		{

			// check for valid channel value
			if( ( channel < 0 ) || ( channel > 3 ) )
				throw new ArgumentOutOfRangeException( "Invalid channel number" );

			// output enable bit
			Bit.ArrayWriteBit( ( ( outputEnable ) ? 1
											  : 0 ), 7, channel * 2, state );

			// optionally set state
			if( outputEnable )
				Bit.ArrayWriteBit( ( ( outputState ) ? 1
												 : 0 ), 6, channel * 2, state );

			// set bitmap field to indicate this register has changed
			Bit.ArrayWriteBit( 1, channel * 2, BITMAP_OFFSET, state );
		}


		/// <summary>
		/// Sets or clears the external power flag in the provided register buffer.
		/// The register buffer is retrieved from the <CODE>readDevice()</CODE> method.
		/// The method WriteDevice() must be called to finalize these
		/// changes to the device.  Note that multiple 'Set' methods can
		/// be called before one call to <CODE>writeDevice()
		/// </summary>
		/// <param name="external">Set to true if external power is used</param>
		/// <param name="state">The state.</param>
		public void SetPower( bool external, byte[] state )
		{

			// sed the flag
			state[EXPOWER_OFFSET] = (byte)( external ? 0x40
														: 0 );

			// set bitmap field to indicate this register has changed
			Bit.ArrayWriteBit( 1, EXPOWER_OFFSET, BITMAP_OFFSET, state );
		}

		//--------
		//-------- Utility methods
		//--------

		/// <summary>
		/// Converts a raw voltage long value for the DS2450 into a valid voltage.
		/// Requires the max voltage value.
		/// </summary>
		/// <param name="rawVoltage">The raw voltage.</param>
		/// <param name="range">The max voltage</param>
		/// <returns>calculated voltage based on the range</returns>
		public static double InterpretVoltage( long rawVoltage, double range )
		{
			return ( ( (double)rawVoltage / 65535.0 ) * range );
		}

		/// <summary>
		/// Converts a voltage double value to the DS2450 specific int value.
		/// Requires the max voltage value.
		/// </summary>
		/// <param name="voltage">The voltage.</param>
		/// <param name="range">Max voltage</param>
		/// <returns>DS2450 voltage</returns>
		public static int VoltageToInt( double voltage, double range )
		{
			return (int)( ( voltage * 65535.0 ) / range );
		}

		//--------
		//-------- Private methods
		//--------


		/// <summary>
		/// Create the memory bank interface to read/write
		/// </summary>
		private void InitMem()
		{

			// readout
			readout = new MemoryBankAD( this );

			// control
			regs = new MemoryBankList();

			MemoryBankAD temp_mb = new MemoryBankAD( this );

			temp_mb.bankDescription = "A/D Control and Status";
			temp_mb.generalPurposeMemory = false;
			temp_mb.startPhysicalAddress = 8;
			temp_mb.readWrite = true;
			temp_mb.readOnly = false;

			regs.Add( temp_mb );

			// Alarms
			temp_mb = new MemoryBankAD( this );
			temp_mb.bankDescription = "A/D Alarm Settings";
			temp_mb.generalPurposeMemory = false;
			temp_mb.startPhysicalAddress = 16;
			temp_mb.readWrite = true;
			temp_mb.readOnly = false;

			regs.Add( temp_mb );

			// calibration
			temp_mb = new MemoryBankAD( this );
			temp_mb.bankDescription = "A/D Calibration";
			temp_mb.generalPurposeMemory = false;
			temp_mb.startPhysicalAddress = 24;
			temp_mb.readWrite = true;
			temp_mb.readOnly = false;

			regs.Add( temp_mb );
		}

		/// <summary>
		/// Performs voltage conversion on all specified channels.  The method
		/// GetADVoltage() can be used to read the result of the conversion.
		/// </summary>
		/// <param name="inputSelectMask">The input select mask.</param>
		/// <param name="readOutControl">The read out control.</param>
		/// <param name="timeUs">Time in microseconds for conversion.</param>
		/// <param name="state">The state.</param>
		private void DoADConvert( byte inputSelectMask, byte readOutControl,
								  int timeUs, byte[] state )
		{

			// check if no conversions
			if( inputSelectMask == 0 ) {
				throw new ArgumentOutOfRangeException(
				   "No conversion will take place.  No channel selected." );
			}

			// Create the command block to be sent.
			byte[] raw_buf = new byte[5];

			raw_buf[0] = CONVERT_COMMAND;
			raw_buf[1] = inputSelectMask;
			raw_buf[2] = (byte)readOutControl;
			raw_buf[3] = (byte)0xFF;
			raw_buf[4] = (byte)0xFF;

			// calculate the CRC16 up to and including readOutControl
			uint crc16 = CRC16.Compute( raw_buf, 0, 3, 0 );

			// Send command block.
			if( adapter.SelectDevice( address ) ) {
				if( IsPowerExternal( state ) ) {

					// good power so send the entire block (with both CRC)
					adapter.DataBlock( raw_buf, 0, 5 );

					// Wait for complete of conversion
					//try
					{
						System.Threading.Thread.Sleep( ( timeUs / 1000 ) + 10 );
					}
					//catch (InterruptedException e){}
					//;

					// calculate the rest of the CRC16
					crc16 = CRC16.Compute( raw_buf, 3, 2, crc16 );
				}
				else {

					// parasite power so send the all but last byte
					adapter.DataBlock( raw_buf, 0, 4 );

					// setup power delivery
					adapter.SetPowerDuration( OWPowerTime.DELIVERY_INFINITE );
					adapter.StartPowerDelivery( OWPowerStart.CONDITION_AFTER_BYTE );

					// get the final CRC byte and start strong power delivery
					raw_buf[4] = (byte)adapter.GetByte();
					crc16 = CRC16.Compute( raw_buf, 3, 2, crc16 );

					// Wait for power delivery to complete the conversion
					//try
					{
						System.Threading.Thread.Sleep( ( timeUs / 1000 ) + 1 );
					}
					//catch (InterruptedException e){}
					//;

					// Turn power off.
					adapter.SetPowerNormal();
				}
			}
			else
				throw new OneWireException( "OneWireContainer20 - Device not found." );

			// check the CRC result
			if( crc16 != 0x0000B001 )
				throw new OneWireIOException(
				   "OneWireContainer20 - Failure during conversion - Bad CRC" );

			// check if still busy
			if( adapter.GetByte() == 0x00 )
				throw new OneWireIOException( "Conversion failed to complete." );
		}


	}
}
