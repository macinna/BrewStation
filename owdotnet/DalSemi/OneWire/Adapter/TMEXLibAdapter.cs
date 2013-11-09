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

//#define USE_DEBUG_LIBRARY

#if USE_DEBUG_LIBRARY
// Enabling the debug library will cause the TMEX adapter to write trace information 
// to a file in the currently executing assembly path.
using TMEXLibrary = DalSemi.OneWire.Adapter.TMEXLibraryDebug;
#else
using TMEXLibrary = DalSemi.OneWire.Adapter.RealTMEXLibrary;
#endif // USE_DEBUG_LIBRARY


using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Diagnostics;



namespace DalSemi.OneWire.Adapter
{
	internal enum TMEXPortType : int
	{
		ParallelPort = 2,
		SerialPort = 5,
		USBPort = 6,
		PassiveSerialPort = 1
	}

	internal class TMEXLibAdapter : PortAdapter
	{

		#region Ralph Maas / RM

		/**
    * Get the default Adapter Name.
    *
    * @return  String containing the name of the default adapter
    */
		public static string DefaultAdapterName
		{
			get
			{
				short[] portNumRef = new short[1];
				short[] portTypeRef = new short[1];
				System.Text.StringBuilder version = new System.Text.StringBuilder( 255 );
				string adapterName = "<na>";

				// get the default num/type
				if( TMEXLibrary.TMReadDefaultPort( portNumRef, portTypeRef ) == 1 ) {
					// read the version again
					if( TMEXLibrary.TMGetTypeVersion( portTypeRef[0], version ) == 1 ) {
						adapterName = GetToken( version.ToString(), TOKEN_DEV );
					}
				}

				return "{" + adapterName + "}";
			}
		}


		/**
		 * Get the default Adapter Port name.
		 *
		 * @return  String containing the name of the default adapter port
		 */
		public static string DefaultPortName
		{
			get
			{
				short[] portNumRef = new short[1];
				short[] portTypeRef = new short[1];
				System.Text.StringBuilder version = new System.Text.StringBuilder( 255 );
				string portName = "<na>";

				// get the default num/type
				if( TMEXLibrary.TMReadDefaultPort( portNumRef, portTypeRef ) == 1 ) {
					// read the version again
					if( TMEXLibrary.TMGetTypeVersion( portTypeRef[0], version ) == 1 ) {
						// get the abreviation from the version string
						string header = GetToken( version.ToString(), TOKEN_ABRV );

						// create the head and port number combo
						// Change COMU to COM
						if( header.Equals( "COMU" ) )
							portName = "COM" + portNumRef[0].ToString();
						else
							portName = header + portNumRef[0].ToString();
					}
				}

				return portName;
			}
		}

		public override string AdapterSpecDescription
		{
			get { return adapterSpecDescription; }
		}

		//--------
		//-------- Adapter detection 
		//--------

		/**
		 * Detect adapter presence on the selected port.
		 *
		 * @return  <code>true</code> if the adapter is confirmed to be connected to
		 * the selected port, <code>false</code> if the adapter is not connected.
		 *
		 * @throws OneWireIOException
		 * @throws OneWireException
		 */
		public override Boolean AdapterDetected
		{
			get
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new OneWireException( "Port not selected" );

				// return success if both port num and type are known
				return ( ( portNum >= 0 ) && ( portType >= 0 ) );
			}
		}

		#endregion


		#region Fields
		private System.Text.StringBuilder mainVersionBuffer
		  = new System.Text.StringBuilder( SIZE_VERSION );

		private System.Text.StringBuilder typeVersionBuffer
		   = new System.Text.StringBuilder( SIZE_VERSION );

		private bool[] adapterSpecFeatures;
		private string adapterSpecDescription = "TMEX adapter. Do 'OpenPort' first, for more specific description";

		private byte[] stateBuffer = new byte[SIZE_STATE];

		private TMEXPortType portType = TMEXPortType.SerialPort;

		/// <summary>
		/// Gets the type of the port.
		/// </summary>
		/// <value>The type of the port.</value>
		internal TMEXPortType PortType
		{
			get { return portType; }
		}

		private int portNum = -1;
		private int sessionHandle = -1;
		private bool inExclusive = false;
		private OWSpeed speed = OWSpeed.SPEED_REGULAR;
		#endregion

		#region Constants
		/// <summary>token indexes into version string </summary>
		private const int TOKEN_ABRV = 0;
		private const int TOKEN_DEV = 1;
		private const int TOKEN_VER = 2;
		private const int TOKEN_DATE = 3;
		private const int TOKEN_TAIL = 255;
		/// <summary>constant for state buffer size </summary>
		private const int SIZE_STATE = 5120;
		/// <summary>constant for size of version string </summary>
		private const int SIZE_VERSION = 256;
		/// <summary>constants for uninitialized ports </summary>
		private const int EMPTY_NEW = -2;
		private const int EMPTY_FREED = -1;
		#endregion

		#region Constructors and Destructors
		/// <summary> Constructs a default adapter
		/// 
		/// </summary>
		/// <throws>  ClassNotFoundException </throws>
		public TMEXLibAdapter()
		{
			// attempt to set the portType, will throw exception if does not exist
			//if (!setPortType_Native(getInfo(), portType))
			if( !SetTMEXPortType( portType ) ) {
				throw new AdapterException( "TMEX adapter type does not exist" );
			}
		}

		/// <summary> Constructs with a specified port type
		/// 
		/// 
		/// </summary>
		/// <param name="">newPortType
		/// </param>
		/// <throws>  ClassNotFoundException </throws>
		public TMEXLibAdapter( TMEXPortType newPortType )
		{
			// attempt to set the portType, will throw exception if does not exist
			//if (!setPortType_Native(getInfo(), portType))
			if( !SetTMEXPortType( newPortType ) ) {
				throw new AdapterException( "TMEX adapter type does not exist" );
			}
		}

		/// <summary> Finalize to Cleanup native</summary>
		protected override void Dispose( bool Disposing )
		{
			// Don't even try unless session is valid
			if( TMEXLibrary.TMValidSession( sessionHandle ) > 0 ) {

				// clean up open port and sessions
				TMEXLibrary.TMClose( sessionHandle );

				// release the session
				TMEXLibrary.TMEndSession( sessionHandle );

				// set flag to indicate this port is now free
				portNum = EMPTY_FREED;
			}

			// Always end exclusive
			sessionHandle = 0;
			inExclusive = false;
		}

		public override string AdapterName
		{
			get
			{
				// get the adapter name from the version string
				return "{" + GetToken( typeVersionBuffer.ToString(), TOKEN_DEV ) + "}";
			}
		}

		public override string PortName
		{
			get
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new AdapterException( "Port not selected" );

				// get the abreviation from the version string
				string header = GetToken( typeVersionBuffer.ToString(), TOKEN_ABRV );

				// create the head and port number combo
				// Change COMU to COM
				if( header.Equals( "COMU" ) )
					header = "COM";

				return header + portNum;
			}
		}

		public override System.Collections.IList PortNames
		{
			get
			{
				System.Collections.ArrayList portVector = new System.Collections.ArrayList( 16 );
				String header = GetToken( typeVersionBuffer.ToString(), TOKEN_ABRV );

				if( header.Equals( "COMU" ) )
					header = "COM";

				for( int i = 0; i < 16; i++ )
					portVector.Add( header + i );

				return ( portVector );
			}
		}

		public override System.String PortTypeDescription
		{
			get
			{
				// get the abreviation from the version string
				string abrv = GetToken( typeVersionBuffer.ToString(), TOKEN_ABRV );

				// Change COMU to COM
				if( abrv.Equals( "COMU" ) )
					abrv = "COM";

				return abrv + " (native)";
			}
		}

		/// <summary> Select the DotNet specified port type (0 to 15)  Use this
		/// method if the constructor with the PortType cannot be used.
		/// 
		/// 
		/// </summary>
		/// <param name="">newPortType
		/// </param>
		/// <returns>  true if port type valid.  Instance is only usable
		/// if this returns false.
		/// </returns>
		public bool SetTMEXPortType( TMEXPortType newPortType )
		{
			// check if already have a session handle open on old port
			if( this.sessionHandle > 0 )
				TMEXLibrary.TMEndSession( sessionHandle );

			this.sessionHandle = 0;
			this.inExclusive = false;

			// read the version strings
			TMEXLibrary.Get_Version( this.mainVersionBuffer );

			// will fail if not valid port type
			if( TMEXLibrary.TMGetTypeVersion( (int)newPortType, this.typeVersionBuffer ) > 0 ) {
				// set default port type
				portType = newPortType;
				return true;
			}
			return false;
		}

		/// <summary> Specify a platform appropriate port name for this adapter.  Note that
		/// even though the port has been selected, it's ownership may be relinquished
		/// if it is not currently held in a 'exclusive' block.  This class will then
		/// try to re-aquire the port when needed.  If the port cannot be re-aquired
		/// then the exception <code>PortInUseException</code> will be thrown.
		/// 
		/// </summary>
		/// <param name="portName"> name of the target port, retrieved from
		/// getPortNames()
		/// 
		/// </param>
		/// <returns> <code>true</code> if the port was aquired, <code>false</code>
		/// if the port is not available.
		/// 
		/// </returns>
		/// <exception cref="AdapterException">If port does not exist, or unable to communicate with port.</exception>
		/// <exception cref=""
		public override bool OpenPort( string portName )
		{
			int prtnum = 0, i;
			bool rt = false; ;

			// free the last port
			Dispose();

			// get the abreviation from the version string
			System.String header = GetToken( typeVersionBuffer.ToString(), TOKEN_ABRV );

			// Change COMU to COM
			if( header.Equals( "COMU" ) )
				header = "COM";

			// loop to make sure that the begining of the port name matches the head
			for( i = 0; i < header.Length; i++ ) {
				if( portName[i] != header[i] )
					return false;
			}

			// i now points to begining of integer (0 TO 15)
			if( ( portName[i] >= '0' ) && ( portName[i] <= '9' ) ) {
				prtnum = portName[i] - '0';
				if( ( ( i + 1 ) < portName.Length ) && ( portName[i + 1] >= '0' ) && ( portName[i + 1] <= '9' ) ) {
					prtnum *= 10;
					prtnum += portName[i + 1] - '0';
				}

				if( prtnum > 15 )
					return false;
			}

			// now have prtnum
			// get a session handle, 16 sec timeout
			TimeSpan timeout = new TimeSpan( 0, 0, 16 );
			DateTime start = DateTime.Now;

			do {
				this.sessionHandle = TMEXLibrary.TMExtendedStartSession( prtnum, (int)portType, null );
				// this port type does not exist
				if( sessionHandle == -201 )
					break;
				// valid handle
				else if( sessionHandle > 0 ) {
					// do setup
					if( TMEXLibrary.TMSetup( sessionHandle ) == 1 ) {
						// read the version again
						TMEXLibrary.TMGetTypeVersion( (int)portType, typeVersionBuffer );
						byte[] specBuffer = new byte[319];
						// get the adapter spec
						TMEXLibrary.TMGetAdapterSpec( sessionHandle, specBuffer );
						adapterSpecDescription = TMEXLibrary.GetDescriptionFromSpecification( specBuffer );
						adapterSpecFeatures = TMEXLibrary.GetFeaturesFromSpecification( specBuffer );

						// record the portnum
						this.portNum = (short)prtnum;
						// do a Search 
						// TODO: Why is this search performed? It won'thread throw an exception and we're not using the return value!
						// TMEXLibrary.TMFirst( sessionHandle, stateBuffer );
						// return success
						rt = true;
					}

					break;
				}
			}
			while( ( DateTime.Now - start ) < timeout );

			// close the session
			TMEXLibrary.TMEndSession( sessionHandle );
			sessionHandle = 0;

			// check if session was not available
			if( !rt ) {
				// free up the port
				Dispose();
				// throw exception
				throw new AdapterException( "1-Wire Net not available" );
			}

			return rt;
		}


		#endregion

		#region Data I/O
		/// <summary> Sends a Reset to the 1-Wire Network.
		/// 
		/// </summary>
		/// <returns>  the result of the reset. Potential results are:
		/// <ul>
		/// <li> 0 (RESET_NOPRESENCE) no devices present on the 1-Wire Network.
		/// <li> 1 (RESET_PRESENCE) normal presence pulse detected on the 1-Wire
		/// Network indicating there is a device present.
		/// <li> 2 (RESET_ALARM) alarming presence pulse detected on the 1-Wire
		/// Network indicating there is a device present and it is in the
		/// alarm condition.  This is only provided by the DS1994/DS2404
		/// devices.
		/// <li> 3 (RESET_SHORT) inticates 1-Wire appears shorted.  This can be
		/// transient conditions in a 1-Wire Network.  Not all adapter types
		/// can detect this condition.
		/// </ul>
		/// 
		/// </returns>
		/// <throws>  OneWireIOException on a 1-Wire communication error </throws>
		/// <throws>  AdapterException on a setup error with the 1-Wire adapter </throws>
		public override OWResetResult Reset()
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			// do 1-Wire reset
			int rt = TMEXLibrary.TMTouchReset( sessionHandle );
			// release the session
			ReleaseSession();

			// check for adapter communcication problems
			if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 || rt > 3 )
				throw new AdapterException( "native TMEX error " + rt );
			else if( rt == 3 )
				throw new AdapterException( "1-Wire Net shorted" );

			return (OWResetResult)rt;
		}

		/// <summary> Sends a bit to the 1-Wire Network.
		/// 
		/// </summary>
		/// <param name="bitValue"> the bit value to send to the 1-Wire Network.
		/// 
		/// </param>
		/// <throws>  OneWireIOException on a 1-Wire communication error </throws>
		/// <throws>  AdapterException on a setup error with the 1-Wire adapter </throws>
		public override void PutBit( bool bitValue )
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			// do 1-Wire bit
			int rt = TMEXLibrary.TMTouchBit( sessionHandle, (short)( bitValue ? 1 : 0 ) );
			// release the session
			ReleaseSession();

			// check for adapter communication problems
			if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error" + rt );

			if( bitValue != ( rt > 0 ) )
				throw new AdapterException( "Error during putBit()" );
		}

		/// <summary> Sends a byte to the 1-Wire Network.
		/// 
		/// </summary>
		/// <param name="byteValue"> the byte value to send to the 1-Wire Network.
		/// 
		/// </param>
		/// <throws>  OneWireIOException on a 1-Wire communication error </throws>
		/// <throws>  AdapterException on a setup error with the 1-Wire adapter </throws>
		public override void PutByte( int byteValue )
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			int rt = TMEXLibrary.TMTouchByte( sessionHandle, (short)byteValue );
			// release the session
			ReleaseSession();

			// check for adapter communcication problems
			if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error " + rt );

			if( rt != ( ( 0x00FF ) & byteValue ) )
				throw new AdapterException( "Error during putByte(), echo was incorrect " );
		}

		/// <summary> Gets a bit from the 1-Wire Network.
		/// 
		/// </summary>
		/// <returns>  the bit value recieved from the the 1-Wire Network.
		/// </returns>
		public override bool GetBit()
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			// do 1-Wire bit
			int rt = TMEXLibrary.TMTouchBit( sessionHandle, (short)1 );
			// release the session
			ReleaseSession();

			// check for adapter communication problems
			if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error" + rt );

			return ( rt > 0 );
		}


		/// <summary> Gets a byte from the 1-Wire Network.
		/// 
		/// </summary>
		/// <returns>  the byte value received from the the 1-Wire Network.
		/// </returns>
		public override byte GetByte()
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			int rt = TMEXLibrary.TMTouchByte( sessionHandle, (short)0x0FF );
			// release the session
			ReleaseSession();

			// check for adapter communcication problems
			if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error " + rt );

			return (byte)rt;
		}

		/// <summary> Gets a byte from the 1-Wire Network.
		/// 
		/// </summary>
		/// <returns>  the byte value received from the the 1-Wire Network.
		/// 
		/// </returns>
		/// <throws>  OneWireIOException on a 1-Wire communication error </throws>
		/// <throws>  AdapterException on a setup error with the 1-Wire adapter </throws>

		/// <summary> Get a block of data from the 1-Wire Network.
		/// 
		/// </summary>
		/// <param name="len"> length of data bytes to receive
		/// 
		/// </param>
		/// <returns>  the data received from the 1-Wire Network.
		/// 
		/// </returns>
		/// <throws>  OneWireIOException on a 1-Wire communication error </throws>
		/// <throws>  AdapterException on a setup error with the 1-Wire adapter </throws>
		public override byte[] GetBlock( int len )
		{
			byte[] barr = new byte[len];

			GetBlock( barr, 0, len );

			return barr;
		}

		/// <summary> Get a block of data from the 1-Wire Network and write it into
		/// the provided array.
		/// 
		/// </summary>
		/// <param name="arr">    array in which to write the received bytes
		/// </param>
		/// <param name="len">    length of data bytes to receive
		/// 
		/// </param>
		/// <throws>  OneWireIOException on a 1-Wire communication error </throws>
		/// <throws>  AdapterException on a setup error with the 1-Wire adapter </throws>
		public override void GetBlock( byte[] arr, int len )
		{
			GetBlock( arr, 0, len );
		}

		/// <summary> Get a block of data from the 1-Wire Network and write it into
		/// the provided array.
		/// 
		/// </summary>
		/// <param name="arr">    array in which to write the received bytes
		/// </param>
		/// <param name="off">    offset into the array to start
		/// </param>
		/// <param name="len">    length of data bytes to receive
		/// 
		/// </param>
		/// <throws>  OneWireIOException on a 1-Wire communication error </throws>
		/// <throws>  AdapterException on a setup error with the 1-Wire adapter </throws>
		public override void GetBlock( byte[] arr, int off, int len )
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			int rt;
			if( off == 0 ) {
				for( int i = 0; i < len; i++ )
					arr[i] = (byte)0x0FF;
				rt = TMEXLibrary.TMBlockStream( sessionHandle, arr, (short)len );
				// release the session
				ReleaseSession();
			}
			else {
				byte[] dataBlock = new byte[len];
				for( int i = 0; i < len; i++ )
					dataBlock[i] = (byte)0x0FF;
				rt = TMEXLibrary.TMBlockStream( sessionHandle, dataBlock, (short)len );
				// release the session
				ReleaseSession();
				Array.Copy( dataBlock, 0, arr, off, len );
			}

			// check for adapter communcication problems
			if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error " + rt );
		}

		/// <summary> Sends a block of data and returns the data received in the same array.
		/// This method is used when sending a block that contains reads and writes.
		/// The 'read' portions of the data block need to be pre-loaded with 0xFF's.
		/// It starts sending data from the index at offset 'off' for length 'len'.
		/// 
		/// </summary>
		/// <param name="dataBlock"> array of data to transfer to and from the 1-Wire Network.
		/// </param>
		/// <param name="off">       offset into the array of data to start
		/// </param>
		/// <param name="len">       length of data to send / receive starting at 'off'
		/// 
		/// </param>
		/// <throws>  OneWireIOException on a 1-Wire communication error </throws>
		/// <throws>  AdapterException on a setup error with the 1-Wire adapter </throws>
		public override void DataBlock( byte[] dataBlock, int off, int len )
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			int rt = 0;
			if( len > 1023 ) {
				byte[] dataBlockBuffer = new byte[1023];
				// Change to only do 1023 bytes at a time
				int numblocks = len / 1023;
				int extra = len % 1023;
				for( int i = 0; i < numblocks; i++ ) {
					Array.Copy( dataBlock, off + i * 1023, dataBlockBuffer, 0, 1023 );
					rt = TMEXLibrary.TMBlockStream( sessionHandle, dataBlockBuffer, (short)1023 );
					Array.Copy( dataBlockBuffer, 0, dataBlock, off + i * 1023, 1023 ); // RM: ADDED
					if( rt != 1023 )
						break;
				}
				if( ( rt >= 0 ) && ( extra > 0 ) ) {
					Array.Copy( dataBlock, off + numblocks * 1023, dataBlockBuffer, 0, extra ); // RM: MODIFIED
					rt = TMEXLibrary.TMBlockStream( sessionHandle, dataBlockBuffer, (short)extra );
					Array.Copy( dataBlockBuffer, 0, dataBlock, off + numblocks * 1023, extra ); // RM: ADDED
				}
			}
			else if( off > 0 ) {
				byte[] dataBlockOffset = new byte[len];
				Array.Copy( dataBlock, off, dataBlockOffset, 0, len );
				rt = TMEXLibrary.TMBlockStream( sessionHandle, dataBlockOffset, (short)len );
				Array.Copy( dataBlockOffset, 0, dataBlock, off, len ); // RM: ADDED
			}
			else {
				rt = TMEXLibrary.TMBlockStream( sessionHandle, dataBlock, (short)len );
			}
			// release the session
			ReleaseSession();

			// check for adapter communcication problems
			if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error " + rt );
		}
		#endregion

		#region Communication Speed
		/// <summary>OWSpeed representing current speed of communication on 1-Wire network
		/// </summary>
		public override OWSpeed Speed
		{
			set
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new AdapterException( "Port not selected" );

				// get a session 
				if( !GetSession() )
					throw new AdapterException( "Port in use" );

				// change speed
				int rt = TMEXLibrary.TMOneWireCom(
				   sessionHandle, TMEXLibrary.LEVEL_SET, (short)value );

				// (1.01) if in overdrive then force an exclusive
				if( value == OWSpeed.SPEED_OVERDRIVE )
					inExclusive = true;
				// release the session
				ReleaseSession();

				// check for adapter communication problems
				if( rt == -3 )
					throw new AdapterException(
					   "Adapter type does not support selected speed" );
				else if( rt == -12 )
					throw new AdapterException(
					   "1-Wire Adapter communication exception" );
				// check for microlan exception
				else if( rt < 0 )
					throw new AdapterException(
					   "native TMEX error" + rt );
				// check for could not set
				else if( rt != (short)value )
					throw new AdapterException(
					   "native TMEX error: could not set adapter to desired speed: " + rt );

				this.speed = value;
			}
			get
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					return OWSpeed.SPEED_REGULAR;

				// get a session 
				if( !GetSession() )
					return OWSpeed.SPEED_REGULAR;

				// change speed
				int rt = TMEXLibrary.TMOneWireCom( sessionHandle,
				   (short)TMEXLibrary.LEVEL_READ, (short)0 );

				// release the session
				ReleaseSession();

				if( rt < 0 || rt > 3 )
					return OWSpeed.SPEED_REGULAR;
				else
					return (OWSpeed)rt;
			}
		}
		#endregion

		#region Power Delivery

		/// <summary> Sets the duration to supply power to the 1-Wire Network.
		/// This method takes a time parameter that indicates the program
		/// pulse length when the method startPowerDelivery().<p>
		/// 
		/// Note: to avoid getting an exception,
		/// use the canDeliverPower() and canDeliverSmartPower()
		/// method to check it's availability. <p>
		/// 
		/// </summary>
		/// <param name="">timeFactor
		/// <ul>
		/// <li>   0 (DELIVERY_HALF_SECOND) provide power for 1/2 second.
		/// <li>   1 (DELIVERY_ONE_SECOND) provide power for 1 second.
		/// <li>   2 (DELIVERY_TWO_SECONDS) provide power for 2 seconds.
		/// <li>   3 (DELIVERY_FOUR_SECONDS) provide power for 4 seconds.
		/// <li>   4 (DELIVERY_SMART_DONE) provide power until the
		/// the device is no longer drawing significant power.
		/// <li>   5 (DELIVERY_INFINITE) provide power until the
		/// setBusNormal() method is called.
		/// </ul>
		/// </param>
		public override void SetPowerDuration( OWPowerTime timeFactor )
		{
			// Right now we only support infinite pull up.
			if( timeFactor != OWPowerTime.DELIVERY_INFINITE )
				throw new AdapterException(
				   "No support for other than infinite power duration" );
		}

		/// <summary> Sets the 1-Wire Network voltage to supply power to an iButton device.
		/// This method takes a time parameter that indicates whether the
		/// power delivery should be done immediately, or after certain
		/// conditions have been met. <p>
		/// 
		/// Note: to avoid getting an exception,
		/// use the canDeliverPower() and canDeliverSmartPower()
		/// method to check it's availability. <p>
		/// 
		/// </summary>
		/// <param name="">changeCondition
		/// <ul>
		/// <li>   0 (CONDITION_NOW) operation should occur immediately.
		/// <li>   1 (CONDITION_AFTER_BIT) operation should be pending
		/// execution immediately after the next bit is sent.
		/// <li>   2 (CONDITION_AFTER_BYTE) operation should be pending
		/// execution immediately after next byte is sent.
		/// </ul>
		/// 
		/// </param>
		/// <returns> <code>true</code> if the voltage change was successful,
		/// <code>false</code> otherwise.
		/// 
		/// </returns>
		/// <throws>  OneWireIOException on a 1-Wire communication error </throws>
		/// <throws>  AdapterException on a setup error with the 1-Wire adapter </throws>
		public override bool StartPowerDelivery( OWPowerStart changeCondition )
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			if( !adapterSpecFeatures[TMEXLibrary.FEATURE_POWER] )
				throw new AdapterException( "Hardware option not available" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			// start 12Volt pulse
			int rt = TMEXLibrary.TMOneWireLevel( sessionHandle,
			   TMEXLibrary.LEVEL_SET, TMEXLibrary.LEVEL_STRONG_PULLUP, (short)changeCondition );
			// release the session
			ReleaseSession();

			// check for adapter communication problems
			if( rt == -3 )
				throw new AdapterException( "Adapter type does not support power delivery" );
			else if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error" + rt );
			// check for could not set
			else if( ( rt != TMEXLibrary.LEVEL_STRONG_PULLUP )
			   && ( changeCondition == OWPowerStart.CONDITION_NOW ) )
				throw new AdapterException( "native TMEX error: could not set adapter to desired level: " + rt );

			return true;
		}

		/// <summary> Sets the duration for providing a program pulse on the
		/// 1-Wire Network.
		/// This method takes a time parameter that indicates the program
		/// pulse length when the method startProgramPulse().<p>
		/// 
		/// Note: to avoid getting an exception,
		/// use the canDeliverPower() method to check it's
		/// availability. <p>
		/// 
		/// </summary>
		/// <param name="">timeFactor
		/// <ul>
		/// <li>   6 (DELIVERY_EPROM) provide program pulse for 480 microseconds
		/// <li>   5 (DELIVERY_INFINITE) provide power until the
		/// setBusNormal() method is called.
		/// </ul>
		/// </param>
		public override void SetProgramPulseDuration( OWPowerTime timeFactor )
		{
			if( timeFactor != OWPowerTime.DELIVERY_EPROM )
				throw new AdapterException(
				   "Only support EPROM length program pulse duration" );
		}

		/// <summary> Sets the 1-Wire Network voltage to eprom programming level.
		/// This method takes a time parameter that indicates whether the
		/// power delivery should be done immediately, or after certain
		/// conditions have been met. <p>
		/// 
		/// Note: to avoid getting an exception,
		/// use the canProgram() method to check it's
		/// availability. <p>
		/// 
		/// </summary>
		/// <param name="">changeCondition
		/// <ul>
		/// <li>   0 (CONDITION_NOW) operation should occur immediately.
		/// <li>   1 (CONDITION_AFTER_BIT) operation should be pending
		/// execution immediately after the next bit is sent.
		/// <li>   2 (CONDITION_AFTER_BYTE) operation should be pending
		/// execution immediately after next byte is sent.
		/// </ul>
		/// </param>
		/// <returns> <code>true</code> if the voltage change was successful,
		/// <code>false</code> otherwise.
		/// </returns>
		public override bool StartProgramPulse( OWPowerStart changeCondition )
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			if( !adapterSpecFeatures[TMEXLibrary.FEATURE_PROGRAM] )
				throw new AdapterException( "Hardware option not available" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			int rt;
			// if pulse is 'now' then use TMProgramPulse
			if( changeCondition == OWPowerStart.CONDITION_NOW ) {
				rt = TMEXLibrary.TMProgramPulse( sessionHandle );
				// change rt value to be compatible with TMOneWireLevel
				if( rt != 0 )
					rt = TMEXLibrary.LEVEL_PROGRAM;
			}
			else {
				// start 12Volt pulse
				rt = TMEXLibrary.TMOneWireLevel( sessionHandle,
				   TMEXLibrary.LEVEL_SET, TMEXLibrary.LEVEL_PROGRAM, (short)changeCondition );
			}
			// release the session
			ReleaseSession();

			// check for adapter communication problems
			if( rt == -3 )
				throw new AdapterException( "Adapter type does not support EPROM programming" );
			else if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error" + rt );
			// check for could not set
			else if( ( rt != TMEXLibrary.LEVEL_PROGRAM )
			   && ( changeCondition == OWPowerStart.CONDITION_NOW ) )
				throw new AdapterException(
				   "native TMEX error: could not set adapter to desired level: " + rt );

			return true;
		}

		/// <summary> Sets the 1-Wire Network voltage to 0 volts.  This method is used
		/// rob all 1-Wire Network devices of parasite power delivery to force
		/// them into a hard reset.
		/// </summary>
		public override void StartBreak()
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			// start break
			int rt = TMEXLibrary.TMOneWireLevel( sessionHandle,
			   TMEXLibrary.LEVEL_SET, TMEXLibrary.LEVEL_BREAK, TMEXLibrary.PRIMED_NONE );
			// release the session
			ReleaseSession();

			// check for adapter communication problems
			if( rt == -3 )
				throw new AdapterException( "Adapter type does not support break" );
			else if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error" + rt );
			// check for could not set
			else if( rt != TMEXLibrary.LEVEL_BREAK )
				throw new AdapterException(
				   "native TMEX error: could not set adapter to break: " + rt );
		}

		/// <summary> Sets the 1-Wire Network voltage to normal level.  This method is used
		/// to disable 1-Wire conditions created by startPowerDelivery and
		/// startProgramPulse.  This method will automatically be called if
		/// a communication method is called while an outstanding power
		/// command is taking place.
		/// 
		/// </summary>
		/// <throws>  OneWireIOException on a 1-Wire communication error </throws>
		/// <throws>  AdapterException on a setup error with the 1-Wire adapter </throws>
		/// <summary>         or the adapter does not support this operation
		/// </summary>
		public override void SetPowerNormal()
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			// set back to normal
			int rt = TMEXLibrary.TMOneWireLevel( sessionHandle,
			   TMEXLibrary.LEVEL_SET,
			   (short)OWLevel.LEVEL_NORMAL, (short)OWPowerStart.CONDITION_NOW );
			// release the session
			ReleaseSession();

			if( rt < 0 )
				throw new AdapterException( "native TMEX error" + rt );
		}

		#endregion

		#region Adapter Features
		/// <summary> Returns whether adapter can physically support overdrive mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do OverDrive,
		/// <code>false</code> otherwise.
		/// </returns>
		public override bool CanOverdrive
		{
			get
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new AdapterException( "Port not selected" );

				return ( adapterSpecFeatures[TMEXLibrary.FEATURE_OVERDRIVE] );
			}
		}

		/// <summary> Returns whether the adapter can physically support hyperdrive mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do HyperDrive,
		/// <code>false</code> otherwise.
		/// </returns>
		public override bool CanHyperdrive
		{
			get
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new AdapterException( "Port not selected" );

				return false;
			}
		}

		/// <summary> Returns whether the adapter can physically support flex speed mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do flex speed,
		/// <code>false</code> otherwise.
		/// </returns>
		public override bool CanFlex
		{
			get
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new AdapterException( "Port not selected" );

				return ( adapterSpecFeatures[TMEXLibrary.FEATURE_FLEX] );
			}
		}


		/// <summary> Returns whether adapter can physically support 12 volt power mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do Program voltage,
		/// <code>false</code> otherwise.
		/// </returns>
		public override bool CanProgram
		{
			get
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new AdapterException( "Port not selected" );

				return ( adapterSpecFeatures[TMEXLibrary.FEATURE_PROGRAM] );
			}
		}

		/// <summary> Returns whether the adapter can physically support strong 5 volt power
		/// mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do strong 5 volt
		/// mode, <code>false</code> otherwise.
		/// </returns>
		public override bool CanDeliverPower
		{
			get
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new AdapterException( "Port not selected" );

				return ( adapterSpecFeatures[TMEXLibrary.FEATURE_POWER] );
			}
		}

		/// <summary> Returns whether the adapter can physically support "smart" strong 5
		/// volt power mode.  "smart" power delivery is the ability to deliver
		/// power until it is no longer needed.  The current drop it detected
		/// and power delivery is stopped.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do "smart" strong
		/// 5 volt mode, <code>false</code> otherwise.
		/// </returns>
		public override bool CanDeliverSmartPower
		{
			get
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new AdapterException( "Port not selected" );

				return false; // currently not implemented
			}
		}

		/// <summary> Returns whether adapter can physically support 0 volt 'break' mode.
		/// </summary>
		/// <returns>  <code>true</code> if this port adapter can do break,
		/// <code>false</code> otherwise.
		/// </returns>
		public override bool CanBreak
		{
			get
			{
				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new AdapterException( "Port not selected" );

				return ( adapterSpecFeatures[TMEXLibrary.FEATURE_BREAK] );
			}
		}
		#endregion

		#region Searching
		private bool resetSearch = true;
		private bool doAlarmSearch = false;
		private bool skipResetOnSearch = false;

		/// <summary> Returns <code>true</code> if the first iButton or 1-Wire device
		/// is found on the 1-Wire Network.
		/// If no devices are found, then <code>false</code> will be returned.
		/// </summary>
		/// <returns>  <code>true</code> if an iButton or 1-Wire device is found.
		/// </returns>
		public override bool GetFirstDevice( byte[] address, int offset )
		{
			// reset the internal rom buffer
			resetSearch = true;

			return GetNextDevice( address, offset );
		}

		/// <summary> Returns <code>true</code> if the next iButton or 1-Wire device
		/// is found. The previous 1-Wire device found is used
		/// as a starting point in the Search.  If no more devices are found
		/// then <code>false</code> will be returned.
		/// </summary>
		/// <returns>  <code>true</code> if an iButton or 1-Wire device is found.
		/// </returns>
		public override bool GetNextDevice( byte[] address, int offset )
		{
			bool retVal = false;

			while( true ) {
				Debug.WriteLine( "DEBUG: TMEXLibAdapter.GetNextDevice(byte[],int) called" );
				Debug.WriteLine( "DEBUG: TMEXLibAdapter.GetNextDevice, resetSearch=" + resetSearch );
				Debug.WriteLine( "DEBUG: TMEXLibAdapter.GetNextDevice, skipResetOnSearch=" + skipResetOnSearch );
				Debug.WriteLine( "DEBUG: TMEXLibAdapter.GetNextDevice, doAlarmSearch=" + doAlarmSearch );
				short[] ROM = new short[8];

				// check if port is selected
				if( ( portNum < 0 ) || ( portType < 0 ) )
					throw new AdapterException( "Port not selected" );

				short rt;

				// get a session 
				if( !GetSession() )
					throw new AdapterException( "Port in use" );
				try {

					rt = TMEXLibrary.TMSearch( sessionHandle, stateBuffer,
					   (short)( resetSearch ? 1 : 0 ), (short)( skipResetOnSearch ? 0 : 1 ),
					   (short)( doAlarmSearch ? 0xEC : 0xF0 ) );

					Debug.WriteLine( "DEBUG: TMEXLibary.TMSearch, rt=" + rt );

					// check for microlan exception
					if( rt < 0 )
						throw new AdapterException( "Native TMEX error " + rt );

					// retrieve the ROM number found
					ROM[0] = 0;
					short romrt = TMEXLibrary.TMRom( sessionHandle, stateBuffer, ROM );
					if( romrt == 1 ) {
						// Copy to state array
						for( int i = 0; i < 8; i++ )
							address[i + offset] = (byte)ROM[i];
					}
					else
						throw new AdapterException( "Native TMEX error " + romrt );

				}
				finally {
					// release the session
					ReleaseSession();
				}

				if( rt > 0 ) {
					resetSearch = false;

					// check if this is an OK family type
					if( IsValidFamily( address[offset] ) ) {
						retVal = true;
						break;
					}

					// Else, loop to the top and do another Search.
				}
				else {
					resetSearch = true;
					break;
				}
			}
			return retVal;
		}

		/// <summary> Verifies that the iButton or 1-Wire device specified is present on
		/// the 1-Wire Network. This does not affect the 'current' device
		/// state information used in searches (findNextDevice...).
		/// </summary>
		/// <param name="address"> device address to verify is present
		/// </param>
		/// <returns>  <code>true</code> if device is present else
		/// <code>false</code>.
		/// </returns>
		/// <seealso cref="Address">
		/// </seealso>
		public override bool IsPresent( byte[] address, int offset )
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			int rt; // RM

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );
			try // RM
			{
				short[] ROM = new short[8];
				for( int i = 0; i < 8; i++ )
					ROM[i] = address[i + offset];

				// get the current rom to restore after isPresent() (1.01)
				short[] oldROM = new short[8];
				oldROM[0] = 0;
				TMEXLibrary.TMRom( sessionHandle, stateBuffer, oldROM );
				// set this rom to TMEX
				TMEXLibrary.TMRom( sessionHandle, stateBuffer, ROM );
				// see if part this present
				rt = TMEXLibrary.TMStrongAccess( sessionHandle, stateBuffer );
				// restore  
				TMEXLibrary.TMRom( sessionHandle, stateBuffer, oldROM );
			}
            finally // RM
			{
				// release the session
				ReleaseSession();
			}

			// check for adapter communcication problems
			if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error" + rt );

			return ( rt > 0 );
		}

		/// <summary> Verifies that the iButton or 1-Wire device specified is present
		/// on the 1-Wire Network and in an alarm state. This does not
		/// affect the 'current' device state information used in searches
		/// (findNextDevice...).
		/// </summary>
		/// <param name="address"> device address to verify is present and alarming
		/// </param>
		/// <returns>  <code>true</code> if device is present and alarming else
		/// <code>false</code>.
		/// </returns>
		/// <seealso cref="Address">
		/// </seealso>
		public override bool IsAlarming( byte[] address, int offset )
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			short[] ROM = new short[8];
			for( int i = 0; i < 8; i++ )
				ROM[i] = address[i + offset];

			// get the current rom to restore after isPresent() (1.01)
			short[] oldROM = new short[8];
			oldROM[0] = 0;
			TMEXLibrary.TMRom( sessionHandle, stateBuffer, oldROM );
			// set this rom to TMEX
			TMEXLibrary.TMRom( sessionHandle, stateBuffer, ROM );
			// see if part this present
			int rt = TMEXLibrary.TMStrongAlarmAccess( sessionHandle, stateBuffer );
			// restore  
			TMEXLibrary.TMRom( sessionHandle, stateBuffer, oldROM );
			// release the session
			ReleaseSession();

			// check for adapter communication problems
			if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error" + rt );

			return ( rt > 0 );
		}

		/// <summary> Selects the specified iButton or 1-Wire device by broadcasting its
		/// address.  This operation is refered to a 'MATCH ROM' operation
		/// in the iButton and 1-Wire device data sheets.  This does not
		/// affect the 'current' device state information used in searches
		/// (findNextDevice...).
		/// 
		/// Warning, this does not verify that the device is currently present
		/// on the 1-Wire Network (See isPresent).
		/// </summary>
		/// <param name="address">    iButton to select
		/// </param>
		/// <returns>  <code>true</code> if device address was sent,<code>false</code>
		/// otherwise.
		/// </returns>
		public override bool SelectDevice( byte[] address, int off )
		{
			// check if port is selected
			if( ( portNum < 0 ) || ( portType < 0 ) )
				throw new AdapterException( "Port not selected" );

			// get a session 
			if( !GetSession() )
				throw new AdapterException( "Port in use" );

			byte[] send_block = new byte[9];
			send_block[0] = (byte)0x55; // match command
			for( int i = 0; i < 8; i++ )
				send_block[i + 1] = address[i + off];

			// Change to use a block, not TMRom/TMAccess
			int rt = TMEXLibrary.TMBlockIO( sessionHandle, send_block, (short)9 );
			// release the session
			ReleaseSession();

			// check for adapter communication problems
			if( rt == -12 )
				throw new AdapterException( "1-Wire Adapter communication exception" );
			// check for no device
			else if( rt == -7 )
				throw new AdapterException( "No device detected" );
			// check for microlan exception
			else if( rt < 0 )
				throw new AdapterException( "native TMEX error" + rt );

			return ( rt >= 1 );
		}

		/// <summary> Set the 1-Wire Network Search to find only iButtons and 1-Wire
		/// devices that are in an 'Alarm' state that signals a need for
		/// attention.  Not all iButton types
		/// have this feature.  Some that do: DS1994, DS1920, DS2407.
		/// This selective searching can be canceled with the
		/// 'setSearchAllDevices()' method.
		/// </summary>
		/// <seealso cref="#setNoResetSearch">
		/// </seealso>
		public override void SetSearchOnlyAlarmingDevices()
		{
			doAlarmSearch = true;
		}

		/// <summary> Set the 1-Wire Network Search to not perform a 1-Wire
		/// reset before a Search.  This feature is chiefly used with
		/// the DS2409 1-Wire coupler.
		/// The normal reset before each Search can be restored with the
		/// 'setSearchAllDevices()' method.
		/// </summary>
		public override void SetNoResetSearch()
		{
			skipResetOnSearch = true;
		}

		/// <summary> Set the 1-Wire Network Search to find all iButtons and 1-Wire
		/// devices whether they are in an 'Alarm' state or not and
		/// restores the default setting of providing a 1-Wire reset
		/// command before each Search. (see setNoResetSearch() method).
		/// </summary>
		/// <seealso cref="#setNoResetSearch">
		/// </seealso>
		public override void SetSearchAllDevices()
		{
			doAlarmSearch = false;
			skipResetOnSearch = false;
		}
		#endregion

		#region Private Helper Methods
		//--------------------------------------------------------------------------
		// Parse the version string for a token
		//
		private static string GetToken( System.String verStr, int token )
		{
			int currentToken = -1;
			bool inToken = false; ;
			System.String toReturn = "";

			for( int i = 0; i < verStr.Length; i++ ) {
				if( ( verStr[i] != ' ' ) && ( !inToken ) ) {
					inToken = true;
					currentToken++;
				}

				if( ( verStr[i] == ' ' ) && ( inToken ) )
					inToken = false;

				if( ( ( inToken ) && ( currentToken == token ) ) ||
				   ( ( token == 255 ) && ( currentToken > TOKEN_DATE ) ) )
					toReturn += verStr[i];
			}
			return toReturn;
		}

		/// <summary> Attempt to get a TMEX session.  If already in an 'exclusive' block
		/// then just return.  
		/// </summary>
		private bool GetSession()
		{
			lock( lockObj ) {
				int[] sessionOptions = new int[] { TMEXLibrary.SESSION_INFINITE };

				// check if in exclusive block
				if( inExclusive ) {
					// make sure still valid (if not get a new one)
					if( TMEXLibrary.TMValidSession( sessionHandle ) > 0 )
						return true;
				}

				// attempt to get a session handle (2 sec timeout)
				TimeSpan timeout = new TimeSpan( 0, 0, 2 );
				DateTime start = DateTime.Now;
				do {
					sessionHandle = TMEXLibrary.TMExtendedStartSession( portNum, (int)portType, sessionOptions );

					// this port type does not exist
					if( sessionHandle == -201 )
						break;
					// valid handle
					else if( sessionHandle > 0 )
						// success
						return true;
				}
				while( ( DateTime.Now - start ) < timeout );

				// timeout or invalid porttype
				sessionHandle = 0;

				return false;
			}
		}

		/// <summary>  Release a TMEX session.  If already in an 'exclusive' block
		/// then just return.  
		/// </summary>
		private bool ReleaseSession()
		{
			lock( lockObj ) {
				// check if in exclusive block
				if( inExclusive )
					return true;

				// close the session
				TMEXLibrary.TMEndSession( sessionHandle );

				// clear out handle (used to indicate not session)
				sessionHandle = 0;

				return true;
			}
		}
		#endregion
	}

	internal class TMEXLibraryDebug
	{
		class TMEXDebugLog
		{
			public static void Log()
			{
				System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace( 1, true );
				StackFrame frame = st.GetFrame( 0 );
				System.IO.File.AppendAllText(
					Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), "TMEXDebug.log" ), frame.GetMethod().Name + "\t" + frame.GetFileLineNumber() + "\n" );
			}
		}

		/// <summary>feature indexes into adapterSpecFeatures array </summary>
		public const int FEATURE_OVERDRIVE = 0;
		public const int FEATURE_POWER = 1;
		public const int FEATURE_PROGRAM = 2;
		public const int FEATURE_FLEX = 3;
		public const int FEATURE_BREAK = 4;
		/// <summary>Speed settings for TMOneWireCOM </summary>
		public const short TIME_NORMAL = 0;
		public const short TIME_OVERDRV = 1;
		public const short TIME_RELAXED = 2;
		/// <summary>for TMOneWireLevel</summary>
		public const short LEVEL_NORMAL = 0;
		public const short LEVEL_STRONG_PULLUP = 1;
		public const short LEVEL_BREAK = 2;
		public const short LEVEL_PROGRAM = 3;
		public const short PRIMED_NONE = 0;
		public const short PRIMED_BIT = 1;
		public const short PRIMED_BYTE = 2;
		public const short LEVEL_READ = 1;
		public const short LEVEL_SET = 0;
		/// <summary>session options </summary>
		public const int SESSION_INFINITE = 1;
		public const int SESSION_RSRC_RELEASE = 2;

		//---------------------------------------------------------------------------
		// TMEX - Sesssion
		public static int TMExtendedStartSession( int portNum, int portType, int[] sessionOptions )
		{
			return RealTMEXLibrary.TMExtendedStartSession( portNum, portType, sessionOptions );
		}

		public static int TMStartSession( int i1 )
		{
			TMEXDebugLog.Log();
			int res = RealTMEXLibrary.TMStartSession( i1 );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMValidSession( int sessionHandle )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMValidSession( sessionHandle );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMEndSession( int sessionHandle )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMEndSession( sessionHandle );
			TMEXDebugLog.Log();
			return res;
		}

		// TMEX - Network
		public static short TMFirst( int sessionHandle, byte[] stateBuffer )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMFirst( sessionHandle, stateBuffer );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMNext( int sessionHandle, byte[] stateBuffer )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMNext( sessionHandle, stateBuffer );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMAccess( int sessionHandle, byte[] stateBuffer )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMAccess( sessionHandle, stateBuffer );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMStrongAccess( int sessionHandle, byte[] stateBuffer )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMStrongAccess( sessionHandle, stateBuffer );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMStrongAlarmAccess( int sessionHandle, byte[] stateBuffer )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMStrongAlarmAccess( sessionHandle, stateBuffer );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMOverAccess( int sessionHandle, byte[] stateBuffer )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMOverAccess( sessionHandle, stateBuffer );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMRom( int sessionHandle, byte[] stateBuffer, short[] ROM )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMRom( sessionHandle, stateBuffer, ROM );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMSearch( int sessionHandle, byte[] stateBuffer, short doResetFlag, short skipResetOnSearchFlag, short searchCommand )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMSearch( sessionHandle, stateBuffer, doResetFlag, skipResetOnSearchFlag, searchCommand );
			TMEXDebugLog.Log();
			return res;
		}

		//public static native short TMFirstAlarm(long, void *);
		//public static native short TMNextAlarm(long, void *);
		//public static native short TMFamilySearchSetup(long, void *, short);
		//public static native short TMSkipFamily(long, void *);
		//public static native short TMAutoOverDrive(long, void *, short);

		// TMEX - transport
		public static short TMBlockIO( int sessionHandle, byte[] dataBlock, short len )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMBlockIO( sessionHandle, dataBlock, len );
			TMEXDebugLog.Log();
			return res;
		}

		// TMEX - Hardware Specific
		public static short TMSetup( int sessionHandle )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMSetup( sessionHandle );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMTouchReset( int sessionHandle )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMTouchReset( sessionHandle );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMTouchByte( int sessionHandle, short byteValue )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMTouchByte( sessionHandle, byteValue );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMTouchBit( int sessionHandle, short bitValue )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMTouchBit( sessionHandle, bitValue );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMProgramPulse( int sessionHandle )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMProgramPulse( sessionHandle );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMClose( int sessionHandle )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMClose( sessionHandle );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMOneWireCom( int sessionHandle, short command, short argument )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMOneWireCom( sessionHandle, command, argument );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMOneWireLevel( int sessionHandle, short command, short argument, short changeCondition )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMOneWireLevel( sessionHandle, command, argument, changeCondition );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMGetTypeVersion( int portType, System.Text.StringBuilder sbuff )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMGetTypeVersion( portType, sbuff );
			TMEXDebugLog.Log();
			return res;
		}

		public static short Get_Version( System.Text.StringBuilder sbuff )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.Get_Version( sbuff );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMBlockStream( int sessionHandle, byte[] dataBlock, short len )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMBlockStream( sessionHandle, dataBlock, len );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMGetAdapterSpec( int sessionHandle, byte[] adapterSpec )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMGetAdapterSpec( sessionHandle, adapterSpec );
			TMEXDebugLog.Log();
			return res;
		}

		public static short TMReadDefaultPort( short[] portTypeRef, short[] portNumRef )
		{
			TMEXDebugLog.Log();
			short res = RealTMEXLibrary.TMReadDefaultPort( portTypeRef, portNumRef );
			TMEXDebugLog.Log();
			return res;
		}
		//---------------------------------------------------------------------------

		public static bool[] GetFeaturesFromSpecification( byte[] adapterSpec )
		{
			TMEXDebugLog.Log();
			bool[] res = RealTMEXLibrary.GetFeaturesFromSpecification( adapterSpec );
			TMEXDebugLog.Log();
			return res;
		}

		public static System.String GetDescriptionFromSpecification( byte[] adapterSpec )
		{
			TMEXDebugLog.Log();
			string res = RealTMEXLibrary.GetDescriptionFromSpecification( adapterSpec );
			TMEXDebugLog.Log();
			return res;
		}
	}

	internal class RealTMEXLibrary
	{
		/// <summary>feature indexes into adapterSpecFeatures array </summary>
		public const int FEATURE_OVERDRIVE = 0;
		public const int FEATURE_POWER = 1;
		public const int FEATURE_PROGRAM = 2;
		public const int FEATURE_FLEX = 3;
		public const int FEATURE_BREAK = 4;
		/// <summary>Speed settings for TMOneWireCOM </summary>
		public const short TIME_NORMAL = 0;
		public const short TIME_OVERDRV = 1;
		public const short TIME_RELAXED = 2;
		/// <summary>for TMOneWireLevel</summary>
		public const short LEVEL_NORMAL = 0;
		public const short LEVEL_STRONG_PULLUP = 1;
		public const short LEVEL_BREAK = 2;
		public const short LEVEL_PROGRAM = 3;
		public const short PRIMED_NONE = 0;
		public const short PRIMED_BIT = 1;
		public const short PRIMED_BYTE = 2;
		public const short LEVEL_READ = 1;
		public const short LEVEL_SET = 0;
		/// <summary>session options </summary>
		public const int SESSION_INFINITE = 1;
		public const int SESSION_RSRC_RELEASE = 2;


		//---------------------------------------------------------------------------
		// TMEX - Sesssion
		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern int TMExtendedStartSession( int portNum, int portType, int[] sessionOptions );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern int TMStartSession( int i1 );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMValidSession( int sessionHandle );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMEndSession( int sessionHandle );

		// TMEX - Network
		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMFirst( int sessionHandle, byte[] stateBuffer );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMNext( int sessionHandle, byte[] stateBuffer );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMAccess( int sessionHandle, byte[] stateBuffer );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMStrongAccess( int sessionHandle, byte[] stateBuffer );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMStrongAlarmAccess( int sessionHandle, byte[] stateBuffer );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMOverAccess( int sessionHandle, byte[] stateBuffer );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMRom( int sessionHandle, byte[] stateBuffer, short[] ROM );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMSearch( int sessionHandle, byte[] stateBuffer,
		   short doResetFlag, short skipResetOnSearchFlag, short searchCommand );

		//public static native short TMFirstAlarm(long, void *);
		//public static native short TMNextAlarm(long, void *);
		//public static native short TMFamilySearchSetup(long, void *, short);
		//public static native short TMSkipFamily(long, void *);
		//public static native short TMAutoOverDrive(long, void *, short);

		// TMEX - transport
		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMBlockIO( int sessionHandle, byte[] dataBlock, short len );

		// TMEX - Hardware Specific
		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMSetup( int sessionHandle );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMTouchReset( int sessionHandle );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMTouchByte( int sessionHandle, short byteValue );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMTouchBit( int sessionHandle, short bitValue );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMProgramPulse( int sessionHandle );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMClose( int sessionHandle );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMOneWireCom( int sessionHandle, short command, short argument );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMOneWireLevel( int sessionHandle, short command, short argument, short changeCondition );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMGetTypeVersion( int portType, System.Text.StringBuilder sbuff );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short Get_Version( System.Text.StringBuilder sbuff );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMBlockStream( int sessionHandle, byte[] dataBlock, short len );

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMGetAdapterSpec( int sessionHandle, byte[] adapterSpec ); /*byte[319]*/

		[DllImport( "IBFS32.dll"/*, CharSet=CharSet.Auto*/)]
		public static extern short TMReadDefaultPort( short[] portTypeRef, short[] portNumRef );
		//---------------------------------------------------------------------------

		public static bool[] GetFeaturesFromSpecification( byte[] adapterSpec )
		{
			bool[] features = new bool[32];
			for( int i = 0; i < 32; i++ )
				features[i] = ( adapterSpec[i * 2] > 0 ) || ( adapterSpec[i * 2 + 1] > 0 );
			return features;
		}

		public static System.String GetDescriptionFromSpecification( byte[] adapterSpec )
		{
			int i;
			// find null terminator for string
			for( i = 64; i < 319; i++ )
				if( adapterSpec[i] == 0 )
					break;
			return new System.String(
			   System.Text.UTF8Encoding.UTF8.GetChars( adapterSpec ), 64, i - 64 );
		}
	}

}
