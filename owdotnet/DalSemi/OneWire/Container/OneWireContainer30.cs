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
	/// 1-Wire container that encapsulates the functionality of the 1-Wire
	/// family type 0x30, Dallas Semiconductor part number: DS2760 (DS2761/DS2762), High Precision Li-ion Battery Monitor.
	/// 
	/// Features
	///		Li-ion safety circuit
	///			Overvoltage protection
	///			Overcurrent/short circuit protection
	///			Undervoltage protection
	///		Two sense resistor configurations
	///			Internal 25 mOhm sense resistor
	///			External user-selectable sense resistor
	///			12-bit bi-directional current measurement
	///			Current accumulation
	///			Voltage measurement
	///			Direct-to-digital temperature measurement
	///			32 bytes of lockable EEPROM
	///			16 bytes of general purpose SRAM
	///			Low power consumption
	///				Active current: 80 &#181A max
	///				Sleep current: 2 &#181A max
	/// </summary>
	public class OneWireContainer30 : OneWireContainer, IADContainer, TemperatureContainer
	{
		public static byte GetFamilyCode()
		{
			return 0x30;
		}

		/// <summary>
		/// Memory functions.
		/// </summary>
		private const byte WRITE_DATA_COMMAND = (byte)0x6C;
		private const byte READ_DATA_COMMAND = (byte)0x69;
		private const byte COPY_DATA_COMMAND = (byte)0x48;
		private const byte RECALL_DATA_COMMAND = (byte)0xB8;
		private const byte LOCK_COMMAND = (byte)0x6A;

		/// <summary>
		/// Address of the Protection Register. Used to set/check flags withSetFlag()/GetFlag().
		/// </summary>
		public const byte PROTECTION_REGISTER = 0;


		/// <summary>
		/// Address of the Status Register. Used to set/check flags with SetFlag()/GetFlag()
		/// </summary>
		public const byte STATUS_REGISTER = 1;


		/// <summary>
		/// Address of the EEPROM Register. Used to set/check flags with SetFlag()/GetFlag().
		/// </summary>
		public const byte EEPROM_REGISTER = 7;

		/// <summary>
		/// Address of the Special Feature Register (SFR). Used to check flags with GetFlag()
		/// </summary>
		public const byte SPECIAL_FEATURE_REGISTER = 8;


		/// <summary>
		/// * PROTECTION REGISTER FLAG: When this flag is true, it
		/// indicates that the battery pack has experienced an overvoltage
		/// condition.
		/// This flag must be reset!
		/// Accessed with GetFlag()
		/// </summary>
		public const byte OVERVOLTAGE_FLAG = 128;


		/// <summary>
		/// PROTECTION REGISTER FLAG: When this flag is true, the
		/// battery pack has experienced an undervoltage.
		/// This flag must be reset!
		/// Accessed with GetFlag()
		/// </summary>
		public const byte UNDERVOLTAGE_FLAG = 64;

		/// <summary>
		/// PROTECTION REGISTER FLAG: When this flag is true the
		/// battery has experienced a charge-direction overcurrent condition.
		/// This flag must be reset!
		/// * Accessed with GetFlag()
		/// </summary>
		public const byte CHARGE_OVERCURRENT_FLAG = 32;

		/// <summary>
		/// * PROTECTION REGISTER FLAG: When this flag is true the
		/// battery has experienced a discharge-direction overcurrent condition.
		/// This flag must be reset!
		/// Accessed with GetFlag()
		/// </summary>
		public const byte DISCHARGE_OVERCURRENT_FLAG = 16;


		/// <summary>
		/// PROTECTION REGISTER FLAG: Mirrors the !CC output pin. Accessed with GetFlag()
		/// </summary>
		public const byte CC_PIN_STATE_FLAG = 8;

		/// <summary>
		/// PROTECTION REGISTER FLAG: Mirrors the !DC output pin. Accessed with GetFlag()
		/// </summary>
		public const byte DC_PIN_STATE_FLAG = 4;

		/// <summary>
		/// PROTECTION REGISTER FLAG: Reseting this flag will disable charging regardless of cell or pack conditions.
		/// Accessed with GetFlag()/SetFlag()
		/// </summary>
		public const byte CHARGE_ENABLE_FLAG = 2;

		/// <summary>
		/// PROTECTION REGISTER FLAG: Reseting this flag will disable discharging.
		/// Accessed with GetFlag()/SetFlag()
		/// </summary>
		public const byte DISCHARGE_ENABLE_FLAG = 1;

		/// <summary>
		/// STATUS REGISTER FLAG: Enables/disables the DS2760 to enter sleep mode
		/// when the DQ line goes low for greater than 2 seconds.
		/// Accessed with GetFlag()/SetFlag()
		/// </summary>
		public const byte SLEEP_MODE_ENABLE_FLAG = 32;


		/// <summary>
		/// STATUS REGISTER FLAG: If set, the opcode for the Read Net Address command
		/// will be set to 33h. If it is not set the opcode is set to 39h.
		/// Accessed with GetFlag()/SetFlag().
		/// </summary>
		public const byte READ_NET_ADDRESS_OPCODE_FLAG = 16;

		/// <summary>
		/// EEPROM REGISTER FLAG: This flag will be true if the Copy
		/// Data Command is in progress. Data may be written to EEPROM when this
		/// reads false.
		/// Accessed with GetFlag()/SetFlag().
		/// </summary>
		public const byte EEPROM_COPY_FLAG = (byte)128;

		/// <summary>
		/// EEPROM REGISTER FLAG: When this flag is true, the Lock
		/// Command is enabled. The lock command is used to make memory permanently
		/// read only.
		/// Accessed with GetFlag()/SetFlag().
		/// </summary>
		public const byte EEPROM_LOCK_ENABLE_FLAG = 64;

		/// <summary>
		/// EEPROM REGISTER FLAG: When this flag is true, Block 1
		/// of the EEPROM (addresses 48-63) is read-only.
		/// Accessed with GetFlag().
		/// </summary>
		public const byte EEPROM_BLOCK_1_LOCK_FLAG = 2;

		/// <summary>
		/// EEPROM REGISTER FLAG: When this flag is true, Block 0
		/// of the EEPROM (addresses 32-47) is read-only.
		/// Accessed with GetFlag().
		/// </summary>
		public const byte EEPROM_BLOCK_0_LOCK_FLAG = 1;

		/// <summary>
		/// SPECIAL FEATURE REGISTER FLAG: Mirrors the state of the !PS pin.
		/// Accessed with GetFlag().
		/// </summary>
		public const byte PS_PIN_STATE_FLAG = (byte)128;

		/// <summary>
		/// SPECIAL FEATURE REGISTER FLAG: Mirrors/sets the state of the PIO pin. The
		/// PIO pin can be used as an output; resetting this flag disables the PIO
		/// output driver.
		/// Accessed with GetFlag()/SetFlag().
		/// </summary>
		public const byte PIO_PIN_SENSE_AND_CONTROL_FLAG = 64;

		/// <summary>
		/// Holds the value of the sensor external resistance.
		/// </summary>
		private double Rsens = 0.05;

		/// <summary>
		/// The value of the internal resistor, in ohms
		/// </summary>
		private double internalResistorValue = 0.025d;

		/// <summary>
		/// When this is true, all calculations are assumed to be done in the part
		/// </summary>
		private bool internalResistor;

		/// <summary>
		/// Initializes a new instance of the <see cref="OneWireContainer30"/> class.
		/// </summary>
		/// <param name="sourceAdapter">adapter object required to communicate with this device.</param>
		/// <param name="newAddress">address of this 1-Wire device</param>
		public OneWireContainer30( PortAdapter sourceAdapter, byte[] newAddress )
			: base( sourceAdapter, newAddress )
		{
			internalResistor = true;
		}

		/// <summary>
		/// Retrieves the Dallas Semiconductor part number of the 1-Wire device as a string.  For example 'Crypto iButton' or 'DS1992'.
		/// </summary>
		/// <returns>1-Wire device name</returns>
		public override string GetName()
		{
			return "DS2760";
		}

		/// <summary>
		/// Retrieves the alternate Dallas Semiconductor part numbers or names.
		/// A 'family' of 1-Wire Network devices may have more than one part number depending on packaging.
		/// There can also be nicknames such as 'Crypto iButton'.
		/// </summary>
		/// <returns>1-Wire device alternate names</returns>
		public override string GetAlternateNames()
		{
			return "DS2761, DS2762. 1-Cell Li-Ion Battery Monitor";
		}

		/// <summary>
		/// Retrieves a short description of the function of the 1-Wire device type.
		/// </summary>
		/// <returns>Device functional description</returns>
		public override string GetDescription()
		{
			return "The DS2760, DS2761, DS2762 is a data acquisition, information storage, and safety"
				   + " protection device tailored for cost-sensitive battery pack applications."
				   + " This low-power device integrates precise temperature, voltage, and"
				   + " current measurement , nonvolatile data storage, and Li-Ion protection"
				   + " into the small footprint of either a TSSOP packet or flip-chip.";
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

			// EEPROM main bank
			MemoryBankEEPROMblock mn = new MemoryBankEEPROMblock( this );

			bank_vector.Add( mn );

			return bank_vector;
		}


		/// <summary>
		/// Sets the DS2760 to use its internal .025 ohm resistor for measurements.
		/// This should only be enabled if there is NO external resistor physically
		/// attached to the device.
		/// </summary>
		public void SetResistorInternal()
		{
			lock( this ) {
				internalResistor = true;
			}
		}


		/// <summary>
		/// Sets the DS2760 to use an external, user-selectable resistance. This
		/// resistance should be wired directly to the VSS (negative terminal of the cell).
		/// </summary>
		/// <param name="Rsens">The resistance of the external resistor</param>
		public void SetResistorExternal( double Rsens )
		{
			lock( this ) {
				internalResistor = false;
				this.Rsens = Rsens;
			}
		}


		/// <summary>
		/// Reads a register byte from the memory of the DS2760.  Note that there
		/// is no error checking as the DS2760 performs no CRC on the data.
		/// 
		/// Note: This function should only be used when reading the register
		/// memory of the DS2760. The EEPROM blocks (addresses 32-64) should be
		/// accessed with WriteBlock/ReadBlock.
		/// </summary>
		/// <param name="memAddr">the address to read (0-255)</param>
		/// <returns>data read from memory</returns>
		public byte ReadByte( int memAddr )
		{
			byte[] buffer = new byte[3];

			DoSpeed();
			adapter.Reset();

			if( adapter.SelectDevice( address ) ) {

				// setup the read
				buffer[0] = READ_DATA_COMMAND;
				buffer[1] = (byte)memAddr;
				buffer[2] = (byte)0xFF;

				adapter.DataBlock( buffer, 0, 3 );

				return buffer[2];
			}
			else
				throw new OneWireException( "OneWireContainer30-Device not found." );
		}

		/// <summary>
		/// Reads bytes from the DS2760.  Note that there is no error-checking as
		/// the DS2760 does not perform a CRC on the data.
		/// 
		/// Note: This function should only be used when reading the register
		/// emory of the DS2760. The EEPROM blocks (addresses 32-64) should be
		/// accessed with writeBlock/readBlock.
		/// </summary>
		/// <param name="memAddr"> the address to read (0-255)</param>
		/// <param name="buffer">buffer to receive data</param>
		/// <param name="start">start position within buffer to place data</param>
		/// <param name="len">length of buffer</param>
		public void ReadBytes( int memAddr, byte[] buffer, int start, int len )
		{
			DoSpeed();
			adapter.Reset();

			if( adapter.SelectDevice( address ) ) {
				for( int i = start; i < start + len; i++ )
					buffer[i] = (byte)0x0ff;

				adapter.PutByte( READ_DATA_COMMAND );
				adapter.PutByte( memAddr & 0x0ff );
				adapter.DataBlock( buffer, start, len );
			}
			else
				throw new OneWireException( "OneWireContainer30-Device not found." );
		}

		/// <summary>
		/// Writes a register byte to the memory of the DS2760.  Note that the
		/// DS2760 does not make any use of cyclic redundancy checks (error-checking).
		/// To ensure error free transmission, double check write operation.
		/// 
		/// Note: This method should only be used when writing to the register memory
		/// of the DS2760. The EEPROM blocks (addresses 32-64) require special treatment
		/// and thus the writeBlock/readBlock functions should be used for those.
		/// </summary>
		/// <param name="memAddr">Address to write (0-255)</param>
		/// <param name="data">Byte to write to memory</param>
		public void WriteByte( int memAddr, byte data )
		{
			byte[] buffer = new byte[3];

			DoSpeed();
			adapter.Reset();

			if( adapter.SelectDevice( address ) ) {

				/* first perform the write */
				buffer[0] = WRITE_DATA_COMMAND;
				buffer[1] = (byte)memAddr;
				buffer[2] = data;

				adapter.DataBlock( buffer, 0, 3 );

				// Don't read it back for verification - some addresses
				// have some read-only bits and some R/W bits. Let the user
				// verify if they want to
			}
			else
				throw new OneWireException( "OneWireContainer30-Device not found." );
		}

		/// <summary>
		/// Reads a 16 byte data block from one of the user EEPROM blocks.
		/// Note that there is no error-checking as the DS2760 performs
		/// no CRCs.
		/// </summary>
		/// <param name="blockNumber">The block number. Acceptable parameters are 0 and 1</param>
		/// <returns>16 data bytes</returns>
		public byte[] ReadEEPROMBlock( int blockNumber )
		{
			byte[] buffer = new byte[18];
			byte[] result = new byte[16];

			// calculate the address (32 and 48 are valid addresses)
			byte memAddr = (byte)( 32 + ( blockNumber * 16 ) );

			// check for valid parameters
			if( ( blockNumber != 0 ) & ( blockNumber != 1 ) )
				throw new ArgumentOutOfRangeException(
				   "OneWireContainer30-Block number " + blockNumber
				   + " is not a valid EEPROM block." );

			// perform the recall/read and verification
			DoSpeed();
			adapter.Reset();

			if( adapter.SelectDevice( address ) ) {

				// first recall the memory to shadow ram
				buffer[0] = RECALL_DATA_COMMAND;
				buffer[1] = memAddr;

				adapter.DataBlock( buffer, 0, 2 );

				// now read the shadow ram
				adapter.Reset();
				adapter.SelectDevice( address );

				buffer[0] = READ_DATA_COMMAND;

				// buffer[1] should still hold memAddr
				for( int i = 0; i < 16; i++ )
					buffer[i + 2] = (byte)0xff;

				adapter.DataBlock( buffer, 0, 18 );

				// keep this result
				Array.Copy( buffer, 2, result, 0, 16 );

				//user can re-read for verification
				return result;
			}
			else
				throw new OneWireException( "OneWireContainer30-Device not found." );
		}

		/// <summary>
		/// Writes a 16 byte data block to one of the user blocks. The block may be
		/// rewritten at any time, except after it is locked with lockBlock().
		/// This method performs error checking by verifying data written.
		/// </summary>
		/// <param name="blockNumber">The block number. Acceptable parameters are 0 and 1</param>
		/// <param name="data">16 bytes of data to write</param>
		public void WriteEEPROMBlock( int blockNumber, byte[] data )
		{
			byte[] buffer = new byte[18];

			// the first block is at address 32 and the second is at address 48
			byte memAddr = (byte)( 32 + ( blockNumber * 16 ) );

			// check for valid parameters
			if( data.Length < 16 )
				throw new ArgumentOutOfRangeException(
				   "OneWireContainer30-Data block must consist of 16 bytes." );

			if( ( blockNumber != 0 ) && ( blockNumber != 1 ) )
				throw new ArgumentOutOfRangeException(
				   "OneWireContainer30-Block number " + blockNumber
				   + " is not a valid EEPROM block." );

			// if the EEPROM block is locked throw a OneWireIOException
			if( ( ( blockNumber == 0 ) && ( GetFlag( EEPROM_REGISTER, EEPROM_BLOCK_0_LOCK_FLAG ) ) )
					|| ( ( blockNumber == 1 )
						&& ( GetFlag( EEPROM_REGISTER, EEPROM_BLOCK_1_LOCK_FLAG ) ) ) )
				throw new OneWireIOException(
				   "OneWireContainer30-Cant write data to locked EEPROM block." );

			// perform the write/verification and copy
			DoSpeed();
			adapter.Reset();

			if( adapter.SelectDevice( address ) ) {

				// first write to shadow rom
				buffer[0] = WRITE_DATA_COMMAND;
				buffer[1] = memAddr;

				for( int i = 0; i < 16; i++ )
					buffer[i + 2] = data[i];

				adapter.DataBlock( buffer, 0, 18 );

				// read the shadow ram back for verification
				adapter.Reset();
				adapter.SelectDevice( address );

				buffer[0] = READ_DATA_COMMAND;

				// buffer[1] should still hold memAddr
				for( int i = 0; i < 16; i++ )
					buffer[i + 2] = (byte)0xff;

				adapter.DataBlock( buffer, 0, 18 );

				// verify data
				for( int i = 0; i < 16; i++ )
					if( buffer[i + 2] != data[i] )
						throw new OneWireIOException(
						   "OneWireContainer30-Error writing EEPROM block"
						   + blockNumber + "." );

				// now perform the copy to EEPROM
				adapter.Reset();
				adapter.SelectDevice( address );

				buffer[0] = COPY_DATA_COMMAND;

				// buffer[1] should still hold memAddr
				adapter.DataBlock( buffer, 0, 2 );
			}
			else
				throw new OneWireException( "OneWireContainer30-Device not found." );
		}

		/// <summary>
		/// Permanently write-protects one of the user blocks of EEPROM.
		/// </summary>
		/// <param name="blockNumber">block number to permanently write protect, acceptable parameters are 0 and 1.</param>
		public void LockBlock( int blockNumber )
		{

			// Compute the byte location
			byte memAddr = (byte)( 32 + ( blockNumber * 16 ) );

			// check if the block is valid
			if( blockNumber < 0 || blockNumber > 1 )
				throw new ArgumentOutOfRangeException(
				  "OneWireContainer30-Block " + blockNumber
				  + " is not a valid EEPROM block." );

			// perform the lock
			DoSpeed();
			adapter.Reset();

			if( adapter.SelectDevice( address ) ) {
				adapter.PutByte( LOCK_COMMAND );
				adapter.PutByte( memAddr );
			}
			else
				throw new OneWireException( "OneWireContainer30-Device not found." );
		}


		/// <summary>
		/// Checks the specified flag in the specified register.
		/// Valid registers are:
		/// PROTECTION_REGISTER
		/// STATUS_REGISTER
		/// EEPROM_REGISTER
		/// SPECIAL_FEATURE_REGISTER
		/// </summary>
		/// <param name="memAddr">Registers address. Pre-defined fields for each register are defined above.</param>
		/// <param name="flagToGet">Bitmask of desired flag, the acceptable parameters pertaining to each register are defined as constant fields above</param>
		/// <returns>value of the flag: true if flag is set (=1)</returns>
		public bool GetFlag( int memAddr, byte flagToGet )
		{

			// read the byte and perform a simple mask to determine if that byte is on
			byte data = ReadByte( memAddr );

			if( ( data & flagToGet ) != 0 )
				return true;

			return false;
		}

		/// <summary>
		/// Sets one of the flags in one of the registers.
		/// Valid registers are:
		/// PROTECTION_REGISTER
		/// STATUS_REGISTER
		/// EEPROM_REGISTER
		/// SPECIAL_FEATURE_REGISTER
		/// </summary>
		/// <param name="memAddr">Registers address. Pre-defined fields for each register are defined above.</param>
		/// <param name="flagToGet">Bitmask of the flag to set, the acceptable parameters pertaining to each register are defined as constant fields above</param>
		/// <param name="flagValue">value to set the flag to</param>
		public void SetFlag( int memAddr, byte flagToSet, bool flagValue )
		{

			// the desired default value for the status register flags has to be
			// set in a seperate register for some reason, so I treat it specially.
			if( memAddr == STATUS_REGISTER )
				memAddr = 49;

			byte data = ReadByte( memAddr );

			if( flagValue )
				data = (byte)( data | flagToSet );
			else
				data = (byte)( data & ~flagToSet );

			WriteByte( memAddr, data );
		}

		/// <summary>
		/// Gets the instantaneous current
		/// </summary>
		/// <param name="state">The device state.</param>
		/// <returns>Current in Amperes</returns>
		public double GetCurrent( byte[] state )
		{

			// grab the data
			int data = ( ( state[14] << 8 ) | ( state[15] & 0xff ) );

			data = data >> 3;

			double result;

			// when the internal resistor is used, the device calculates it for you
			// the resolution is .625 mA
			if( internalResistor )
				result = ( data * .625 ) / 1000;

				// otherwise convert to Amperes
			else
				result = data * .000015625 / Rsens;

			return result;
		}

		/// <summary>
		/// Allows user to set the remaining capacity. Good for accurate capacity
		/// measurements using temperature and battery life.
		/// By measuring the battery's current and voltage when it is fully
		/// charged and when it is empty, the voltage corresponding to an empty battery
		/// and the current corresponding to a full one can be derived. These
		/// values can be detected in user program and the remaining capacity can
		/// be set to the empty/full levels accordingly for nice accuracy.
		/// </summary>
		/// <param name="remainingCapacity">Capacity remaining capacity in mAH</param>
		public void SetRemainingCapacity( double remainingCapacity )
		{
			int data;

			// if the internal resistor is used, it can be stored as is (in mAH)
			if( internalResistor )
				data = (int)( remainingCapacity * 4 );
			else
				data = (int)( remainingCapacity * Rsens / .00626 );

			// break into bytes and store
			WriteByte( 16, (byte)( data >> 8 ) );
			WriteByte( 17, (byte)( data & 0xff ) );
		}

		/// <summary>
		/// Calculates the remaining capacity in mAHours from the current accumulator. Accurate to +/- .25 mAH.
		/// </summary>
		/// <param name="state">The device state.</param>
		/// <returns>mAHours of battery capacity remaining</returns>
		public double GetRemainingCapacity( byte[] state )
		{
			double result = 0;

			// grab the data
			int data = ( ( state[16] & 0xff ) << 8 ) | ( state[17] & 0xff );

			// if the internal resistor is being used the part calculates it for us
			if( internalResistor )
				result = data / 4.0;

				// this equation can be found on the data sheet
			else
				result = data * .00626 / Rsens;

			return result;
		}

		/// <summary>
		/// Sets the state for the Programmable Input/Output pin.  In order to
		/// operate as a switch, PIO must be tied to a pull-up resistor (4.7kOhm) to VDD.
		/// </summary>
		/// <param name="on">State of the PIO to set</param>
		public void SetLatchState( bool on )
		{
			//since bit 0 is read-only and bits 2-7 are don't cares,
			//we don't need to read location 8 first, we can just write
			WriteByte( 8, (byte)( on ? 0x40 : 0x00 ) );
		}

		/// <summary>
		/// Returns the latch state of the Programmable Input/Ouput pin on the DS2760.
		/// </summary>
		/// <returns>State of the Programmable Input/Ouput pin</returns>
		public bool GetLatchState()
		{
			return ( ( ReadByte( 8 ) & 0x40 ) == 0x40 );
		}

		/// <summary>
		/// Clears the overvoltage, undervoltage, charge overcurrent,
		/// and discharge overcurrent flags.  Each time a violation
		/// occurs, these flags stay set until reset.  This method
		/// resets all 4 flags.
		/// </summary>
		public void ClearConditions()
		{
			byte protect_reg = ReadByte( 0 );

			WriteByte( 0, (byte)( protect_reg & 0x0f ) );
		}

		/////////////////////////////////////////////////////////////////////
		//
		//  BEGIN CONTAINER INTERFACE METHODS
		//
		////////////////////////////////////////////////////////////////////
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
			return 2;
		}

		/// <summary>
		/// Determines whether this A/D measuring device has high/low alarms.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if this device has high/low trip alarms
		/// </returns>
		public bool HasADAlarms()
		{
			return false;
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
			double[] result = new double[1];

			result[0] = 5.0;

			return result;
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
			double[] result = new double[1];

			result[0] = GetADResolution( channel, null );

			return result;
		}


		/// <summary>
		/// Determines whether this A/D supports doing multiple voltage convesions at the same time
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if the device can do multi-channel voltage reads
		/// </returns>
		public bool CanADMultiChannelRead()
		{
			return false;
		}

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
		public void DoADConvert( int channel, byte[] state )
		{
			// As the voltage is constantly being read when the part is
			// in active mode, we can just read it out from the state.
			// The part only leaves active mode if the battery runs out
			// (i.e. voltage goes below threshold) in which case we should
			// return the lower bound anyway.
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
			throw new OneWireException(
			   "This device does not support multi-channel reading" );
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
			throw new OneWireException( "This device does not support multi-channel reading" );
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
			if( channel < 0 || channel > 1 )
				throw new OneWireException( "Invalid channel" );

			Int16 data;
			double result = 0.0;

			// Get the two bytes
			byte msb = state[12 + channel * 2];
			byte lsb = state[13 + channel * 2];

			// Merge the two bits
			data = (Int16)( msb << 8 );
			data |= (Int16)lsb;

			if( channel == 0 ) {
				// the voltage measurement channel
				// Once the two bytes are ORed, a right shift of 5 must occur
				// (signed shift)
				data >>= 5;
			}
			else {
				// the current sensing channel
				// Once the two bytes are ORed, a right shift of 3 must occur
				// (signed shift)
				data >>= 3;
			}

			// That raw measurement is in 'resolution' units -> convert to volts
			result = data * GetADResolution( channel, state );

			return result;
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
			throw new OneWireException( "This device does not have AD alarms" );
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
			throw new OneWireException( "This device does not have AD alarms" );
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
			throw new OneWireException( "This device does not have AD alarms" );
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
			if( channel == 0 ) {
				// 4.88 mV
				return 0.00488;   //its always the same!
			}
			else {
				// if internal resistor is used
				if( internalResistor )
					// Resolution is in 0.625 mA units so to get the resolution in Volts,
					// multiply with the value of the internal resistor.
					return 0.000625d * internalResistorValue;
				else
					// external resistor is used
					// 15.625 uV units
					return 0.000015625d;
			}
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
			return 5.0;
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
			throw new OneWireException( "This device does not have AD alarms" );
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
			throw new OneWireException( "This device does not have AD alarms" );
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
			// No resolutions to set
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
			// Only one range on this part
		}

		//--------
		//-------- Temperature Feature methods
		//--------

		/// <summary>
		/// Determines whether this temperature measuring device has high/low trip alarms.
		/// </summary>
		/// <returns>
		/// true if this TemperatureContainer has high/low trip alarms
		/// </returns>
		/// <see cref="GetTemperatureAlarm"/>
		/// <see cref="GetTemperatureAlarm"/>
		public bool HasTemperatureAlarms()
		{
			return false;
		}

		/// <summary>
		/// Determines whether this device has selectable temperature resolution
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if this device has selectable temperature resolution; otherwise, <c>false</c>.
		/// </returns>
		/// <see cref="GetTemperatureResolution"/>
		/// <see cref="GetTemperatureResolutions"/>
		/// <see cref="SetTemperatureResolution"/>
		public bool HasSelectableTemperatureResolution()
		{
			return false;
		}

		/// <summary>
		/// Get an array of available temperature resolutions in Celsius.
		/// </summary>
		/// <returns>
		/// byte array of available temperature resolutions in Celsius with
		/// minimum resolution as the first element and maximum resolution as the last element
		/// </returns>
		/// <see cref="HasSelectableTemperatureResolution"/>
		/// <see cref="GetTemperatureResolution"/>
		/// <see cref="SetTemperatureResolution"/>
		public double[] GetTemperatureResolutions()
		{
			double[] result = new double[1];

			result[0] = 0.125;

			return result;
		}

		/// <summary>
		/// Gets the temperature alarm resolution in Celsius.
		/// </summary>
		/// <returns>
		/// Temperature alarm resolution in Celsius for this 1-wire device
		/// </returns>
		/// <exception cref="OneWireException">Device does not support temperature alarms</exception>
		/// <see cref="HasTemperatureAlarms"/>
		/// <see cref="GetTemperatureAlarm"/>
		/// <see cref="SetTemperatureAlarm"/>
		public double GetTemperatureAlarmResolution()
		{
			throw new OneWireException( "This device does not have temperature alarms" );
		}

		/// <summary>
		/// Gets the maximum temperature in Celsius.
		/// </summary>
		/// <returns>
		/// Maximum temperature in Celsius for this 1-wire device
		/// </returns>
		public double GetMaxTemperature()
		{
			return 85.0;
		}

		/// <summary>
		/// Gets the minimum temperature in Celsius.
		/// </summary>
		/// <returns>
		/// Minimum temperature in Celsius for this 1-wire device
		/// </returns>
		public double GetMinTemperature()
		{
			return -40.0;
		}

		//--------
		//-------- Temperature I/O Methods
		//--------


		/// <summary>
		/// Performs a temperature conversion.
		/// </summary>
		/// <param name="state">byte array with device state information</param>
		/// <exception cref="OneWireException">Part could not be found [ fatal ]</exception>
		/// <exception cref="OneWireIOException">Data wasn'thread transferred properly [ recoverable ]</exception>
		public void DoTemperatureConvert( byte[] state )
		{
			// For the same reason we don't have to do an AD conversion,
			// we don't have to do a temperature conversion -- its done continuously
		}

		//--------
		//-------- Temperature 'get' Methods
		//--------

		/// <summary>
		/// Gets the temperature value in Celsius from the state data retrieved from the EeadDevice() method.
		/// </summary>
		/// <param name="state">byte array with device state information</param>
		/// <returns></returns>
		/// <exception cref="OneWireIOException">In the case of invalid temperature data</exception>
		public double GetTemperature( byte[] state )
		{
			double temperature;
			int data;

			// the MSB is at 24, the LSB at 25 and the format is so that when
			// attached, the whole thing must be shifted right 5 (Signed)
			data = ( state[24] << 8 ) | ( state[25] & 0xff );
			data = data >> 5;

			// that raw measurement is in .125 degree units
			temperature = data / 8.0;

			return temperature;
		}


		/// <summary>
		/// Gets the specified temperature alarm value in Celsius from the state data retrieved from the ReadDevice() method.
		/// </summary>
		/// <param name="alarmType">Type of the alarm. Valid types are: TemperatureContainerConsts.ALARM_HIGH or TemperatureContainerConsts.ALARM_LOW</param>
		/// <param name="state">byte array with device state information</param>
		/// <returns></returns>
		/// <exception cref="OneWireException">Device does not support temperature alarms</exception>
		/// <see cref="HasTemperatureAlarms"/>
		/// <see cref="SetTemperatureAlarm"/>
		public double GetTemperatureAlarm( int alarmType, byte[] state )
		{
			throw new OneWireException( "This device does not have temperature alarms" );
		}

		/// <summary>
		/// Gets the current temperature resolution in Celsius from the state data retrieved from the ReadDevice() method.
		/// </summary>
		/// <param name="state">byte array with device state information</param>
		/// <returns>
		/// resolution in Celsius for this 1-wire device
		/// </returns>
		/// <see cref="HasSelectableTemperatureResolution"/>
		/// <see cref="GetTemperatureResolutions"/>
		/// <see cref="SetTemperatureResolution"/>
		public double GetTemperatureResolution( byte[] state )
		{
			return 0.125;
		}

		//--------
		//-------- Temperature 'set' Methods
		//--------

		/// <summary>
		/// Sets the temperature alarm value in Celsius in the provided state data.
		/// Use the method WriteDevice() with this data to finalize the change to the device.
		/// </summary>
		/// <param name="alarmType">Type of the alarm. Valid types are: TemperatureContainerConsts.ALARM_HIGH or TemperatureContainerConsts.ALARM_LOW</param>
		/// <param name="alarmValue">alarm trip value in Celsius</param>
		/// <param name="state">byte array with device state information</param>
		/// <see cref="HasTemperatureAlarms"/>
		/// <see cref="GetTemperatureAlarm"/>
		public void SetTemperatureAlarm( int alarmType, double alarmValue,
										 byte[] state )
		{
			throw new OneWireException( "This device does not have temperature alarms" );
		}

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
		public void SetTemperatureResolution( double resolution, byte[] state )
		{
			// There can be only ONE resolution!
		}

		//--------
		//-------- Sensor I/O methods
		//--------

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
			byte[] result = new byte[32];

			/* perform the read twice to ensure a good transmission */
			DoSpeed();
			adapter.Reset();

			if( adapter.SelectDevice( address ) ) {

				/* do the first read */
				adapter.PutByte( READ_DATA_COMMAND );
				adapter.PutByte( 0 );
				adapter.GetBlock( result, 0, 32 );
			}
			else
				throw new OneWireException( "OneWireContainer30-Device not found." );

			return result;
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

			/* need to write the following bytes:
			 *   0 Protection register
			 *   1 Status register
			 *   7 EEPROM register
			 *   8 Special feature register
			 *   16 Accumulated current register MSB
			 *   17 Accumulated current register LSB
			 */

			//drain this....let's just make everything happen in real time
		}


	}
}
