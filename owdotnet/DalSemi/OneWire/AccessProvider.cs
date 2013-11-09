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
using DalSemi.OneWire.Adapter;
using System.IO;

namespace DalSemi.OneWire
{
    public class AccessProvider
    {
        /// <summary>
        /// Gets the specified 1-Wire property. 
        /// NOTE: This function differs from the original Java implementation in that it does not
        /// return any 'smart values' except for "onewire.adapter.default" and "onewire.port.default".
        /// Also, it only looks in the file OneWire.properties in the same folder as the executing assembly.
        /// Properties specified as "key=", i.e. without any value will return an empty string
        /// </summary>
        /// <param name="propName">name of the property to read</param>
        /// <returns>string representing the property value or null if not found</returns>
        public static string GetProperty( string propName )
        {
            // permal: Modifying this code to be capable of reading .ini files would take little effort if that is desired, 
            // all that needs to be done is to first search for the [section] to read from and then parse the lines
            // until the next [section] is found.
            // Also, reading the entire file into memory isn't the best way, especially if running under CF.Net

            string retValue = null;

            // Do the best we can to handle invalid input
            if( propName == null ) {
                return retValue;
            }
            propName = propName.Trim();
            if( propName.Length == 0 ) {
                return retValue;
            }


            // Read the property from the file in the path below
            string filePath = Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );
            filePath = Path.Combine( filePath, "OneWire.properties" );

            try {
                if( File.Exists( filePath ) ) {
                    string[] lines = File.ReadAllLines( filePath );
                    char[] tokens = new char[] { '=' };
                    foreach( string line in lines ) {
                        // Split the line to get key and value pairs
                        string[] parts = line.Split( tokens, 2 );
                        if( parts.Length == 2 ) {
                            if( parts[0] == propName ) {
                                // We've found the property we want
                                retValue = parts[1];
                                break;
                            }
                        }
                    }
                }
            }
            catch( Exception ex ) {
                Debug.DebugStr( "GetProperty exception", ex );
            }

            // if defaults still not found then check DotNet default
            if( retValue == null ) {
                try {
                    if( propName.Equals( "onewire.adapter.default" ) )
                        retValue = AccessProvider.DefaultAdapter.AdapterName;
                    else if( propName.Equals( "onewire.port.default" ) )
                        retValue = AccessProvider.DefaultAdapter.PortName;

                    // if did not get real string then null out
                    if( retValue != null ) {
                        if( retValue.Trim().Length == 0 )
                            retValue = null;
                    }
                }
                catch {
                    // DRAIN
                }

            }

            return retValue != null ? retValue.Trim() : null;
        }

        /// <summary>
        /// Gets the instance of the specified adapter type, or throws
        /// an AdapterException if the type wasn't found 
        /// </summary>
        /// <param name="adapterName">Name of the adapter</param>
        /// <returns>An instance of the specified adapter</returns>
        /// <exception cref="AdapterException"/>
        public static PortAdapter GetAdapter( string adapterName )
        {
            if( adapterName.Equals( "DS9097U" ) ) {
				throw new AdapterException( "The pure serial adapter class is not ready for general usage!" );
				//return new SerialAdapter();
            }
            else if( adapterName.Equals( "{DS9097U}" ) ) {
                return new TMEXLibAdapter( TMEXPortType.SerialPort );
            }
            else if( adapterName.Equals( "{DS9490}" ) ) {
                return new TMEXLibAdapter( TMEXPortType.USBPort );
            }
            else if( adapterName.Equals( "{DS9097}" ) ) {
                return new TMEXLibAdapter( TMEXPortType.PassiveSerialPort );
            }
            else if( adapterName.Equals( "{DS1410E}" ) ) {
                return new TMEXLibAdapter( TMEXPortType.ParallelPort );
            }
            else if( adapterName.Equals( "HOMEBREW" ) ) {
                return new OWN.CustomAdapter.HomebrewSerialAdapter();
            }
            else {
                throw new AdapterException( "Bad adapter name: " + adapterName );
            }
        }

        /// <summary>
        /// Gets the in instance of the specified adapter type and tries to open the given port,
        /// or throws an AdapterException if an error occurs
        /// </summary>
        /// <param name="adapterName">Name of the adapter</param>
        /// <param name="portname">The portname, such as COM1 or USB1</param>
        /// <returns>An instance of the specified adapter</returns>
        /// <exception cref="AdapterException">When the adapter named doesn't exist, or if unable to open port</exception>
        public static PortAdapter GetAdapter( string adapterName, string portname )
        {
            PortAdapter adapter = GetAdapter( adapterName );
            if( !adapter.OpenPort( portname ) ) {
                throw new AdapterException( "Failed to open port: " + adapterName + ", " + portname );
            }
            return adapter;
        }


        /// <summary>
        /// Finds, opens, and verifies the default adapter and
        /// port.  See GetProperty() for details on where it looks for the settings
        /// </summary>
        /// <exception cref="AdapterException"/>
        public static PortAdapter DefaultAdapter
        {
            get
            {
                return GetAdapter( TMEXLibAdapter.DefaultAdapterName, TMEXLibAdapter.DefaultPortName );
            }
        }

        /// <summary>
        /// Gets a list of all available adapters. If an error occurs during the discovery,
        /// this will not be indicated to the caller.
        /// </summary>
        /// <returns>An AdapterList containing all available adapters.</returns>
        public static AdapterList GetAllAdapters()
        {
            AdapterList al = new AdapterList();
            
			// Don't include the pure the Serial adapter until the class has
			// received a thorough workover.
			//al.Add( new SerialAdapter() );

            try { al.Add( new TMEXLibAdapter( TMEXPortType.SerialPort ) ); }
#if DEBUG
            catch( Exception ex ) { Debug.DebugStr( "Error adding SerialPort adapter", ex ); }
#else // DEBUG
            catch {}    
#endif // ! DEBUG

            try { al.Add( new TMEXLibAdapter( TMEXPortType.PassiveSerialPort ) ); }
#if DEBUG
            catch( Exception ex ) { Debug.DebugStr( "Error adding PassiveSerialPort adapter", ex ); }
#else // DEBUG
            catch {}    
#endif // ! DEBUG
            try { al.Add( new TMEXLibAdapter( TMEXPortType.ParallelPort ) ); }
#if DEBUG
            catch( Exception ex ) { Debug.DebugStr( "Error adding ParallelPort adapter", ex ); }
#else // DEBUG
            catch {}    
#endif // ! DEBUG
            try { al.Add( new TMEXLibAdapter( TMEXPortType.USBPort ) ); }
#if DEBUG
            catch( Exception ex ) { Debug.DebugStr( "Error adding USBPort adapter", ex ); }
#else // DEBUG
            catch {}    
#endif // ! DEBUG
            return al;
        }

        /// <summary>
        /// Searches for the presence of an adapter on each port and returns a
        /// list of adapters which port could be opened.
        /// Note: These adapters must be closed unless they are used, or they will
        /// occupy the respective ports. This can be done using the CloseAdapters( AdapterList ) method.
        /// </summary>
        /// <returns></returns>
        public static AdapterList GetAndOpenAllAdapters()
        {
            AdapterList list = new AdapterList();
            try {
                AdapterList allAdapters = GetAllAdapters();

                // Try the operation on all adapter types.
                foreach( PortAdapter pa in allAdapters ) {
                    // Verify the presence of the current type of adapter on the current port
                    foreach( string port in pa.PortNames ) {
                        // Create a new instance for each port
                        PortAdapter newInstance;
                        if( pa is SerialAdapter ) {
                            newInstance = (PortAdapter)Activator.CreateInstance( pa.GetType() );
                        }
                        else {
                            // TMEXLibAdapter needs a type argument passed on creation
                            newInstance = (PortAdapter)Activator.CreateInstance( pa.GetType(), new object[] { ( (TMEXLibAdapter)pa ).PortType } );
                        }

                        try {
                            if( newInstance.OpenPort( port ) && newInstance.AdapterDetected ) {
                                // Adapter found, add to return list
                                list.Add( newInstance );
                            }
                            else {
                                // No adapter found on the current port
                                newInstance.Dispose();
                            }
                        }
                        catch {
                            // Consider any exception as and indication that the adapter is
                            // not present on the current port
                            newInstance.Dispose();
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        /// <summary>
        /// Closes the adapters in the given adapter list.
        /// </summary>
        /// <param name="list">The list.</param>
        public static void CloseAdapters( AdapterList list )
        {
            foreach( PortAdapter pa in list ) {
                try { pa.Dispose(); }
                catch { }
            }
        }
    }

    public class AdapterList : System.Collections.Generic.List<PortAdapter>
    {
    }

}
