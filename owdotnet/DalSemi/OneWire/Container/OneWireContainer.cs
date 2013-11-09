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

using DalSemi.OneWire.Adapter; // PortAdapter

namespace DalSemi.OneWire.Container
{
	/// <summary>
	/// Base class for all 1-Wire containers
	/// </summary>
	public class OneWireContainer
	{
		// Protected object used for thread synchronizing - do not use "lock( this )" !
		protected object lockObj = new object();

		/// <summary>
		/// Create a container with a provided adapter object and the address of the iButton or 1-Wire device.
		/// This is one of the methods to construct a container.  The other is through creating a OneWireContainer with NO parameters.
		/// </summary>
		/// <param name="sourceAdapter">adapter object required to communicate with this device.</param>
		/// <param name="newAddress">address of this 1-Wire device</param>
		/// <see cref="OneWireContainer()"/>
		/// <see cref="DalSemi.OneWire.Utils.Address"/>
		public OneWireContainer( PortAdapter sourceAdapter, byte[] newAddress )
		{
			SetupContainer( sourceAdapter, newAddress );
		}

		/// <summary>
		/// Reference to the adapter that is needed to communicate with this iButton or 1-Wire device.
		/// </summary>
		protected PortAdapter adapter;


		/// <summary>
		/// 1-Wire Network Address of this iButton or 1-Wire device.
		/// Family code is byte at offset 0.
		/// </summary>
		/// <see cref="DalSemi.OneWire.Utils.Address"/>
		protected byte[] address;


		/// <summary>
		/// Temporary copy of 1-Wire Network Address of this iButton or 1-Wire device.
		/// </summary>
		/// <see cref="DalSemi.OneWire.Utils.Address"/>
		private byte[] addressCopy;


		/// <summary>
		/// Communication speed requested.
		/// 0 (SPEED_REGULAR)
		/// 1 (SPEED_FLEX)
		/// 2 (SPEED_OVERDRIVE)
		/// 3 (SPEED_HYPERDRIVE)
		/// >3 future speeds
		/// </summary>
		/// <see cref="DSPortAdapter.SetSpeed"/>
		protected OWSpeed speed;

		/// <summary>
		/// Flag to indicate that falling back to a slower speed then requested is OK.
		/// </summary>
		protected Boolean speedFallBackOK;

		/// <summary>
		/// Provides this container with the adapter object used to access this device and the address of the iButton or 1-Wire device.
		/// </summary>
		/// <param name="sourceAdapter">adapter object required to communicate with this iButton</param>
		/// <param name="newAddress">address of this 1-Wire device</param>
		/// <see cref="DalSemi.OneWire.Utils.Address"/>
		protected virtual void SetupContainer( PortAdapter sourceAdapter, byte[] newAddress ) // protected virtual for OneWireContainer16
		{

			// get a reference to the source adapter (will need this to communicate)
			adapter = sourceAdapter;

			// set the Address
			lock( lockObj ) {
				address = new byte[8];
				addressCopy = new byte[8];

				Array.Copy( newAddress, 0, address, 0, 8 );
			}

			// set desired speed to be SPEED_REGULAR by default with no fallback
			speed = OWSpeed.SPEED_REGULAR;
			speedFallBackOK = false;
		}

		/// <summary>
		/// Retrieves the port adapter object used to create this container.
		/// </summary>
		/// <value>The adapter instance</value>
		public PortAdapter Adapter
		{
			get { return adapter; }
		}

		/// <summary>
		/// Sets the maximum speed for this container.  Note this may be slower then the devices maximum speed. 
		/// This method can be used by an application to restrict the communication rate due 1-Wire line conditions.
		/// </summary>
		/// <param name="newSpeed">The new speed.</param>
		/// <param name="fallBack">if set to <c>true</c> it is OK to fall back to a slower speed</param>
		public void SetSpeed( OWSpeed newSpeed, Boolean fallBack )
		{
			speed = newSpeed;
			speedFallBackOK = fallBack;
		}

		/// <summary>
		/// Set desired speed to be MAX with fallback
		/// </summary>
		public void TryMaxSpeed()
		{
			SetSpeed( GetMaxSpeed(), true );
		}

		/// <summary>
		/// Set desired speed to be MAX with no fallback
		/// </summary>
		public void ForceMaxSpeed()
		{
			SetSpeed( GetMaxSpeed(), false );
		}

		/// <summary>
		/// Returns the maximum speed this iButton or 1-Wire device can communicate at.
		/// Override this method if derived iButton type can go faster than SPEED_REGULAR(0).
		/// </summary>
		/// <returns>The maxumin speed for the this device</returns>
		/// <see cref="DSPortAdapter.SetSpeed"/>
		public virtual OWSpeed GetMaxSpeed()
		{
			return OWSpeed.SPEED_REGULAR;
		}




		/// <summary>
		/// Retrieves the Dallas Semiconductor part number of the 1-Wire device as a string.  For example 'Crypto iButton' or 'DS1992'.
		/// </summary>
		/// <returns>1-Wire device name</returns>
		public virtual string GetName()
		{
			lock( lockObj ) {
				return string.Format( "Device type: {0:X2}", address[0] );
			}
		}



		/// <summary>
		/// Retrieves the alternate Dallas Semiconductor part numbers or names.
		/// A 'family' of 1-Wire Network devices may have more than one part number depending on packaging.
		/// There can also be nicknames such as 'Crypto iButton'.
		/// </summary>
		/// <returns>1-Wire device alternate names</returns>
		public virtual string GetAlternateNames()
		{
			return "";
		}


		/// <summary>
		/// Retrieves a short description of the function of the 1-Wire device type.
		/// </summary>
		/// <returns>Device functional description</returns>
		public virtual string GetDescription()
		{
			return "No description available.";
		}


		/// <summary>
		/// Returns an a MemoryBankList of MemoryBanks.  Default is no memory banks.
		/// </summary>
		/// <returns>enumeration of memory banks to read and write memory on this iButton or 1-Wire device</returns>
		/// <see cref="MemoryBank"/>
		public virtual MemoryBankList GetMemoryBanks()
		{
			return new MemoryBankList(); // empty...
		}



		/// <summary>
		/// Verifies that the iButton or 1-Wire device is present on the 1-Wire Network.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if this instance is present; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="OneWireIOException">on a 1-Wire communication error such as a read back verification fails.</exception>
		/// <exception cref="OneWireException">if adapter is not open</exception>
		public Boolean IsPresent()
		{
			lock( lockObj ) {
				return adapter.IsPresent( address );
			}
		}


		/// <summary>
		/// Verifies that the iButton or 1-Wire device is present on the 1-Wire Network and in an alarm state.
		/// This does not apply to all device types.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if this instance is present and alarming; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="OneWireIOException">on a 1-Wire communication error such as a read back verification fails.</exception>
		/// <exception cref="OneWireException">if adapter is not open</exception>
		public Boolean IsAlarming()
		{
			lock( lockObj ) {
				return adapter.IsAlarming( address, 0 );
			}
		}


		/// <summary>
		/// Go to the specified speed for this container. This method uses the containers selected speed (method SetSpeed(speed, fallback)) and
		/// will optionally fall back to a slower speed if communciation failed. Only call this method once to get the device into the desired speed
		/// as long as the device is still responding. 
		/// </summary>
		/// <exception cref="OneWireIOException">When selected speed fails AND fallback is false</exception>
		/// <exception cref="OneWireException">When hyperdrive is selected speed</exception>
		/// <see cref="SetSpeed(int,boolean)"/>
		public void DoSpeed()
		{
			bool is_present = false;

			try {
				// check if already at speed and device present
				if( ( speed == adapter.Speed ) && adapter.IsPresent( address ) )
					return;
			}
			catch( OneWireIOException ) {
				// VOID
			}

			// speed Overdrive
			if( speed == OWSpeed.SPEED_OVERDRIVE ) {
				try {
					// get this device and adapter to overdrive
					adapter.Speed = OWSpeed.SPEED_REGULAR;
					adapter.Reset();
					adapter.PutByte( 0x69 );
					adapter.Speed = OWSpeed.SPEED_OVERDRIVE;
				}
				catch( OneWireIOException ) {
					// VOID
				}

				// get copy of address
				lock( lockObj ) {
					Array.Copy( address, 0, addressCopy, 0, 8 );
					adapter.DataBlock( addressCopy, 0, 8 );
				}

				try {
					is_present = adapter.IsPresent( address );
				}
				catch( OneWireIOException ) {
					// VOID
				}

				// check if new speed is OK
				if( !is_present ) {

					// check if allow fallback
					if( speedFallBackOK )
						adapter.Speed = OWSpeed.SPEED_REGULAR;
					else
						throw new OneWireIOException(
						   "Failed to get device to selected speed (overdrive)" );
				}
			}
			// speed regular or flex
			else if( ( speed == OWSpeed.SPEED_REGULAR )
					 || ( speed == OWSpeed.SPEED_FLEX ) )
				adapter.Speed = speed;
			// speed hyperdrive, don'thread know how to do this
			else
				throw new OneWireException(
				   "Speed selected (hyperdrive) is not supported by this method" );
		}


		/// <summary>
		/// Gets the 1-Wire Network address of this device as an array of bytes.
		/// </summary>
		/// <value>1-Wire address</value>
		/// <see cref="DalSemi.OneWire.Utils.Address"/>
		public byte[] Address
		{
			get { return address; }
		}


		/// <summary>
		/// Gets this device's 1-Wire Network address as a String.
		/// </summary>
		/// <value>The address as string.</value>
		/// <see cref="DalSemi.OneWire.Utils.Address"/>
		public string AddressAsString
		{
			get { return DalSemi.OneWire.Utils.Address.ToString( address ); }
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			return DalSemi.OneWire.Utils.Address.ToString( this.address ) + " " + this.GetName();
		}


	}

	public class MemoryBankList : List<MemoryBank>
	{
	}

}
