// TODO: check some things like DateTime / Calendar etc.

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
using DalSemi.Utils; // CRC16

namespace DalSemi.OneWire.Container
{

    /**
     * <P> 1-Wire&#174 container for a Temperature and Humidity/A-D Logging iButton, DS1922.
     * This container encapsulates the functionality of the 1-Wire family type <B>22</B> (hex).
     * </P>
     *
     * <H3> Features </H3>
     * <UL>
     *   <LI> Logs up to 8192 consecutive temperature/humidity/A-D measurements in
     *        nonvolatile, read-only memory
     *   <LI> Real-Time clock
     *   <LI> Programmable high and low temperature alarms
     *   <LI> Programmable high and low humidity/A-D alarms
     *   <LI> Automatically 'wakes up' and logs temperature at user-programmable intervals
     *   <LI> 4096 bits of general-purpose read/write nonvolatile memory
     *   <LI> 256-bit scratchpad ensures integrity of data transfer
     *   <LI> On-chip 16-bit CRC generator to verify read operations
     * </UL>
     *
     * <H3> Memory </H3>
     *
     * <P> The memory can be accessed through the objects that are returned
     * from the {@link #getMemoryBanks() getMemoryBanks} method. </P>
     *
     * The following is a list of the MemoryBank instances that are returned:
     *
     * <UL>
     *   <LI> <B> Scratchpad with CRC and Password support </B>
     *      <UL>
     *         <LI> <I> Implements </I> {@link com.dalsemi.onewire.container.MemoryBank MemoryBank},
     *                  {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     *         <LI> <I> Size </I> 32 starting at physical address 0
     *         <LI> <I> Features</I> Read/Write not-general-purpose volatile
     *         <LI> <I> Pages</I> 1 page of length 32 bytes
     *         <LI> <I> Page Features </I> page-device-CRC
     *         <li> <i> Extra information for each page</i>  Target address, offset, length 3
     *         <LI> <i> Supports Copy Scratchpad With Password command </I>
     *      </UL>
     *   <LI> <B> Main Memory </B>
     *      <UL>
     *         <LI> <I> Implements </I> {@link com.dalsemi.onewire.container.MemoryBank MemoryBank},
     *                  {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     *         <LI> <I> Size </I> 512 starting at physical address 0
     *         <LI> <I> Features</I> Read/Write general-purpose non-volatile
     *         <LI> <I> Pages</I> 16 pages of length 32 bytes giving 29 bytes Packet data payload
     *         <LI> <I> Page Features </I> page-device-CRC
     *         <LI> <I> Read-Only and Read/Write password </I> if enabled, passwords are required for
     *                  reading from and writing to the device.
     *      </UL>
     *   <LI> <B> Register control </B>
     *      <UL>
     *         <LI> <I> Implements </I> {@link com.dalsemi.onewire.container.MemoryBank MemoryBank},
     *                  {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     *         <LI> <I> Size </I> 64 starting at physical address 512
     *         <LI> <I> Features</I> Read/Write not-general-purpose non-volatile
     *         <LI> <I> Pages</I> 2 pages of length 32 bytes
     *         <LI> <I> Page Features </I> page-device-CRC
     *         <LI> <I> Read-Only and Read/Write password </I> if enabled, passwords are required for
     *                  reading from and writing to the device.
     *      </UL>
     *   <LI> <B> Temperature/Humidity/A-D log </B>
     *      <UL>
     *         <LI> <I> Implements </I> {@link com.dalsemi.onewire.container.MemoryBank MemoryBank},
     *                  {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     *         <LI> <I> Size </I> 8192 starting at physical address 4096
     *         <LI> <I> Features</I> Read-only not-general-purpose non-volatile
     *         <LI> <I> Pages</I> 256 pages of length 32 bytes
     *         <LI> <I> Page Features </I> page-device-CRC
     *         <LI> <I> Read-Only and Read/Write password </I> if enabled, passwords are required for
     *                  reading from and writing to the device.
     *      </UL>
     * </UL>
     *
     * <H3> Usage </H3>
     *
     * <p>The code below starts a mission with the following characteristics:
     * <ul>
     *     <li>Rollover flag enabled.</li>
     *     <li>Sets both channels (temperature and humidity) to low resolution</li>
     *     <li>High temperature alarm of 28.0&#176 and a low temperature alarm of 23.0&#176 C.</li>
     *     <li>High humidity alarm of 70%RH and a low temperature alarm of 20%RH.</li>
     *     <li>Sets the Real-Time Clock to the host system's clock.</li>
     *     <li>The mission will start in 2 minutes.</li>
     *     <li>A sample rate of 1.5 minutes.</li>
     * </ul></p>
     * <pre><code>
     *       // "ID" is a byte array of size 8 with an address of a part we
     *       // have already found with family code 22 hex
     *       // "access" is a DSPortAdapter
     *       OneWireContainer41 ds1922 = (OneWireContainer41)access.getDeviceContainer(ID);
     *       ds1922.setupContainer(access,ID);
     *       //  stop the currently running mission, if there is one
     *       ds1922.stopMission();
     *       //  clear the previous mission results
     *       ds1922.clearMemory();
     *       //  set the high temperature alarm to 28 C
     *       ds1922.setMissionAlarm(ds1922.TEMPERATURE_CHANNEL, ds1922.ALARM_HIGH, 28);
     *       ds1922.setMissionAlarmEnable(ds1922.TEMPERATURE_CHANNEL,
     *          ds1922.ALARM_HIGH, true);
     *       //  set the low temperature alarm to 23 C
     *       ds1922.setMissionAlarm(ds1922.TEMPERATURE_CHANNEL, ds1922.ALARM_LOW, 23);
     *       ds1922.setMissionAlarmEnable(ds1922.TEMPERATURE_CHANNEL,
     *          ds1922.ALARM_LOW, true);
     *       //  set the high humidity alarm to 70%RH
     *       ds1922.setMissionAlarm(ds1922.DATA_CHANNEL, ds1922.ALARM_HIGH, 70);
     *       ds1922.setMissionAlarmEnable(ds1922.DATA_CHANNEL,
     *          ds1922.ALARM_HIGH, true);
     *       //  set the low humidity alarm to 20%RH
     *       ds1922.setMissionAlarm(ds1922.DATA_CHANNEL, ds1922.ALARM_LOW, 20);
     *       ds1922.setMissionAlarmEnable(ds1922.DATA_CHANNEL,
     *          ds1922.ALARM_LOW, true);
     *       // set both channels to low resolution.
     *       ds1922.setMissionResolution(ds1922.TEMPERATURE_CHANNEL,
     *          ds1922.getMissionResolutions()[0]);
     *       ds1922.setMissionResolution(ds1922.DATA_CHANNEL,
     *          ds1922.getMissionResolutions()[0]);
     *       // enable both channels
     *       boolean[] enableChannel = new boolean[ds1922.getNumberMissionChannels()];
     *       enableChannel[ds1922.TEMPERATURE_CHANNEL] = true;
     *       enableChannel[ds1922.DATA_CHANNEL] = true;
     *       //  now start the mission with a sample rate of 1 minute
     *       ds1922.startMission(90, 2, true, true, enableChannel);
     * </code></pre>
     * <p>The following code processes the mission log:</p>
     * <code><pre>
     *       System.out.println("Temperature Readings");
     *       if(ds1922.getMissionChannelEnable(owc.TEMPERATURE_CHANNEL))
     *       {
     *          int dataCount =
     *             ds1922.getMissionSampleCount(ds1922.TEMPERATURE_CHANNEL);
     *          System.out.println("SampleCount = " + dataCount);
     *          for(int i=0; i&lt;dataCount; i++)
     *          {
     *             System.out.println(
     *                ds1922.getMissionSample(ds1922.TEMPERATURE_CHANNEL, i));
     *          }
     *       }
     *       System.out.println("Humidity Readings");
     *       if(ds1922.getMissionChannelEnable(owc.DATA_CHANNEL))
     *       {
     *          int dataCount =
     *             ds1922.getMissionSampleCount(ds1922.DATA_CHANNEL);
     *          System.out.println("SampleCount = " + dataCount);
     *          for(int i=0; i&lt;dataCount; i++)
     *          {
     *             System.out.println(
     *                ds1922.getMissionSample(ds1922.DATA_CHANNEL, i));
     *          }
     *       }
     * </pre></code>
     *
     * <p>Also see the usage examples in the {@link com.dalsemi.onewire.container.TemperatureContainer TemperatureContainer}
     * and {@link com.dalsemi.onewire.container.ClockContainer ClockContainer}
     * and {@link com.dalsemi.onewire.container.ADContainer ADContainer}
     * interfaces.</p>
     *
     * For examples regarding memory operations,
     * <uL>
     * <li> See the usage example in
     * {@link com.dalsemi.onewire.container.OneWireContainer OneWireContainer}
     * to enumerate the MemoryBanks.
     * <li> See the usage examples in
     * {@link com.dalsemi.onewire.container.MemoryBank MemoryBank} and
     * {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     * for bank specific operations.
     * </uL>
     *
     * <H3> DataSheet </H3>
     * <P>DataSheet link is unavailable at time of publication.  Please visit the website
     * and Search for DS1922 or DS2422 to find the current datasheet.
     * <DL>
     * <DD><A HREF="http://www.maxim-ic.com/">Maxim Website</A>
     * </DL>
     *
     * @see com.dalsemi.onewire.container.OneWireSensor
     * @see com.dalsemi.onewire.container.SwitchContainer
     * @see com.dalsemi.onewire.container.TemperatureContainer
     * @see com.dalsemi.onewire.container.ADContainer
     * @see com.dalsemi.onewire.container.MissionContainer
     * @see com.dalsemi.onewire.container.PasswordContainer
     *
     * @version    1.00, 1 June 2003
     * @author     shughes
     *
     */

    public class OneWireContainer41 : OneWireContainer, PasswordContainer, MissionContainer,
              ClockContainer, TemperatureContainer,
              IADContainer, IHumidityContainer
    {

        public static byte GetFamilyCode()
        {
            return 0x41;
        }

        // when reading a page, the memory bank may throw a crc exception if the device
        // is sampling or starts sampling during the read.  This value sets how many
        // times the device retries before passing the exception on to the application.
        private const int MAX_READ_RETRY_CNT = 48;

        // the length of the Read-Only and Read/Write password registers
        private const int PASSWORD_LENGTH = 8;

        // indicates whether or not the device configuration has been read
        // and all the ranges for the part have been set.
        private Boolean isContainerVariablesSet = false;

        // memory bank for scratchpad
        private MemoryBankScratchCRCPW scratch = null;
        // memory bank for general-purpose user data
        private MemoryBankNVCRCPW userDataMemory = null;
        // memory bank for control register
        private MemoryBankNVCRCPW register = null;
        // memory bank for mission log
        private MemoryBankNVCRCPW log = null;

        // Maxim/Dallas Semiconductor Part number
        private string partNumber = null;

        // Temperature range low temperaturein degrees Celsius
        private double temperatureRangeLow = -40.0;

        // Temperature range width in degrees Celsius
        private double temperatureRangeWidth = 125.0;

        // Temperature resolution in degrees Celsius
        //private double temperatureResolution = 0.5;

        // A-D Reference voltage
        private double adReferenceVoltage = 5.02d;
        // Number of valid bits in A-D Result
        private int adDeviceBits = 10;
        // Force mission results to return value as A-D, not humidity
        private Boolean adForceResults = false;

        // should we update the Real time clock?
        private Boolean updatertc = false;

        // should we check the speed
        private Boolean doSpeedEnable = true;

        /** The current password for readingfrom this device.*/
        private byte[] readPassword = new byte[8];
        private Boolean readPasswordSet = false;
        private Boolean readOnlyPasswordEnabled = false;

        /** The current password for reading/writing from/to this device. */
        private byte[] readWritePassword = new byte[8];
        private Boolean readWritePasswordSet = false;
        private Boolean readWritePasswordEnabled = false;

        /** indicates whether or not the results of a mission are successfully loaded */
        private Boolean isMissionLoaded = false;
        /** holds the missionRegister, which details the status of the current mission */
        private byte[] missionRegister = null;
        /** The mission logs */
        private byte[] dataLog = null, temperatureLog = null;
        /** Number of bytes used to store temperature values (0, 1, or 2) */
        private int temperatureBytes = 0;
        /** Number of bytes used to stroe data valuas (0, 1, or 2) */
        private int dataBytes = 0;
        /** indicates whether or not the log has rolled over */
        private Boolean rolledOver = false;
        /** start time offset for the first sample, if rollover occurred */
        private int timeOffset = -1;
        /** the time (unix time) when mission started */
        private long missionTimeStamp = -1;
        /** The rate at which samples are taken, and the number of samples */
        private int sampleRate = -1, sampleCount = -1;
        /** total number of samples, including rollover */
        private int sampleCountTotal;

        // indicates whether or not to use calibration for the humidity values
        private Boolean useHumdCalibrationRegisters = false;
        // reference humidities that the calibration was calculated over
        private double Href1 = 20, Href2 = 60, Href3 = 90;
        // the average value for each reference point
        private double Hread1 = 0, Hread2 = 0, Hread3 = 0;
        // the average error for each reference point
        private double Herror1 = 0, Herror2 = 0, Herror3 = 0;
        // the coefficients for calibration
        private double humdCoeffA, humdCoeffB, humdCoeffC;

        // indicates whether or not to use calibration for the temperature values
        private Boolean useTempCalibrationRegisters = false;
        // reference temperatures that the calibration was calculated over
        private double Tref1 = 0, Tref2 = 0, Tref3 = 0;
        // the average value for each reference point
        private double Tread1 = 0, Tread2 = 0, Tread3 = 0;
        // the average error for each reference point
        private double Terror1 = 0, Terror2 = 0, Terror3 = 0;
        // the coefficients for calibration of temperature
        private double tempCoeffA, tempCoeffB, tempCoeffC;

        // indicates whether or not to temperature compensate the humidity values
        private Boolean useTemperatureCompensation = false;
        // indicates whether or not to use the temperature log for compensation
        private Boolean overrideTemperatureLog = false;
        // default temperature in case of no log or override log
        private double defaultTempCompensationValue = 25;

        // indicates whether or not this is a DS1922H
        private Boolean hasHumiditySensor = false;


        // temperature is 8-bit or 11-bit
        private readonly double[] temperatureResolutions = new double[] { .5d, .0625d };
        // data is 10-bit or 16-bit
        private readonly double[] dataResolutions = new double[] { .5d, 0.001953125 };
        private readonly double[] humidityResolutions = new double[] { .5d, .125d };

        private string descriptionString =
            "Rugged, self-sufficient 1-Wire device that, once setup for "
            + "a mission, will measure temperature and A-to-D, with the "
            + "result recorded in a protected memory section. It stores up "
            + "to 8192 1-byte measurements, which can be filled with 1- or "
            + "2-byte temperature readings and 1- or 2-byte A-to-D/Humidity readings "
            + "taken at a user-specified rate.";

        // first year that calendar starts counting years from
        private const int FIRST_YEAR_EVER = 2000;

        // used to 'enable' passwords
        private const byte ENABLE_BYTE = (byte)0xAA;
        // used to 'disable' passwords
        private const byte DISABLE_BYTE = 0x00;

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // 1-Wire Commands
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /** Refers to the Temperature Channel for this device */
        public const int TEMPERATURE_CHANNEL = 0;
        /** Refers to the Humidity/A-D Channel for this device */
        public const int DATA_CHANNEL = 1;

        /** 1-Wire command for Write Scratchpad */
        public const byte WRITE_SCRATCHPAD_COMMAND = (byte)0x0F;
        /** 1-Wire command for Read Scratchpad */
        public const byte READ_SCRATCHPAD_COMMAND = (byte)0xAA;
        /** 1-Wire command for Copy Scratchpad With Password */
        public const byte COPY_SCRATCHPAD_PW_COMMAND = (byte)0x99;
        /** 1-Wire command for Read Memory CRC With Password */
        public const byte READ_MEMORY_CRC_PW_COMMAND = (byte)0x69;
        /** 1-Wire command for Clear Memory With Password */
        public const byte CLEAR_MEMORY_PW_COMMAND = (byte)0x96;
        /** 1-Wire command for Start Mission With Password */
        public const byte START_MISSION_PW_COMMAND = (byte)0xCC;
        /** 1-Wire command for Stop Mission With Password */
        public const byte STOP_MISSION_PW_COMMAND = (byte)0x33;
        /** 1-Wire command for Forced Conversion */
        public const byte FORCED_CONVERSION = (byte)0x55;

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Register addresses and control bits
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /** Address of the Real-time Clock Time value*/
        public const int RTC_TIME = 0x200;
        /** Address of the Real-time Clock Date value*/
        public const int RTC_DATE = 0x203;

        /** Address of the Sample Rate Register */
        public const int SAMPLE_RATE = 0x206;// 2 bytes, LSB first, MSB no greater than 0x3F

        /** Address of the Temperature Low Alarm Register */
        public const int TEMPERATURE_LOW_ALARM_THRESHOLD = 0x208;
        /** Address of the Temperature High Alarm Register */
        public const int TEMPERATURE_HIGH_ALARM_THRESHOLD = 0x209;

        /** Address of the Data Low Alarm Register */
        public const int DATA_LOW_ALARM_THRESHOLD = 0x20A;
        /** Address of the Data High Alarm Register */
        public const int DATA_HIGH_ALARM_THRESHOLD = 0x20B;

        /** Address of the last temperature conversion's LSB */
        public const int LAST_TEMPERATURE_CONVERSION_LSB = 0x20C;
        /** Address of the last temperature conversion's MSB */
        public const int LAST_TEMPERATURE_CONVERSION_MSB = 0x20D;

        /** Address of the last data conversion's LSB */
        public const int LAST_DATA_CONVERSION_LSB = 0x20E;
        /** Address of the last data conversion's MSB */
        public const int LAST_DATA_CONVERSION_MSB = 0x20F;

        /** Address of Temperature Control Register */
        public const int TEMPERATURE_CONTROL_REGISTER = 0x210;
        /** Temperature Control Register Bit: Enable Data Low Alarm */
        public const byte TCR_BIT_ENABLE_TEMPERATURE_LOW_ALARM = (byte)0x01;
        /** Temperature Control Register Bit: Enable Data Low Alarm */
        public const byte TCR_BIT_ENABLE_TEMPERATURE_HIGH_ALARM = (byte)0x02;

        /** Address of Data Control Register */
        public const int DATA_CONTROL_REGISTER = 0x211;
        /** Data Control Register Bit: Enable Data Low Alarm */
        public const byte DCR_BIT_ENABLE_DATA_LOW_ALARM = (byte)0x01;
        /** Data Control Register Bit: Enable Data High Alarm */
        public const byte DCR_BIT_ENABLE_DATA_HIGH_ALARM = (byte)0x02;

        /** Address of Real-Time Clock Control Register */
        public const int RTC_CONTROL_REGISTER = 0x212;
        /** Real-Time Clock Control Register Bit: Enable Oscillator */
        public const byte RCR_BIT_ENABLE_OSCILLATOR = (byte)0x01;
        /** Real-Time Clock Control Register Bit: Enable High Speed Sample */
        public const byte RCR_BIT_ENABLE_HIGH_SPEED_SAMPLE = (byte)0x02;

        /** Address of Mission Control Register */
        public const int MISSION_CONTROL_REGISTER = 0x213;
        /** Mission Control Register Bit: Enable Temperature Logging */
        public const byte MCR_BIT_ENABLE_TEMPERATURE_LOGGING = (byte)0x01;
        /** Mission Control Register Bit: Enable Data Logging */
        public const byte MCR_BIT_ENABLE_DATA_LOGGING = (byte)0x02;
        /** Mission Control Register Bit: Set Temperature Resolution */
        public const byte MCR_BIT_TEMPERATURE_RESOLUTION = (byte)0x04;
        /** Mission Control Register Bit: Set Data Resolution */
        public const byte MCR_BIT_DATA_RESOLUTION = (byte)0x08;
        /** Mission Control Register Bit: Enable Rollover */
        public const byte MCR_BIT_ENABLE_ROLLOVER = (byte)0x10;
        /** Mission Control Register Bit: Start Mission on Temperature Alarm */
        public const byte MCR_BIT_START_MISSION_ON_TEMPERATURE_ALARM = (byte)0x20;

        /** Address of Alarm Status Register */
        public const int ALARM_STATUS_REGISTER = 0x214;
        /** Alarm Status Register Bit: Temperature Low Alarm */
        public const byte ASR_BIT_TEMPERATURE_LOW_ALARM = (byte)0x01;
        /** Alarm Status Register Bit: Temperature High Alarm */
        public const byte ASR_BIT_TEMPERATURE_HIGH_ALARM = (byte)0x02;
        /** Alarm Status Register Bit: Data Low Alarm */
        public const byte ASR_BIT_DATA_LOW_ALARM = (byte)0x04;
        /** Alarm Status Register Bit: Data High Alarm */
        public const byte ASR_BIT_DATA_HIGH_ALARM = (byte)0x08;
        /** Alarm Status Register Bit: Battery On Reset */
        public const byte ASR_BIT_BATTERY_ON_RESET = (byte)0x80;

        /** Address of General Status Register */
        public const int GENERAL_STATUS_REGISTER = 0x215;
        /** General Status Register Bit: Sample In Progress */
        public const byte GSR_BIT_SAMPLE_IN_PROGRESS = (byte)0x01;
        /** General Status Register Bit: Mission In Progress */
        public const byte GSR_BIT_MISSION_IN_PROGRESS = (byte)0x02;
        /** General Status Register Bit: Conversion In Progress */
        public const byte GSR_BIT_CONVERSION_IN_PROGRESS = (byte)0x04;
        /** General Status Register Bit: Memory Cleared */
        public const byte GSR_BIT_MEMORY_CLEARED = (byte)0x08;
        /** General Status Register Bit: Waiting for Temperature Alarm */
        public const byte GSR_BIT_WAITING_FOR_TEMPERATURE_ALARM = (byte)0x10;
        /** General Status Register Bit: Forced Conversion In Progress */
        public const byte GSR_BIT_FORCED_CONVERSION_IN_PROGRESS = (byte)0x20;

        /** Address of the Mission Start Delay */
        public const int MISSION_START_DELAY = 0x216; // 3 bytes, LSB first

        /** Address of the Mission Timestamp Time value*/
        public const int MISSION_TIMESTAMP_TIME = 0x219;
        /** Address of the Mission Timestamp Date value*/
        public const int MISSION_TIMESTAMP_DATE = 0x21C;

        /** Address of Device Configuration Register */
        public const int DEVICE_CONFIGURATION_BYTE = 0x226;
        /** Value of Device Configuration Register for DS1922S */
        public const byte DCB_DS2422S = 0x00;
        /** Value of Device Configuration Register for DS1922H */
        public const byte DCB_DS1922H = 0x20;
        /** Value of Device Configuration Register for DS1922L */
        public const byte DCB_DS1922L = 0x40;
        /** Value of Device Configuration Register for DS1922T */
        public const byte DCB_DS1922T = 0x60;

        // 1 byte, alternating ones and zeroes indicates passwords are enabled
        /** Address of the Password Control Register. */
        public const int PASSWORD_CONTROL_REGISTER = 0x227;

        // 8 bytes, write only, for setting the Read Access Password
        /** Address of Read Access Password. */
        public const int READ_ACCESS_PASSWORD = 0x228;

        // 8 bytes, write only, for setting the Read Access Password
        /** Address of the Read Write Access Password. */
        public const int READ_WRITE_ACCESS_PASSWORD = 0x230;

        // 3 bytes, LSB first
        /** Address of the Mission Sample Count */
        public const int MISSION_SAMPLE_COUNT = 0x220;

        // 3 bytes, LSB first
        /** Address of the Device Sample Count */
        public const int DEVICE_SAMPLE_COUNT = 0x223;

        /** maximum size of the mission log */
        public const int MISSION_LOG_SIZE = 8192;

        /**
         * mission log size for odd combination of resolutions (i.e. 8-bit temperature
         * & 16-bit data or 16-bit temperature & 8-bit data
         */
        public const int ODD_MISSION_LOG_SIZE = 7680;

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Constructors and Initializers
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /**
         * Creates a new <code>OneWireContainer</code> for communication with a
         * DS1922.
         *
         * @param  sourceAdapter     adapter object required to communicate with
         * this iButton
         * @param  newAddress        address of this DS1922
         *
         * @see #OneWireContainer41()
         * @see #OneWireContainer41(com.dalsemi.onewire.adapter.DSPortAdapter,long)   OneWireContainer41(DSPortAdapter,long)
         * @see #OneWireContainer41(com.dalsemi.onewire.adapter.DSPortAdapter,java.lang.String) OneWireContainer41(DSPortAdapter,String)
         */
        public OneWireContainer41(PortAdapter sourceAdapter, byte[] newAddress)
            : base(sourceAdapter, newAddress)
        {
            // initialize the memory banks
// RM: this is already done by 'base(sourceAdapter, newAddress)', which calls 'SetupContainer(sourceAdapter, newAddress)'
//            InitMem();
//            SetContainerVariables(null);
        }

        /**
         * Provides this container with the adapter object used to access this device and
         * the address of the iButton or 1-Wire device.
         *
         * @param  sourceAdapter     adapter object required to communicate with
         *                           this iButton
         * @param  newAddress        address of this 1-Wire device
         * @see com.dalsemi.onewire.utils.Address
         */
        protected override void SetupContainer(PortAdapter sourceAdapter, byte[] newAddress)
        {
            base.SetupContainer(sourceAdapter, newAddress);

            // initialize the memory banks
            InitMem();
            SetContainerVariables(null);
        }

        private void CheckContainerVariablesSet()
        {
            if (!isContainerVariablesSet)
                ReadDevice();
        }

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Sensor read/write
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /**
         * Retrieves the 1-Wire device sensor state.  This state is
         * returned as a byte array.  Pass this byte array to the 'get'
         * and 'set' methods.  If the device state needs to be changed then call
         * the 'writeDevice' to finalize the changes.
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
            byte[] buffer = new byte[96];

            int retryCnt = MAX_READ_RETRY_CNT;
            int page = 0;
            do
            {
                try
                {
                    switch (page)
                    {
                        case 0:
                            register.ReadPageCRC(0, false, buffer, 0);
                            page++;
                            break;
                        case 1:
                            register.ReadPageCRC(1, retryCnt == MAX_READ_RETRY_CNT, buffer, 32);
                            page++;
                            break;
                        case 2:
                            register.ReadPageCRC(2, retryCnt == MAX_READ_RETRY_CNT, buffer, 64);
                            page++;
                            break;
                    }
                    retryCnt = MAX_READ_RETRY_CNT;
                }
                catch (OneWireIOException owioe)
                {
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
                    Debug.DebugStr("readDevice exc, retryCnt=" + retryCnt, owioe);
#endif
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
                    if (--retryCnt == 0)
                        throw owioe;
                }
                catch (OneWireException owe)
                {
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
                    Debug.DebugStr("readDevice exc, retryCnt=" + retryCnt, owe);
#endif
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
                    if (--retryCnt == 0)
                        throw owe;
                }
            }
            while (page < 3);

            if (!isContainerVariablesSet)
                SetContainerVariables(buffer);

            return buffer;
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
            int start = updatertc ? 0 : 6;

            register.Write(start, state, start, 32 - start);

            lock (this)
            {
                updatertc = false;
            }
        }

        /**
         * Reads a single byte from the DS1922.  Note that the preferred manner
         * of reading from the DS1922 Thermocron is through the <code>readDevice()</code>
         * method or through the <code>MemoryBank</code> objects returned in the
         * <code>getMemoryBanks()</code> method.
         *
         * @param memAddr the address to read from  (in the range of 0x200-0x21F)
         *
         * @return the data byte read
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         *
         * @see #readDevice()
         * @see #getMemoryBanks()
         */
        public byte ReadByte(int memAddr)
        {
            // break the address up into bytes
            byte msbAddress = (byte)((memAddr >> 8) & 0x0ff);
            byte lsbAddress = (byte)(memAddr & 0x0ff);

            /* check the validity of the address */
            if ((msbAddress > 0x2F) || (msbAddress < 0))
                throw new ArgumentOutOfRangeException(
                   "OneWireContainer41-Address for read out of range.");

            int numBytesToEndOfPage = 32 - (lsbAddress & 0x1F);
            byte[] buffer = new byte[11 + numBytesToEndOfPage + 2];

            if (doSpeedEnable)
                DoSpeed();

            if (adapter.SelectDevice(address))
            {
                buffer[0] = READ_MEMORY_CRC_PW_COMMAND;
                buffer[1] = lsbAddress;
                buffer[2] = msbAddress;

                if (IsContainerReadWritePasswordSet())
                    GetContainerReadWritePassword(buffer, 3);
                else
                    GetContainerReadOnlyPassword(buffer, 3);

                for (int i = 11; i < buffer.Length; i++)
                    buffer[i] = (byte)0x0ff;

                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
                Debug.DebugStr("Send-> ", buffer);
#endif
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
                adapter.DataBlock(buffer, 0, buffer.Length);
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
                Debug.DebugStr("Recv<- ", buffer);
#endif
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

                // exclude password from CRC 16
                if (CRC16.Compute(buffer, 11, buffer.Length - 11, CRC16.Compute(buffer, 0, 3, 0))
                   != 0x0000B001)
                    throw new OneWireIOException(
                       "Invalid CRC16 read from device.  Password may be incorrect or a sample may be in progress.");

                return buffer[11];
            }
            else
                throw new OneWireException("OneWireContainer41-Device not present.");
        }

        /**
         * <p>Gets the status of the specified flag from the specified register.
         * This method actually communicates with the DS1922.  To improve
         * performance if you intend to make multiple calls to this method,
         * first call <code>readDevice()</code> and use the
         * <code>getFlag(int, byte, byte[])</code> method instead.</p>
         *
         * <p>The DS1922 has several sets of flags.</p>
         * <ul>
         *    <LI>Register: <CODE> TEMPERATURE_CONTROL_REGISTER </CODE><BR>
         *       Flags:
         *       <UL>
         *          <li><code> TCR_BIT_ENABLE_TEMPERATURE_LOW_ALARM  </code></li>
         *          <li><code> TCR_BIT_ENABLE_TEMPERATURE_HIGH_ALARM </code></li>
         *       </UL>
         *    </LI>
         *    <LI>Register: <CODE> DATA_CONTROL_REGISTER </CODE><BR>
         *       Flags:
         *       <UL>
         *          <li><code> DCR_BIT_ENABLE_DATA_LOW_ALARM  </code></li>
         *          <li><code> DCR_BIT_ENABLE_DATA_HIGH_ALARM </code></li>
         *       </UL>
         *    </LI>
         *    <LI>Register: <CODE> RTC_CONTROL_REGISTER </CODE><BR>
         *       Flags:
         *       <UL>
         *          <li><code> RCR_BIT_ENABLE_OSCILLATOR        </code></li>
         *          <li><code> RCR_BIT_ENABLE_HIGH_SPEED_SAMPLE </code></li>
         *       </UL>
         *    </LI>
         *    <LI>Register: <CODE> MISSION_CONTROL_REGISTER </CODE><BR>
         *       Flags:
         *       <UL>
         *          <li><code> MCR_BIT_ENABLE_TEMPERATURE_LOGGING           </code></li>
         *          <li><code> MCR_BIT_ENABLE_DATA_LOGGING                  </code></li>
         *          <li><code> MCR_BIT_TEMPERATURE_RESOLUTION               </code></li>
         *          <li><code> MCR_BIT_DATA_RESOLUTION                      </code></li>
         *          <li><code> MCR_BIT_ENABLE_ROLLOVER                      </code></li>
         *          <li><code> MCR_BIT_START_MISSION_UPON_TEMPERATURE_ALARM </code></li>
         *       </UL>
         *    </LI>
         *    <LI>Register: <CODE> ALARM_STATUS_REGISTER </CODE><BR>
         *       Flags:
         *       <UL>
         *          <li><code> ASR_BIT_TEMPERATURE_LOW_ALARM  </code></li>
         *          <li><code> ASR_BIT_TEMPERATURE_HIGH_ALARM </code></li>
         *          <li><code> ASR_BIT_DATA_LOW_ALARM         </code></li>
         *          <li><code> ASR_BIT_DATA_HIGH_ALARM        </code></li>
         *          <li><code> ASR_BIT_BATTERY_ON_RESET       </code></li>
         *       </UL>
         *    </LI>
         *    <LI>Register: <CODE> GENERAL_STATUS_REGISTER </CODE><BR>
         *       Flags:
         *       <UL>
         *          <li><code> GSR_BIT_SAMPLE_IN_PROGRESS            </code></li>
         *          <li><code> GSR_BIT_MISSION_IN_PROGRESS           </code></li>
         *          <li><code> GSR_BIT_MEMORY_CLEARED                </code></li>
         *          <li><code> GSR_BIT_WAITING_FOR_TEMPERATURE_ALARM </code></li>
         *       </UL>
         *    </LI>
         * </ul>
         *
         * @param register address of register containing the flag (see above for available options)
         * @param bitMask the flag to read (see above for available options)
         *
         * @return the status of the flag, where <code>true</code>
         * signifies a "1" and <code>false</code> signifies a "0"
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         *
         * @see #getFlag(int,byte,byte[])
         * @see #readDevice()
         * @see #setFlag(int,byte,boolean)
         */
        public Boolean GetFlag(int register, byte bitMask)
        {
            int retryCnt = MAX_READ_RETRY_CNT;
            while (true)
            {
                try
                {
                    return ((ReadByte(register) & bitMask) != 0);
                }
                catch (OneWireException owe)
                {
                    if (--retryCnt == 0)
                        throw owe;
                }
            }
        }

        /**
         * <p>Gets the status of the specified flag from the specified register.
         * This method is the preferred manner of reading the control and
         * status flags.</p>
         *
         * <p>For more information on valid values for the <code>bitMask</code>
         * parameter, see the {@link #getFlag(int,byte) getFlag(int,byte)} method.</p>
         *
         * @param register address of register containing the flag (see
         * {@link #getFlag(int,byte) getFlag(int,byte)} for available options)
         * @param bitMask the flag to read (see {@link #getFlag(int,byte) getFlag(int,byte)}
         * for available options)
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return the status of the flag, where <code>true</code>
         * signifies a "1" and <code>false</code> signifies a "0"
         *
         * @see #getFlag(int,byte)
         * @see #readDevice()
         * @see #setFlag(int,byte,boolean,byte[])
         */
        public Boolean GetFlag(int register, byte bitMask, byte[] state)
        {
            return ((state[register & 0x3F] & bitMask) != 0);
        }

        /**
         * <p>Sets the status of the specified flag in the specified register.
         * If a mission is in progress a <code>OneWireIOException</code> will be thrown
         * (one cannot write to the registers while a mission is commencing).  This method
         * actually communicates with the DS1922.  To improve
         * performance if you intend to make multiple calls to this method,
         * first call <code>readDevice()</code> and use the
         * <code>setFlag(int,byte,boolean,byte[])</code> method instead.</p>
         *
         * <p>For more information on valid values for the <code>bitMask</code>
         * parameter, see the {@link #getFlag(int,byte) getFlag(int,byte)} method.</p>
         *
         * @param register address of register containing the flag (see
         * {@link #getFlag(int,byte) getFlag(int,byte)} for available options)
         * @param bitMask the flag to read (see {@link #getFlag(int,byte) getFlag(int,byte)}
         * for available options)
         * @param flagValue new value for the flag (<code>true</code> is logic "1")
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         *         In the case of the DS1922, this could also be due to a
         *         currently running mission.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         *
         * @see #getFlag(int,byte)
         * @see #getFlag(int,byte,byte[])
         * @see #setFlag(int,byte,boolean,byte[])
         * @see #readDevice()
         */
        public void SetFlag(int register, byte bitMask, Boolean flagValue)
        {
            byte[] state = ReadDevice();

            SetFlag(register, bitMask, flagValue, state);

            WriteDevice(state);
        }


        /**
         * <p>Sets the status of the specified flag in the specified register.
         * If a mission is in progress a <code>OneWireIOException</code> will be thrown
         * (one cannot write to the registers while a mission is commencing).  This method
         * is the preferred manner of setting the DS1922 status and control flags.
         * The method <code>writeDevice()</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice()</code>.</p>
         *
         * <p>For more information on valid values for the <code>bitMask</code>
         * parameter, see the {@link #getFlag(int,byte) getFlag(int,byte)} method.</p>
         *
         * @param register address of register containing the flag (see
         * {@link #getFlag(int,byte) getFlag(int,byte)} for available options)
         * @param bitMask the flag to read (see {@link #getFlag(int,byte) getFlag(int,byte)}
         * for available options)
         * @param flagValue new value for the flag (<code>true</code> is logic "1")
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @see #getFlag(int,byte)
         * @see #getFlag(int,byte,byte[])
         * @see #setFlag(int,byte,boolean)
         * @see #readDevice()
         * @see #writeDevice(byte[])
         */
        public void SetFlag(int register, byte bitMask, Boolean flagValue,
                             byte[] state)
        {
            register = register & 0x3F;

            byte flags = state[register];

            if (flagValue)
                flags = (byte)(flags | bitMask);
            else
                flags = (byte)(flags & ~(bitMask));

            // write the regs back
            state[register] = flags;
        }

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Container Functions
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /**
         * Gets an enumeration of memory bank instances that implement one or more
         * of the following interfaces:
         * {@link com.dalsemi.onewire.container.MemoryBank MemoryBank},
         * {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank},
         * and {@link com.dalsemi.onewire.container.OTPMemoryBank OTPMemoryBank}.
         * @return <CODE>Enumeration</CODE> of memory banks
         */
        public override MemoryBankList GetMemoryBanks()
        {
            MemoryBankList bank_vector = new MemoryBankList();

            bank_vector.Add(scratch);
            bank_vector.Add(userDataMemory);
            bank_vector.Add(register);
            bank_vector.Add(log);

            return bank_vector;
        }


        /**
         * Returns instance of the memory bank representing this device's
         * scratchpad.
         *
         * @return scratchpad memory bank
         */
        public MemoryBankScratchCRCPW GetScratchpadMemoryBank()
        {
            return this.scratch;
        }

        /**
         * Returns instance of the memory bank representing this device's
         * general-purpose user data memory.
         *
         * @return user data memory bank
         */
        public MemoryBankNVCRCPW GetUserDataMemoryBank()
        {
            return this.userDataMemory;
        }

        /**
         * Returns instance of the memory bank representing this device's
         * data log.
         *
         * @return data log memory bank
         */
        public MemoryBankNVCRCPW GetDataLogMemoryBank()
        {
            return this.log;
        }

        /**
         * Returns instance of the memory bank representing this device's
         * special function registers.
         *
         * @return register memory bank
         */
        public MemoryBankNVCRCPW GetRegisterMemoryBank()
        {
            return this.register;
        }

        /**
         * Returns the maximum speed this iButton device can
         * communicate at.
         *
         * @return maximum speed
         * @see DSPortAdapter#SetSpeed
         */
        public override OWSpeed GetMaxSpeed()
        {
            return OWSpeed.SPEED_OVERDRIVE;
        }

        /**
         * Gets the Dallas Semiconductor part number of the iButton
         * or 1-Wire Device as a <code>java.lang.String</code>.
         * For example "DS1992".
         *
         * @return iButton or 1-Wire device name
         */
        public override string GetName()
        {
            CheckContainerVariablesSet();
            return partNumber;
        }

        /**
         * Retrieves the alternate Dallas Semiconductor part numbers or names.
         * A 'family' of MicroLAN devices may have more than one part number
         * depending on packaging.  There can also be nicknames such as
         * "Crypto iButton".
         *
         * @return  the alternate names for this iButton or 1-Wire device
         */
        public override string GetAlternateNames()
        {
            return "Hygrochron";
        }

        /**
         * Gets a short description of the function of this iButton
         * or 1-Wire Device type.
         *
         * @return device description
         */
        public override string GetDescription()
        {
            return descriptionString;
        }

        /**
         * Directs the container to avoid the calls to doSpeed() in methods that communicate
         * with the DS1922/DS2422. To ensure that all parts can talk to the 1-Wire bus
         * at their desired speed, each method contains a call
         * to <code>doSpeed()</code>.  However, this is an expensive operation.
         * If a user manages the bus speed in an
         * application,  call this method with <code>doSpeedCheck</code>
         * as <code>false</code>.  The default behavior is
         * to call <code>doSpeed()</code>.
         *
         * @param doSpeedCheck <code>true</code> for <code>doSpeed()</code> to be called before every
         * 1-Wire bus access, <code>false</code> to skip this expensive call
         *
         * @see OneWireContainer#doSpeed()
         */
        public void SetSpeedCheck(Boolean doSpeedCheck)
        {
            lock (this)
            {
                doSpeedEnable = doSpeedCheck;
            }
        }

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // DS1922 Device Specific Functions
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /**
         * Stops the currently running mission.
         *
         */
        public void StopMission()
        {
            /* read a user specified amount of memory and verify its validity */
            if (doSpeedEnable)
                DoSpeed();

            if (!adapter.SelectDevice(address))
                throw new OneWireException("OneWireContainer41-Device not present.");

            byte[] buffer = new byte[10];
            buffer[0] = STOP_MISSION_PW_COMMAND;
            GetContainerReadWritePassword(buffer, 1);
            buffer[9] = (byte)0xFF;

            adapter.DataBlock(buffer, 0, 10);

            if (GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MISSION_IN_PROGRESS))
                throw new OneWireException(
                   "OneWireContainer41-Stop mission failed.  Check read/write password.");
        }

        /**
         * Starts a new mission.  Assumes all parameters have been set by either
         * writing directly to the device registers, or by calling other setup
         * methods.
         */
        public void StartMission()
        {
            if (GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MISSION_IN_PROGRESS))
                throw new OneWireException(
                   "OneWireContainer41-Cannot start a mission while a mission is in progress.");

            if (!GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MEMORY_CLEARED))
                throw new OneWireException(
                   "OneWireContainer41-Must clear memory before calling start mission.");

            if (doSpeedEnable)
                DoSpeed();

            if (!adapter.SelectDevice(address))
                throw new OneWireException("OneWireContainer41-Device not present.");

            byte[] buffer = new byte[10];
            buffer[0] = START_MISSION_PW_COMMAND;
            GetContainerReadWritePassword(buffer, 1);
            buffer[9] = (byte)0xFF;

            adapter.DataBlock(buffer, 0, 10);
        }

        /**
         * Erases the log memory from this missioning device.
         */
        public void ClearMemory()
        {
            if (GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MISSION_IN_PROGRESS))
                throw new OneWireException(
                   "OneWireContainer41-Cannot clear memory while mission is in progress.");

            if (doSpeedEnable)
                DoSpeed();

            if (!adapter.SelectDevice(address))
                throw new OneWireException("OneWireContainer41-Device not present.");

            byte[] buffer = new byte[10];
            buffer[0] = CLEAR_MEMORY_PW_COMMAND;
            GetContainerReadWritePassword(buffer, 1);
            buffer[9] = (byte)0xFF;

            adapter.DataBlock(buffer, 0, 10);

            // wait 2 ms for Clear Memory to comlete
            System.Threading.Thread.Sleep(2);

            if (!GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MEMORY_CLEARED))
                throw new OneWireException(
                   "OneWireContainer41-Clear Memory failed.  Check read/write password.");
        }


        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Read/Write Password Functions
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /**
         * Returns the length in bytes of the Read-Only password.
         *
         * @return the length in bytes of the Read-Only password.
         */
        public int GetReadOnlyPasswordLength()
        {
            return PASSWORD_LENGTH;
        }

        /**
         * Returns the length in bytes of the Read/Write password.
         *
         * @return the length in bytes of the Read/Write password.
         */
        public int GetReadWritePasswordLength()
        {
            return PASSWORD_LENGTH;
        }

        /**
         * Returns the length in bytes of the Write-Only password.
         *
         * @return the length in bytes of the Write-Only password.
         */
        public int GetWriteOnlyPasswordLength()
        {
            throw new OneWireException("The DS1922 does not have a write only password.");
        }

        /**
         * Returns the absolute address of the memory location where
         * the Read-Only password is written.
         *
         * @return the absolute address of the memory location where
         *         the Read-Only password is written.
         */
        public int GetReadOnlyPasswordAddress()
        {
            return READ_ACCESS_PASSWORD;
        }

        /**
         * Returns the absolute address of the memory location where
         * the Read/Write password is written.
         *
         * @return the absolute address of the memory location where
         *         the Read/Write password is written.
         */
        public int GetReadWritePasswordAddress()
        {
            return READ_WRITE_ACCESS_PASSWORD;
        }

        /**
         * Returns the absolute address of the memory location where
         * the Write-Only password is written.
         *
         * @return the absolute address of the memory location where
         *         the Write-Only password is written.
         */
        public int GetWriteOnlyPasswordAddress()
        {
            throw new OneWireException("The DS1922 does not have a write password.");
        }

        /**
         * Returns true if this device has a Read-Only password.
         * If false, all other functions dealing with the Read-Only
         * password will throw an exception if called.
         *
         * @return <code>true</code> always, since DS1922 has Read-Only password.
         */
        public Boolean HasReadOnlyPassword()
        {
            return true;
        }

        /**
         * Returns true if this device has a Read/Write password.
         * If false, all other functions dealing with the Read/Write
         * password will throw an exception if called.
         *
         * @return <code>true</code> always, since DS1922 has Read/Write password.
         */
        public Boolean HasReadWritePassword()
        {
            return true;
        }

        /**
         * Returns true if this device has a Write-Only password.
         * If false, all other functions dealing with the Write-Only
         * password will throw an exception if called.
         *
         * @return <code>false</code> always, since DS1922 has no Write-Only password.
         */
        public Boolean HasWriteOnlyPassword()
        {
            return false;
        }

        /**
         * Returns true if the device's Read-Only password has been enabled.
         *
         * @return <code>true</code> if the device's Read-Only password has been enabled.
         */
        public Boolean GetDeviceReadOnlyPasswordEnable()
        {
            return readOnlyPasswordEnabled;
        }

        /**
         * Returns true if the device's Read/Write password has been enabled.
         *
         * @return <code>true</code> if the device's Read/Write password has been enabled.
         */
        public Boolean GetDeviceReadWritePasswordEnable()
        {
            return readWritePasswordEnabled;
        }

        /**
         * Returns true if the device's Write-Only password has been enabled.
         *
         * @return <code>true</code> if the device's Write-Only password has been enabled.
         */
        public Boolean GetDeviceWriteOnlyPasswordEnable()
        {
            throw new OneWireException("The DS1922 does not have a Write Only Password.");
        }

        /**
         * Returns true if this device has the capability to enable one type of password
         * while leaving another type disabled.  i.e. if the device has Read-Only password
         * protection and Write-Only password protection, this method indicates whether or
         * not you can enable Read-Only protection while leaving the Write-Only protection
         * disabled.
         *
         * @return <code>true</code> if the device has the capability to enable one type
         *         of password while leaving another type disabled.
         */
        public Boolean HasSinglePasswordEnable()
        {
            return false;
        }

        /**
         * <p>Enables/Disables passwords for this Device.  This method allows you to
         * individually enable the different types of passwords for a particular
         * device.  If <code>hasSinglePasswordEnable()</code> returns true,
         * you can selectively enable particular types of passwords.  Otherwise,
         * this method will throw an exception if all supported types are not
         * enabled.</p>
         *
         * <p>For this to be successful, either write-protect passwords must be disabled,
         * or the write-protect password(s) for this container must be set and must match
         * the value of the write-protect password(s) in the device's register.</p>
         *
         * <P><B>
         * WARNING: Enabling passwords requires that both the read password and the
         * read/write password be re-written to the part.  Before calling this method,
         * you should set the container read password and read/write password values.
         * This will ensure that the correct value is written into the part.
         * </B></P>
         *
         * @param enableReadOnly if <code>true</code> Read-Only passwords will be enabled.
         * @param enableReadWrite if <code>true</code> Read/Write passwords will be enabled.
         * @param enableWriteOnly if <code>true</code> Write-Only passwords will be enabled.
         */
        public void SetDevicePasswordEnable(Boolean enableReadOnly,
           Boolean enableReadWrite, Boolean enableWriteOnly)
        {
            if (enableWriteOnly)
                throw new OneWireException(
                   "The DS1922 does not have a write only password.");
            if (enableReadOnly != enableReadWrite)
                throw new OneWireException(
                   "Both read-only and read/write will be set with enable.");
            if (!IsContainerReadOnlyPasswordSet())
                throw new OneWireException("Container Read Password is not set");
            if (!IsContainerReadWritePasswordSet())
                throw new OneWireException("Container Read/Write Password is not set");

            // must write both passwords for this to work
            byte[] bothPasswordsEnable = new byte[17];
            bothPasswordsEnable[0] = (enableReadOnly ? ENABLE_BYTE : DISABLE_BYTE);
            GetContainerReadOnlyPassword(bothPasswordsEnable, 1);
            GetContainerReadWritePassword(bothPasswordsEnable, 9);

            register.Write(PASSWORD_CONTROL_REGISTER & 0x3F, bothPasswordsEnable, 0, 17);

            if (enableReadOnly)
            {
                readOnlyPasswordEnabled = true;
                readWritePasswordEnabled = true;
            }
            else
            {
                readOnlyPasswordEnabled = false;
                readWritePasswordEnabled = false;
            }
        }

        /**
         * <p>Enables/Disables passwords for this device.  If the part has more than one
         * type of password (Read-Only, Write-Only, or Read/Write), all passwords
         * will be enabled.  This function is equivalent to the following:
         *    <code> owc41.setDevicePasswordEnable(
         *                    owc41.hasReadOnlyPassword(),
         *                    owc41.hasReadWritePassword(),
         *                    owc41.hasWriteOnlyPassword() ); </code></p>
         *
         * <p>For this to be successful, either write-protect passwords must be disabled,
         * or the write-protect password(s) for this container must be set and must match
         * the value of the write-protect password(s) in the device's register.</P>
         *
         * <P><B>
         * WARNING: Enabling passwords requires that both the read password and the
         * read/write password be re-written to the part.  Before calling this method,
         * you should set the container read password and read/write password values.
         * This will ensure that the correct value is written into the part.
         * </B></P>
         *
         * @param enableAll if <code>true</code>, all passwords are enabled.  Otherwise,
         *        all passwords are disabled.
         */
        public void SetDevicePasswordEnableAll(Boolean enableAll)
        {
            SetDevicePasswordEnable(enableAll, enableAll, false);
        }

        /**
         * <p>Writes the given password to the device's Read-Only password register.  Note
         * that this function does not enable the password, just writes the value to
         * the appropriate memory location.</p>
         *
         * <p>For this to be successful, either write-protect passwords must be disabled,
         * or the write-protect password(s) for this container must be set and must match
         * the value of the write-protect password(s) in the device's register.</p>
         *
         * <P><B>
         * WARNING: Setting the read password requires that both the read password
         * and the read/write password be written to the part.  Before calling this
         * method, you should set the container read/write password value.
         * This will ensure that the correct value is written into the part.
         * </B></P>
         *
         * @param password the new password to be written to the device's Read-Only
         *        password register.  Length must be
         *        <code>(offset + getReadOnlyPasswordLength)</code>
         * @param offset the starting point for copying from the given password array
         */
        public void SetDeviceReadOnlyPassword(byte[] password, int offset)
        {
            if (GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MISSION_IN_PROGRESS))
                throw new OneWireIOException(
                   "OneWireContainer41-Cannot change password while mission is in progress.");

            if (!IsContainerReadWritePasswordSet())
                throw new OneWireException("Container Read/Write Password is not set");

            // must write both passwords for this to work
            byte[] bothPasswords = new byte[16];
            Array.Copy(password, offset, bothPasswords, 0, 8);
            GetContainerReadWritePassword(bothPasswords, 8);

            register.Write(READ_ACCESS_PASSWORD & 0x3F, bothPasswords, 0, 16);
            SetContainerReadOnlyPassword(password, offset);
        }

        /**
         * <p>Writes the given password to the device's Read/Write password register.  Note
         * that this function does not enable the password, just writes the value to
         * the appropriate memory location.</p>
         *
         * <p>For this to be successful, either write-protect passwords must be disabled,
         * or the write-protect password(s) for this container must be set and must match
         * the value of the write-protect password(s) in the device's register.</p>
         *
         * @param password the new password to be written to the device's Read-Write
         *        password register.  Length must be
         *        <code>(offset + getReadWritePasswordLength)</code>
         * @param offset the starting point for copying from the given password array
         */
        public void SetDeviceReadWritePassword(byte[] password, int offset)
        {
            if (GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MISSION_IN_PROGRESS))
                throw new OneWireIOException(
                   "OneWireContainer41-Cannot change password while mission is in progress.");

            register.Write(READ_WRITE_ACCESS_PASSWORD & 0x3F, password, offset, 8);
            SetContainerReadWritePassword(password, offset);
        }

        /**
         * <p>Writes the given password to the device's Write-Only password register.  Note
         * that this function does not enable the password, just writes the value to
         * the appropriate memory location.</p>
         *
         * <p>For this to be successful, either write-protect passwords must be disabled,
         * or the write-protect password(s) for this container must be set and must match
         * the value of the write-protect password(s) in the device's register.</p>
         *
         * @param password the new password to be written to the device's Write-Only
         *        password register.  Length must be
         *        <code>(offset + getWriteOnlyPasswordLength)</code>
         * @param offset the starting point for copying from the given password array
         */
        public void SetDeviceWriteOnlyPassword(byte[] password, int offset)
        {
            throw new OneWireException("The DS1922 does not have a write only password.");
        }

        /**
         * Sets the Read-Only password used by the API when reading from the
         * device's memory.  This password is not written to the device's
         * Read-Only password register.  It is the password used by the
         * software for interacting with the device only.
         *
         * @param password the new password to be used by the API when
         *        reading from the device's memory.  Length must be
         *        <code>(offset + getReadOnlyPasswordLength)</code>
         * @param offset the starting point for copying from the given password array
         */
        public void SetContainerReadOnlyPassword(byte[] password, int offset)
        {
            Array.Copy(password, offset, readPassword, 0, PASSWORD_LENGTH);
            readPasswordSet = true;
        }

        /**
         * Sets the Read/Write password used by the API when reading from  or
         * writing to the device's memory.  This password is not written to
         * the device's Read/Write password register.  It is the password used
         * by the software for interacting with the device only.
         *
         * @param password the new password to be used by the API when
         *        reading from or writing to the device's memory.  Length must be
         *        <code>(offset + getReadWritePasswordLength)</code>
         * @param offset the starting point for copying from the given password array
         */
        public void SetContainerReadWritePassword(byte[] password, int offset)
        {
            Array.Copy(password, offset, readWritePassword, 0, 8);
            readWritePasswordSet = true;
        }

        /**
         * Sets the Write-Only password used by the API when writing to the
         * device's memory.  This password is not written to the device's
         * Write-Only password register.  It is the password used by the
         * software for interacting with the device only.
         *
         * @param password the new password to be used by the API when
         *        writing to the device's memory.  Length must be
         *        <code>(offset + getWriteOnlyPasswordLength)</code>
         * @param offset the starting point for copying from the given password array
         */
        public void SetContainerWriteOnlyPassword(byte[] password, int offset)
        {
            throw new OneWireException("The DS1922 does not have a write only password.");
        }

        /**
         * Returns true if the password used by the API for reading from the
         * device's memory has been set.  The return value is not affected by
         * whether or not the read password of the container actually matches
         * the value in the device's password register
         *
         * @return <code>true</code> if the password used by the API for
         * reading from the device's memory has been set.
         */
        public Boolean IsContainerReadOnlyPasswordSet()
        {
            return readPasswordSet;
        }

        /**
         * Returns true if the password used by the API for reading from or
         * writing to the device's memory has been set.  The return value is
         * not affected by whether or not the read/write password of the
         * container actually matches the value in the device's password
         * register.
         *
         * @return <code>true</code> if the password used by the API for
         * reading from or writing to the device's memory has been set.
         */
        public Boolean IsContainerReadWritePasswordSet()
        {
            return readWritePasswordSet;
        }

        /**
         * Returns true if the password used by the API for writing to the
         * device's memory has been set.  The return value is not affected by
         * whether or not the write password of the container actually matches
         * the value in the device's password register.
         *
         * @return <code>true</code> if the password used by the API for
         * writing to the device's memory has been set.
         */
        public Boolean IsContainerWriteOnlyPasswordSet()
        {
            throw new OneWireException("The DS1922 does not have a write only password");
        }

        /**
         * Gets the Read-Only password used by the API when reading from the
         * device's memory.  This password is not read from the device's
         * Read-Only password register.  It is the password used by the
         * software for interacting with the device only and must have been
         * set using the <code>setContainerReadOnlyPassword</code> method.
         *
         * @param password array for holding the password that is used by the
         *        API when reading from the device's memory.  Length must be
         *        <code>(offset + getWriteOnlyPasswordLength)</code>
         * @param offset the starting point for copying into the given password array
         */
        public void GetContainerReadOnlyPassword(byte[] password, int offset)
        {
            Array.Copy(readPassword, 0, password, offset, PASSWORD_LENGTH);
        }

        /**
         * Gets the Read/Write password used by the API when reading from or
         * writing to the device's memory.  This password is not read from
         * the device's Read/Write password register.  It is the password used
         * by the software for interacting with the device only and must have
         * been set using the <code>setContainerReadWritePassword</code> method.
         *
         * @param password array for holding the password that is used by the
         *        API when reading from or writing to the device's memory.  Length must be
         *        <code>(offset + getReadWritePasswordLength)</code>
         * @param offset the starting point for copying into the given password array
         */
        public void GetContainerReadWritePassword(byte[] password, int offset)
        {
            Array.Copy(readWritePassword, 0, password, offset, PASSWORD_LENGTH);
        }

        /**
         * Gets the Write-Only password used by the API when writing to the
         * device's memory.  This password is not read from the device's
         * Write-Only password register.  It is the password used by the
         * software for interacting with the device only and must have been
         * set using the <code>setContainerWriteOnlyPassword</code> method.
         *
         * @param password array for holding the password that is used by the
         *        API when writing to the device's memory.  Length must be
         *        <code>(offset + getWriteOnlyPasswordLength)</code>
         * @param offset the starting point for copying into the given password array
         */
        public void GetContainerWriteOnlyPassword(byte[] password, int offset)
        {
            throw new OneWireException("The DS1922 does not have a write only password");
        }

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Mission Interface Functions
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /**
         * Returns a default friendly label for each channel supported by this
         * Missioning device.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @return friendly label for the specified channel
         */
        public string GetMissionLabel(int channel)
        {
            if (channel == TEMPERATURE_CHANNEL)
            {
                return "Temperature";
            }
            else if (channel == DATA_CHANNEL)
            {
                if (hasHumiditySensor && !adForceResults)
                    return "Humidity";
                else
                    return "Voltage";
            }
            else
                throw new OneWireException("Invalid Channel");
        }

        /**
         * Sets the SUTA (Start Upon Temperature Alarm) bit in the Mission Control
         * register.  This method will communicate with the device directly.
         *
         * @param enable sets/clears the SUTA bit in the Mission Control register.
         */
        public void SetStartUponTemperatureAlarmEnable(Boolean enable)
        {
            SetFlag(MISSION_CONTROL_REGISTER,
                    MCR_BIT_START_MISSION_ON_TEMPERATURE_ALARM, enable);
        }

        /**
         * Sets the SUTA (Start Upon Temperature Alarm) bit in the Mission Control
         * register.  This method will set the bit in the provided 'state' array,
         * which should be acquired through a call to <code>readDevice()</code>.
         * After updating the 'state', the method <code>writeDevice(byte[])</code>
         * should be called to commit your changes.
         *
         * @param enable sets/clears the SUTA bit in the Mission Control register.
         * @param state current state of the device returned from <code>readDevice()</code>
         */
        public void SetStartUponTemperatureAlarmEnable(Boolean enable, byte[] state)
        {
            SetFlag(MISSION_CONTROL_REGISTER,
                    MCR_BIT_START_MISSION_ON_TEMPERATURE_ALARM, enable, state);
        }

        /**
         * Returns true if the SUTA (Start Upon Temperature Alarm) bit in the
         * Mission Control register is set.  This method will communicate with
         * the device to read the status of the SUTA bit.
         *
         * @return <code>true</code> if the SUTA bit in the Mission Control register is set.
         */
        public Boolean IsStartUponTemperatureAlarmEnabled()
        {
            return GetFlag(MISSION_CONTROL_REGISTER,
               MCR_BIT_START_MISSION_ON_TEMPERATURE_ALARM);
        }

        /**
         * Returns true if the SUTA (Start Upon Temperature Alarm) bit in the
         * Mission Control register is set.  This method will check for  the bit
         * in the provided 'state' array, which should be acquired through a call
         * to <code>readDevice()</code>.
         *
         * @param state current state of the device returned from <code>readDevice()</code>
         * @return <code>true</code> if the SUTA bit in the Mission Control register is set.
         */
        public Boolean IsStartUponTemperatureAlarmEnabled(byte[] state)
        {
            return GetFlag(MISSION_CONTROL_REGISTER,
               MCR_BIT_START_MISSION_ON_TEMPERATURE_ALARM, state);
        }

        /**
         * Returns true if the currently loaded mission results indicate
         * that this mission has the SUTA bit enabled.
         *
         * @return <code>true</code> if the currently loaded mission
         *         results indicate that this mission has the SUTA bit
         *         enabled.
         */
        public Boolean IsMissionSUTA()
        {
            if (isMissionLoaded)
                return GetFlag(MISSION_CONTROL_REGISTER,
                   MCR_BIT_START_MISSION_ON_TEMPERATURE_ALARM, missionRegister);
            else
                return GetFlag(MISSION_CONTROL_REGISTER,
                   MCR_BIT_START_MISSION_ON_TEMPERATURE_ALARM);
        }

        /**
         * Returns true if the currently loaded mission results indicate
         * that this mission has the SUTA bit enabled and is still
         * Waiting For Temperature Alarm (WFTA).
         *
         * @return <code>true</code> if the currently loaded mission
         *         results indicate that this mission has the SUTA bit
         *         enabled and is still Waiting For Temperature Alarm (WFTA).
         */
        public Boolean IsMissionWFTA()
        {
            // check for MIP=1 and SUTA=1 before returning value of WFTA.
            // if MIP=0 or SUTA=0, WFTA could be in invalid state if previous
            // mission did not get a temperature alarm.  Clear Memory should
            // clear this bit, so this is the workaround.
            if (IsMissionRunning() && IsMissionSUTA())
            {
                if (isMissionLoaded)
                    return GetFlag(GENERAL_STATUS_REGISTER,
                       GSR_BIT_WAITING_FOR_TEMPERATURE_ALARM, missionRegister);
                else
                    return GetFlag(GENERAL_STATUS_REGISTER,
                       GSR_BIT_WAITING_FOR_TEMPERATURE_ALARM);
            }
            return false;
        }

        /**
         * Begins a new mission on this missioning device.
         *
         * @param sampleRate indicates the sampling rate, in seconds, that
         *        this missioning device should log samples.
         * @param missionStartDelay indicates the amount of time, in seconds,
         *        that should pass before the mission begins.
         * @param rolloverEnabled if <code>false</code>, this device will stop
         *        recording new samples after the data log is full.  Otherwise,
         *        it will replace samples starting at the beginning.
         * @param syncClock if <code>true</code>, the real-time clock of this
         *        missioning device will be synchronized with the current time
         *        according to this <code>java.util.Date</code>.
         */
        public void StartNewMission(int sampleRate, int missionStartDelay,
                                    Boolean rolloverEnabled, Boolean syncClock,
                                    Boolean[] channelEnabled)
        {
            byte[] state = ReadDevice();
            //if(isMissionLoaded)
            //   state = missionRegister;
            //else
            //   state = readDevice();

            SetFlag(MISSION_CONTROL_REGISTER, MCR_BIT_ENABLE_TEMPERATURE_LOGGING,
                    channelEnabled[TEMPERATURE_CHANNEL], state);
            SetFlag(MISSION_CONTROL_REGISTER, MCR_BIT_ENABLE_DATA_LOGGING,
                    channelEnabled[DATA_CHANNEL], state);

            if (sampleRate % 60 == 0 || sampleRate > 0x03FFF)
            {
                //convert to minutes
                sampleRate = (sampleRate / 60) & 0x03FFF;
                SetFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_HIGH_SPEED_SAMPLE, false, state);
            }
            else
            {
                SetFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_HIGH_SPEED_SAMPLE, true, state);
            }

            DalSemi.OneWire.Utils.Convert.ToByteArray(sampleRate,
                                state, SAMPLE_RATE & 0x3F, 2);

            DalSemi.OneWire.Utils.Convert.ToByteArray(missionStartDelay,
                                state, MISSION_START_DELAY & 0x3F, 2);

            SetFlag(MISSION_CONTROL_REGISTER,
                    MCR_BIT_ENABLE_ROLLOVER, rolloverEnabled, state);

            if (syncClock)
            {
                SetClock(DateTime.Now.ToFileTime(), state); // TODO: check "new Date().getTime()"
            }
            else if (!GetFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_OSCILLATOR, state))
            {
                SetFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_OSCILLATOR, true, state);
            }

            ClearMemory();
            WriteDevice(state);
            StartMission();
        }

        /**
         * Loads the results of the currently running mission.  Must be called
         * before all mission result/status methods.
         */
        public void LoadMissionResults()
        {
            lock (this)
            {
                // read the register contents
                missionRegister = ReadDevice();

                // get the number of samples
                sampleCount = DalSemi.OneWire.Utils.Convert.ToInt(missionRegister,
                                            MISSION_SAMPLE_COUNT & 0x3F, 3);
                sampleCountTotal = sampleCount;

                // sample rate, in seconds
                sampleRate = DalSemi.OneWire.Utils.Convert.ToInt(missionRegister, SAMPLE_RATE & 0x3F, 2);
                if (!GetFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_HIGH_SPEED_SAMPLE, missionRegister))
                    // if sample rate is in minutes, convert to seconds
                    sampleRate *= 60;

                //grab the time
                int[] time = GetTime(MISSION_TIMESTAMP_TIME & 0x3F, missionRegister);
                //grab the date
                int[] date = GetDate(MISSION_TIMESTAMP_DATE & 0x3F, missionRegister);

                //date[1] - 1 because Java months are 0 offset
                /* TODO: check
                      Calendar d = new GregorianCalendar(date[0], date[1] - 1, date[2],
                                                         time[2], time[1], time[0]);

                      missionTimeStamp = d.getTime().getTime();
                */
                missionTimeStamp = 0; // remove this when above code is checked

                // figure out how many bytes for each temperature sample
                temperatureBytes = 0;
                // if it's being logged, add 1 to the size
                if (GetFlag(MISSION_CONTROL_REGISTER,
                           MCR_BIT_ENABLE_TEMPERATURE_LOGGING, missionRegister))
                {
                    temperatureBytes += 1;
                    // if it's 16-bit resolution, add another 1 to the size
                    if (GetFlag(MISSION_CONTROL_REGISTER,
                               MCR_BIT_TEMPERATURE_RESOLUTION, missionRegister))
                        temperatureBytes += 1;
                }


                // figure out how many bytes for each data sample
                dataBytes = 0;
                // if it's being logged, add 1 to the size
                if (GetFlag(MISSION_CONTROL_REGISTER,
                           MCR_BIT_ENABLE_DATA_LOGGING, missionRegister))
                {
                    dataBytes += 1;
                    // if it's 16-bit resolution, add another 1 to the size
                    if (GetFlag(MISSION_CONTROL_REGISTER,
                               MCR_BIT_DATA_RESOLUTION, missionRegister))
                        dataBytes += 1;
                }

                // default size of the log, could be different if using an odd
                // sample size combination.
                //int logSize = MISSION_LOG_SIZE;

                // figure max number of samples
                int maxSamples = 0;
                switch (temperatureBytes + dataBytes)
                {
                    case 1:
                        maxSamples = 8192;
                        break;
                    case 2:
                        maxSamples = 4096;
                        break;
                    case 3:
                        maxSamples = 2560;
                        //logSize = ODD_MISSION_LOG_SIZE;
                        break;
                    case 4:
                        maxSamples = 2048;
                        break;
                    default:
                    case 0:
                        // assert! should never, ever get here
                        break;
                }

                // check for rollover
                int wrapCount = 0, offsetDepth = 0;
                if (GetFlag(MISSION_CONTROL_REGISTER, MCR_BIT_ENABLE_ROLLOVER, missionRegister)
                    && (rolledOver = (sampleCount > maxSamples)))// intentional assignment
                {
                    wrapCount = sampleCount / maxSamples;
                    offsetDepth = sampleCount % maxSamples;
                    sampleCount = maxSamples;
                }

                //DEBUG: For bad SOICS
                if (!GetFlag(MISSION_CONTROL_REGISTER, MCR_BIT_ENABLE_ROLLOVER, missionRegister)
                   && rolledOver)
                {
                    throw new OneWireException("Device Error: rollover was not enabled, but it did occur.");
                }

                // figure out where the temperature bytes end, that's where
                // the data bytes begin
                int temperatureLogSize = temperatureBytes * maxSamples;

                // calculate first log entry time offset, in seconds
                timeOffset = ((wrapCount * maxSamples) + offsetDepth) * sampleRate;

                // temperature log
                temperatureLog = new byte[sampleCount * temperatureBytes];
                // data log
                dataLog = new byte[sampleCount * dataBytes];
                // cache for entire log
                byte[] missionLogBuffer = new byte[Math.Max(temperatureLog.Length, dataLog.Length)];
                byte[] pagebuffer = new byte[32];

                if (temperatureLog.Length > 0)
                {
                    // read the data log for temperature
                    int numPages = (temperatureLog.Length / 32)
                                   + ((temperatureLog.Length % 32) > 0 ? 1 : 0);
                    int retryCnt = MAX_READ_RETRY_CNT;
                    for (int i = 0; i < numPages; )
                    {
                        try
                        {
                            log.ReadPageCRC(i, i > 0 && retryCnt == MAX_READ_RETRY_CNT,
                                            pagebuffer, 0);
                            Array.Copy(pagebuffer, 0, missionLogBuffer, i * 32,
                                             Math.Min(32, temperatureLog.Length - (i * 32)));
                            retryCnt = MAX_READ_RETRY_CNT;
                            i++;
                        }
                        catch (OneWireIOException owioe)
                        {
                            if (--retryCnt == 0)
                                throw owioe;
                        }
                        catch (OneWireException owe)
                        {
                            if (--retryCnt == 0)
                                throw owe;
                        }
                    }

                    // get the temperature bytes in order
                    int offsetIndex = offsetDepth * temperatureBytes;
                    Array.Copy(missionLogBuffer, offsetIndex,
                                     temperatureLog, 0,
                                     temperatureLog.Length - offsetIndex);
                    Array.Copy(missionLogBuffer, 0,
                                     temperatureLog, temperatureLog.Length - offsetIndex,
                                     offsetIndex);
                }

                if (dataLog.Length > 0)
                {
                    // read the data log for humidity
                    int numPages = (dataLog.Length / 32)
                                   + ((dataLog.Length % 32) > 0 ? 1 : 0);
                    int retryCnt = MAX_READ_RETRY_CNT;
                    for (int i = 0; i < numPages; )
                    {
                        try
                        {
                            log.ReadPageCRC((temperatureLogSize / 32) + i, i > 0 && retryCnt == MAX_READ_RETRY_CNT,
                                            pagebuffer, 0);
                            Array.Copy(pagebuffer, 0, missionLogBuffer, i * 32,
                                             Math.Min(32, dataLog.Length - (i * 32)));
                            retryCnt = MAX_READ_RETRY_CNT;
                            i++;
                        }
                        catch (OneWireIOException owioe)
                        {
                            if (--retryCnt == 0)
                                throw owioe;
                        }
                        catch (OneWireException owe)
                        {
                            if (--retryCnt == 0)
                                throw owe;
                        }
                    }

                    // get the data bytes in order
                    int offsetIndex = offsetDepth * dataBytes;
                    Array.Copy(missionLogBuffer, offsetIndex,
                                     dataLog, 0,
                                     dataLog.Length - offsetIndex);
                    Array.Copy(missionLogBuffer, 0,
                                     dataLog, dataLog.Length - offsetIndex,
                                     offsetIndex);
                }

                isMissionLoaded = true;
            }
        }

        /**
         * Returns true if the mission results have been loaded from the device.
         *
         * @return <code>true</code> if the mission results have been loaded.
         */
        public Boolean IsMissionLoaded()
        {
            return isMissionLoaded;
        }

        /**
         * Gets the number of channels supported by this Missioning device.
         * Channel specific methods will use a channel number specified
         * by an integer from [0 to (<code>getNumberOfMissionChannels()</code> - 1)].
         *
         * @return the number of channels
         */
        public int GetNumberMissionChannels()
        {
            // temperature and data
            return 2;
        }

        /**
         * Enables/disables the specified mission channel, indicating whether or
         * not the channel's readings will be recorded in the mission log.
         *
         * @param channel the channel to enable/disable
         * @param enable if true, the channel is enabled
         */
        public void SetMissionChannelEnable(int channel, Boolean enable)
        {
            if (!isMissionLoaded)
                missionRegister = ReadDevice();

            if (channel == TEMPERATURE_CHANNEL)
            {
                SetFlag(MISSION_CONTROL_REGISTER,
                        MCR_BIT_ENABLE_TEMPERATURE_LOGGING,
                        enable, missionRegister);
            }
            else if (channel == DATA_CHANNEL)
            {
                SetFlag(MISSION_CONTROL_REGISTER,
                        MCR_BIT_ENABLE_DATA_LOGGING,
                        enable, missionRegister);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Channel");
            }
            WriteDevice(missionRegister);
        }

        /**
         * Returns true if the specified mission channel is enabled, indicating
         * that the channel's readings will be recorded in the mission log.
         *
         * @param channel the channel to enable/disable
         * @param enable if true, the channel is enabled
         */
        public Boolean GetMissionChannelEnable(int channel)
        {
            if (!isMissionLoaded)
                missionRegister = ReadDevice();

            if (channel == TEMPERATURE_CHANNEL)
            {
                return GetFlag(MISSION_CONTROL_REGISTER,
                               MCR_BIT_ENABLE_TEMPERATURE_LOGGING,
                               missionRegister);
            }
            else if (channel == DATA_CHANNEL)
            {
                return GetFlag(MISSION_CONTROL_REGISTER,
                               MCR_BIT_ENABLE_DATA_LOGGING,
                               missionRegister);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Channel");
            }
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // - Mission Results
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /**
         * Returns the amount of time, in seconds, between samples taken
         * by this missioning device.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @return time, in seconds, between sampling
         */
        public int GetMissionSampleRate(int channel)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            return sampleRate;
        }

        /**
         * Returns the number of samples available for the specified channel
         * during the current mission.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @return number of samples available for the specified channel
         */
        public int GetMissionSampleCount(int channel)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            return sampleCount;
        }

        /**
         * Reads the device and returns the total number of samples logged
         * since the first power-on of this device.
         *
         * @return the total number of samples logged since the first power-on
         * of this device.
         */
        public int GetDeviceSampleCount()
        {
            return GetDeviceSampleCount(ReadDevice());
        }

        /**
         * Returns the total number of samples logged since the first power-on
         * of this device.
         *
         * @param state The current state of the device as return from <code>readDevice()</code>
         * @return the total number of samples logged since the first power-on
         * of this device.
         */
        public int GetDeviceSampleCount(byte[] state)
        {
            return DalSemi.OneWire.Utils.Convert.ToInt(state, DEVICE_SAMPLE_COUNT & 0x3F, 3);
        }


        /**
         * Returns the total number of samples taken for the specified channel
         * during the current mission.  This number can be more than the actual
         * sample count if rollover is enabled and the log has been filled.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @return number of samples taken for the specified channel
         */
        public int GetMissionSampleCountTotal(int channel)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            return sampleCountTotal;
        }

        /**
         * Returns the sample as degrees celsius if temperature channel is specified
         * or as percent relative humidity if data channel is specified.  If the
         * device is a DS2422 configuration (or A-D results are forced on the DS1922H),
         * the data channel will return samples as the voltage measured.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @param sampleNum the sample number to return, between <code>0</code> and
         *        <code>(getMissionSampleCount(channel)-1)</code>
         * @return the sample's value in degrees Celsius or percent RH.
         */
        public double GetMissionSample(int channel, int sampleNum)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            if (sampleNum >= sampleCount || sampleNum < 0)
                throw new ArgumentOutOfRangeException("Invalid sample number");

            double val = 0;
            if (channel == TEMPERATURE_CHANNEL)
            {
                val = DecodeTemperature(
                         temperatureLog, sampleNum * temperatureBytes, temperatureBytes, true);
                if (useTempCalibrationRegisters)
                {
                    double valsq = val * val;
                    double error
                       = tempCoeffA * valsq + tempCoeffB * val + tempCoeffC;
                    val = val - error;
                }
            }
            else if (channel == DATA_CHANNEL)
            {
                if (hasHumiditySensor && !adForceResults)
                {
                    val = DecodeHumidity(dataLog, sampleNum * dataBytes, dataBytes, true);

                    if (useTemperatureCompensation)
                    {
                        double T;
                        if (!overrideTemperatureLog
                           && GetMissionSampleCount(TEMPERATURE_CHANNEL) > 0)
                            T = GetMissionSample(TEMPERATURE_CHANNEL, sampleNum);
                        else
                            T = (double)defaultTempCompensationValue;

                        double Vout = val * 0.0307 + 0.958;
                        /*if(T<=15)
                           val = (Vout - (0.8767-0.0035*T + 0.000043*T*T))/
                              (0.035 + 0.000067*T - 0.000002*T*T);
                        else
                           val = (Vout - (0.8767-0.0035*T + 0.000043*T*T))/
                              (0.0317 + 0.000067*T - 0.000002*T*T);*/
                        val = (Vout - (0.9858 - 0.0035 * T + 0.00013 * T * T)) /
                           (0.031 + 0.000097 * T - 0.000002 * T * T);
                    }

                    if (useHumdCalibrationRegisters)
                    {
                        double valsq = val * val;
                        double error
                           = humdCoeffA * valsq + humdCoeffB * val + humdCoeffC;
                        val = val - error;
                    }
                }
                else
                {
                    val = GetADVoltage(dataLog, sampleNum * dataBytes, dataBytes, true);
                }
            }
            else
                throw new ArgumentOutOfRangeException("Invalid Channel");

            return val;
        }


        /**
         * Returns the sample as an integer value.  This value is not converted to
         * degrees Celsius for temperature or to percent RH for Humidity.  It is
         * simply the 8 or 16 bits of digital data written in the mission log for
         * this sample entry.  It is up to the user to mask off the unused bits
         * and convert this value to it's proper units.  This method is primarily
         * for users of the DS2422 who are using an input device which is not an
         * A-D or have an A-D wholly dissimilar to the one specified in the
         * datasheet.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @param sampleNum the sample number to return, between <code>0</code> and
         *        <code>(getMissionSampleCount(channel)-1)</code>
         * @return the sample's timestamp, in milliseconds
         */
        public int GetMissionSampleAsInteger(int channel, int sampleNum)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            if (sampleNum >= sampleCount || sampleNum < 0)
                throw new ArgumentOutOfRangeException("Invalid sample number");

            int i = 0;
            if (channel == TEMPERATURE_CHANNEL)
            {
                if (temperatureBytes == 2)
                {
                    i = (temperatureLog[sampleNum * temperatureBytes] << 8)
                      | (temperatureLog[sampleNum * temperatureBytes + 1]);
                }
                else
                {
                    i = temperatureLog[sampleNum * temperatureBytes];
                }
            }
            else if (channel == DATA_CHANNEL)
            {
                if (dataBytes == 2)
                {
                    i = (dataLog[sampleNum * dataBytes] << 8)
                      | (dataLog[sampleNum * dataBytes + 1]);
                }
                else
                {
                    i = dataLog[sampleNum * dataBytes];
                }
            }
            else
                throw new ArgumentOutOfRangeException("Invalid Channel");

            return i;
        }


        /**
         * Returns the time, in milliseconds, that each sample was taken by the
         * current mission.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @param sampleNum the sample number to return, between <code>0</code> and
         *        <code>(getMissionSampleCount(channel)-1)</code>
         * @return the sample's timestamp, in milliseconds
         */
        public long GetMissionSampleTimeStamp(int channel, int sampleNum)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            return (timeOffset + sampleNum * sampleRate) * 1000 + missionTimeStamp;
        }

        /**
         * Returns <code>true</code> if a mission is currently running.
         * @return <code>true</code> if a mission is currently running.
         */
        public Boolean IsMissionRunning()
        {
            return GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MISSION_IN_PROGRESS);
        }

        /**
         * Returns <code>true</code> if a rollover is enabled.
         * @return <code>true</code> if a rollover is enabled.
         */
        public Boolean IsMissionRolloverEnabled()
        {
            if (isMissionLoaded)
                return GetFlag(MISSION_CONTROL_REGISTER, MCR_BIT_ENABLE_ROLLOVER,
                               missionRegister);
            else
                return GetFlag(MISSION_CONTROL_REGISTER, MCR_BIT_ENABLE_ROLLOVER);
        }

        /**
         * Returns <code>true</code> if a mission has rolled over.
         * @return <code>true</code> if a mission has rolled over.
         */
        public Boolean HasMissionRolloverOccurred()
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            return this.rolledOver;
        }

        /**
         * Clears the mission results and erases the log memory from this
         * missioning device.
         */
        public void ClearMissionResults()
        {
            ClearMemory();
            isMissionLoaded = false;
        }

        /**
         * Returns the time, in milliseconds, that the mission began.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @return time, in milliseconds, that the mission began
         */
        public long GetMissionTimeStamp(int channel)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            return this.missionTimeStamp;
        }

        /**
         * Returns the amount of time, in milliseconds, before the first sample
         * occurred.  If rollover disabled, or datalog didn'thread fill up, this
         * will be 0.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @return time, in milliseconds, before first sample occurred
         */
        public long GetFirstSampleOffset(int channel)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            return timeOffset * 1000;
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // - Mission Resolutions
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /**
         * Returns all available resolutions for the specified mission channel.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @return all available resolutions for the specified mission channel.
         */
        public double[] GetMissionResolutions(int channel)
        {
            if (channel == TEMPERATURE_CHANNEL)
                return new double[] { temperatureResolutions[0], temperatureResolutions[1] };
            else if (channel == DATA_CHANNEL)
                if (hasHumiditySensor && !adForceResults)
                    return new double[] { humidityResolutions[0], humidityResolutions[1] };
                else
                    return new double[] { dataResolutions[0], dataResolutions[1] };
            else
                throw new ArgumentOutOfRangeException("Invalid Channel");
        }

        /**
         * Returns the currently selected resolution for the specified
         * channel.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @return the currently selected resolution for the specified channel.
         */
        public double GetMissionResolution(int channel)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            double resolution = 0;
            if (channel == TEMPERATURE_CHANNEL)
            {
                if (GetFlag(MISSION_CONTROL_REGISTER,
                           MCR_BIT_TEMPERATURE_RESOLUTION,
                           missionRegister))
                    resolution = temperatureResolutions[1];
                else
                    resolution = temperatureResolutions[0];
            }
            else if (channel == DATA_CHANNEL)
            {
                if (GetFlag(MISSION_CONTROL_REGISTER,
                           MCR_BIT_DATA_RESOLUTION,
                           missionRegister))
                    if (hasHumiditySensor && !adForceResults)
                        resolution = humidityResolutions[1];
                    else
                        resolution = dataResolutions[1];
                else
                    if (hasHumiditySensor && !adForceResults)
                        resolution = humidityResolutions[0];
                    else
                        resolution = dataResolutions[0];
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Channel");
            }
            return resolution;
        }

        /**
         * Sets the selected resolution for the specified channel.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @param resolution the new resolution for the specified channel.
         */
        public void SetMissionResolution(int channel, double resolution)
        {
            if (!isMissionLoaded)
                missionRegister = ReadDevice();

            if (channel == TEMPERATURE_CHANNEL)
            {
                SetFlag(MISSION_CONTROL_REGISTER,
                        MCR_BIT_TEMPERATURE_RESOLUTION, resolution == temperatureResolutions[1],
                        missionRegister);
            }
            else if (channel == DATA_CHANNEL)
            {
                if (hasHumiditySensor && !adForceResults)
                    SetFlag(MISSION_CONTROL_REGISTER,
                            MCR_BIT_DATA_RESOLUTION, resolution == humidityResolutions[1],
                            missionRegister);
                else
                    SetFlag(MISSION_CONTROL_REGISTER,
                            MCR_BIT_DATA_RESOLUTION, resolution == dataResolutions[1],
                            missionRegister);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Channel");
            }

            WriteDevice(missionRegister);
        }

        /**
         * Enables/Disables the usage of calibration registers.  Only
         * applies to the DS1922H configuration.  The calibration depends
         * on an average error at 3 known reference points.  This average
         * error is written to 3 registers on the DS1922.  The container
         * use these values to calibrate the recorded humidity values
         * and improve the accuracy of the device.  This method allows you
         * to turn off calibration so that you may download the actual
         * data recorded to the device's memory and perform a manual
         * calibration.
         *
         * @param use if <code>true</code>, all humidity values read from
         *        device will be calibrated.
         *
         */
        public void SetTemperatureCalibrationRegisterUsage(Boolean use)
        {
            this.useTempCalibrationRegisters = use;
        }

        /**
         * Enables/Disables the usage of the humidity calibration registers.
         * Only applies to the DS1922H configuration.  The calibration depends
         * on an average error at 3 known reference points.  This average
         * error is written to 3 registers on the DS1922.  The container
         * use these values to calibrate the recorded humidity values
         * and improve the accuracy of the device.  This method allows you
         * to turn off calibration so that you may download the actual
         * data recorded to the device's memory and perform a manual
         * calibration.
         *
         * @param use if <code>true</code>, all humidity values read from
         *        device will be calibrated.
         */
        public void SetHumidityCalibrationRegisterUsage(Boolean use)
        {
            this.useHumdCalibrationRegisters = use;
        }

        /**
         * Enables/Disables the usage of temperature compensation.  Only
         * applies to the DS1922H configuration.  The temperature
         * compensation adjusts the humidity values based on the known
         * effects of temperature on the humidity sensor.  If this is
         * a joint humidity and temperature mission, the temperature
         * values used could (should?) come from the temperature log
         * itself.  If, however, there is no temperature log the
         * default temperature value can be set for the mission using
         * the <code>setDefaultTemperatureCompensationValue</code> method.
         *
         * @param use if <code>true</code>, all humidity values read from
         *        device will be compensated for temperature.
         *
         * @see #setDefaultTemperatureCompensationValue
         */
        public void SetTemperatureCompensationUsage(Boolean use)
        {
            this.useTemperatureCompensation = use;
        }

        /**
         * Sets the default temperature value for temperature compensation.  This
         * value will be used if there is no temperature log data or if the
         * <code>override</code> parameter is true.
         *
         * @param temperatureValue the default temperature value for temperature
         *        compensation.
         * @param override if <code>true</code>, the default temperature value
         *        will always be used (instead of the temperature log data).
         *
         * @see #setDefaultTemperatureCompensationValue
         */
        public void SetDefaultTemperatureCompensationValue(double temperatureValue, Boolean override_)
        {
            this.defaultTempCompensationValue = temperatureValue;
            this.overrideTemperatureLog = override_;
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // - Mission Alarms
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /**
         * Indicates whether or not the specified channel of this missioning device
         * has mission alarm capabilities.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @return true if the device has mission alarms for the specified channel.
         */
        public Boolean HasMissionAlarms(int channel)
        {
            return true;
        }

        /**
         * Returns true if the specified channel's alarm value of the specified
         * type has been triggered during the mission.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @param alarmType valid value: <code>ALARM_HIGH</code> or
         *                   <code>ALARM_LOW</code>
         * @return true if the alarm was triggered.
         */
        public Boolean HasMissionAlarmed(int channel, int alarmType)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            if (channel == TEMPERATURE_CHANNEL)
            {
                if (alarmType == MissionContainerConstants.ALARM_HIGH)
                {
                    return GetFlag(ALARM_STATUS_REGISTER,
                                   ASR_BIT_TEMPERATURE_HIGH_ALARM,
                                   missionRegister);
                }
                else
                {
                    return GetFlag(ALARM_STATUS_REGISTER,
                                   ASR_BIT_TEMPERATURE_LOW_ALARM,
                                   missionRegister);
                }
            }
            else if (channel == DATA_CHANNEL)
            {
                if (alarmType == MissionContainerConstants.ALARM_HIGH)
                {
                    return GetFlag(ALARM_STATUS_REGISTER,
                                   ASR_BIT_DATA_HIGH_ALARM,
                                   missionRegister);
                }
                else
                {
                    return GetFlag(ALARM_STATUS_REGISTER,
                                   ASR_BIT_DATA_LOW_ALARM,
                                   missionRegister);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Channel");
            }
        }

        /**
         * Returns true if the alarm of the specified type has been enabled for
         * the specified channel.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @param  alarmType valid value: <code>ALARM_HIGH</code> or
         *                   <code>ALARM_LOW</code>
         * @return true if the alarm of the specified type has been enabled for
         *         the specified channel.
         */
        public Boolean GetMissionAlarmEnable(int channel, int alarmType)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            if (channel == TEMPERATURE_CHANNEL)
            {
                if (alarmType == MissionContainerConstants.ALARM_HIGH)
                {
                    return GetFlag(TEMPERATURE_CONTROL_REGISTER,
                                   TCR_BIT_ENABLE_TEMPERATURE_HIGH_ALARM,
                                   missionRegister);
                }
                else
                {
                    return GetFlag(TEMPERATURE_CONTROL_REGISTER,
                                   TCR_BIT_ENABLE_TEMPERATURE_LOW_ALARM,
                                   missionRegister);
                }
            }
            else if (channel == DATA_CHANNEL)
            {
                if (alarmType == MissionContainerConstants.ALARM_HIGH)
                {
                    return GetFlag(DATA_CONTROL_REGISTER,
                                   DCR_BIT_ENABLE_DATA_HIGH_ALARM,
                                   missionRegister);
                }
                else
                {
                    return GetFlag(DATA_CONTROL_REGISTER,
                                   DCR_BIT_ENABLE_DATA_LOW_ALARM,
                                   missionRegister);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Channel");
            }
        }

        /**
         * Enables/disables the alarm of the specified type for the specified channel
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @param alarmType valid value: <code>ALARM_HIGH</code> or
         *                   <code>ALARM_LOW</code>
         * @param enable if true, alarm is enabled.
         */
        public void SetMissionAlarmEnable(int channel, int alarmType, Boolean enable)
        {
            if (!isMissionLoaded)
                missionRegister = ReadDevice();

            if (channel == TEMPERATURE_CHANNEL)
            {
                if (alarmType == MissionContainerConstants.ALARM_HIGH)
                {
                    SetFlag(TEMPERATURE_CONTROL_REGISTER,
                            TCR_BIT_ENABLE_TEMPERATURE_HIGH_ALARM,
                            enable,
                            missionRegister);
                }
                else
                {
                    SetFlag(TEMPERATURE_CONTROL_REGISTER,
                            TCR_BIT_ENABLE_TEMPERATURE_LOW_ALARM,
                            enable,
                            missionRegister);
                }
            }
            else if (channel == DATA_CHANNEL)
            {
                if (alarmType == MissionContainerConstants.ALARM_HIGH)
                {
                    SetFlag(DATA_CONTROL_REGISTER,
                            DCR_BIT_ENABLE_DATA_HIGH_ALARM,
                            enable,
                            missionRegister);
                }
                else
                {
                    SetFlag(DATA_CONTROL_REGISTER,
                            DCR_BIT_ENABLE_DATA_LOW_ALARM,
                            enable,
                            missionRegister);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Channel");
            }
            WriteDevice(missionRegister);
        }

        /**
         * Returns the threshold value which will trigger the alarm of the
         * specified type on the specified channel.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @param alarmType valid value: <code>ALARM_HIGH</code> or
         *                   <code>ALARM_LOW</code>
         * @return the threshold value which will trigger the alarm
         */
        public double GetMissionAlarm(int channel, int alarmType)
        {
            if (!isMissionLoaded)
                throw new OneWireException("Must load mission results first.");

            double th = 0;
            if (channel == TEMPERATURE_CHANNEL)
            {
                if (alarmType == MissionContainerConstants.ALARM_HIGH)
                {
                    th = DecodeTemperature(missionRegister,
                                           TEMPERATURE_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
                }
                else
                {
                    th = DecodeTemperature(missionRegister,
                                           TEMPERATURE_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
                }
            }
            else if (channel == DATA_CHANNEL)
            {
                if (alarmType == MissionContainerConstants.ALARM_HIGH)
                {
                    if (hasHumiditySensor && !adForceResults)
                        th = DecodeHumidity(missionRegister, DATA_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
                    else
                        th = GetADVoltage(missionRegister, DATA_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
                }
                else
                {
                    if (hasHumiditySensor && !adForceResults)
                        th = DecodeHumidity(missionRegister, DATA_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
                    else
                        th = GetADVoltage(missionRegister, DATA_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
                }

                if (hasHumiditySensor && useHumdCalibrationRegisters && !adForceResults)
                {
                    double thsq = th * th;
                    double error
                       = humdCoeffA * thsq + humdCoeffB * th + humdCoeffC;
                    th = th - error;
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Channel");
            }
            return th;
        }

        /**
         * Sets the threshold value which will trigger the alarm of the
         * specified type on the specified channel.
         *
         * @param channel the mission channel, between <code>0</code> and
         *        <code>(getNumberOfMissionChannels()-1)</code>
         * @param alarmType valid value: <code>ALARM_HIGH</code> or
         *                   <code>ALARM_LOW</code>
         * @param threshold the threshold value which will trigger the alarm
         */
        public void SetMissionAlarm(int channel, int alarmType, double threshold)
        {
            if (!isMissionLoaded)
                missionRegister = ReadDevice();

            if (channel == TEMPERATURE_CHANNEL)
            {
                if (alarmType == MissionContainerConstants.ALARM_HIGH)
                {
                    EncodeTemperature(threshold, missionRegister,
                                      TEMPERATURE_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
                }
                else
                {
                    EncodeTemperature(threshold, missionRegister,
                                      TEMPERATURE_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
                }
            }
            else if (channel == DATA_CHANNEL)
            {
                if (useHumdCalibrationRegisters)
                    threshold =
                       ((1 - humdCoeffB)
                         - Math.Sqrt(((humdCoeffB - 1) * (humdCoeffB - 1))
                         - 4 * humdCoeffA * (humdCoeffC + threshold))
                       ) / (2 * humdCoeffA);

                if (alarmType == MissionContainerConstants.ALARM_HIGH)
                {
                    if (hasHumiditySensor && !adForceResults)
                        EncodeHumidity(threshold, missionRegister, DATA_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
                    else
                        SetADVoltage(threshold, missionRegister, DATA_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
                }
                else
                {
                    if (hasHumiditySensor && !adForceResults)
                        EncodeHumidity(threshold, missionRegister, DATA_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
                    else
                        SetADVoltage(threshold, missionRegister, DATA_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Channel");
            }
            WriteDevice(missionRegister);
        }

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Temperature Interface Functions
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /**
         * Checks to see if this temperature measuring device has high/low
         * trip alarms.
         *
         * @return <code>true</code> if this <code>TemperatureContainer</code>
         *         has high/low trip alarms
         *
         * @see    #getTemperatureAlarm
         * @see    #setTemperatureAlarm
         */
        public Boolean HasTemperatureAlarms()
        {
            return true;
        }

        /**
         * Checks to see if this device has selectable temperature resolution.
         *
         * @return <code>true</code> if this <code>TemperatureContainer</code>
         *         has selectable temperature resolution
         *
         * @see    #getTemperatureResolution
         * @see    #getTemperatureResolutions
         * @see    #setTemperatureResolution
         */
        public Boolean HasSelectableTemperatureResolution()
        {
            return false;
        }

        /**
         * Get an array of available temperature resolutions in Celsius.
         *
         * @return byte array of available temperature resolutions in Celsius with
         *         minimum resolution as the first element and maximum resolution
         *         as the last element
         *
         * @see    #hasSelectableTemperatureResolution
         * @see    #getTemperatureResolution
         * @see    #setTemperatureResolution
         */
        public double[] GetTemperatureResolutions()
        {
            double[] d = new double[1];

            d[0] = temperatureResolutions[1];

            return d;
        }

        /**
         * Gets the temperature alarm resolution in Celsius.
         *
         * @return temperature alarm resolution in Celsius for this 1-wire device
         *
         * @see    #hasTemperatureAlarms
         * @see    #getTemperatureAlarm
         * @see    #setTemperatureAlarm
         *
         */
        public double GetTemperatureAlarmResolution()
        {
            return temperatureResolutions[0];
        }

        /**
         * Gets the maximum temperature in Celsius.
         *
         * @return maximum temperature in Celsius for this 1-wire device
         *
         * @see #getMinTemperature()
         */
        public double GetMaxTemperature()
        {
            return temperatureRangeLow + temperatureRangeWidth;
        }

        /**
         * Gets the minimum temperature in Celsius.
         *
         * @return minimum temperature in Celsius for this 1-wire device
         *
         * @see #getMaxTemperature()
         */
        public double GetMinTemperature()
        {
            return temperatureRangeLow;
        }

        /**
         * Performs a temperature conversion.  Use the <code>state</code>
         * information to calculate the conversion time.
         *
         * @param  state byte array with device state information
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         *         In the case of the DS1922 Thermocron, this could also be due to a
         *         currently running mission.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         */
        public void DoTemperatureConvert(byte[] state)
        {
            /* check for mission in progress */
            if (GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MISSION_IN_PROGRESS, state))
                throw new OneWireIOException("OneWireContainer41-Cant force "
                                             + "temperature read during a mission.");
            /* check that the RTC is running */
            if (!GetFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_OSCILLATOR, state))
                throw new OneWireIOException("OneWireContainer41-Cant force "
                   + "temperature conversion if the oscillator is not enabled");

            /* get the temperature*/
            if (doSpeedEnable)
                DoSpeed();   //we aren'thread worried about how long this takes...we're sleeping for 750 ms!

            adapter.Reset();

            if (adapter.SelectDevice(address))
            {
                // perform the temperature conversion
                byte[] buffer = new byte[] { FORCED_CONVERSION, (byte)0xFF };
                adapter.DataBlock(buffer, 0, 2);

                System.Threading.Thread.Sleep(750);

                // grab the temperature
                state[LAST_TEMPERATURE_CONVERSION_LSB & 0x3F]
                   = ReadByte(LAST_TEMPERATURE_CONVERSION_LSB);
                state[LAST_TEMPERATURE_CONVERSION_MSB & 0x3F]
                   = ReadByte(LAST_TEMPERATURE_CONVERSION_MSB);
            }
            else
                throw new OneWireException("OneWireContainer41-Device not found!");
        }

        /**
         * Gets the temperature value in Celsius from the <code>state</code>
         * data retrieved from the <code>readDevice()</code> method.
         *
         * @param  state byte array with device state information
         *
         * @return temperature in Celsius from the last
         *                     <code>doTemperatureConvert()</code>
         */
        public double GetTemperature(byte[] state)
        {
            double val = DecodeTemperature(state, LAST_TEMPERATURE_CONVERSION_LSB & 0x3F, 2, false);
            if (useTempCalibrationRegisters)
            {
                double valsq = val * val;
                double error
                   = tempCoeffA * valsq + tempCoeffB * val + tempCoeffC;
                val = val - error;
            }
            return val;
        }

        /**
         * Gets the specified temperature alarm value in Celsius from the
         * <code>state</code> data retrieved from the
         * <code>readDevice()</code> method.
         *
         * @param  alarmType valid value: <code>ALARM_HIGH</code> or
         *                   <code>ALARM_LOW</code>
         * @param  state     byte array with device state information
         *
         * @return temperature alarm trip values in Celsius for this 1-wire device
         *
         * @see    #hasTemperatureAlarms
         * @see    #setTemperatureAlarm
         */
        public double GetTemperatureAlarm(int alarmType, byte[] state)
        {
            double th = 0;
            if (alarmType == TemperatureContainerConsts.ALARM_HIGH)
                th = DecodeTemperature(state,
                        TEMPERATURE_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
            else
                th = DecodeTemperature(state,
                        TEMPERATURE_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
            if (useTempCalibrationRegisters)
            {
                double thsq = th * th;
                double error
                   = tempCoeffA * thsq + tempCoeffB * th + tempCoeffC;
                th = th - error;
            }
            return th;
        }

        /**
         * Gets the current temperature resolution in Celsius from the
         * <code>state</code> data retrieved from the <code>readDevice()</code>
         * method.
         *
         * @param  state byte array with device state information
         *
         * @return temperature resolution in Celsius for this 1-wire device
         *
         * @see    #hasSelectableTemperatureResolution
         * @see    #getTemperatureResolutions
         * @see    #setTemperatureResolution
         */
        public double GetTemperatureResolution(byte[] state)
        {
            return temperatureResolutions[1];
        }

        /**
         * Sets the temperature alarm value in Celsius in the provided
         * <code>state</code> data.
         * Use the method <code>writeDevice()</code> with
         * this data to finalize the change to the device.
         *
         * @param  alarmType  valid value: <code>ALARM_HIGH</code> or
         *                    <code>ALARM_LOW</code>
         * @param  alarmValue alarm trip value in Celsius
         * @param  state      byte array with device state information
         *
         * @see    #hasTemperatureAlarms
         * @see    #getTemperatureAlarm
         */
        public void SetTemperatureAlarm(int alarmType, double alarmValue,
                                         byte[] state)
        {
            if (useTempCalibrationRegisters)
            {
                alarmValue =
                   ((1 - tempCoeffB)
                     - Math.Sqrt(((tempCoeffB - 1) * (tempCoeffB - 1))
                     - 4 * tempCoeffA * (tempCoeffC + alarmValue))
                   ) / (2 * tempCoeffA);
            }

            if (alarmType == TemperatureContainerConsts.ALARM_HIGH)
            {
                EncodeTemperature(alarmValue, state,
                   TEMPERATURE_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
            }
            else
            {
                EncodeTemperature(alarmValue, state,
                   TEMPERATURE_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
            }
        }


        /**
         * Sets the current temperature resolution in Celsius in the provided
         * <code>state</code> data.   Use the method <code>writeDevice()</code>
         * with this data to finalize the change to the device.
         *
         * @param  resolution temperature resolution in Celsius
         * @param  state      byte array with device state information
         *
         * @throws OneWireException if the device does not support
         * selectable temperature resolution
         *
         * @see    #hasSelectableTemperatureResolution
         * @see    #getTemperatureResolution
         * @see    #getTemperatureResolutions
         */
        public void SetTemperatureResolution(double resolution, byte[] state)
        {
            throw new OneWireException("Selectable Temperature Resolution Not Supported");
        }

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Humidity Interface Functions
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /**
         * Checks to see if humidity value given is a 'relative' humidity value.
         *
         * @return <code>true</code> if this <code>HumidityContainer</code>
         *         provides a relative humidity reading
         *
         * @see    #getHumidityResolution
         * @see    #getHumidityResolutions
         * @see    #setHumidityResolution
         */
        public Boolean IsRelative()
        {
            return true;
        }

        /**
         * Checks to see if this Humidity measuring device has high/low
         * trip alarms.
         *
         * @return <code>true</code> if this <code>HumidityContainer</code>
         *         has high/low trip alarms
         *
         * @see    #getHumidityAlarm
         * @see    #setHumidityAlarm
         */
        public Boolean HasHumidityAlarms()
        {
            return true;
        }

        /**
         * Checks to see if this device has selectable Humidity resolution.
         *
         * @return <code>true</code> if this <code>HumidityContainer</code>
         *         has selectable Humidity resolution
         *
         * @see    #getHumidityResolution
         * @see    #getHumidityResolutions
         * @see    #setHumidityResolution
         */
        public Boolean HasSelectableHumidityResolution()
        {
            return false;
        }

        /**
         * Get an array of available Humidity resolutions in percent humidity (0 to 100).
         *
         * @return byte array of available Humidity resolutions in percent with
         *         minimum resolution as the first element and maximum resolution
         *         as the last element.
         *
         * @see    #hasSelectableHumidityResolution
         * @see    #getHumidityResolution
         * @see    #setHumidityResolution
         */
        public double[] GetHumidityResolutions()
        {
            double[] d = new double[1];

            d[0] = humidityResolutions[1];

            return d;
        }

        /**
         * Gets the Humidity alarm resolution in percent.
         *
         * @return Humidity alarm resolution in percent for this 1-wire device
         *
         * @throws OneWireException         Device does not support Humidity
         *                                  alarms
         *
         * @see    #hasHumidityAlarms
         * @see    #getHumidityAlarm
         * @see    #setHumidityAlarm
         *
         */
        public double GetHumidityAlarmResolution()
        {
            return humidityResolutions[0];
        }

        //--------
        //-------- Humidity I/O Methods
        //--------

        /**
         * Performs a Humidity conversion.
         *
         * @param  state byte array with device state information
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         */
        public void DoHumidityConvert(byte[] state)
        {
            /* check for mission in progress */
            if (GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MISSION_IN_PROGRESS, state))
                throw new OneWireIOException("OneWireContainer41-Cant force "
                                             + "Humidity read during a mission.");

            /* check that the RTC is running */
            if (!GetFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_OSCILLATOR, state))
                throw new OneWireIOException("OneWireContainer41-Cant force "
                   + "Humidity conversion if the oscillator is not enabled");

            /* get the temperature*/
            if (doSpeedEnable)
                DoSpeed();   //we aren'thread worried about how long this takes...we're sleeping for 750 ms!

            adapter.Reset();

            if (adapter.SelectDevice(address))
            {
                // perform the temperature conversion
                byte[] buffer = new byte[] { FORCED_CONVERSION, (byte)0xFF };
                adapter.DataBlock(buffer, 0, 2);

                System.Threading.Thread.Sleep(750);

                // grab the temperature
                state[LAST_DATA_CONVERSION_LSB & 0x3F]
                   = ReadByte(LAST_DATA_CONVERSION_LSB);
                state[LAST_DATA_CONVERSION_MSB & 0x3F]
                   = ReadByte(LAST_DATA_CONVERSION_MSB);
            }
            else
                throw new OneWireException("OneWireContainer41-Device not found!");
        }


        //--------
        //-------- Humidity 'get' Methods
        //--------

        /**
         * Gets the humidity expressed as a percent value (0.0 to 100.0) of humidity.
         *
         * @param  state byte array with device state information
         * @return humidity expressed as a percent
         *
         * @see    #hasSelectableHumidityResolution
         * @see    #getHumidityResolution
         * @see    #setHumidityResolution
         */
        public double GetHumidity(byte[] state)
        {
            double val = DecodeHumidity(state, LAST_DATA_CONVERSION_LSB & 0x3F, 2, false);
            if (useHumdCalibrationRegisters)
            {
                double valsq = val * val;
                double error
                   = humdCoeffA * valsq + humdCoeffB * val + humdCoeffC;
                val = val - error;
            }
            return val;
        }

        /**
         * Gets the current Humidity resolution in percent from the
         * <code>state</code> data retrieved from the <code>readDevice()</code>
         * method.
         *
         * @param  state byte array with device state information
         *
         * @return Humidity resolution in percent for this 1-wire device
         *
         * @see    #hasSelectableHumidityResolution
         * @see    #getHumidityResolutions
         * @see    #setHumidityResolution
         */
        public double GetHumidityResolution(byte[] state)
        {
            return humidityResolutions[1];
        }

        /**
         * Gets the specified Humidity alarm value in percent from the
         * <code>state</code> data retrieved from the
         * <code>readDevice()</code> method.
         *
         * @param  alarmType valid value: <code>ALARM_HIGH</code> or
         *                   <code>ALARM_LOW</code>
         * @param  state     byte array with device state information
         *
         * @return Humidity alarm trip values in percent for this 1-wire device
         *
         * @throws OneWireException         Device does not support Humidity
         *                                  alarms
         *
         * @see    #hasHumidityAlarms
         * @see    #setHumidityAlarm
         */
        public double GetHumidityAlarm(int alarmType, byte[] state)
        {
            double th;
            if (alarmType == HumidityContainerConstants.ALARM_HIGH)
                th = DecodeHumidity(state, DATA_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
            else
                th = DecodeHumidity(state, DATA_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
            // calibrate the alarm value before returning
            if (useHumdCalibrationRegisters)
            {
                double thsq = th * th;
                double error
                   = humdCoeffA * thsq + humdCoeffB * th + humdCoeffC;
                th = th - error;
            }
            return th;
        }

        //--------
        //-------- Humidity 'set' Methods
        //--------

        /**
         * Sets the Humidity alarm value in percent in the provided
         * <code>state</code> data.
         * Use the method <code>writeDevice()</code> with
         * this data to finalize the change to the device.
         *
         * @param  alarmType  valid value: <code>ALARM_HIGH</code> or
         *                    <code>ALARM_LOW</code>
         * @param  alarmValue alarm trip value in percent
         * @param  state      byte array with device state information
         *
         * @throws OneWireException         Device does not support Humidity
         *                                  alarms
         *
         * @see    #hasHumidityAlarms
         * @see    #getHumidityAlarm
         */
        public void SetHumidityAlarm(int alarmType, double alarmValue,
                                         byte[] state)
        {
            // uncalibrate the alarm value before writing
            if (useHumdCalibrationRegisters)
            {
                alarmValue =
                   ((1 - humdCoeffB)
                     - Math.Sqrt(((humdCoeffB - 1) * (humdCoeffB - 1))
                     - 4 * humdCoeffA * (humdCoeffC + alarmValue))
                   ) / (2 * humdCoeffA);
            }

            if (alarmType == HumidityContainerConstants.ALARM_HIGH)
                EncodeHumidity(alarmValue, state, DATA_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
            else
                EncodeHumidity(alarmValue, state, DATA_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
        }

        /**
         * Sets the current Humidity resolution in percent in the provided
         * <code>state</code> data.   Use the method <code>writeDevice()</code>
         * with this data to finalize the change to the device.
         *
         * @param  resolution Humidity resolution in percent
         * @param  state      byte array with device state information
         *
         * @throws OneWireException         Device does not support selectable
         *                                  Humidity resolution
         *
         * @see    #hasSelectableHumidityResolution
         * @see    #getHumidityResolution
         * @see    #getHumidityResolutions
         */
        public void SetHumidityResolution(double resolution, byte[] state)
        {
            throw new OneWireException("Selectable Humidity Resolution Not Supported");
        }

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // A-to-D Interface Functions
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************
        /**
         * Gets the number of channels supported by this A/D.
         * Channel specific methods will use a channel number specified
         * by an integer from [0 to (<code>getNumberADChannels()</code> - 1)].
         *
         * @return the number of channels
         */
        public int GetNumberADChannels()
        {
            return 1;
        }

        /**
         * Checks to see if this A/D measuring device has high/low
         * alarms.
         *
         * @return true if this device has high/low trip alarms
         */
        public Boolean HasADAlarms()
        {
            return true;
        }

        /**
         * Gets an array of available ranges for the specified
         * A/D channel.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         *
         * @return array indicating the available ranges starting
         *         from the largest range to the smallest range
         *
         * @see #getNumberADChannels()
         */
        public double[] GetADRanges(int channel)
        {
            return new double[] { 127 };
        }

        /**
         * Gets an array of available resolutions based
         * on the specified range on the specified A/D channel.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param range A/D range setting from the <code>getADRanges(int)</code> method
         *
         * @return array indicating the available resolutions on this
         *         <code>channel</code> for this <code>range</code>
         *
         * @see #getNumberADChannels()
         * @see #getADRanges(int)
         */
        public double[] GetADResolutions(int channel, double range)
        {
            return new double[] { dataResolutions[1] };
        }

        /**
         * Checks to see if this A/D supports doing multiple voltage
         * conversions at the same time.
         *
         * @return true if the device can do multi-channel voltage reads
         *
         * @see #doADConvert(boolean[],byte[])
         */
        public Boolean CanADMultiChannelRead()
        {
            return true;
        }

        /**
         * Performs a voltage conversion on one specified channel.
         * Use the method <code>getADVoltage(int,byte[])</code> to read
         * the result of this conversion, using the same <code>channel</code>
         * argument as this method uses.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         no 1-Wire device present.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         *         This is usually a recoverable error.
         * @throws OneWireException on a communication or setup error with the
         *         1-Wire adapter.  This is usually a non-recoverable error.
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #getADVoltage(int,byte[])
         */
        public void DoADConvert(int channel, byte[] state)
        {
            /* check for mission in progress */
            if (GetFlag(GENERAL_STATUS_REGISTER, GSR_BIT_MISSION_IN_PROGRESS, state))
                throw new OneWireIOException("OneWireContainer41-Cant force "
                                             + "temperature read during a mission.");

            /* check that the RTC is running */
            if (!GetFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_OSCILLATOR, state))
                throw new OneWireIOException("OneWireContainer41-Cant force "
                   + "A/D conversion if the oscillator is not enabled");

            /* get the temperature*/
            if (doSpeedEnable)
                DoSpeed();   //we aren'thread worried about how long this takes...we're sleeping for 750 ms!

            adapter.Reset();

            if (adapter.SelectDevice(address))
            {
                // perform the conversion
                byte[] buffer = new byte[] { FORCED_CONVERSION, (byte)0xFF };
                adapter.DataBlock(buffer, 0, 2);

                System.Threading.Thread.Sleep(750);

                // grab the data
                state[LAST_DATA_CONVERSION_LSB & 0x3F]
                   = ReadByte(LAST_DATA_CONVERSION_LSB);
                state[LAST_DATA_CONVERSION_MSB & 0x3F]
                   = ReadByte(LAST_DATA_CONVERSION_MSB);
            }
            else
                throw new OneWireException("OneWireContainer41-Device not found!");
        }

        /**
         * Performs voltage conversion on one or more specified
         * channels.  The method <code>getADVoltage(byte[])</code> can be used to read the result
         * of the conversion(s). This A/D must support multi-channel read,
         * reported by <code>canADMultiChannelRead()</code>, if more then 1 channel is specified.
         *
         * @param doConvert array of size <code>getNumberADChannels()</code> representing
         *                  which channels should perform conversions
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         no 1-Wire device present.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         *         This is usually a recoverable error.
         * @throws OneWireException on a communication or setup error with the
         *         1-Wire adapter.  This is usually a non-recoverable error.
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #getADVoltage(byte[])
         * @see #canADMultiChannelRead()
         */
        public void DoADConvert(Boolean[] doConvert, byte[] state)
        {
            DoADConvert(DATA_CHANNEL, state);
        }

        /**
         * Reads the value of the voltages after a <code>doADConvert(boolean[],byte[])</code>
         * method call.  This A/D device must support multi-channel reading, reported by
         * <code>canADMultiChannelRead()</code>, if more than 1 channel conversion was attempted
         * by <code>doADConvert()</code>.
         *
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return array with the voltage values for all channels
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         no 1-Wire device present.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         *         This is usually a recoverable error.
         * @throws OneWireException on a communication or setup error with the
         *         1-Wire adapter.  This is usually a non-recoverable error.
         *
         * @see #doADConvert(boolean[],byte[])
         */
        public double[] GetADVoltage(byte[] state)
        {
            return new double[] { GetADVoltage(DATA_CHANNEL, state) };
        }

        /**
         * Reads the value of the voltages after a <code>doADConvert(int,byte[])</code>
         * method call.  If more than one channel has been read it is more
         * efficient to use the <code>getADVoltage(byte[])</code> method that
         * returns all channel voltage values.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return the voltage value for the specified channel
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         no 1-Wire device present.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         *         This is usually a recoverable error.
         * @throws OneWireException on a communication or setup error with the
         *         1-Wire adapter.  This is usually a non-recoverable error.
         *
         * @see #doADConvert(int,byte[])
         * @see #getADVoltage(byte[])
         */
        public double GetADVoltage(int channel, byte[] state)
        {
            return GetADVoltage(state, LAST_DATA_CONVERSION_LSB & 0x3F, 2, false);
        }

        /**
         * Reads the value of the specified A/D alarm on the specified channel.
         * Not all A/D devices have alarms.  Check to see if this device has
         * alarms first by calling the <code>hasADAlarms()</code> method.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param alarmType the desired alarm, <code>ALARM_HIGH</code> or <code>ALARM_LOW</code>
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return the alarm value in volts
         *
         * @throws OneWireException if this device does not have A/D alarms
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #hasADAlarms()
         */
        public double GetADAlarm(int channel, int alarmType, byte[] state)
        {
            double th = 0;
            if (alarmType == ADContainerConstants.ALARM_HIGH)
            {
                th = GetADVoltage(state,
                                  DATA_HIGH_ALARM_THRESHOLD & 0x3F, 1, false);
            }
            else
            {
                th = GetADVoltage(state,
                                  DATA_LOW_ALARM_THRESHOLD & 0x3F, 1, false);
            }
            return th;
        }

        /**
         * Checks to see if the specified alarm on the specified channel is enabled.
         * Not all A/D devices have alarms.  Check to see if this device has
         * alarms first by calling the <code>hasADAlarms()</code> method.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param alarmType the desired alarm, <code>ALARM_HIGH</code> or <code>ALARM_LOW</code>
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return true if specified alarm is enabled
         *
         * @throws OneWireException if this device does not have A/D alarms
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #hasADAlarms()
         */
        public Boolean GetADAlarmEnable(int channel, int alarmType, byte[] state)
        {
            Boolean b = false;
            if (alarmType == ADContainerConstants.ALARM_HIGH)
            {
                b = GetFlag(DATA_CONTROL_REGISTER,
                            DCR_BIT_ENABLE_DATA_HIGH_ALARM, state);
            }
            else
            {
                b = GetFlag(DATA_CONTROL_REGISTER,
                            DCR_BIT_ENABLE_DATA_LOW_ALARM, state);
            }
            return b;
        }

        /**
         * Checks the state of the specified alarm on the specified channel.
         * Not all A/D devices have alarms.  Check to see if this device has
         * alarms first by calling the <code>hasADAlarms()</code> method.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param alarmType the desired alarm, <code>ALARM_HIGH</code> or <code>ALARM_LOW</code>
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return true if specified alarm occurred
         *
         * @throws OneWireException if this device does not have A/D alarms
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #hasADAlarms()
         * @see #getADAlarmEnable(int,int,byte[])
         * @see #setADAlarmEnable(int,int,boolean,byte[])
         */
        public Boolean HasADAlarmed(int channel, int alarmType, byte[] state)
        {
            Boolean b = false;
            if (alarmType == ADContainerConstants.ALARM_HIGH)
            {
                b = GetFlag(ALARM_STATUS_REGISTER,
                            ASR_BIT_DATA_HIGH_ALARM, state);
            }
            else
            {
                b = GetFlag(ALARM_STATUS_REGISTER,
                            ASR_BIT_DATA_LOW_ALARM, state);
            }
            return b;
        }

        /**
         * Returns the currently selected resolution for the specified
         * channel.  This device may not have selectable resolutions,
         * though this method will return a valid value.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return the current resolution of <code>channel</code> in volts
         *
         * @see #getADResolutions(int,double)
         * @see #setADResolution(int,double,byte[])
         */
        public double GetADResolution(int channel, byte[] state)
        {
            return dataResolutions[1];
        }

        /**
         * Returns the currently selected range for the specified
         * channel.  This device may not have selectable ranges,
         * though this method will return a valid value.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return the input voltage range
         *
         * @see #getADRanges(int)
         * @see #setADRange(int,double,byte[])
         */
        public double GetADRange(int channel, byte[] state)
        {
            return 127;
        }

        /**
         * Sets the voltage value of the specified alarm on the specified channel.
         * The method <code>writeDevice()</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice()</code>.  Also note that
         * not all A/D devices have alarms.  Check to see if this device has
         * alarms first by calling the <code>hasADAlarms()</code> method.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param alarmType the desired alarm, <code>ALARM_HIGH</code> or <code>ALARM_LOW</code>
         * @param alarm new alarm value
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @throws OneWireException if this device does not have A/D alarms
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[])
         * @see #hasADAlarms()
         * @see #getADAlarm(int,int,byte[])
         * @see #getADAlarmEnable(int,int,byte[])
         * @see #setADAlarmEnable(int,int,boolean,byte[])
         * @see #hasADAlarmed(int,int,byte[])
         */
        public void SetADAlarm(int channel, int alarmType, double alarm,
                                byte[] state)
        {
            if (alarmType == ADContainerConstants.ALARM_HIGH)
            {
                SetADVoltage(alarm,
                             state, DATA_HIGH_ALARM_THRESHOLD & 0x3F, 1,
                             false);
            }
            else
            {
                SetADVoltage(alarm,
                             state, DATA_LOW_ALARM_THRESHOLD & 0x3F, 1,
                             false);
            }
        }

        /**
         * Enables or disables the specified alarm on the specified channel.
         * The method <code>writeDevice()</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice()</code>.  Also note that
         * not all A/D devices have alarms.  Check to see if this device has
         * alarms first by calling the <code>hasADAlarms()</code> method.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param alarmType the desired alarm, <code>ALARM_HIGH</code> or <code>ALARM_LOW</code>
         * @param alarmEnable true to enable the alarm, false to disable
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @throws OneWireException if this device does not have A/D alarms
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[])
         * @see #hasADAlarms()
         * @see #getADAlarm(int,int,byte[])
         * @see #setADAlarm(int,int,double,byte[])
         * @see #getADAlarmEnable(int,int,byte[])
         * @see #hasADAlarmed(int,int,byte[])
         */
        public void SetADAlarmEnable(int channel, int alarmType,
                                      Boolean alarmEnable, byte[] state)
        {
            if (alarmType == ADContainerConstants.ALARM_HIGH)
            {
                SetFlag(DATA_CONTROL_REGISTER,
                        DCR_BIT_ENABLE_DATA_HIGH_ALARM, alarmEnable, state);
            }
            else
            {
                SetFlag(DATA_CONTROL_REGISTER,
                        DCR_BIT_ENABLE_DATA_LOW_ALARM, alarmEnable, state);
            }
        }

        /**
         * Sets the conversion resolution value for the specified channel.
         * The method <code>writeDevice()</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice()</code>.  Also note that
         * not all A/D devices have alarms.  Check to see if this device has
         * alarms first by calling the <code>hasADAlarms()</code> method.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param resolution one of the resolutions returned by <code>getADResolutions(int,double)</code>
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @see #getADResolutions(int,double)
         * @see #getADResolution(int,byte[])
         *
         */
        public void SetADResolution(int channel, double resolution, byte[] state)
        {
            //throw new OneWireException("Selectable A-D Resolution Not Supported");
        }

        /**
         * Sets the input range for the specified channel.
         * The method <code>writeDevice()</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice()</code>.  Also note that
         * not all A/D devices have alarms.  Check to see if this device has
         * alarms first by calling the <code>hasADAlarms()</code> method.
         *
         * @param channel channel number in the range [0 to (<code>getNumberADChannels()</code> - 1)]
         * @param range one of the ranges returned by <code>getADRanges(int)</code>
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @see #getADRanges(int)
         * @see #getADRange(int,byte[])
         */
        public void SetADRange(int channel, double range, byte[] state)
        {
            //throw new OneWireException("Selectable A-D Range Not Supported");
        }


        public void SetADReferenceVoltage(double referenceVoltage)
        {
            adReferenceVoltage = referenceVoltage;
        }

        public double GetADReferenceVoltage()
        {
            return adReferenceVoltage;
        }

        public void SetADDeviceBitCount(int bits)
        {
            if (bits > 16)
                bits = 16;
            if (bits < 8)
                bits = 8;
            adDeviceBits = bits;
        }

        public int GetADDeviceBitCount()
        {
            return adDeviceBits;
        }

        public void SetForceADResults(Boolean force)
        {
            adForceResults = force;
        }

        public Boolean GetForceADResults()
        {
            return adForceResults;
        }

        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Clock Interface Functions
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /**
         * Checks to see if the clock has an alarm feature.
         *
         * @return false, since this device does not have clock alarms
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
         * Checks to see if the clock can be disabled.
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
         * Gets the clock resolution in milliseconds
         *
         * @return the clock resolution in milliseconds
         */
        public long GetClockResolution()
        {
            return 1000;
        }

        //--------
        //-------- Clock 'get' Methods
        //--------

        /**
         * Extracts the Real-Time clock value in milliseconds.
         *
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return the time represented in this clock in milliseconds since 1970
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#readDevice()
         * @see #setClock(long,byte[])
         */
        public long GetClock(byte[] state)
        {
            //grab the time
            int[] time = GetTime(RTC_TIME & 0x3F, state);
            //grab the date
            int[] date = GetDate(RTC_DATE & 0x3F, state);

            //date[1] - 1 because Java months are 0 offset
            /* TODO: check this...
                  Calendar d = new GregorianCalendar(date[0], date[1] - 1, date[2],
                                                     time[2], time[1], time[0]);

                  return d.getTime().getTime();
            */
            return 0; // TODO: remove when the code above is checked
        }

        /**
         * Extracts the clock alarm value for the Real-Time clock.
         *
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @return milliseconds since 1970 that the clock alarm is set to
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
            throw new OneWireException(
               "Device does not support clock alarms");
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
            return GetFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_OSCILLATOR, state);
        }

        //--------
        //-------- Clock 'set' Methods
        //--------

        /**
         * Sets the Real-Time clock.
         * The method <code>writeDevice()</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice()</code>.
         *
         * @param time new value for the Real-Time clock, in milliseconds
         * since January 1, 1970
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[])
         * @see #getClock(byte[])
         */
        public void SetClock(long time, byte[] state)
        {
            /* TODO: check
                  Date     x = new Date(time);
                  Calendar d = new GregorianCalendar();

                  d.setTime(x);
                  setTime(RTC_TIME&0x3F, d.get(Calendar.HOUR) + (d.get(Calendar.AM_PM) == Calendar.PM ? 12 : 0),
                          d.get(Calendar.MINUTE), d.get(Calendar.SECOND), false, state);
                  setDate(RTC_DATE&0x3F, d.get(Calendar.YEAR), d.get(Calendar.MONTH) + 1, d.get(Calendar.DATE), state);

                  if(!getFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_OSCILLATOR, state))
                     setFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_OSCILLATOR, true, state);

                  lock (this)
                  {
                     updatertc = true;
                  }
            */
        }

        /**
         * Sets the clock alarm.
         * The method <code>writeDevice()</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice()</code>.  Also note that
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
            throw new OneWireException(
               "Device does not support clock alarms");
        }

        /**
         * Enables or disables the oscillator, turning the clock 'on' and 'off'.
         * The method <code>writeDevice()</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice()</code>.  Also note that
         * not all clock devices can disable their oscillators.  Check to see if this device can
         * disable its oscillator first by calling the <code>canDisableClock()</code> method.
         *
         * @param runEnable true to enable the clock oscillator
         * @param state current state of the device returned from <code>readDevice()</code>
         *
         * @see com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[])
         * @see #canDisableClock()
         * @see #isClockRunning(byte[])
         */
        public void SetClockRunEnable(Boolean runEnable, byte[] state)
        {
            SetFlag(RTC_CONTROL_REGISTER, RCR_BIT_ENABLE_OSCILLATOR, runEnable, state);
        }

        /**
         * Enables or disables the clock alarm.
         * The method <code>writeDevice()</code> must be called to finalize
         * changes to the device.  Note that multiple 'set' methods can
         * be called before one call to <code>writeDevice()</code>.  Also note that
         * not all clock devices have alarms.  Check to see if this device has
         * alarms first by calling the <code>hasClockAlarm()</code> method.
         *
         * @param alarmEnable true to enable the clock alarm
         * @param state current state of the device returned from <code>readDevice()</code>
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
            throw new OneWireException("Device does not support clock alarms");
        }

        /**
         * Gets the time of day fields in 24-hour time from button
         * returns int[] = {seconds, minutes, hours}
         *
         * @param timeReg which register offset to pull the time from
         * @param state acquired from call to readDevice
         * @return array representing {seconds, minutes, hours}
         */
        private int[] GetTime(int timeReg, byte[] state)
        {
            byte upper, lower;
            int[] result = new int[3];

            // First grab the seconds. Upper half holds the 10's of seconds
            lower = state[timeReg++];
            upper = (byte)((lower >> 4) & 0x07);
            lower = (byte)(lower & 0x0f);
            result[0] = (int)lower + (int)upper * 10;

            // now grab minutes. The upper half holds the 10s of minutes
            lower = state[timeReg++];
            upper = (byte)((lower >> 4) & 0x07);
            lower = (byte)(lower & 0x0f);
            result[1] = (int)lower + (int)upper * 10;

            // now grab the hours. The lower half is single hours again, but the
            // upper half of the byte is determined by the 2nd bit - specifying
            // 12/24 hour time.
            lower = state[timeReg];
            upper = (byte)((lower >> 4) & 0x07);
            lower = (byte)(lower & 0x0f);

            byte PM = 0;
            // if the 2nd bit is 1, convert 12 hour time to 24 hour time.
            if ((upper & 0x04) != 0)
            {
                // extract the AM/PM byte (PM is indicated by a 1)
                if ((upper & 0x02) > 0)
                    PM = 12;

                // isolate the 10s place
                upper &= 0x01;
            }

            result[2] = (int)(upper * 10) + (int)lower + (int)PM;

            return result;
        }

        /**
         * Set the time in the DS1922 time register format.
         */
        private void SetTime(int timeReg, int hours, int minutes, int seconds,
                              Boolean AMPM, byte[] state)
        {
            byte upper, lower;

            /* format in bytes and write seconds */
            upper = (byte)(((seconds / 10) << 4) & 0xf0);
            lower = (byte)((seconds % 10) & 0x0f);
            state[timeReg++] = (byte)(upper | lower);

            /* format in bytes and write minutes */
            upper = (byte)(((minutes / 10) << 4) & 0xf0);
            lower = (byte)((minutes % 10) & 0x0f);
            state[timeReg++] = (byte)(upper | lower);

            /* format in bytes and write hours/(12/24) bit */
            if (AMPM)
            {
                upper = (byte)0x04;

                if (hours > 11)
                    upper = (byte)(upper | 0x02);

                // this next logic simply checks for a decade hour
                if (((hours % 12) == 0) || ((hours % 12) > 9))
                    upper = (byte)(upper | 0x01);

                if (hours > 12)
                    hours = hours - 12;

                if (hours == 0)
                    lower = (byte)0x02;
                else
                    lower = (byte)((hours % 10) & 0x0f);
            }
            else
            {
                upper = (byte)(hours / 10);
                lower = (byte)(hours % 10);
            }

            upper = (byte)((upper << 4) & 0xf0);
            lower = (byte)(lower & 0x0f);
            state[timeReg] = (byte)(upper | lower);
        }

        /**
         * Grab the date from one of the time registers.
         * returns int[] = {year, month, date}
         *
         * @param timeReg which register offset to pull the date from
         * @param state acquired from call to readDevice
         * @return array representing {year, month, date}
         */
        private int[] GetDate(int timeReg, byte[] state)
        {
            byte upper, lower;
            int[] result = new int[] { 0, 0, 0 };

            /* extract the day of the month */
            lower = state[timeReg++];
            upper = (byte)((lower >> 4) & 0x0f);
            lower = (byte)(lower & 0x0f);
            result[2] = upper * 10 + lower;

            /* extract the month */
            lower = state[timeReg++];
            if ((lower & 0x80) == 0x80)
                result[0] = 100;
            upper = (byte)((lower >> 4) & 0x01);
            lower = (byte)(lower & 0x0f);
            result[1] = upper * 10 + lower;

            /* grab the year */
            lower = state[timeReg++];
            upper = (byte)((lower >> 4) & 0x0f);
            lower = (byte)(lower & 0x0f);
            result[0] += upper * 10 + lower + FIRST_YEAR_EVER;

            return result;
        }

        /**
         * Set the current date in the DS1922's real time clock.
         *
         * year - The year to set to, i.e. 2001.
         * month - The month to set to, i.e. 1 for January, 12 for December.
         * day - The day of month to set to, i.e. 1 to 31 in January, 1 to 30 in April.
         */
        private void SetDate(int timeReg, int year, int month, int day, byte[] state)
        {
            byte upper, lower;

            /* write the day byte (the upper holds 10s of days, lower holds single days) */
            upper = (byte)(((day / 10) << 4) & 0xf0);
            lower = (byte)((day % 10) & 0x0f);
            state[timeReg++] = (byte)(upper | lower);

            /* write the month bit in the same manner, with the MSBit indicating
               the century (1 for 2000, 0 for 1900) */
            upper = (byte)(((month / 10) << 4) & 0xf0);
            lower = (byte)((month % 10) & 0x0f);
            state[timeReg++] = (byte)(upper | lower);

            // now write the year
            year = year - FIRST_YEAR_EVER;
            if (year > 100)
            {
                state[timeReg - 1] |= 0x80;
                year -= 100;
            }
            upper = (byte)(((year / 10) << 4) & 0xf0);
            lower = (byte)((year % 10) & 0x0f);
            state[timeReg] = (byte)(upper | lower);
        }



        // *****************************************************************************
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Private initilizers
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // *****************************************************************************

        /**
         * Construct the memory banks used for I/O.
         */
        private void InitMem()
        {

            // scratchpad
            scratch = new MemoryBankScratchCRCPW(this);

            // User Data Memory
            userDataMemory = new MemoryBankNVCRCPW(this, scratch);
            userDataMemory.numberPages = 16;
            userDataMemory.size = 512;
            userDataMemory.bankDescription = "User Data Memory";
            userDataMemory.startPhysicalAddress = 0x0000;
            userDataMemory.generalPurposeMemory = true;
            userDataMemory.readOnly = false;
            userDataMemory.readWrite = true;

            // Register
            register = new MemoryBankNVCRCPW(this, scratch);
            register.numberPages = 32;
            register.size = 1024;
            register.bankDescription = "Register control";
            register.startPhysicalAddress = 0x0200;
            register.generalPurposeMemory = false;

            // Data Log
            log = new MemoryBankNVCRCPW(this, scratch);
            log.numberPages = 256;
            log.size = 8192;
            log.bankDescription = "Data log";
            log.startPhysicalAddress = 0x1000;
            log.generalPurposeMemory = false;
            log.readOnly = true;
            log.readWrite = false;
        }

        /**
         * Sets the following, calculated from the 12-bit code of the 1-Wire Net Address:
         * 1)  The part numbers:
         *     DS1922H - Temperature/Humidity iButton
         *     DS1922L - Temperature iButton
         *     DS1922T - Extended Temperature iButton
         *     DS1i22S - Temperature/A-D iButton
         */
        private void SetContainerVariables(byte[] registerPages)
        {
            // clear this flag..  Gets set later if registerPages!=null
            isContainerVariablesSet = false;

            // reset mission parameters
            hasHumiditySensor = false;
            isMissionLoaded = false;
            missionRegister = null;
            dataLog = null;
            temperatureLog = null;
            adReferenceVoltage = 5.02d;
            adDeviceBits = 10;
            adForceResults = false;

            byte configByte = (byte)0xFF;
            if (registerPages != null)
            {
                configByte = registerPages[DEVICE_CONFIGURATION_BYTE & 0x03F];
            }

            switch (configByte)
            {
                case DCB_DS2422S:
                    partNumber = "DS2422";
                    temperatureRangeLow = -40;
                    temperatureRangeWidth = 125;
                    break;
                case DCB_DS1922H:
                    partNumber = "DS1922H/1923";
                    temperatureRangeLow = -40;
                    temperatureRangeWidth = 125;
                    hasHumiditySensor = true;
                    descriptionString +=
                       " The A-to-D reading of this device represents the value of relative humidity.";
                    break;
                case DCB_DS1922L:
                    partNumber = "DS1922L";
                    temperatureRangeLow = -40;
                    temperatureRangeWidth = 125;
                    descriptionString +=
                       " The A-to-D reading of this device has no meaning.";
                    break;
                case DCB_DS1922T:
                    partNumber = "DS1922T";
                    temperatureRangeLow = 0;
                    temperatureRangeWidth = 125;
                    descriptionString +=
                       " The A-to-D reading of this device has no meaning.";
                    break;
                default:
                    partNumber = "DS1922/2422";
                    temperatureRangeLow = -40;
                    temperatureRangeWidth = 125;
                    break;
            }

            if (registerPages != null)
            {
                isContainerVariablesSet = true;

                // if humidity device, calculate the calibration coefficients
                if (hasHumiditySensor)
                {
                    useHumdCalibrationRegisters = true;

                    // DEBUG: Product samples were sent out uncalibrated.  This flag
                    // allows the customer to not use the temperature calibration
                    string useHumdCal =
                       AccessProvider.GetProperty(
                          "DS1922H.useHumidityCalibrationRegisters");
                    if (useHumdCal != null && useHumdCal.ToLower().Equals("false"))
                    {
                        useHumdCalibrationRegisters = false;
                        Console.Out.WriteLine("DEBUG: Disabling Humidity Calibration Usage in Container");
                    }

                    Href1 = DecodeHumidity(registerPages, 0x48, 2, true);
                    Hread1 = DecodeHumidity(registerPages, 0x4A, 2, true);
                    Herror1 = Hread1 - Href1;
                    Href2 = DecodeHumidity(registerPages, 0x4C, 2, true);
                    Hread2 = DecodeHumidity(registerPages, 0x4E, 2, true);
                    Herror2 = Hread2 - Href2;
                    Href3 = DecodeHumidity(registerPages, 0x50, 2, true);
                    Hread3 = DecodeHumidity(registerPages, 0x52, 2, true);
                    Herror3 = Hread3 - Href3;

                    double Href1sq = Href1 * Href1;
                    double Href2sq = Href2 * Href2;
                    double Href3sq = Href3 * Href3;
                    humdCoeffB =
                       ((Href2sq - Href1sq) * (Herror3 - Herror1) + Href3sq * (Herror1 - Herror2)
                         + Href1sq * (Herror2 - Herror1)) /
                       ((Href2sq - Href1sq) * (Href3 - Href1) + (Href3sq - Href1sq) * (Href1 - Href2));
                    humdCoeffA =
                       (Herror2 - Herror1 + humdCoeffB * (Href1 - Href2)) /
                       (Href2sq - Href1sq);
                    humdCoeffC =
                       Herror1 - humdCoeffA * Href1sq - humdCoeffB * Href1;
                }

                useTempCalibrationRegisters = true;

                // DEBUG: Product samples were sent out uncalibrated.  This flag
                // allows the customer to not use the temperature calibration
                String useTempCal =
                   AccessProvider.GetProperty(
                      "DS1922H.useTemperatureCalibrationRegisters");
                if (useTempCal != null && useTempCal.ToLower().Equals("false"))
                {
                    useTempCalibrationRegisters = false;
                    Console.Out.WriteLine("DEBUG: Disabling Temperature Calibration Usage in Container");
                }

                Tref2 = DecodeTemperature(registerPages, 0x40, 2, true);
                Tread2 = DecodeTemperature(registerPages, 0x42, 2, true);
                Terror2 = Tread2 - Tref2;
                Tref3 = DecodeTemperature(registerPages, 0x44, 2, true);
                Tread3 = DecodeTemperature(registerPages, 0x46, 2, true);
                Terror3 = Tread3 - Tref3;
                Tref1 = 60d;
                Terror1 = Terror2;
                Tread1 = Tref1 + Terror1;

#if DEBUG_OneWireContainer41
                Debug.DebugStr("Tref1=" + Tref1);
                Debug.DebugStr("Tread1=" + Tread1);
                Debug.DebugStr("Terror1=" + Terror1);
                Debug.DebugStr("Tref2=" + Tref2);
                Debug.DebugStr("Tread2=" + Tread2);
                Debug.DebugStr("Terror2=" + Terror2);
                Debug.DebugStr("Tref3=" + Tref3);
                Debug.DebugStr("Tread3=" + Tread3);
                Debug.DebugStr("Terror3=" + Terror3);
#endif

                double Tref1sq = Tref1 * Tref1;
                double Tref2sq = Tref2 * Tref2;
                double Tref3sq = Tref3 * Tref3;
                tempCoeffB =
                   ((Tref2sq - Tref1sq) * (Terror3 - Terror1) + Tref3sq * (Terror1 - Terror2)
                     + Tref1sq * (Terror2 - Terror1)) /
                   ((Tref2sq - Tref1sq) * (Tref3 - Tref1) + (Tref3sq - Tref1sq) * (Tref1 - Tref2));
                tempCoeffA =
                   (Terror2 - Terror1 + tempCoeffB * (Tref1 - Tref2)) /
                   (Tref2sq - Tref1sq);
                tempCoeffC =
                   Terror1 - tempCoeffA * Tref1sq - tempCoeffB * Tref1;
            }
        }

        /**
         * helper method for decoding temperature values
         */
        private double DecodeTemperature(byte[] data,
                                                      int offset,
                                                      int length,
                                                      Boolean reverse)
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
            Debug.DebugStr("decodeTemperature, data", data, offset, length);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            double whole, fraction = 0;
            if (reverse && length == 2)
            {
                fraction = ((data[offset + 1] & 0x0FF) / 512d);
                whole = (data[offset] & 0x0FF) / 2d + (temperatureRangeLow - 1);
            }
            else if (length == 2)
            {
                fraction = ((data[offset] & 0x0FF) / 512d);
                whole = (data[offset + 1] & 0x0FF) / 2d + (temperatureRangeLow - 1);
            }
            else
            {
                whole = (data[offset] & 0x0FF) / 2d + (temperatureRangeLow - 1);
            }
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
            Debug.DebugStr("decodeTemperature, temperatureRangeLow= " + temperatureRangeLow);
            Debug.DebugStr("decodeTemperature, whole= " + whole);
            Debug.DebugStr("decodeTemperature, fraction= " + fraction);
            Debug.DebugStr("decodeTemperature, (whole+fraction)= " + (whole + fraction));
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            return whole + fraction;
        }

        /**
         * helper method for encoding temperature values
         */
        private void EncodeTemperature(double temperature,
                                             byte[] data,
                                             int offset,
                                             int length,
                                             Boolean reverse)
        {
            double val = 2 * ((temperature) - (temperatureRangeLow - 1));
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
            Debug.DebugStr(
                   "encodeTemperature, temperature=" + temperature
                   + ", temperatureRangeLow=" + temperatureRangeLow
                   + ", val=" + val);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            if (reverse && length == 2)
            {
                data[offset + 1] = (byte)(0x0C0 & (byte)(val * 256));
                data[offset] = (byte)val;
            }
            else if (length == 2)
            {
                data[offset] = (byte)(0x0C0 & (byte)(val * 256));
                data[offset + 1] = (byte)val;
            }
            else
            {
                data[offset] = (byte)val;
            }
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
            Debug.DebugStr(
                   "encodeTemperature, data", data, offset, length);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        }

        /**
         * helper method for decoding humidity values
         */
        private double DecodeHumidity(byte[] data,
                                            int offset,
                                            int length,
                                            Boolean reverse)
        {
            // get the 10-bit value of Vout
            double val = GetADVoltage(data, offset, length, reverse);

            // convert Vout to a humidity reading
            // this formula is from HIH-3610 sensor datasheet
            val = (val - .958) / .0307;

            return val;
        }

        /**
         * helper method for encoding humidity values
         */
        private void EncodeHumidity(double humidity,
                                          byte[] data,
                                          int offset,
                                          int length,
                                           Boolean reverse)
        {
            // convert humidity value to Vout value
            double val = (humidity * .0307) + .958;
            // convert Vout to byte[]
            SetADVoltage(val, data, offset, length, reverse);
        }

        private double GetADVoltage(byte[] data, int offset, int length,
                                          Boolean reverse)
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
            Debug.DebugStr("getADVoltage, data", data, offset, length);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            // get the 10-bit value of vout
            int ival = 0;
            if (reverse && length == 2)
            {
                ival = ((data[offset] & 0x0FF) << (adDeviceBits - 8));
                ival |= ((data[offset + 1] & 0x0FF) >> (16 - adDeviceBits));
            }
            else if (length == 2)
            {
                ival = ((data[offset + 1] & 0x0FF) << (adDeviceBits - 8));
                ival |= ((data[offset] & 0x0FF) >> (16 - adDeviceBits));
            }
            else
            {
                ival = ((data[offset] & 0x0FF) << (adDeviceBits - 8));
            }

            double dval = (ival * adReferenceVoltage) / (1 << adDeviceBits);

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
            Debug.DebugStr("getADVoltage, ival=" + ival);
            Debug.DebugStr("getADVoltage, voltage=" + dval);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            return dval;
        }

        private void SetADVoltage(double voltage,
                                        byte[] data, int offset, int length,
                                        Boolean reverse)
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
            Debug.DebugStr("setADVoltage, voltage=" + voltage);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            int val = (int)((voltage * (1 << adDeviceBits)) / adReferenceVoltage);
            val = val & ((1 << adDeviceBits) - 1);
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
            Debug.DebugStr("setADVoltage, val=" + val);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            if (reverse && length == 2)
            {
                data[offset] = (byte)(val >> (adDeviceBits - 8));
                data[offset + 1] = (byte)(val << (16 - adDeviceBits));
            }
            else if (length == 2)
            {
                data[offset + 1] = (byte)(val >> (adDeviceBits - 8));
                data[offset] = (byte)(val << (16 - adDeviceBits));
            }
            else
            {
                data[offset] = (byte)((val & 0x3FC) >> (adDeviceBits - 8));
            }
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_OneWireContainer41
            Debug.DebugStr("setADVoltage, data", data, offset, length);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        }
    }
}
