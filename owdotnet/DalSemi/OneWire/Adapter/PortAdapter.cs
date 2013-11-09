/*---------------------------------------------------------------------------
 * Copyright (C) 2004 Dallas Semiconductor Corporation, All Rights Reserved.
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
using System.Runtime.Serialization;
using DalSemi.Serial;

using DalSemi.OneWire.Container;

using System.Collections.Generic;
using System.Reflection;
using OWN.CustomContainer;

namespace DalSemi.OneWire.Adapter
{

	#region enums
	/// <summary>
	/// Indicates the communication speed of the 1-Wire line
	/// </summary>
	public enum OWSpeed : int
	{
		/// <summary>Speed modes for 1-Wire Network, regular                    </summary>
		SPEED_REGULAR = 0,
		/// <summary>Speed modes for 1-Wire Network, overdrive                  </summary>
		SPEED_OVERDRIVE = 1,
		/// <summary>Speed modes for 1-Wire Network, flexible for long lines    </summary>
		SPEED_FLEX = 2,
		/// <summary>Speed modes for 1-Wire Network, hyperdrive                 </summary>
		SPEED_HYPERDRIVE = 3
	}

	/// <summary>
	/// Indicates the power level of the 1-Wire line
	/// </summary>
	public enum OWLevel : int
	{
		/// <summary>1-Wire Network level, normal (weak 5Volt pullup)                            </summary>
		LEVEL_NORMAL = 0,
		/// <summary>1-Wire Network level, (strong 5Volt pullup, used for power delivery) </summary>
		LEVEL_POWER_DELIVERY = 1,
		/// <summary>1-Wire Network level, (strong pulldown to 0Volts, reset 1-Wire)      </summary>
		LEVEL_BREAK = 2,
		/// <summary>1-Wire Network level, (strong 12Volt pullup, used to program eprom ) </summary>
		LEVEL_PROGRAM = 3
	}

	/// <summary>
	/// Indicates result of 1-Wire line reset
	/// </summary>
	public enum OWResetResult : int
	{
		/// <summary>1-Wire Network reset result = no presence </summary>
		RESET_NOPRESENCE = 0x00,
		/// <summary>1-Wire Network reset result = presence    </summary>
		RESET_PRESENCE = 0x01,
		/// <summary>1-Wire Network reset result = alarm       </summary>
		RESET_ALARM = 0x02,
		/// <summary>1-Wire Network reset result = shorted     </summary>
		RESET_SHORT = 0x03
	}

	/// <summary>
	/// Indicates the change condition to begin power delivery
	/// </summary>
	public enum OWPowerStart : int
	{
		/// <summary>Condition for power state change, immediate                      </summary>
		CONDITION_NOW = 0,
		/// <summary>Condition for power state change, after next bit communication   </summary>
		CONDITION_AFTER_BIT = 1,
		/// <summary>Condition for power state change, after next byte communication  </summary>
		CONDITION_AFTER_BYTE = 2
	}

	/// <summary>
	/// Indicates the amount of time to deliver power
	/// </summary>
	public enum OWPowerTime : int
	{
		/// <summary>Duration used in delivering power to the 1-Wire, 1/2 second         </summary>
		DELIVERY_HALF_SECOND = 0,
		/// <summary>Duration used in delivering power to the 1-Wire, 1 second           </summary>
		DELIVERY_ONE_SECOND = 1,
		/// <summary>Duration used in delivering power to the 1-Wire, 2 seconds          </summary>
		DELIVERY_TWO_SECONDS = 2,
		/// <summary>Duration used in delivering power to the 1-Wire, 4 second           </summary>
		DELIVERY_FOUR_SECONDS = 3,
		/// <summary>Duration used in delivering power to the 1-Wire, smart complete     </summary>
		DELIVERY_SMART_DONE = 4,
		/// <summary>Duration used in delivering power to the 1-Wire, infinite           </summary>
		DELIVERY_INFINITE = 5,
		/// <summary>Duration used in delivering power to the 1-Wire, current detect     </summary>
		DELIVERY_CURRENT_DETECT = 6,
		/// <summary>Duration used in delivering power to the 1-Wire, 480 us             </summary>
		DELIVERY_EPROM = 7
	}

	/// <summary>
	/// Indicates how a newly created container will be (speed) inited
	/// </summary>
	public enum PASpeedInit : int
	{
		/// <summary>Leave untouched          </summary>
		INITSPEED_NONE = 0,
		/// <summary>Try to run at max speed  </summary>
		INITSPEED_TRY_MAX = 1,
		/// <summary>Force to run at max speed</summary>
		INITSPEED_FORCE_MAX = 2
	}

	#endregion

	/// <summary>
	/// Exception object thrown by all PortAdapters, to represent adapter communication
	/// exceptions
	/// </summary>
	public class AdapterException : Exception
	{
		/// <summary>
		/// constructs exception with the given message
		/// </summary>
		/// <param name="msg"></param>
		public AdapterException( string msg )
			: base( msg )
		{ }

		/// <summary>
		/// constructs exception with the given inner exception
		/// </summary>
		/// <param name="ex"></param>
		public AdapterException( Exception ex )
			: base( "AdapterException: " + ex.Message, ex )
		{ }

		/// <summary>
		/// Constructs exception with the given message, and given internal exception
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="ex"></param>
		public AdapterException( String msg, Exception ex )
			: base( msg, ex )
		{ }
	}


	/// <summary>
	/// Abstract base class for all 1-Wire Adapter objects.
	/// </summary>
	public abstract class PortAdapter : IDisposable
	{
		// Protected object used for thread synchronizing - do not use "lock( this )" !
		protected object lockObj = new object();

		public virtual string AdapterSpecDescription
		{
			get { return GetType().ToString(); }
		}

		/// <summary>
		/// Returns a OneWireContainer object corresponding to the first iButton
		/// or 1-Wire device found on the 1-Wire Network. If no devices are found,
		/// then a null reference will be returned. In most cases, all further
		/// communication with the device is done through the OneWireContainer
		/// </summary>
		/// <returns>
		/// The first OneWireContainer object found on the
		/// 1-Wire Network, or null if no devices found.
		/// </returns>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error</exception>
		/// <exception cref="OneWireException">On a setup error with the 1-Wire adapter</exception>
		public OneWireContainer GetFirstDeviceContainer()
		{
			byte[] address = new byte[8];
			if( GetFirstDevice( address, 0 ) ) {
				return GetDeviceContainer( address );
			}
			else
				return null;
		}


		/// <summary> Verifies that the iButton or 1-Wire device specified is present on
		/// the 1-Wire Network. This does not affect the 'current' device
		/// state information used in searches (findNextDevice...).
		/// </summary>
		/// <param name="address"> device address to verify is present
		/// </param>
		/// <returns>true if device is present, else false.
		/// </returns>
		public bool IsPresent( byte[] address )
		{
			return IsPresent( address, 0 );
		}


		/// <summary> Selects the specified iButton or 1-Wire device by broadcasting its
		/// address.  This operation is refered to a 'MATCH ROM' operation
		/// in the iButton and 1-Wire device data sheets.  This does not
		/// affect the 'current' device state information used in searches
		/// (GetNextDevice...).
		/// 
		/// Warning, this does not verify that the device is currently present
		/// on the 1-Wire Network (See IsPresent).
		/// </summary>
		/// <param name="address">byte array containing the addres of the iButton to select</param>
		/// <returns>true if device address was sent, false otherwise.</returns>
		public bool SelectDevice( byte[] address )
		{
			return SelectDevice( address, 0 );
		}

		/// <summary>
		/// Selects the specified iButton or 1-Wire device by broadcasting its
		/// address.  This operation is refered to a 'MATCH ROM' operation
		/// in the iButton and 1-Wire device data sheets.  This does not
		/// affect the 'current' device state information used in searches
		/// (GetNextDevice...).
		/// In addition, this method asserts that the select did find some
		/// devices on the 1-Wire net.  If no devices were found, a OneWireException
		/// is thrown.
		/// Warning, this does not verify that the device is currently present
		/// on the 1-Wire Network (See isPresent).
		/// </summary>
		/// <param name="address">byte array containing the addres of the device to select</param>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error, or if there are no devices on the 1-Wire net.</exception>
		/// <exception cref="OneWireException">On a setup error with the 1-Wire adapter</exception>  
		/// <seealso cref="DalSemi.OneWire.Adapter.PortAdapter.IsPresent(byte[])"/>
		/// <seealso cref="Address"/>
		public void AssertSelectDevice( byte[] address )
		{
			if( !SelectDevice( address ) )
				throw new OneWireIOException( "Device " + DalSemi.OneWire.Utils.Address.ToString( address ) + " not present." );
		}

		/// <summary>
		/// Gets the next device container.
		/// </summary>
		/// <returns>
		/// Returns a OneWireContainer object corresponding to the next iButton
		/// or 1-Wire device found. The previous 1-Wire device found is used
		/// as a starting point in the Search.  If no devices are found,
		/// then a null  reference will be returned. In most cases, all further
		/// communication with the device is done through the OneWireContainer
		/// </returns>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error</exception>
		/// <exception cref="OneWireException">On a setup error with the 1-Wire adapter</exception>
		public OneWireContainer GetNextDeviceContainer()
		{
			byte[] address = new byte[8];
			if( GetNextDevice( address, 0 ) ) {
				return GetDeviceContainer( address );
			}
			else
				return null;
		}

		private PASpeedInit speedInit = PASpeedInit.INITSPEED_NONE;

		/// <summary>
		/// Initicates how the speed of a (newly) created container is inited
		/// </summary>
		public PASpeedInit SpeedInit
		{
			get { return speedInit; }
			set { speedInit = value; }
		}

		/// <summary>
		/// Init the (newly) created container with some defaults indicated by the adapter
		/// </summary>
		/// <param name="container"></param>
		private void InitContainer( OneWireContainer container )
		{
			switch( speedInit ) {
			case PASpeedInit.INITSPEED_TRY_MAX: container.TryMaxSpeed(); break;
			case PASpeedInit.INITSPEED_FORCE_MAX: container.ForceMaxSpeed(); break;
			}
		}

		/// <summary>
		/// Constructs a OneWireContainer object with a user supplied 1-Wire network address.
		/// Like a factory...
		/// </summary>
		/// <param name="ROM">The ROM code (i.e. address) for the device</param>
		/// <returns>The OneWireContainer object</returns>
		/// <seealso cref="Address"/>
		/// <exception cref="AdapterException">If a custom device does not derive from CustomOneWireContainer</exception>
		public OneWireContainer GetDeviceContainer( byte[] ROM )
		{
			Type type;
			OneWireContainer retVal = null;

			// Dallas likes to invert the MSB of the familycode in their own products, such as
			// the ID chip in DS9490 adapter which reports family code 0x81, but really is a 0x01.
			// So, to be able to detect the correct class, we must mask the family code.
			byte familyCode = (byte)( ROM[0] & 0x7F );

			// First try to find a custom device that matches the the ROM code.
			string className = null;
			if( customContainersByROMCode.ContainsKey( Utils.Address.ToString( ROM ) ) ) {
				// First try with custom classes registered for a specific family name at runtime
				className = customContainersByROMCode[Utils.Address.ToString( ROM )].Name;
			}
			else {
				// Now try with a device with a specific ROM code
				className = AccessProvider.GetProperty( "CustomDevice." + Utils.Address.ToString( ROM ) );
			}
			if( className != null && className.Length > 0 ) {
				// The ROM was found in the property file, create an instance of the given class if registered
				if( customContainers.ContainsKey( className ) ) {
					Type t = customContainers[className];
					if( t.IsSubclassOf( typeof( CustomOneWireContainer ) ) ) {
						// A custom class exists for this device, create the instance
						retVal = (OneWireContainer)System.Activator.CreateInstance( t, new object[] { this, ROM } );
					}
					else {
						// This should never happen - custom devices are filtered when registered.
						throw new AdapterException( "Type " + t.Name + " is registered as a custom device, but does not derive from CustomOneWireContainer" );
					}
				}
			}
			// No custom device is specified, use the normal creation method
			else if( containerClasses.TryGetValue( familyCode, out type ) ) {
				retVal = (OneWireContainer)System.Activator.CreateInstance( type, new object[] { this, ROM } ); // call constructor (PortAdapter sourceAdapter, byte[] newAddress)
			}

			// If not found either as a custom or a standard device, then return a default wrapper for this ROM
			if( retVal == null )
				retVal = new OneWireContainer( this, ROM );

			// do some default initialization
			InitContainer( retVal );

			return retVal;
		}

		/// <summary>
		/// Constructs a OneWireContainer object with a user supplied 1-Wire network address.
		/// </summary>
		/// <param name="address">Device address with which to create a new container</param>
		/// <returns>The OneWireContainer object</returns>
		/// <seealso cref="Address"/>
		public OneWireContainer GetDeviceContainer( string address )
		{
			return GetDeviceContainer( DalSemi.OneWire.Utils.Address.ToByteArray( address ) );
		}


		/// <summary>
		/// Registers a custom OneWireContainer class.
		/// Call this routine like this: PortAdapter.RegisterOneWireContainerClass(typeof( CustomClassName ));
		/// </summary>
		/// <param name="oneWireContainerClass">The one wire container class.</param>
		public static void RegisterOneWireContainerClass( Type oneWireContainerClass )
		{
			if( oneWireContainerClass.IsSubclassOf( typeof( CustomOneWireContainer ) ) ) {
				if( customContainers.ContainsKey( oneWireContainerClass.Name ) ) {
					customContainers.Remove( oneWireContainerClass.Name );
				}
				customContainers.Add( oneWireContainerClass.Name, oneWireContainerClass );
			}
			else {
				byte familyCode = FamilyCodeByType( oneWireContainerClass );
				if( containerClasses.ContainsKey( familyCode ) )
					containerClasses.Remove( familyCode ); // remove (old) registered type
				containerClasses.Add( familyCode, oneWireContainerClass );
			}
		}

		/// <summary>
		/// Registers the custom container.
		/// </summary>
		/// <param name="ROM">The ROM.</param>
		/// <param name="type">The type.</param>
		public static void RegisterCustomContainer( string ROM, Type type )
		{
			// Remove old registration first
			if( customContainersByROMCode.ContainsKey( ROM ) ) {
				customContainersByROMCode.Remove( ROM );
			}
			customContainersByROMCode.Add( ROM, type );
			RegisterOneWireContainerClass( type );
		}

		/// <summary>
		/// Gets the type of the custom container.
		/// </summary>
		/// <param name="ROM">The ROM.</param>
		public static Type GetCustomContainerType( string ROM )
		{
			Type t = null;
			if( customContainersByROMCode.ContainsKey( ROM ) ) {
				t = customContainersByROMCode[ROM];
			}
			return t;
		}

		/// <summary>
		/// Retrievs the family code for the given Type.
		/// </summary>
		/// <param name="type">The Type, to retrieve the family code from</param>
		/// <returns>The family code of the given type</returns>
		/// <exception cref="ArgumentException">If the supplied type doesn't derive from OneWireContainer or if the GetFamilyCode method is missing</exception>
		private static byte FamilyCodeByType( Type type )
		{
			if( !type.IsSubclassOf( typeof( OneWireContainer ) ) )
				throw new ArgumentException( string.Format( "{0} does not derive from OneWireContainer", type.Name ) );
			foreach( MethodInfo mi in type.GetMethods( BindingFlags.Static | BindingFlags.Public ) ) {
				if( mi.Name == "GetFamilyCode" )
					return (byte)( (byte)mi.Invoke( null, null ) & 0x7F );
			}
			throw new ArgumentException( string.Format( "Method GetFamilyCode not found on {0}", type.Name ) );
		}

		private static Dictionary<byte, Type> containerClasses = new Dictionary<byte, Type>();
		private static Dictionary<string, Type> customContainers = new Dictionary<string, Type>();
		private static Dictionary<string, Type> customContainersByROMCode = new Dictionary<string, Type>();


		private static void InitAssemblyContainerClasses( Dictionary<byte, Type> dict, Dictionary<string, Type> customContainers )
		{
			// find all OneWireContainer in the loaded assemblies
			foreach( Assembly assembly in AppDomain.CurrentDomain.GetAssemblies() ) {
				foreach( Type type in assembly.GetTypes() ) {
					if( type.IsSubclassOf( typeof( CustomOneWireContainer ) ) ) {
						customContainers.Add( type.Name, type );
					}
					else if( !type.Equals( typeof( CustomOneWireContainer ) ) && type.IsSubclassOf( typeof( OneWireContainer ) ) ) {
						byte familyCode = FamilyCodeByType( type );
						if( familyCode == 0 )
							throw new Exception( string.Format( "Container class {0} returns a FamilyCode of 0. That is not allowed!", type.Name ) );
						Type t;
						if( dict.TryGetValue( familyCode, out t ) )
							throw new Exception( string.Format( "Trying to register class {0} for family 0x{1:X2}, but {2} is already registered for that family!", type.Name, familyCode, t.Name ) );
						dict.Add( familyCode, type );
					}
				}
			}
		}

		/// <summary>
		/// Gets the device containers present on the network.
		/// </summary>
		/// <returns>A DeviceContainerList, with all found devices</returns>
		/// <exception cref="OneWireIOException"/>
		/// <exception cref="OneWireException"/>
		public DeviceContainerList GetDeviceContainers()
		{
			DeviceContainerList dcl = new DeviceContainerList();
			OneWireContainer owc = GetFirstDeviceContainer();
			while( owc != null ) {
				dcl.Add( owc );
				owc = GetNextDeviceContainer();
			}
			return dcl;
		}

		#region Semaphores
		private System.Threading.Mutex exclusiveMutex = new System.Threading.Mutex( false );
		private int exclusiveCount = 0;
		private object BeginLock = new object();

		/// <summary>
		/// Obtain exclusive control of this adapter object
		/// </summary>
		/// <param name="blocking">if true, blocks until available</param>
		/// <returns>if true, exclusive control has been granted</returns>
		public virtual Boolean BeginExclusive( bool blocking )
		{
			/* not CF friendly
			if(!blocking)
			   return lockObject.WaitOne(1000, true);
			else
			   return lockObject.WaitOne();
			*/
			if( blocking || exclusiveCount == 0 ) {
				lock( BeginLock ) {
					if( blocking || exclusiveCount == 0 ) {
						if( exclusiveMutex.WaitOne() ) {
							exclusiveCount++;
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Release exclusive control of this adapter object
		/// </summary>
		public virtual void EndExclusive()
		{
			if( exclusiveCount > 0 ) {
				try {
					// throws exception if not owned
					exclusiveMutex.ReleaseMutex();
					// won't execute if above failes
					exclusiveCount--;
				}
				catch { }
			}
		}
		#endregion

		#region 1-Wire I/O

		/// <summary> Sends a Reset to the 1-Wire Network.</summary>
		/// <returns>  the result of the reset.</returns>
		public abstract OWResetResult Reset();

		/// <summary>Sends a bit to the 1-Wire Network.</summary>
		/// <param name="bitValue"> the bit value to send to the 1-Wire Network.</param>
		public abstract void PutBit( bool bitValue );


		/// <summary> Sends a byte to the 1-Wire Network.</summary>
		/// <param name="byteValue"> the byte value to send to the 1-Wire Network.</param>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error</exception>
		/// <exception cref="AdapterException">On a setup error with the 1-Wire adapter</exception>
		public abstract void PutByte( int byteValue );

		/// <summary>Gets a bit from the 1-Wire Network.</summary>
		/// <returns>The bit value recieved from the the 1-Wire Network.</returns>
		public abstract bool GetBit();

		/// <summary>Gets a byte from the 1-Wire Network.</summary>
		/// <returns>The byte value received from the the 1-Wire Network.</returns>
		public abstract byte GetByte();

		/// <summary>Get a block of data from the 1-Wire Network.</summary>
		/// <param name="len"> length of data bytes to receive</param>
		/// <returns>The data received from the 1-Wire Network.</returns>
		public abstract byte[] GetBlock( int len );

		/// <summary>
		/// Get a block of data from the 1-Wire Network and write it into
		/// the provided array.
		/// </summary>
		/// <param name="arr">Array in which to write the received bytes</param>
		/// <param name="len">Length of data bytes to receive</param>
		public abstract void GetBlock( byte[] arr, int len );

		/// <summary>
		/// Get a block of data from the 1-Wire Network and write it into
		/// the provided array.
		/// </summary>
		/// <param name="arr">Array in which to write the received bytes</param>
		/// <param name="off">Offset into the array to start</param>
		/// <param name="len">Length of data bytes to receive</param>
		public abstract void GetBlock( byte[] arr, int off, int len );

		/// <summary> Sends a block of data and returns the data received in the same array.
		/// This method is used when sending a block that contains reads and writes.
		/// The 'read' portions of the data block need to be pre-loaded with 0xFF's.
		/// It starts sending data from the index at offset 'off' for length 'len'.
		/// </summary>
		/// <param name="data">Array of data to transfer to and from the 1-Wire Network.</param>
		/// <param name="offset">offset into the array of data to start</param>
		/// <param name="length">length of data to send / receive starting at 'off'</param>
		/// <exception cref="OneWireIOException">On a 1-Wire communication error</exception>
		/// <exception cref="AdapterException">On a setup error with the 1-Wire adapter</exception>
		public abstract void DataBlock( byte[] data, int offset, int length );

		#endregion

		#region Communication Speed
		/// <summary>
		/// OWSpeed representing current speed of communication on 1-Wire network
		/// </summary>
		public abstract OWSpeed Speed
		{ set; get; }
		#endregion

		#region Power Delivery

		/// <summary> Sets the duration to supply power to the 1-Wire Network.
		/// This method takes a time parameter that indicates the program
		/// pulse length when the method startPowerDelivery().
		/// 
		/// Note: to avoid getting an exception,
		/// use the canDeliverPower() and canDeliverSmartPower()
		/// method to check it's availability.
		/// 
		/// </summary>
		/// <param name="powerDur">time factor
		/// </param>
		public abstract void SetPowerDuration( OWPowerTime powerDur );

		/// <summary> Sets the 1-Wire Network voltage to supply power to an iButton device.
		/// This method takes a time parameter that indicates whether the
		/// power delivery should be done immediately, or after certain
		/// conditions have been met.
		/// 
		/// Note: to avoid getting an exception,
		/// use the canDeliverPower() and canDeliverSmartPower()
		/// method to check it's availability.
		/// 
		/// </summary>
		/// <param name="changeCondition">change condition
		/// </param>
		/// <returns> <code>true</code> if the voltage change was successful,
		/// <code>false</code> otherwise.
		/// </returns>
		public abstract bool StartPowerDelivery( OWPowerStart changeCondition );

		/// <summary> Sets the duration for providing a program pulse on the
		/// 1-Wire Network.
		/// This method takes a time parameter that indicates the program
		/// pulse length when the method startProgramPulse().
		/// 
		/// Note: to avoid getting an exception,
		/// use the canDeliverPower() method to check it's
		/// availability.
		/// 
		/// </summary>
		/// <param name="pulseDur">time factor
		/// </param>
		public abstract void SetProgramPulseDuration( OWPowerTime pulseDur );

		/// <summary> Sets the 1-Wire Network voltage to eprom programming level.
		/// This method takes a time parameter that indicates whether the
		/// power delivery should be done immediately, or after certain
		/// conditions have been met.
		/// 
		/// Note: to avoid getting an exception,
		/// use the canProgram() method to check it's
		/// availability.
		/// 
		/// </summary>
		/// <param name="changeCondition">change condition
		/// </param>
		/// <returns> <code>true</code> if the voltage change was successful,
		/// <code>false</code> otherwise.
		/// 
		/// @throws OneWireIOException on a 1-Wire communication error
		/// @throws OneWireException on a setup error with the 1-Wire adapter
		/// or the adapter does not support this operation
		/// </returns>
		public abstract bool StartProgramPulse( OWPowerStart changeCondition );


		/// <summary> Sets the 1-Wire Network voltage to 0 volts.  This method is used
		/// rob all 1-Wire Network devices of parasite power delivery to force
		/// them into a hard reset.
		/// </summary>
		public abstract void StartBreak();


		/// <summary> Sets the 1-Wire Network voltage to normal level.  This method is used
		/// to disable 1-Wire conditions created by startPowerDelivery and
		/// startProgramPulse.  This method will automatically be called if
		/// a communication method is called while an outstanding power
		/// command is taking place.
		/// 
		/// @throws OneWireIOException on a 1-Wire communication error
		/// @throws OneWireException on a setup error with the 1-Wire adapter
		/// or the adapter does not support this operation
		/// </summary>
		public abstract void SetPowerNormal();

		#endregion

		#region Adapter Features

		/// <summary> Returns whether adapter can physically support overdrive mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do OverDrive,
		/// <code>false</code> otherwise.
		/// </returns>
		public abstract bool CanOverdrive { get; }

		/// <summary> Returns whether the adapter can physically support hyperdrive mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do HyperDrive,
		/// <code>false</code> otherwise.
		/// </returns>
		public abstract bool CanHyperdrive { get; }

		/// <summary> Returns whether the adapter can physically support flex speed mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do flex speed,
		/// <code>false</code> otherwise.
		/// </returns>
		public abstract bool CanFlex { get; }


		/// <summary> Returns whether adapter can physically support 12 volt power mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do Program voltage,
		/// <code>false</code> otherwise.
		/// </returns>
		public abstract bool CanProgram { get; }

		/// <summary> Returns whether the adapter can physically support strong 5 volt power
		/// mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do strong 5 volt
		/// mode, <code>false</code> otherwise.
		/// </returns>
		public abstract bool CanDeliverPower { get; }

		/// <summary> Returns whether the adapter can physically support "smart" strong 5
		/// volt power mode.  "smart" power delivery is the ability to deliver
		/// power until it is no longer needed.  The current drop it detected
		/// and power delivery is stopped.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do "smart" strong
		/// 5 volt mode, <code>false</code> otherwise.
		/// </returns>
		public abstract bool CanDeliverSmartPower { get; }

		/// <summary> Returns whether adapter can physically support 0 volt 'break' mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do break,
		/// <code>false</code> otherwise.
		/// </returns>
		public abstract bool CanBreak { get; }
		#endregion

		#region Searching

		/// <summary>
		/// Exclude family codes
		/// </summary>
		protected internal byte[] exclude = null;
		/// <summary>
		/// Include family codes
		/// </summary>
		protected internal byte[] include = null;

		/// <summary>
		/// Returns true if the first iButton or 1-Wire device is found on the 1-Wire Network.
		/// If no devices are found, then false will be returned.
		/// </summary>
		/// <param name="address">device address found</param>
		/// <param name="offset">offset into array where address begins</param>
		/// <returns>true if an iButton or 1-Wire device is found.</returns>
		public abstract bool GetFirstDevice( byte[] address, int offset );

		/// <summary> Returns true if the next iButton or 1-Wire device
		/// is found. The previous 1-Wire device found is used
		/// as a starting point in the Search.  If no more devices are found
		/// then false will be returned.
		/// </summary>
		/// <param name="address">device address found</param>
		/// <param name="offset">offset into array where address begins</param>
		/// <returns>true if an iButton or 1-Wire device is found.</returns>
		public abstract bool GetNextDevice( byte[] address, int offset );

		/// <summary>Verifies that the iButton or 1-Wire device specified is present on
		/// the 1-Wire Network. This does not affect the 'current' device
		/// state information used in searches (findNextDevice...).
		/// </summary>
		/// <param name="address">Device address to verify is present</param>
		/// <param name="offset">offset into array where address begins</param>
		/// <returns>  <code>true</code> if device is present else false.</returns>
		public abstract bool IsPresent( byte[] address, int offset );

		/// <summary>Verifies that the iButton or 1-Wire device specified is present
		/// on the 1-Wire Network and in an alarm state. This does not
		/// affect the 'current' device state information used in searches
		/// (GetNextDevice...).
		/// </summary>
		/// <param name="address">Device address to verify is present and alarming</param>
		/// <param name="offset">offset into array where address begins</param>
		/// <returns>  <code>true</code> if device is present and alarming else false.
		/// </returns>
		public abstract bool IsAlarming( byte[] address, int offset );

		/// <summary> Selects the specified iButton or 1-Wire device by broadcasting its
		/// address.  This operation is refered to a 'MATCH ROM' operation
		/// in the iButton and 1-Wire device data sheets.  This does not
		/// affect the 'current' device state information used in searches
		/// (GetNextDevice...).
		/// 
		/// Warning, this does not verify that the device is currently present
		/// on the 1-Wire Network (See isPresent).
		/// </summary>
		/// <param name="address">iButton to select</param>
		/// <param name="offset">offset into array where address begins</param>
		/// <returns>  <code>true</code> if device address was sent, false otherwise.</returns>
		public virtual bool SelectDevice( byte[] address, int offset )
		{
			// send 1-Wire Reset
			OWResetResult rslt = Reset();

			// broadcast the MATCH ROM command and address
			byte[] send_packet = new byte[9];

			send_packet[0] = (byte)( 0x55 ); // MATCH ROM command

			Array.Copy( address, offset, send_packet, 1, 8 );
			DataBlock( send_packet, 0, 9 );

			// success if any device present on 1-Wire Network
			return ( ( rslt == OWResetResult.RESET_PRESENCE ) || ( rslt == OWResetResult.RESET_ALARM ) );
		}

		/// <summary> Set the 1-Wire Network Search to find only iButtons and 1-Wire
		/// devices that are in an 'Alarm' state that signals a need for
		/// attention.  Not all iButton types
		/// have this feature.  Some that do: DS1994, DS1920, DS2407.
		/// This selective searching can be canceled with the
		/// 'setSearchAllDevices()' method.
		/// </summary>
		public abstract void SetSearchOnlyAlarmingDevices();

		/// <summary> Set the 1-Wire Network Search to not perform a 1-Wire
		/// reset before a Search.  This feature is chiefly used with
		/// the DS2409 1-Wire coupler.
		/// The normal reset before each Search can be restored with the
		/// 'setSearchAllDevices()' method.
		/// </summary>
		public abstract void SetNoResetSearch();

		/// <summary> Set the 1-Wire Network Search to find all iButtons and 1-Wire
		/// devices whether they are in an 'Alarm' state or not and
		/// restores the default setting of providing a 1-Wire reset
		/// command before each Search. (see setNoResetSearch() method).
		/// </summary>
		public abstract void SetSearchAllDevices();

		/// <summary> Removes any selectivity during a Search for iButtons or 1-Wire devices
		/// by family type.  The unique address for each iButton and 1-Wire device
		/// contains a family descriptor that indicates the capabilities of the
		/// device.
		/// </summary>
		public virtual void TargetAllFamilies()
		{
			include = null;
			exclude = null;
		}

		/// <summary> Takes an integer to selectively Search for this desired family type.
		/// If this method is used, then no devices of other families will be
		/// found by any of the Search methods.
		/// </summary>
		/// <param name="family">  the code of the family type to target for searches
		/// </param>
		public virtual void TargetFamily( int family )
		{
			if( ( include == null ) || ( include.Length != 1 ) )
				include = new byte[1];

			include[0] = (byte)family;
		}

		/// <summary> Takes an array of bytes to use for selectively searching for acceptable
		/// family codes.  If used, only devices with family codes in this array
		/// will be found by any of the Search methods.
		/// </summary>
		/// <param name="family"> array of the family types to target for searches
		/// </param>
		public virtual void TargetFamily( byte[] family )
		{
			if( ( include == null ) || ( include.Length != family.Length ) )
				include = new byte[family.Length];

			Array.Copy( family, 0, include, 0, family.Length );
		}

		/// <summary> Takes an integer family code to avoid when searching for iButtons.
		/// or 1-Wire devices.
		/// If this method is used, then no devices of this family will be
		/// found by any of the Search methods.
		/// </summary>
		/// <param name="family">  the code of the family type NOT to target in searches
		/// </param>
		public virtual void ExcludeFamily( int family )
		{
			if( ( exclude == null ) || ( exclude.Length != 1 ) )
				exclude = new byte[1];

			exclude[0] = (byte)family;
		}

		/// <summary> Takes an array of bytes containing family codes to avoid when finding
		/// iButtons or 1-Wire devices.  If used, then no devices with family
		/// codes in this array will be found by any of the Search methods.
		/// </summary>
		/// <param name="family"> array of family cods NOT to target for searches
		/// </param>
		public virtual void ExcludeFamily( byte[] family )
		{
			if( ( exclude == null ) || ( exclude.Length != family.Length ) )
				exclude = new byte[family.Length];

			Array.Copy( family, 0, exclude, 0, family.Length );
		}

		/// <summary> Checks to see if the family found is in the desired
		/// include group.
		/// 
		/// </summary>
		/// <returns>  <code>true</code> if in include group
		/// </returns>
		protected internal virtual bool IsValidFamily( byte familyCode )
		{
			if( exclude != null ) {
				for( int i = 0; i < exclude.Length; i++ ) {
					if( familyCode == exclude[i] ) {
						return false;
					}
				}
			}

			if( include != null ) {
				for( int i = 0; i < include.Length; i++ ) {
					if( familyCode == include[i] ) {
						return true;
					}
				}

				return false;
			}

			return true;
		}
		#endregion


		static PortAdapter()
		{
			InitAssemblyContainerClasses( containerClasses, customContainers );
		}

		~PortAdapter()
		{
			Dispose( false );
		}

		/// <summary>
		/// Dispose's all resources for this object
		/// </summary>
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		/// Dispose's all resources for this object
		/// </summary>
		protected abstract void Dispose( bool disposing );

		/// <summary>
		/// Opens the specified port and verifies existance of the adapter
		/// </summary>
		/// <returns>true if adapter found on specified port</returns>
		public abstract bool OpenPort( string portName );

		/// <summary>
		/// Closes the port and frees all resources from use
		/// </summary>
		public void FreePort()
		{
			Dispose();
		}

		/// <summary>
		/// The name of this adapter type
		/// </summary>
		public abstract string AdapterName { get; }

		/// <summary>
		/// The port name for this adapter type (i.e. COM1, LPT1, etc)
		/// </summary>
		public abstract string PortName { get; }


		//--------
		//-------- Adapter detection
		//--------

		/// <summary>
		/// Detects adapter presence on the selected port.
		/// </summary>
		/// <value><c>true</c> if adapter is detected; otherwise, <c>false</c>.</value>
		/// <returns>true if the adapter is confirmed to be connected to the selected port, false if the adapter is not connected.</returns>
		/// <exception cref="OneWireIOException"/>
		/// <exception cref="OneWireException"/>
		public abstract Boolean AdapterDetected { get; }

		/// <summary>
		/// Detailed description for this port type
		/// </summary>
		public abstract string PortTypeDescription { get; }
		/// <summary>
		/// Collection of valid port names for this port type
		/// </summary>
		public abstract System.Collections.IList PortNames { get; }

		#region Equals, HashCode, ToString
		/// <summary>
		/// Get Hash Code (proxies to .ToString().GetHashCode())
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}
		/// <summary>
		/// Tells if objects are equal (proxies to .ToString().Equals())
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public override bool Equals( System.Object o )
		{
			if( o != null && o is PortAdapter ) {
				if( o == this || o.ToString().Equals( this.ToString() ) ) {
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Returns a string representation of this adapter (i.e. "DS9097U COM1")
		/// </summary>
		/// <returns>A string representing the adapter, port name included</returns>
		public override string ToString()
		{
			return AdapterName + " " + PortName;
		}
		#endregion
	}

	public class DeviceContainerList : List<OneWireContainer>
	{
	}

}
