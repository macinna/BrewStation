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

namespace DalSemi.OneWire.Container
{
    /// <summary>
    /// 1-Wire container for 1-Wire(MicroLAN) Coupler, DS2409.
    /// This container encapsulates the functionality of the 1-Wire family type 0x1F.
    /// 
    /// Setting the latch on the DS2409 to 'on' connects the channel Main(0) or Auxillary(1)
    /// to the 1-Wire data line.  Note that this is the opposite of the OneWireContainer12 DS2406
    /// and OneWireContainer05 DS2405 which connect thier I/O lines to ground.
    /// 
    /// Low impedance coupler to create large common-ground, multi-level MicroLAN networks
    /// Keeps inactive branches pulled high to 5V
    /// Simplifies network topology analysis by logically decoupling devices on active network segments
    /// Conditional Search for fast event signaling
    /// Auxiliary 1-Wire TM line to connect a memory chip or to be used as digital input
    /// Programmable, general purpose open drain control output
    /// Operating temperature range from -40C to +85C
    /// Compact, low cost 6-pin TSOC surface mount package
    /// </summary>
    public class OneWireContainer1F : OneWireContainer, SwitchContainer
    {
        #region Static Final Variables

        /// <summary>
        /// Offset of BITMAP in array returned from read state.
        /// </summary>
        protected const int BITMAP_OFFSET = 3;


        /// <summary>
        /// Offset of Status in array returned from read state.
        /// </summary>
        protected const int STATUS_OFFSET = 0;

        /// <summary>
        /// Offset of Main channel flag in array returned from read state.
        /// </summary>
        protected const int MAIN_OFFSET = 1;

        /// <summary>
        /// Offset of Main channel flag in array returned from read state.
        /// </summary>
        protected const int AUX_OFFSET = 2;

        /// <summary>
        /// Channel flag to indicate turn off.
        /// </summary>
        protected const int SWITCH_OFF = 0;

        /// <summary>
        /// Channel flag to indicate turn on.
        /// </summary>
        protected const int SWITCH_ON = 1;

        /// <summary>
        /// Channel flag to indicate smart on. 
        /// </summary>
        protected const int SWITCH_SMART = 2;

        /// <summary>
        /// Read Write Status register commmand.
        /// </summary>
        protected const byte READ_WRITE_STATUS_COMMAND = (byte)0x5A;

        /// <summary>
        /// All lines off command.
        /// </summary>
        protected const byte ALL_LINES_OFF_COMMAND = (byte)0x66;

        /// <summary>
        /// Discharge command.
        /// </summary>
        protected const byte DISCHARGE_COMMAND = (byte)0x99;

        /// <summary>
        /// Direct on main command.
        /// </summary>
        protected const byte DIRECT_ON_MAIN_COMMAND = (byte)0xA5;

        /// <summary>
        /// Smart on main command.
        /// </summary>
        protected const byte SMART_ON_MAIN_COMMAND = (byte)0xCC;

        /// <summary>
        /// Smart on aux command.
        /// </summary>
        protected const byte SMART_ON_AUX_COMMAND = (byte)0x33;

        /// <summary>
        /// Main Channel number.
        /// </summary>
        public const int CHANNEL_MAIN = 0;

        /// <summary>
        /// Aux Channel number.
        /// </summary>
        public const int CHANNEL_AUX = 1;

        #endregion //  Static Final Variables

        #region Variables

        /// <summary>
        /// Flag to clear the activity on a write operation
        /// </summary>
        private bool clearActivityOnWrite;

        /// <summary>
        /// Flag to do speed checking
        /// </summary>
        private bool doSpeedEnable = true;

        /// <summary>
        /// Flag to indicated devices detected on branch during smart-on
        /// </summary>
        private bool devicesOnBranch = false;

        #endregion // Variables

        #region Constructors

        /// <summary>
        /// Create a container with the provided adapter instance
        /// and the address of the iButton or 1-Wire device.
        /// This is one of the methods to construct a container. The other is
        /// through creating a OneWireContainer with NO parameters.
        /// </summary>
        /// <param name="sourceAdapter">adapter object required to communicate with this device.</param>
        /// <param name="newAddress">address of this 1-Wire device</param>
        /// <see cref="OneWireContainer()"/>
        /// <see cref="DalSemi.OneWire.Utils.Address"/>
        public OneWireContainer1F( PortAdapter sourceAdapter, byte[] newAddress )
            : base( sourceAdapter, newAddress )
        {
            clearActivityOnWrite = false;
        }

        #endregion // Constructors

        #region Methods

        /// <summary>
        /// Gets the family code.
        /// </summary>
        /// <returns></returns>
        public static byte GetFamilyCode()
        {
            return 0x1F;
        }
        
        /// <summary>
        /// Retrieves the Dallas Semiconductor part number of the 1-Wire device as a string.  For example 'Crypto iButton' or 'DS1992'.
        /// </summary>
        /// <returns>1-Wire device name</returns>
        public override string GetName()
        {
            return "DS2409";
        }

        /// <summary>
        /// Retrieves the alternate Dallas Semiconductor part numbers or names.
        /// A 'family' of 1-Wire Network devices may have more than one part number depending on packaging.
        /// There can also be nicknames such as 'Crypto iButton'.
        /// </summary>
        /// <returns>1-Wire device alternate names</returns>
        public override string GetAlternateNames()
        {
            return "Coupler";
        }

        /// <summary>
        /// Retrieves a short description of the function of the 1-Wire device type.
        /// </summary>
        /// <returns>Device functional description</returns>
        public override string GetDescription()
        {
            return "1-Wire Network Coupler with dual addressable "
                   + "switches and a general purpose open drain control "
                   + "output.  Provides a common ground for all connected"
                   + "multi-level MicroLan networks.  Keeps inactive branches"
                   + "Pulled to 5V.";
        }

        /// <summary>
        /// Directs the container to avoid the calls to doSpeed() in methods that communicate
        /// with the Thermocron. To ensure that all parts can talk to the 1-Wire bus
        /// at their desired speed, each method contains a call to DoSpeed(). However, this is an expensive operation.
        /// If a user manages the bus speed in an application, call this method with doSpeedCheck as false.
        /// The default behavior is to call DoSpeed()
        /// </summary>
        /// <param name="doSpeedCheck">if set to <c>true</c> DoSpeed() is called before every bus access. Set to falst to skip this expensive operation</param>
        /// <see cref="OneWireContainer.DoSpeed()"/>
        public void SetSpeedCheck( bool doSpeedCheck )
        {
            lock( lockObj ) {
                doSpeedEnable = doSpeedCheck;
            }
        }

        #endregion // Methods

        #region Sensor I/O methods

        /// <summary>
        /// Retrieves the 1-Wire device sensor state.  This state is
        /// returned as a byte array.  Pass this byte array to the 'Get'
        /// and 'Set' methods.  If the device state needs to be changed then call
        /// the 'EriteDevice' to finalize the changes.
        /// </summary>
        /// <returns>1-Wire device sensor state</returns>
        /// <exception cref="OneWireIOException">
        ///  On a 1-Wire communication error such as
        ///  reading an incorrect CRC from a 1-Wire device.  This could be
        ///  caused by a physical interruption in the 1-Wire Network due to
        ///  shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
        ///  </exception>
        ///  <exception cref="OneWireException">
        ///  OneWireException on a communication or setup error with the 1-Wire adapter
        ///  </exception>
        public byte[] ReadDevice()
        {
            byte[] ret_buf = new byte[4];

            if( doSpeedEnable )
                DoSpeed();

            // read the status byte
            byte[] tmp_buf = DeviceOperation( READ_WRITE_STATUS_COMMAND,
                                             (byte)0x00FF, 2 );

            // extract the status byte
            ret_buf[0] = tmp_buf[2];

            return ret_buf;
        }

        /// <summary>
        /// Writes the 1-Wire device sensor state that
        /// have been changed by 'set' methods. Only the state registers that
        /// changed are updated.  This is done by referencing a field information
        /// appended to the state data.
        /// </summary>
        /// <param name="state">1-Wire device sensor state</param>
        /// <exception cref="OneWireIOException">
        ///  On a 1-Wire communication error such as
        ///  reading an incorrect CRC from a 1-Wire device.  This could be
        ///  caused by a physical interruption in the 1-Wire Network due to
        ///  shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
        ///  </exception>
        ///  <exception cref="OneWireException">
        ///  OneWireException on a communication or setup error with the 1-Wire adapter
        ///  </exception>
        public void WriteDevice( byte[] state )
        {
            int extra = 0;
            byte command, first_byte;
            byte[] tmp_buf = null;

            if( doSpeedEnable )
                DoSpeed();

            // check for both switches set to on
            if( ( Bit.ArrayReadBit( MAIN_OFFSET, BITMAP_OFFSET, state ) == 1 )
                    && ( Bit.ArrayReadBit( AUX_OFFSET, BITMAP_OFFSET, state ) == 1 ) ) {
                if( ( state[MAIN_OFFSET] != SWITCH_OFF )
                        && ( state[AUX_OFFSET] != SWITCH_OFF ) )
                    throw new OneWireException(
                       "Attempting to set both channels on, only single channel on at a time" );
            }

            // check if need to set control
            if( Bit.ArrayReadBit( STATUS_OFFSET, BITMAP_OFFSET, state ) == 1 ) {

                // create a command based on bit 6/7 of status
                first_byte = 0;

                // mode bit
                if( Bit.ArrayReadBit( 7, STATUS_OFFSET, state ) == 1 )
                    first_byte |= (byte)0x20;

                // Control output
                if( Bit.ArrayReadBit( 6, STATUS_OFFSET, state ) == 1 )
                    first_byte |= (byte)0xC0;

                tmp_buf = DeviceOperation( READ_WRITE_STATUS_COMMAND, first_byte,
                                            2 );
                state[0] = (byte)tmp_buf[2];
            }

            // check for AUX state change
            command = 0;

            if( Bit.ArrayReadBit( AUX_OFFSET, BITMAP_OFFSET, state ) == 1 ) {
                if( ( state[AUX_OFFSET] == SWITCH_ON )
                        || ( state[AUX_OFFSET] == SWITCH_SMART ) ) {
                    command = SMART_ON_AUX_COMMAND;
                    extra = 2;
                }
                else {
                    command = ALL_LINES_OFF_COMMAND;
                    extra = 0;
                }
            }

            // check for MAIN state change
            if( Bit.ArrayReadBit( MAIN_OFFSET, BITMAP_OFFSET, state ) == 1 ) {
                if( state[MAIN_OFFSET] == SWITCH_ON ) {
                    command = DIRECT_ON_MAIN_COMMAND;
                    extra = 0;
                }
                else if( state[MAIN_OFFSET] == SWITCH_SMART ) {
                    command = SMART_ON_MAIN_COMMAND;
                    extra = 2;
                }
                else {
                    command = ALL_LINES_OFF_COMMAND;
                    extra = 0;
                }
            }

            // check if there are events to clear and not about to do clear anyway
            if( ( clearActivityOnWrite ) && ( command != ALL_LINES_OFF_COMMAND ) ) {
                if( ( Bit.ArrayReadBit( 4, STATUS_OFFSET, state ) == 1 )
                        || ( Bit.ArrayReadBit( 5, STATUS_OFFSET, state ) == 1 ) ) {

                    // clear the events
                    DeviceOperation( ALL_LINES_OFF_COMMAND, (byte)0xFF, 0 );

                    // set the channels back to the correct state
                    if( command == 0 ) {
                        if( Bit.ArrayReadBit( 0, STATUS_OFFSET, state ) == 0 )
                            command = SMART_ON_MAIN_COMMAND;
                        else if( Bit.ArrayReadBit( 2, STATUS_OFFSET, state ) == 0 )
                            command = SMART_ON_AUX_COMMAND;

                        extra = 2;
                    }
                }
            }

            // check if there is a command to send
            if( command != 0 )
                tmp_buf = DeviceOperation( command, (byte)0xFF, extra );

            // if doing a SMART_ON, then look at result data for presence
            if( ( command == SMART_ON_MAIN_COMMAND ) ||
                ( command == SMART_ON_AUX_COMMAND ) ) {
                // devices on branch indicated if 3rd byte is 0
                devicesOnBranch = ( tmp_buf[2] == 0 );
            }
            else
                devicesOnBranch = false;

            // clear clear activity on write
            clearActivityOnWrite = false;

            // clear the bitmap
            state[BITMAP_OFFSET] = 0;
        }


        /// <summary>
        /// Force a power-on reset for parasitically powered 1-Wire 
        /// devices connected to the main or auxiliary output of the DS2409.  
        /// IMPORTANT: the duration of the discharge time should be 100ms minimum.
        /// </summary>
        /// <param name="time">The number of milliseconds the lines are to be discharged for (minimum 100)</param>
        /// <exception cref="OneWireIOException">
        ///  On a 1-Wire communication error such as
        ///  reading an incorrect CRC from a 1-Wire device.  This could be
        ///  caused by a physical interruption in the 1-Wire Network due to
        ///  shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
        ///  </exception>
        ///  <exception cref="OneWireException">
        ///  OneWireException on a communication or setup error with the 1-Wire adapter
        ///  </exception>
        public void DischargeLines( int time )
        {

            // Error checking
            if( time < 100 )
                time = 100;

            if( doSpeedEnable )
                DoSpeed();

            // discharge the lines
            DeviceOperation( DISCHARGE_COMMAND, (byte)0xFF, 0 );

            // Wait for desired time and return.
            System.Threading.Thread.Sleep( time );

            // Clear the discharge
            DeviceOperation( READ_WRITE_STATUS_COMMAND, (byte)0x00FF, 2 );
        }

        #endregion // Sensor I/O methods

        #region Switch Feature methods

        /// <summary>
        /// Checks to see if the channels of this switch are 'high side'
        /// switches.  This indicates that when 'on' or <c>true</c>, the switch output is
        /// connect to the 1-Wire data.  If this method returns  <c>false</c>
        /// then when the switch is 'on' or <c>true</c>, when the switch is connected to ground.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if the switch is a 'high side' switch; otherwise, <c>false</c>.
        /// </returns>
        /// <see cref="GetLatchState(int,byte[])"/>
        public bool IsHighSideSwitch()
        {
            return true;
        }

        /// <summary>
        /// Checks to see if the channels of this switch support
        /// activity sensing.  If this method returns <c>true</c> then the method <c>GetSensedActivity(int,byte[])</c> can be used.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if channels support activity sensing; otherwise, <c>false</c>.
        /// </returns>
        public bool HasActivitySensing()
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
        public bool HasSmartOn()
        {
            return true;
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
            return true;
        }

        #endregion // Switch Feature methods
        
        #region Switch 'get' Methods

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
        public bool GetLevel( int channel, byte[] state )
        {
            return ( Bit.ArrayReadBit( 1 + channel * 2, STATUS_OFFSET, state ) == 1 );
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
        public bool GetLatchState( int channel, byte[] state )
        {
            return ( Bit.ArrayReadBit( channel * 2, STATUS_OFFSET, state ) == 0 );
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
            return ( Bit.ArrayReadBit( 4 + channel, STATUS_OFFSET, state ) == 1 );
        }

        #endregion // Switch 'get' Methods

        #region DS2409 Specific Switch 'get' Methods

        /// <summary>
        /// Checks if the control I/O pin mode is automatic (see DS2409 data sheet).
        /// </summary>
        /// <param name="state">current state of the device returned from ReadDevice()</param>
        /// <returns>
        /// 	<c>true</c> if control mode is automatic.
        /// </returns>
        public Boolean IsModeAuto( byte[] state )
        {
            return ( Bit.ArrayReadBit( 7, STATUS_OFFSET, state ) == 0 );
        }


        /// <summary>
        /// Checks the channel association of the control pin.
        /// This value only makes sense if the control mode is automatic (see IsModeAuto)
        /// </summary>
        /// <param name="state">The channel that is associated with the control pin.</param>
        /// <returns></returns>
        public int GetControlChannelAssociation( byte[] state )
        {
            return Bit.ArrayReadBit( 6, STATUS_OFFSET, state );
        }


        /// <summary>
        /// Checks the control data value. This value only makes sense if the control mode is manual (see IsModeAuto).
        /// 0 = output transistor off
        /// 1 = output transistor on
        /// </summary>
        /// <param name="state">Current state of the device returned from ReadDevice()</param>
        /// <returns>The control output transistor state</returns>
        public int GetControlData( byte[] state )
        {
            return Bit.ArrayReadBit( 6, STATUS_OFFSET, state );
        }


        /// <summary>
        /// Gets flag that indicates if a device was present when doing the 
        /// last smart on.  Note that this flag is only valid if the DS2409 
        /// flag was cleared with an ALL_LINES_OFF command and the last writeDevice 
        /// performed a 'smart-on' on one of the channels.
        /// </summary>
        /// <returns>true, if device detected on branch</returns>
        public Boolean GetLastSmartOnDeviceDetect()
        {
            return devicesOnBranch;
        }

        #endregion // DS2409 Specific Switch 'get' Methods

        #region Switch 'set' Methods

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

            // set the state flag
            if( latchState )
                state[channel + 1] = (byte)( ( doSmart ) ? SWITCH_SMART
                                                          : SWITCH_ON );
            else
                state[channel + 1] = (byte)SWITCH_OFF;

            // indicate in bitmap the the state has changed
            Bit.ArrayWriteBit( 1, channel + 1, BITMAP_OFFSET, state );
        }

        /// <summary>
        /// Clears the activity latches the next time possible.  For
        /// example, on a DS2406/07, this happens the next time the
        /// status is read with <c>ReadDevice()</c>.
        /// The activity latches will only be cleared once.  With the
        /// DS2406/07, this means that only the first call to <c>ReadDevice()</c>
        /// will clear the activity latches.  Subsequent calls to <c>ReadDevice()</c>
        /// will leave the activity latch states intact, unless this method has been invoked
        /// since the last call to ReadDevice()
        /// </summary>
        /// <exception cref="OneWireException">If this device does not support activity sensing</exception>
        public void ClearActivity()
        {
            clearActivityOnWrite = true;
        }

        #endregion // Switch 'set' Methods

        #region DS2409 Specific Switch 'set' Methods

        /// <summary>
        /// Sets the control pin mode. 
        /// The method <code>writeDevice(byte[])</code> must be called to finalize 
        /// changes to the device.  Note that multiple 'set' methods can 
        /// be called before one call to <code>writeDevice(byte[])</code>. 
        /// </summary>
        /// <param name="makeAuto">if set to <c>true</c> use auto mode.</param>
        /// <param name="state">Current state of the device returned from <c>ReadDevice()</c></param>
        public void SetModeAuto( Boolean makeAuto, byte[] state )
        {
            // set the bit
            Bit.ArrayWriteBit( ( makeAuto ? 0 : 1 ), 7, STATUS_OFFSET, state );

            // indicate in bitmap the the state has changed
            Bit.ArrayWriteBit( 1, STATUS_OFFSET, BITMAP_OFFSET, state );
        }


        /// <summary>
        /// Sets the control pin channel association.  This only makes sense 
        /// if the contol pin is in automatic mode. 
        /// The method <code>writeDevice(byte[])</code> must be called to finalize 
        /// changes to the device.  Note that multiple 'set' methods can 
        /// be called before one call to WriteDevice(byte[]). 
        /// </summary>
        /// <param name="channel">The channel to associate with control pin.</param>
        /// <param name="state">Current state of the device returned from ReadDevice()</param>
        /// <exception cref="OneWireException">When trying to set channel association in manual mode</exception>
        public void SetControlChannelAssociation( int channel, byte[] state )
        {

            // check for invalid mode
            if( !IsModeAuto( state ) )
                throw new OneWireException(
                   "Trying to set channel association in manual mode" );

            // set the bit
            Bit.ArrayWriteBit( channel, 6, STATUS_OFFSET, state );

            // indicate in bitmap the the state has changed
            Bit.ArrayWriteBit( 1, STATUS_OFFSET, BITMAP_OFFSET, state );
        }


        /// <summary>
        /// Sets the control pin data to a value. Note this 
        /// method only works if the control pin is in manual mode. 
        /// The method <code>writeDevice(byte[])</code> must be called to finalize 
        /// changes to the device.  Note that multiple 'set' methods can 
        /// be called before one call to WriteDevice(byte[])
        /// </summary>
        /// <param name="data">true for on and falst for off</param>
        /// <param name="state">The current state returned from ReadDevice()</param>
        /// /// <exception cref="OneWireException">When trying to set channel association in manual mode</exception>
        public void SetControlData( Boolean data, byte[] state )
        {
            // check for invalid mode
            if( IsModeAuto( state ) )
                throw new OneWireException(
                   "Trying to set control data when control is in automatic mode" );

            // set the bit
            Bit.ArrayWriteBit( ( data ? 1
                                    : 0 ), 6, STATUS_OFFSET, state );

            // indicate in bitmap the the state has changed
            Bit.ArrayWriteBit( 1, STATUS_OFFSET, BITMAP_OFFSET, state );
        }

        #endregion // DS2409 Specific Switch 'set' Methods

        #region Private methods
        /// <summary>
        /// Do a DS2409 specidific operation.
        /// </summary>
        /// <param name="command">The command code to send</param>
        /// <param name="sendByte">Data byte to send</param>
        /// <param name="extra">The extra number of bytes to send</param>
        /// <returns>Block of the complete resulting transaction</returns>
        /// <exception cref="OneWireIOException">On a 1-Wire communication error such as
        /// reading an incorrect CRC from a 1-Wire device.  This could be
        /// caused by a physical interruption in the 1-Wire Network due to
        /// shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
        /// </exception>
        /// <exception cref="OneWireException">On a communication or setup error with the 1-Wire adapter</exception>
        private byte[] DeviceOperation( byte command, byte sendByte, int extra )
        {
            OneWireIOException exc = null;
            for( int attemptCounter = 2; attemptCounter > 0; attemptCounter-- ) {
                // Variables.
                byte[] raw_buf = new byte[extra + 2];

                // build block.
                raw_buf[0] = (byte)command;
                raw_buf[1] = (byte)sendByte;

                for( int i = 2; i < raw_buf.Length; i++ )
                    raw_buf[i] = (byte)0xFF;

                // Select the device.
                if( adapter.SelectDevice( address ) ) {

                    // send the block
                    adapter.DataBlock( raw_buf, 0, raw_buf.Length );

                    // verify
                    if( command == READ_WRITE_STATUS_COMMAND ) {
                        if( (byte)raw_buf[raw_buf.Length - 1]
                                != (byte)raw_buf[raw_buf.Length - 2] ) {
                            if( exc == null )
                                exc = new OneWireIOException(
                                      "OneWireContainer1F verify on command incorrect" );
                            continue;
                        }
                    }
                    else {
                        if( (byte)raw_buf[raw_buf.Length - 1] != (byte)command ) {
                            if( exc == null )
                                exc = new OneWireIOException(
                                      "OneWireContainer1F verify on command incorrect" );
                            continue;
                        }
                    }

                    return raw_buf;
                }
                else
                    throw new OneWireIOException(
                       "OneWireContainer1F failure - Device not found." );
            }
            // get here after a few attempts
            throw exc;
        }

        #endregion // Private methods
    }
}
