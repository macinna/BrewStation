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
using DalSemi.Utils; // CRC8

namespace DalSemi.OneWire.Container
{
	/// <summary>
	/// 1-Wire container that encapsulates the functionality of the 1-Wire
	/// family type 26 (hex), Dallas Semiconductor part number: DS2438,
	/// Smart Battery Monitor.

	/// Features  
	/// - direct-to-digital temperature sensor
	/// - A/D converters which measures the battery voltage and current
	/// - integrated current accumulator which keeps a running
	/// - total of all current going into and out of the battery
	/// - elapsed time meter
	/// - 40 bytes of nonvolatile EEPROM memory for storage of important parameters
	/// - Operating temperature range from -40C to +85C

	/// Note
	/// Sometimes the VAD input will report 10.23 V even if nothing is attached.
	/// This value is also the maximum voltage that part can report.

	/// DataSheet
	/// http://pdfserv.maxim-ic.com/en/ds/DS2438.pdf
	/// http://www.ibutton.com/weather/humidity.html (no longer active 2008-09-02)
	/// </summary>
	public class OneWireContainer26 : OneWireContainer, IADContainer, TemperatureContainer, ClockContainer
	{
		/// <summary>
		/// Gets the family code.
		/// </summary>
		/// <returns></returns>
		public static byte GetFamilyCode()
		{
			return 0x26;
		}


		// Memory commands.
		private const byte READ_SCRATCHPAD_COMMAND = 0xBE;
		private const byte RECALL_MEMORY_COMMAND = 0xB8;
		private const byte COPY_SCRATCHPAD_COMMAND = 0x48;
		private const byte WRITE_SCRATCHPAD_COMMAND = 0x4E;
		private const byte CONVERT_TEMP_COMMAND = 0x44;
		private const byte CONVERT_VOLTAGE_COMMAND = 0xB4;


		/// <summary>
		///Channel selector for the VDD input.  Meant to be used with a battery.
		/// </summary>
		public const int CHANNEL_VDD = 0x00;

		/// <summary>
		/// Channel selector for the VAD input.  This is the general purpose A-D input.
		/// </summary>
		public const int CHANNEL_VAD = 0x01;

		/// <summary>
		/// Channel to select the the IAD input. Measures voltage across a resistor, Rsense, for calculating current.
		/// </summary>
		public const int CHANNEL_VSENSE = 0x02;

		/// <summary>
		/// Flag to set/check the Current A/D Control bit with SetFlag/GetFlag. When
		/// this bit is true, the current A/D and the ICA are enabled and
		/// current measurements will be taken at the rate of 36.41 Hz.
		/// </summary>
		public const byte IAD_FLAG = 0x01;

		/// <summary>
		/// Flag to set/check the Current Accumulator bit with setFlag/getFlag. When
		/// this bit is true, both the total discharging and charging current are
		/// integrated into seperate registers and can be used for determining
		/// full/empty levels.  When this bit is zero the memory (page 7) can be used
		/// as user memory.
		/// </summary>
		public const byte CA_FLAG = 0x02;

		/// <summary>
		/// Flag to set/check the Current Accumulator Shadow Selector bit with
		/// setFlag/getFlag.  When this bit is true the CCA/DCA registers used to
		/// add up charging/discharging current are shadowed to EEPROM to protect
		/// against loss of data if the battery pack becomes discharged.
		/// </summary>
		public const byte EE_FLAG = 0x04;

		/// <summary>
		/// Flag to set/check the voltage A/D Input Select Bit with SetFlag/GetFlag
		/// When this bit is true the battery input is (VDD) is selected as input for
		/// the voltage A/D input. When false the general purpose A/D input (VAD) is
		/// selected as the voltage A/D input.
		/// </summary>
		public const byte AD_FLAG = 0x08;

		/// <summary>
		/// Flag to check whether or not a temperature conversion is in progress
		/// using GetFlag().
		/// </summary>
		public const byte TB_FLAG = 0x10;

		/// <summary>
		/// Flag to check whether or not an operation is being performed on the
		/// nonvolatile memory using getFlag.
		/// </summary>
		public const byte NVB_FLAG = 0x20;

		/// <summary>
		/// Flag to check whether or not the A/D converter is busy using getFlag().
		/// </summary>
		public const byte ADB_FLAG = 0x40;

		/// <summary>
		/// Holds the value of the sensor resistance.
		/// </summary>
		private double Rsens = .05;

		/// <summary>
		/// Flag to indicate need to check speed
		/// </summary>
		private Boolean doSpeedEnable = true;

		//--------
		//-------- Constructors
		//--------

		/// <summary>
		/// Create a container with a provided adapter object
		/// and the address of the 1-Wire device.
		/// </summary>
		/// <param name="sourceAdapter">adapter object required to communicate with this device.</param>
		/// <param name="newAddress">address of this 1-Wire device</param>
		/// <see cref="OneWireContainer()"/>
		/// <see cref="DalSemi.OneWire.Utils.Address"/>
		public OneWireContainer26( PortAdapter sourceAdapter, byte[] newAddress )
			: base( sourceAdapter, newAddress )
		{
		}

		/// <summary>
		/// Returns an a MemoryBankList of MemoryBanks.  Default is no memory banks.
		/// 
		/// Gets an MemoryBankList of memory bank instances that implement one or more
		/// of the following interfaces:
		/// MemoryBank, PagedMemoryBank, OTPMemoryBank
		/// </summary>
		/// <returns>
		/// a list of memory banks to read and write memory on this iButton or 1-Wire device
		/// </returns>
		public override MemoryBankList GetMemoryBanks()
		{
			MemoryBankList bank_vector = new MemoryBankList();

			// Status
			bank_vector.Add( new MemoryBankSBM( this ) );

			// Temp/Volt/Current
			MemoryBankSBM temp = new MemoryBankSBM( this );
			temp.bankDescription = "Temperature/Voltage/Current";
			temp.generalPurposeMemory = false;
			temp.startPhysicalAddress = 1;
			temp.size = 6;
			temp.readWrite = false;
			temp.readOnly = true;
			temp.nonVolatile = false;
			temp.powerDelivery = false;
			bank_vector.Add( temp );

			// Threshold
			temp = new MemoryBankSBM( this );
			temp.bankDescription = "Threshold";
			temp.generalPurposeMemory = false;
			temp.startPhysicalAddress = 7;
			temp.size = 1;
			temp.readWrite = true;
			temp.readOnly = false;
			temp.nonVolatile = true;
			temp.powerDelivery = true;
			bank_vector.Add( temp );

			// Elapsed Timer Meter
			temp = new MemoryBankSBM( this );
			temp.bankDescription = "Elapsed Timer Meter";
			temp.generalPurposeMemory = false;
			temp.startPhysicalAddress = 8;
			temp.size = 5;
			temp.readWrite = true;
			temp.readOnly = false;
			temp.nonVolatile = false;
			temp.powerDelivery = true;
			bank_vector.Add( temp );

			// Current Offset
			temp = new MemoryBankSBM( this );
			temp.bankDescription = "Current Offset";
			temp.generalPurposeMemory = false;
			temp.startPhysicalAddress = 13;
			temp.size = 2;
			temp.readWrite = true;
			temp.readOnly = false;
			temp.nonVolatile = true;
			temp.powerDelivery = true;
			bank_vector.Add( temp );

			// Disconnect / End of Charge
			temp = new MemoryBankSBM( this );
			temp.bankDescription = "Disconnect / End of Charge";
			temp.generalPurposeMemory = false;
			temp.startPhysicalAddress = 16;
			temp.size = 8;
			temp.readWrite = true;
			temp.readOnly = false;
			temp.nonVolatile = false;
			temp.powerDelivery = true;
			bank_vector.Add( temp );

			// User Main Memory
			temp = new MemoryBankSBM( this );
			temp.bankDescription = "User Main Memory";
			temp.generalPurposeMemory = true;
			temp.startPhysicalAddress = 24;
			temp.size = 32;
			temp.readWrite = true;
			temp.readOnly = false;
			temp.nonVolatile = true;
			temp.powerDelivery = true;
			bank_vector.Add( temp );

			// User Memory / CCA / DCA
			temp = new MemoryBankSBM( this );
			temp.bankDescription = "User Memory / CCA / DCA";
			temp.generalPurposeMemory = false;
			temp.startPhysicalAddress = 56;
			temp.size = 8;
			temp.readWrite = true;
			temp.readOnly = false;
			temp.nonVolatile = true;
			temp.powerDelivery = true;
			bank_vector.Add( temp );

			return bank_vector;
		}

		/// <summary>
		/// Returns the Dallas Semiconductor part number of this 1-Wire device as a string.
		/// </summary>
		/// <returns>Representation of this 1-Wire device's name</returns>
		public override string GetName()
		{
			return "DS2438";
		}

		/// <summary>
		/// Return the alternate Dallas Semiconductor part number or name. ie. Smart Battery Monitor
		/// </summary>
		/// <returns>representation of the alternate name(s)</returns>
		public override string GetAlternateNames()
		{
			return "Smart Battery Monitor";
		}

		/// <summary>
		/// Retrieves a short description of the function of the 1-Wire device type.
		/// </summary>
		/// <returns>Device functional description</returns>
		public override string GetDescription()
		{
			return "1-Wire device that integrates the total current charging or "
				   + "discharging through a battery and stores it in a register. "
				   + "It also returns the temperature (accurate to 2 degrees celcius),"
				   + " as well as the instantaneous current and voltage and also "
				   + "provides 40 bytes of EEPROM storage.";
		}

		/// <summary>
		/// Set the value of the sense resistor used to determine
		/// battery current. This value is used in the <c>GetCurrent()</c> calculation.
		/// See the DS2438 datasheet for more information on sensing battery current.
		/// </summary>
		/// <param name="resistance">Value of the sense resistor in Ohms</param>
		public void SetSenseResistor( double resistance )
		{
			lock( lockObj ) {
				Rsens = resistance;
			}
		}

		/// <summary>
		/// Get the value used for the sense resistor in the <c>GetCurrent()</c> calculations.
		/// </summary>
		/// <returns>Currently stored value of the sense resistor in Ohms</returns>
		public double GetSenseResistor()
		{
			return Rsens;
		}

		/// <summary>
		/// Directs the container to avoid the calls to DoSpeed() in methods that communicate
		/// with the Thermocron. To ensure that all parts can talk to the 1-Wire bus
		/// at their desired speed, each method contains a call to <c>DoSpeed()</c>.
		/// However, this is an expensive operation. If a user manages the bus speed in an
		/// application, call this method with <c>doSpeedCheck</c> as <c>false</c>.
		/// The default behavior is to call <c>DoSpeed()</c>.
		/// </summary>
		/// <param name="doSpeedCheck">if set to DoSpeed() will be called before every 1-Wire bus access</param>
		public void SetSpeedCheck( Boolean doSpeedCheck )
		{
			lock( lockObj ) {
				doSpeedEnable = doSpeedCheck;
			}
		}

		/// <summary>
		/// Reads the specified 8 byte page and returns the data in an array.
		/// </summary>
		/// <param name="page">The page number to read</param>
		/// <returns>eight byte array that make up the page</returns>
		/// <exception cref="OneWireIOException">Error reading data</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public byte[] ReadPage( int page )
		{
			byte[] buffer = new byte[11]; // Holds 2 command bytes, 8 data bytes and 1 CRC byte
			byte[] result = new byte[8];
			uint crc8;   // this device uses a crc 8

			// Check validity of parameter */
			if( page < 0 || page > 7 ) {
				throw new ArgumentOutOfRangeException( "OneWireContainer26-Page " + page + " is an invalid page." );
			}

			// Perform the read/verification
			if( doSpeedEnable )
				DoSpeed();

			if( adapter.SelectDevice( address ) ) {
				// Recall memory to the scratchpad
				buffer[0] = RECALL_MEMORY_COMMAND;
				buffer[1] = (byte)page;

				adapter.DataBlock( buffer, 0, 2 );

				// Perform the read scratchpad by using a combined write and read buffer
				adapter.Reset();
				adapter.SelectDevice( address );

				buffer[0] = READ_SCRATCHPAD_COMMAND;
				buffer[1] = (byte)page;

				for( int i = 2; i < 11; i++ )
					buffer[i] = 0xff;

				adapter.DataBlock( buffer, 0, 11 );

				// CRC check. By including the CRC byte (the last byte) in the calculation
				// the calculated CRC will be 0 if it is valid
				crc8 = CRC8.Compute( buffer, 2, 9 );

				if( crc8 != 0x0 )
					throw new OneWireIOException(
					   "OneWireContainer26-Bad CRC during read: " + crc8 );

				// copy the data into the result
				Array.Copy( buffer, 2, result, 0, 8 );
			}
			else
				throw new OneWireException( "OneWireContainer26-device not found." );

			return result;
		}

		/// <summary>
		/// Writes a page of memory to this device. Pages 3-6 are always
		/// available for user storage and page 7 is available if the CA bit is set
		/// to 0 (false) with <c>SetFlag()</c>
		/// </summary>
		/// <param name="page">The page number</param>
		/// <param name="source">Data to be written to the page</param>
		/// <param name="offset">Offset with page to begin writting</param>
		/// <exception cref="OneWireIOException">Error reading data</exception>
		/// <exception cref="OneWireException"> Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public void WritePage( int page, byte[] source, int offset )
		{
			byte[] buffer = new byte[10];

			// Check parameter validity
			if( page < 0 || page > 7 )
				throw new ArgumentOutOfRangeException( "OneWireContainer26-Page " + page + " is an invalid page." );

			if( source.Length < 8 )
				throw new ArgumentOutOfRangeException( "OneWireContainer26-Invalid data page passed to writePage." );

			if( doSpeedEnable )
				DoSpeed();

			if( adapter.SelectDevice( address ) ) {

				// write the page to the scratchpad first
				buffer[0] = WRITE_SCRATCHPAD_COMMAND;
				buffer[1] = (byte)page;

				Array.Copy( source, offset, buffer, 2, 8 );
				adapter.DataBlock( buffer, 0, 10 );

				// now copy that part of the scratchpad to memory
				adapter.Reset();
				adapter.SelectDevice( address );

				buffer[0] = COPY_SCRATCHPAD_COMMAND;
				buffer[1] = (byte)page;

				adapter.DataBlock( buffer, 0, 2 );
			}
			else
				throw new OneWireException( "OneWireContainer26-Device not found." );
		}

		/// <summary>
		/// Checks the specified flag in the status/configuration register
		/// and returns its status as a boolean.
		/// </summary>
		/// <param name="flagToGet">Flag bitmask
		/// Acceptable parameters: IAD_FLAG, CA_FLAG, EE_FLAG, AD_FLAG, TB_FLAG, NVB_FLAG, ADB_FLAG
		/// (may be ORed with | to check the status of more than one).</param>
		/// <returns>The ORed result of the requested flags</returns>
		/// <exception cref="OneWireIOException">Error reading data</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public bool GetFlag( byte flagToGet )
		{
			byte[] data = ReadPage( 0 );

			if( ( data[0] & flagToGet ) != 0 )
				return true;

			return false;
		}

		/// <summary>
		/// Set one of the flags in the STATUS/CONFIGURATION register.
		/// </summary>
		/// <param name="flagToSet">bitmask of the flag to set
		/// Acceptable parameters: IAD_FLAG, CA_FLAG, EE_FLAG, AD_FLAG, TB_FLAG,
		/// NVB_FLAG, ADB_FLAG.
		/// </param>
		/// <param name="flagValue">value to set flag to</param>
		/// <exception cref="OneWireIOException">Error writting data</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public void SetFlag( byte flagToSet, bool flagValue )
		{
			byte[] data = ReadPage( 0 );

			if( flagValue )
				data[0] = (byte)( data[0] | flagToSet );
			else
				data[0] = (byte)( data[0] & ~( flagToSet ) );

			WritePage( 0, data, 0 );
		}

		/// <summary>
		/// Get the instantaneous current. The IAD flag must be true!!
		/// Remember to set the Sense resistor value using
		/// <c>setSenseResitor(double)</c>
		/// </summary>
		/// <param name="state">The current state of device as retrieved by <c>ReadDevice()</c></param>
		/// <returns>Current value in Amperes</returns>
		public double GetCurrent( byte[] state )
		{
			short rawCurrent = (short)( ( state[6] << 8 ) | ( state[5] & 0xff ) );

			return rawCurrent / ( 4096.0 * Rsens );
		}

		/// <summary>
		/// Gets the remaining capacity.Calculate the remaining capacity in mAH as outlined in the data sheet.
		/// </summary>
		/// <returns>battery capacity remaining in mAH</returns>
		/// <exception cref="OneWireIOException">Error reading data</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public double GetRemainingCapacity()
		{
			int ica = GetICA();

			return ( 1000 * ica / ( 2048 * Rsens ) );
		}

		/// <summary>
		/// Determines if the battery is charging. Note, the device must be correctly connected
		/// or this function will return the opposite state.
		/// </summary>
		/// <param name="state">The current state of device as retrieved by <c>ReadDevice()</c></param>
		/// <returns>
		/// 	<c>true</c> if the battery is charging; <c>false</c> if idle or discharging.
		/// </returns>
		/// <exception cref="OneWireIOException">Error reading data</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public bool IsCharging( byte[] state )
		{
			// positive current (if the thing is hooked up right) is charging
			return GetCurrent( state ) > 0;
		}

		/// <summary>
		/// Calibrate the current ADC. Although the part is shipped calibrated,
		/// calibrations should be done whenever possible for best results.
		/// NOTE: You MUST force zero current through Rsens (the sensor resistor) while calibrating.
		/// </summary>
		/// <exception cref="OneWireIOException">Error calibrating</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public void CalibrateCurrentADC()
		{
			byte[] data;
			byte currentLSB, currentMSB;

			// grab the current IAD settings so that we dont change anything
			bool IADvalue = GetFlag( IAD_FLAG );

			// the IAD bit must be set to "0" to write to the Offset Register
			SetFlag( IAD_FLAG, false );

			// write all zeroes to the offset register
			data = ReadPage( 1 );
			data[5] = data[6] = 0;

			WritePage( 1, data, 0 );

			// enable current measurements once again
			SetFlag( IAD_FLAG, true );

			// read the Current Register value
			data = ReadPage( 0 );
			currentLSB = data[5];
			currentMSB = data[6];

			// disable current measurements so that we can write to the offset reg
			SetFlag( IAD_FLAG, false );

			// change the sign of the current register value and store it as the offset
			data = ReadPage( 1 );
			data[5] = (byte)( ~( currentLSB ) + 1 );
			data[6] = (byte)( ~( currentMSB ) );

			WritePage( 1, data, 0 );

			// Reset the IAD settings back to normal
			SetFlag( IAD_FLAG, IADvalue );
		}

		/// <summary>
		/// Set the minimum current measurement magnitude for which the ICA/CCA/DCA
		/// are incremented. This is important for applications where the current
		/// may get very small for long periods of time. Small currents can be
		/// inaccurate by a high percentage, which leads to very inaccurate
		/// accumulations.
		/// </summary>
		/// <param name="thresholdValue">
		/// Minimum number of bits a current measurement must have to be accumulated,
		/// Only 0,2,4 and 8 are valid parameters
		/// </param>
		/// <exception cref="OneWireIOException">Error setting the threshold</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public void SetThreshold( byte thresholdValue )
		{
			byte thresholdReg;
			byte[] data;

			switch( thresholdValue ) {

				case 0:
					thresholdReg = 0;
					break;
				case 2:
					thresholdReg = 64;
					break;
				case 4:
					thresholdReg = (byte)128;
					break;
				case 8:
					thresholdReg = (byte)192;
					break;
				default:
					throw new ArgumentOutOfRangeException(
					  "OneWireContainer26-Threshold value must be 0, 2, 4, or 8." );
			}

			// first save their original IAD settings so we dont change anything
			bool IADvalue = GetFlag( IAD_FLAG );

			// current measurements must be off to write to the threshold register
			SetFlag( IAD_FLAG, false );

			// write the threshold register
			data = ReadPage( 0 );
			data[7] = thresholdReg;

			WritePage( 0, data, 0 );

			// set the IAD back to the way the user had it
			SetFlag( IAD_FLAG, IADvalue );
		}

		/// <summary>
		/// Retrieves the current ICA value in mVHr.
		/// </summary>
		/// <returns>value in the ICA register</returns>
		/// <exception cref="OneWireIOException">Error reading data</exception>
		/// <exception cref="OneWireException"> Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public int GetICA()
		{
			byte[] data = ReadPage( 1 );

			return (int)( data[4] & 0x000000ff );
		}

		/// <summary>
		/// Retrieves the current CCA value in mVHr. This value is accumulated over
		/// the lifetime of the part (until it is set to 0 or the CA flag is set
		/// to false) and includes only charging current (positive).
		/// </summary>
		/// <returns>CCA value</returns>
		/// <exception cref="OneWireIOException">Error reading data</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public int GetCCA()
		{
			byte[] data = ReadPage( 7 );

			return ( ( data[5] << 8 ) & 0x0000ff00 ) | ( data[4] & 0x000000ff );
		}

		/// <summary>
		/// Retrieves the value of the DCA in mVHr. This value is accumulated over
		/// the lifetime of the part (until explicitly set to 0 or if the CA flag
		/// is set to false) and includes only discharging current (negative).
		/// </summary>
		/// <returns>DCA value</returns>
		/// <exception cref="OneWireIOException">Error reading data</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public int GetDCA()
		{
			byte[] data = ReadPage( 7 );

			return ( ( data[7] << 8 ) & 0x0000ff00 ) | ( data[6] & 0x000000ff );
		}

		/// <summary>
		/// Set the value of the ICA.
		/// </summary>
		/// <param name="icaValue">The new ICA value.</param>
		/// <exception cref="OneWireIOException">Error writing data</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public void SetICA( int icaValue )
		{
			byte[] data = ReadPage( 1 );

			data[4] = (byte)( icaValue & 0x000000ff );

			WritePage( 1, data, 0 );
		}

		/// <summary>
		/// Set the value of the CCA.
		/// </summary>
		/// <param name="ccaValue">The new CCA value.</param>
		/// <exception cref="OneWireIOException">Error writing data</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public void SetCCA( int ccaValue )
		{
			byte[] data = ReadPage( 7 );

			data[4] = (byte)( ccaValue & 0x00ff );
			data[5] = (byte)( ( ccaValue & 0xff00 ) >> 8 );

			WritePage( 7, data, 0 );
		}

		/// <summary>
		/// Sets the DCA.
		/// </summary>
		/// <param name="dcaValue">The bew DCA value.</param>
		/// <exception cref="OneWireIOException">Error writing data</exception>
		/// <exception cref="OneWireException">Could not find part</exception>
		/// <exception cref="IllegalArgumentException">Bad parameters passed</exception>
		public void SetDCA( int dcaValue )
		{
			byte[] data = ReadPage( 7 );

			data[6] = (byte)( dcaValue & 0x00ff );
			data[7] = (byte)( ( dcaValue & 0xff00 ) >> 8 );

			WritePage( 7, data, 0 );
		}

		/// <summary>
		/// This method extracts the Clock Value in milliseconds from the
		/// state data retrieved from the <c>ReadDevice()</c> method.
		/// </summary>
		/// <param name="state">The current state of device as retrieved by <c>ReadDevice()</c></param>
		/// <returns>Time in milliseconds that have passed since 1 Jan 1970</returns>
		public long GetDisconnectTime( byte[] state )
		{
			return GetTime( state, 16 ) * 1000;
		}

		/// <summary>
		/// This method extracts the Clock Value in milliseconds from the
		/// state data retrieved from the <c>ReadDevice()</c> method.
		/// </summary>
		/// <param name="state">The current state of device as retrieved by <c>ReadDevice()</c></param>
		/// <returns>Time in milliseconds that have passed since 1 Jan 1970</returns>
		public long GetEndOfChargeTime( byte[] state )
		{
			return GetTime( state, 20 ) * 1000;
		}

		/// <summary>
		/// Extracts the time from the byte array
		/// this method could also be called ByteArrayToLong, only used in time functions
		/// </summary>
		/// <param name="state">The current state of device as retrieved by <c>ReadDevice()</c></param>
		/// <param name="start">Byte offset to start at</param>
		/// <returns></returns>
		private long GetTime( byte[] state, int start )
		{
			long time = state[start]
						| ( state[start + 1] << 8 )
						| ( state[start + 2] << 16 )
						| ( state[start + 3] << 24 );

			return time;
		}

		//////////////////////////////////////////////////////////////////////////////
		//
		//      INTERFACE METHODS
		//
		//////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Gets the number A/D channels.
		/// Channel specific methods will use a channel number specified
		/// by an integer from [0 to (<c>GetNumberADChannels()</c> - 1)]
		/// </summary>
		/// <returns>The number of channels</returns>
		public int GetNumberADChannels()
		{
			return 3;   //has VDD, VAD channel  (battery, gen purpose)
			// and it has a Vsense channel for current sensing
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

			if( channel == CHANNEL_VSENSE )
				result[0] = .250;
			else
				result[0] = 10.23;

			/* for VAD, not entirely true--this should be
			   2 * VDD.  If you hook up VDD to the
			   one-wire in series with a diode and then
			   hang a .1 microF capacitor off the line to ground,
			   you can get about 9.5 for the high end accurately
							 ----------------------------------
							 |             *****************  |
			   One-Wire------- DIODE-------*VDD     ONEWIRE*---
									   |   *               *
									   |   *        GROUND *---
									   C   *               *  |
									   |   *    2438       *  |
									  gnd  *               *  |
									   |   *****************  |
									   |----------------------|

			 */
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

			if( channel == CHANNEL_VSENSE )
				result[0] = 0.2441;
			else
				result[0] = 0.01;   //10 mV

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
		/// <exception cref="OneWireException">
		/// On a communication or setup error with the 1-Wire adapter.
		/// This is usually a non-recoverable error.</exception>
		public void DoADConvert( int channel, byte[] state )
		{
			if( channel == CHANNEL_VSENSE ) {
				if( ( state[0] & IAD_FLAG ) == 0 ) {
					// enable the current sense channel
					SetFlag( IAD_FLAG, true );
					state[0] |= IAD_FLAG;

					// updates once every 27.6 milliseconds
					System.Threading.Thread.Sleep( 30 );
				}

				byte[] data = ReadPage( 0 );
				// update the state
				Array.Copy( data, 5, state, 5, 2 );
			}
			else {
				SetFlag( AD_FLAG, channel == CHANNEL_VDD );

				// first perform the conversion
				if( doSpeedEnable )
					DoSpeed();

				if( adapter.SelectDevice( address ) ) {
					adapter.PutByte( CONVERT_VOLTAGE_COMMAND );

					System.Threading.Thread.Sleep( 4 );

					byte[] data = ReadPage( 0 );

					// Update state with this info
					Array.Copy( data, 0, state, 0, 8 );

					// Save off the voltage in our state's holding area
					state[24 + channel * 2] = data[4];
					state[24 + channel * 2 + 1] = data[3];
				}
				else
					throw new OneWireException( "OneWireContainer26-Device not found." );
			}
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
			throw new OneWireException( "This device cannot do multi-channel reads" );
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
			throw new OneWireException( "This device cannot do multi-channel reads" );
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
			double result = 0;

			if( channel == CHANNEL_VSENSE ) {
				// Resolution: 0.2441mV (1/4096)
				result = ( ( state[6] << 8 ) | ( state[5] & 0xff ) ) / 4096d;
			}
			else {
				// Resolution: 10mV
				result =
						( ( state[24 + channel * 2] << 8 ) & 0x300 )
						|
						( state[24 + channel * 2 + 1] & 0xff );
				// And convert from units to to real voltage value
				result *= GetADResolution( channel, state );
			}
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
			throw new OneWireException( "This device does not have A/D alarms" );
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
		public Boolean GetADAlarmEnable( int channel, int alarmType, byte[] state )
		{
			throw new OneWireException( "This device does not have A/D alarms" );
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
			throw new OneWireException( "This device does not have A/D alarms" );
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
			//this is easy, its always 0.01 V = 10 mV
			return 0.01;
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
			if( channel == CHANNEL_VSENSE )
				return .250;
			else
				return 10.23;
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
			throw new OneWireException( "This device does not have A/D alarms" );
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
									  Boolean alarmEnable, byte[] state )
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
			// You can't select the resolution for this part, just make it do nothing
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
			// You can't change the ranges here without changing VDD. Make this method do nothing
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
			// should return the first three pages
			// and then 4 extra bytes, 2 for channel 1 voltage and
			// 2 for channel 2 voltage
			byte[] state = new byte[28];

			for( int i = 0; i < 3; i++ ) {
				byte[] pg = ReadPage( i );

				Array.Copy( pg, 0, state, i * 8, 8 );
			}

			// The last four bytes are used this way:
			// The current voltage reading is kept in page 0,
			// but if a new voltage reading is asked for we move it to the
			// end so it can be there in case it is asked for again,
			// so we kind of weasel around this whole ADcontainer thing

			// Here is a little map
			//    byte[24] VDD high byte
			//    byte[25] VDD low byte
			//    byte[26] VAD high byte
			//    byte[27] VAD low byte

			return state;
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
			WritePage( 0, state, 0 );
			WritePage( 1, state, 8 );
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
		public Boolean HasSelectableTemperatureResolution()
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

			result[0] = 0.03125;

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
			throw new OneWireException(
			   "This device does not have temperature alarms" );
		}

		/// <summary>
		/// Gets the maximum temperature in Celsius.
		/// </summary>
		/// <returns>
		/// Maximum temperature in Celsius for this 1-wire device
		/// </returns>
		public double GetMaxTemperature()
		{
			return 125.0;
		}


		/// <summary>
		/// Gets the minimum temperature in Celsius.
		/// </summary>
		/// <returns>
		/// Minimum temperature in Celsius for this 1-wire device
		/// </returns>
		public double GetMinTemperature()
		{
			return -55.0;
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
			byte[] data;   // hold page

			if( doSpeedEnable )
				DoSpeed();

			if( adapter.SelectDevice( address ) ) {

				// perform the temperature conversion
				adapter.PutByte( CONVERT_TEMP_COMMAND );

				System.Threading.Thread.Sleep( 10 );

				data = ReadPage( 0 );
				state[2] = data[2];
				state[1] = data[1];
			}
			else
				throw new OneWireException( "OneWireContainer26-Device not found." );
		}

		//--------
		//-------- Temperature 'get' Methods
		//--------


		/// <summary>
		/// Gets the temperature value in Celsius from the state data retrieved from the ReadDevice() method.
		/// </summary>
		/// <param name="state">byte array with device state information</param>
		/// <returns></returns>
		/// <exception cref="OneWireIOException">In the case of invalid temperature data</exception>
		public double GetTemperature( byte[] state )
		{
			double temp = ( (short)( ( state[2] << 8 ) | ( state[1] & 0xff ) ) >> 3 )
						  * 0.03125;

			return temp;
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
			throw new OneWireException(
			   "This device does not have temperature alarms" );
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
			return 0.03125;
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
		public void SetTemperatureAlarm( int alarmType, double alarmValue, byte[] state )
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
			// Only one resolution supported, do nothing
		}

		//--------
		//-------- Clock Feature methods
		//--------


		/// <summary>
		/// Determines whether the clock has an alarm feature
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if present; otherwise, <c>false</c>.
		/// </returns>
		public bool HasClockAlarm()
		{
			return false;
		}

		/// <summary>
		/// Checks to see if the clock can be disabled.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if the clock can be disabled; otherwise, <c>false</c>.
		/// </returns>
		public bool CanDisableClock()
		{
			return false;
		}

		/// <summary>
		/// Gets the clock resolution in milliseconds
		/// </summary>
		/// <returns>the clock resolution in milliseconds</returns>
		public long GetClockResolution()
		{
			return 1000;
		}

		//--------
		//-------- Clock 'get' Methods
		//--------


		/// <summary>
		/// Extracts the Real-Time clock value in milliseconds.
		/// </summary>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// The time represented in this clock in milliseconds since 1970
		/// </returns>
		public long GetClock( byte[] state )
		{
			return GetTime( state, 8 ) * 1000;
		}


		/// <summary>
		/// Extracts the clock alarm value for the Real-Time clock.
		/// </summary>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// the set value of the clock alarm in milliseconds since 1970
		/// </returns>
		/// <exception cref="OneWireException">if this device does not have clock alarms</exception>
		public long GetClockAlarm( byte[] state )
		{
			throw new OneWireException( "This device does not have a clock alarm!" );
		}


		/// <summary>
		/// Checks if the clock alarm flag has been set.
		/// This will occur when the value of the Real-Time clock equals the value of the clock alarm.
		/// </summary>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// 	<c>true</c> if the Real-Time clock is alarming
		/// </returns>
		public bool IsClockAlarming( byte[] state )
		{
			return false;
		}

		/// <summary>
		/// Determines whether the clock alarm is enabled.
		/// </summary>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns><c>true</c> if clock alarm is enabled.</returns>
		public bool IsClockAlarmEnabled( byte[] state )
		{
			return false;
		}

		/// <summary>
		/// Checks if the device's oscillator is enabled.  The clock will not increment if the clock oscillator is not enabled.
		/// </summary>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns><c>true</c> if the clock is running.</returns>
		public bool IsClockRunning( byte[] state )
		{
			return true;
		}

		//--------
		//-------- Clock 'set' Methods
		//--------

		/// <summary>
		/// Sets the Real-Time clock. The method <c>writeDevice()</c> must be called to finalize
		/// changes to the device.  Note that multiple 'set' methods can be called before one call
		/// to <c>WriteDevice()</c>.
		/// </summary>
		/// <param name="time">New value for the Real-Time clock, in milliseconds since January 1, 1970</param>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		public void SetClock( long time, byte[] state )
		{
			time = time / 1000;   //convert to seconds
			state[8] = (byte)time;
			state[9] = (byte)( time >> 8 );
			state[10] = (byte)( time >> 16 );
			state[11] = (byte)( time >> 24 );
		}

		/// <summary>
		/// Sets the clock alarm.
		/// The method <c>WriteDevice()</c> must be called to finalize
		/// changes to the device.  Note that multiple 'set' methods can
		/// be called before one call to <c>WriteDevice()</c>.  Also note that
		/// not all clock devices have alarms.  Check to see if this device has
		/// alarms first by calling the <code>HasClockAlarm()</code> method.
		/// </summary>
		/// <param name="time">new value for the Real-Time clock alarm, in milliseconds since January 1, 1970</param>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		public void SetClockAlarm( long time, byte[] state )
		{
			throw new OneWireException( "This device does not have a clock alarm!" );
		}

		/// <summary>
		/// Enables or disables the oscillator, turning the clock 'on' and 'off'.
		/// The method <c>WriteDevice()</c> must be called to finalize
		/// changes to the device.  Note that multiple 'set' methods can
		/// be called before one call to <c>WriteDevice()</c>.  Also note that
		/// not all clock devices can disable their oscillators.  Check to see if this device can
		/// disable its oscillator first by calling the <c>CanDisableClock()</c> method.
		/// </summary>
		/// <param name="runEnable">true to enable the clock oscillator</param>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <exception cref="OneWireException">if the clock oscillator cannot be disabled</exception>
		public void SetClockRunEnable( Boolean runEnable, byte[] state )
		{
			if( !runEnable )
				throw new OneWireException( "This device's clock cannot be disabled!" );
		}


		/// <summary>
		/// Enables or disables the clock alarm.
		/// The method <c>WriteDevice()</c> must be called to finalize
		/// changes to the device.  Note that multiple 'set' methods can
		/// be called before one call to <c>WriteDevice()</c>.  Also note that
		/// not all clock devices have alarms.  Check to see if this device has
		/// alarms first by calling the <c>HasClockAlarm()</c> method.
		/// </summary>
		/// <param name="alarmEnable">true to enable the clock alarm</param>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <exception cref="OneWireException">if this device does not have clock alarms</exception>
		public void SetClockAlarmEnable( Boolean alarmEnable, byte[] state )
		{
			throw new OneWireException( "This device does not have a clock alarm!" );
		}
	}
}
