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

    /**
     * <P> 1-Wire container for Real-Time-Clock (RTC) iButton, DS1904 and 1-Wire Chip, DS2415.
     * This container encapsulates the functionality of the iButton family
     * type <B>24</B> (hex)</P>
     *
     * <P> This iButton is used as a portable real-time-clock. </P>
     *
     * <H3> Features </H3>
     * <UL>
     *   <LI> Real-Time Clock with fully compatible 1-Wire MicroLAN interface
     *   <LI> Uses the same binary time/date representation as the DS2404
     *        but with 1 second resolution
     *   <LI> Clock accuracy ± 2 minutes per month at 25&#176
     *   <LI> Operating temperature range from -40&#176C to
     *        +70&#176C (iButton), -40&#176C to +85&#176C (1-Wire chip)
     *   <LI> Over 10 years of data retention (iButton form factor)
     *   <LI> Low power, 200 nA typical with oscillator running
     * </UL>
     *
     * <H3> Alternate Names </H3>
     * <UL>
     *   <LI> DS2415
     * </UL>
     *
     * <H3> Clock </H3>
     *
     * <P>The clock methods can be organized into the following categories.  Note that methods
     *    that are implemented for the {@link com.dalsemi.onewire.container.ClockContainer ClockContainer}
     *    interface are marked with (*): </P>
     * <UL>
     *   <LI> <B> Information </B>
     *     <UL>
     *       <LI> {@link #hasClockAlarm() hasClockAlarm} *
     *       <LI> {@link #canDisableClock() canDisableClock} *
     *       <LI> {@link #getClockResolution() getClockResolution} *
     *     </UL>
     *   <LI> <B> Read </B>
     *     <UL>
     *       <LI> <I> Clock </I>
     *         <UL>
     *           <LI> {@link #getClock(byte[]) getClock} *
     *           <LI> {@link #getClockAlarm(byte[]) getClockAlarm} *
     *           <LI> {@link #isClockAlarming(byte[]) isClockAlarming} *
     *           <LI> {@link #isClockAlarmEnabled(byte[]) isClockAlarmEnabled} *
     *           <LI> {@link #isClockRunning(byte[]) isClockRunning} *
     *         </UL>
     *       <LI> <I> Misc </I>
     *         <UL>
     *           <LI> {@link #readDevice() readDevice} *
     *         </UL>
     *     </UL>
     *   <LI> <B> Write </B>
     *     <UL>
     *       <LI> <I> Clock </I>
     *         <UL>
     *           <LI> {@link #setClock(long, byte[]) setClock} *
     *           <LI> {@link #setClockAlarm(long, byte[]) setClockAlarm} *
     *           <LI> {@link #setClockRunEnable(boolean, byte[]) setClockRunEnable} *
     *           <LI> {@link #setClockAlarmEnable(boolean, byte[]) setClockAlarmEnable} *
     *         </UL>
     *       <LI> <I> Misc </I>
     *         <UL>
     *           <LI> {@link #writeDevice(byte[]) writeDevice} *
     *         </UL>
     *     </UL>
     *  </UL>
     *
     * <H3> Usage </H3>
     *
     * <DL>
     * <DD> See the usage examples in
     * {@link com.dalsemi.onewire.container.ClockContainer ClockContainer}
     * for basic clock operations.
     * </DL>
     *
     * <H3> DataSheets </H3>
     * <DL>
     * <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS1904.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS1904.pdf</A>
     * <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS2415.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS2415.pdf</A>
     * </DL>
     *
     * @see com.dalsemi.onewire.container.MemoryBank
     * @see com.dalsemi.onewire.container.PagedMemoryBank
     * @see com.dalsemi.onewire.container.ClockContainer
     *
     * @version    1.00, 26 September 2001
     * @author     CO,BA
     */

    public class OneWireContainer24 : OneWireContainer, ClockContainer
    {

        public static byte GetFamilyCode()
        {
            return 0x24;
        }
        // finals
        protected const int RTC_OFFSET = 1;
        protected const int CONTROL_OFFSET = 0;
        protected const byte READ_CLOCK_COMMAND = (byte)0x66;
        protected const byte WRITE_CLOCK_COMMAND = (byte)0x99;

        //--- Constructors


        /**
         * Create a container with the provided adapter instance
         * and the address of the iButton or 1-Wire device.<p>
         *
         * This is one of the methods to construct a container.  The other is
         * through creating a OneWireContainer with NO parameters.
         *
         * @param  sourceAdapter     adapter instance used to communicate with
         * this iButton
         * @param  newAddress        {@link com.dalsemi.onewire.utils.Address Address}
         *                           of this 1-Wire device
         *
         * @see #OneWireContainer24() OneWireContainer24
         * @see com.dalsemi.onewire.utils.Address utils.Address
         */
        public OneWireContainer24(PortAdapter sourceAdapter, byte[] newAddress)
            : base(sourceAdapter, newAddress)
        {
        }

        //--------
        //-------- Methods
        //--------

        /**
         * Get the Dallas Semiconductor part number of the iButton
         * or 1-Wire Device as a string.  For example 'DS1992'.
         *
         * @return iButton or 1-Wire device name
         */
        public override string GetName()
        {
            return "DS1904";
        }

        /**
         * Get the alternate Dallas Semiconductor part numbers or names.
         * A 'family' of 1-Wire Network devices may have more than one part number
         * depending on packaging.  There can also be nicknames such as
         * 'Crypto iButton'.
         *
         * @return 1-Wire device alternate names
         */
        public override string GetAlternateNames()
        {
            return "DS2415";
        }

        /**
         * Get a short description of the function of this iButton
         * or 1-Wire Device type.
         *
         * @return device description
         */
        public override string GetDescription()
        {
            return "Real time clock implemented as a binary counter " +
                   "that can be used to add functions such as " +
                   "calendar, time and date stamp and logbook to any " +
                   "type of electronic device or embedded application that " +
                   "uses a microcontroller.";
        }

        //--------
        //-------- Clock Feature methods
        //--------

        /**
         * Query to see if the clock has an alarm feature.
         *
         * @return true if the Real-Time clock has an alarm
         *
         * @see #getClockAlarm(byte[])
         * @see #isClockAlarmEnabled(byte[])
         * @see #isClockAlarming(byte[])
         * @see #setClockAlarm(long,byte[])
         * @see #setClockAlarmEnable(boolean,byte[])
         */
        public Boolean HasClockAlarm()
        {
            return false;
        }

        /**
         * Query to see if the clock can be disabled.
         *
         * @return true if the clock can be enabled and disabled
         *
         * @see #isClockRunning(byte[])
         * @see #setClockRunEnable(boolean,byte[])
         */
        public Boolean CanDisableClock()
        {
            return true;
        }

        /**
         * Query to get the clock resolution in milliseconds
         *
         * @return the clock resolution in milliseconds
         */
        public long GetClockResolution()
        {
            return 1000;
        }

        //--------
        //-------- Clock IO Methods
        //--------

        /**
         * Retrieves the five byte state over the 1-Wire bus.  This state array must be passed
         * to the Get/Set methods as well as the WriteDevice method.
         *
         * @return 1-Wire device sensor state
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         */
        public byte[] ReadDevice()
        {
            byte[] state = new byte[5];
            if (adapter.SelectDevice(address))
            {
                // send out the read clock command
                // first write the command to the 1-wire bus
                adapter.PutByte(READ_CLOCK_COMMAND);
                // now grab the five bytes
                adapter.GetBlock(state, 0, 5);
                return state;
            }
            else
                // failed to get a match
                throw new OneWireIOException("Device not found on 1-Wire Network");
        }

        /**
         * Writes the 1-Wire device sensor state that
         * have been changed by 'set' methods.  Only the state registers that
         * changed are updated.  This is done by referencing a field information
         * appended to the state data.
         *
         * @param  state 1-Wire device sensor state
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         */
        public void WriteDevice(byte[] state)
        {
            if (adapter.SelectDevice(address))
            {
                byte[] writeblock = new byte[6];
                writeblock[0] = WRITE_CLOCK_COMMAND;
                Array.Copy(state, 0, writeblock, 1, 5);
                // send the write clock command with the five bytes appended
                adapter.DataBlock(writeblock, 0, 6);

                // double check by reading the clock bytes back
                byte[] readblock = ReadDevice();
                if ((readblock[0] & 0x0C) != (state[0] & 0x0C))
                    throw new OneWireIOException("Failed to write to the clock register page");
                for (int i = 1; i < 5; i++)
                    if (readblock[i] != state[i])
                        throw new OneWireIOException("Failed to write to the clock register page");
            }
            else
                throw new OneWireIOException("Device not found on one-wire network");
        }

        //--------
        //-------- Clock 'get' Methods
        //--------

        /**
         * Extracts the Real-Time clock value in milliseconds.
         *
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return the time represented in this clock in milliseconds since some reference time,
         *         as chosen by the user (ie. 12:00am, Jan 1st 1970)
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #setClock(long,byte[])
         */
        public long GetClock(byte[] state)
        {
            return DalSemi.OneWire.Utils.Convert.ToLong(state, RTC_OFFSET, 4) * 1000;
        }

        /**
         * Extracts the clock alarm value for the Real-Time clock.
         *
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return milliseconds since 1970 that the clock alarm is set to
         *
         * @throws OneWireException if this device does not have clock alarms
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #hasClockAlarm()
         * @see #isClockAlarmEnabled(byte[])
         * @see #isClockAlarming(byte[])
         * @see #setClockAlarm(long,byte[])
         * @see #setClockAlarmEnable(boolean,byte[])
         */
        public long GetClockAlarm(byte[] state)
        {
            throw new OneWireException("This device does not support clock alarms.");
        }

        /**
         * Checks if the clock alarm flag has been set.
         * This will occur when the value of the Real-Time
         * clock equals the value of the clock alarm.
         *
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return true if the Real-Time clock is alarming
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #hasClockAlarm()
         * @see #isClockAlarmEnabled(byte[])
         * @see #getClockAlarm(byte[])
         * @see #setClockAlarm(long,byte[])
         * @see #setClockAlarmEnable(boolean,byte[])
         */
        public Boolean IsClockAlarming(byte[] state)
        {
            return false;
        }

        /**
         * Checks if the clock alarm is enabled.
         *
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return true if clock alarm is enabled
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #hasClockAlarm()
         * @see #isClockAlarming(byte[])
         * @see #getClockAlarm(byte[])
         * @see #setClockAlarm(long,byte[])
         * @see #setClockAlarmEnable(boolean,byte[])
         */
        public Boolean IsClockAlarmEnabled(byte[] state)
        {
            return false;
        }

        /**
         * Checks if the device's oscillator is enabled.  The clock
         * will not increment if the clock oscillator is not enabled.
         *
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return true if the clock is running
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #canDisableClock()
         * @see #setClockRunEnable(boolean,byte[])
         */
        public Boolean IsClockRunning(byte[] state)
        {
            return (Bit.ArrayReadBit(3, CONTROL_OFFSET, state) == 1);
        }

        //--------
        //-------- Clock 'set' Methods
        //--------

        /**
         * Sets the Real-Time clock.
         * The method <code>writeDevice(byte[])</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice(byte[])</code>.
         *
         * @param time new value for the Real-Time clock, in milliseconds
         * since some reference time (ie. 12:00am, January 1st, 1970)
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[])
         * @see #getClock(byte[])
         */
        public void SetClock(long time, byte[] state)
        {
            DalSemi.OneWire.Utils.Convert.ToByteArray((time / 1000L), state, RTC_OFFSET, 4);
        }

        /**
         * Sets the clock alarm.
         * The method <code>writeDevice(byte[])</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice(byte[])</code>.  Also note that
         * not all clock devices have alarms.  Check to see if this device has
         * alarms first by calling the <code>hasClockAlarm()</code> method.
         *
         * @param time - new value for the Real-Time clock alarm, in milliseconds
         * since January 1, 1970
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @throws OneWireException if this device does not have clock alarms
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[])
         * @see #hasClockAlarm()
         * @see #isClockAlarmEnabled(byte[])
         * @see #getClockAlarm(byte[])
         * @see #isClockAlarming(byte[])
         * @see #setClockAlarmEnable(boolean,byte[])
         */
        public void SetClockAlarm(long time, byte[] state)
        {
            throw new OneWireException("This device does not support clock alarms.");
        }

        /**
         * Enables or disables the clock alarm.
         * The method <code>writeDevice(byte[])</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice(byte[])</code>.  Also note that
         * not all clock devices have alarms.  Check to see if this device has
         * alarms first by calling the <code>hasClockAlarm()</code> method.
         *
         * @param alarmEnable true to enable the clock alarm
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @throws OneWireException if this device does not have clock alarms
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[])
         * @see #hasClockAlarm()
         * @see #isClockAlarmEnabled(byte[])
         * @see #getClockAlarm(byte[])
         * @see #setClockAlarm(long,byte[])
         * @see #isClockAlarming(byte[])
         */
        public void SetClockAlarmEnable(Boolean alarmEnable, byte[] state)
        {
            throw new OneWireException("This device does not support clock alarms.");
        }


        /**
         * Enables or disables the oscillator, turning the clock 'on' and 'off'.
         * The method <code>writeDevice(byte[])</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice(byte[])</code>.  Also note that
         * not all clock devices can disable their oscillators.  Check to see if this device can
         * disable its oscillator first by calling the <code>canDisableClock()</code> method.
         *
         * @param runEnable true to enable the clock oscillator
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[])
         * @see #canDisableClock()
         * @see #isClockRunning(byte[])
         */
        public void SetClockRunEnable(Boolean runEnable, byte[] state)
        {
            /* When writing oscillator enable, both bits should have identical data. */
            Bit.ArrayWriteBit(runEnable ? 1
                                        : 0, 3, CONTROL_OFFSET, state);
            Bit.ArrayWriteBit(runEnable ? 1
                                        : 0, 2, CONTROL_OFFSET, state);
        }


    }
}
