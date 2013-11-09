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
using DalSemi; // Debug

namespace DalSemi.OneWire.Container
{

    /**
     * <P> 1-Wire container for Java-powered iButton, DS195X.  This container
     * encapsulates the functionality of the iButton family  type
     * <B>16</B> and <B>96</B> (hex)</P>
     *
     * <H3> Features </H3>
     * <UL>
     *   <LI> Java Card&#8482 2.0-compliant
     *   <LI> True 32-bit Java integers for straightforward computation
     *   <LI> Automatic garbage collection for efficient reuse of memory space
     *   <LI> Resizable scratchpad optimizes memory usage and allows large atomic transactions
     *   <LI> Dynamic applet capability for post-issuance updates
     *   <LI> up to 64-kbyte ROM for Java VM and operating system
     *   <LI> 6-kbyte NV SRAM–writes in 100 nanoseconds–supports multiple applications
     *   <LI> Java-accessible True Time Clock time-stamps transactions
     *   <LI> Java-accessible random number generator for cryptographic keys
     *   <LI> JavaCardX.Crypto, SHA-1, RSA DES, triple DES cryptographic classes for secret key digital signatures
     *   <LI> PKI support: PKCS # 11(Netscape&#174), CSP (Microsoft&#174), X509 certificates
     *   <LI> Support for Win2000 log-on
     *   <LI> Physically secure iButtonTM case zeroizes contents on tampering
     *   <LI> 3V to 5V operating range
     *   <LI> ESD protection >25,000V
     *   <LI> Over 10 years of data retention
     * </UL>
     *
     * <H3> Usage </H3>
     *
     * <DL>
     * <DD> See the usage examples in
     * {@link com.dalsemi.onewire.container.CommandAPDU CommandAPDU}
     * to create <code>CommandAPDU</code>.
     * <DD> See the usage examples in
     * {@link com.dalsemi.onewire.container.ResponseAPDU ResponseAPDU}
     * to create <code>ResponseAPDU</code>.
     * </DL>
     *
     * <H3> DataSheet </H3>
     * <DL>
     * <DD><A HREF="http://www.ibutton.com/ibuttons/java.html">
     *              http://www.ibutton.com/ibuttons/java.html</A>
     * </DL>
     *
     * @see com.dalsemi.onewire.container.CommandAPDU
     * @see com.dalsemi.onewire.container.ResponseAPDU
     *
     * @version   0.00, 28 Aug 2000
     * @author    YL
     */


    public class OneWireContainer16 : OneWireContainer
    {

        public static byte GetFamilyCode()
        {
            return 0x16;
        }

        /** CLA byte in the <code>CommandAPDU</code> header. */
        const byte CLA = (byte)0xD0;

        /** INS byte in the <code>CommandAPDU</code> header. */
        const byte INS = (byte)0x95;

        /** size of password length in byte */
        public const int PASSWORD_LENGTH_SIZE = 1;

        /** maximum length of password in byte */
        public const int PASSWORD_SIZE = 8;

        /** size of AID length in byte */
        public const int AID_LENGTH_SIZE = 1;

        /** maximum length of AID in byte */
        public const int AID_SIZE = 16;

        /** offset of AID length in applet APDU data stream */
        public const int AID_LENGTH_OFFSET = PASSWORD_LENGTH_SIZE
                                                    + PASSWORD_SIZE;

        /** offset of AID name in applet APDU data stream */
        public const int AID_NAME_OFFSET = PASSWORD_LENGTH_SIZE
                                                  + PASSWORD_SIZE
                                                  + AID_LENGTH_SIZE;

        /** size of applet file header in byte */
        public const int APPLET_FILE_HEADER_SIZE = PASSWORD_LENGTH_SIZE
                                                             + PASSWORD_SIZE
                                                             + AID_LENGTH_SIZE
                                                             + AID_SIZE;

        /** default APDU data packet length in byte */
        public static int APDU_PACKET_LENGTH = 64;

        /** password */
        private string password;

        /** current CommandaAPDU sent */
        private CommandAPDU capdu = null;

        /** current <code>ResponseAPDU</code> received */
        private ResponseAPDU rapdu = null;

        /** JibComm object for communicating with adapter */
        private JibComm jibComm;

        /** default JibComm run time */
        private int runTime = 0;

        /**
         * Default JibComm run time for select operations.  These operations need
         * special consideration because the select operation could take longer 
         * depending on what kind of APDU processing needs to be done.
         */
        private int selectRunTime = 1;

        /**
         * Default JibComm run time for the last loadApplet packet.  This packet
         * requires special consideration because extra processing is done after the
         * packet is loaded.
         */
        private int loadRunTime = 2;

        /**
         * Creates a OneWireContainer16 with the provided adapter object
         * and the address of this Java iButton.
         *
         * This is one of the methods to construct a OneWireContainer16.  The
         * others are through creating a OneWireContainer16 with different parameters
         * types.
         *
         * @param  sourceAdapter     adapter object required to communicate with
         *                           this Java iButton
         * @param  newAddress        address of this Java iButton
         *
         * @see com.dalsemi.onewire.utils.Address
         * @see #OneWireContainer16()
         * @see #OneWireContainer16(DSPortAdapter,long)
         * @see #OneWireContainer16(DSPortAdapter,String)
         */
        public OneWireContainer16(PortAdapter sourceAdapter, byte[] newAddress)
            : base(sourceAdapter, newAddress)
        {
        }

        //-------------------------------------------------------------------------
        //-------- Methods
        //-------------------------------------------------------------------------
        //-------------------------------------------------------------------------

        /** Gets the Dallas Semiconductor part number of this Java iButton
         *  as a string.  For example "DS195X".
         *
         *  @return string represetation of this Java iButton name
         */
        public override string GetName()
        {
            return "DS195X";
        }   // getName()

        /** Gets the alternate Dallas Semiconductor part numbers or names.
         *  A 'family' of One-Wire devices may have more than one part number
         *  depending on packaging.  There can also be nicknames such as
         *  'Crypto iButton'.
         *
         *  @return string represetation of the alternate names
         */
        public override string GetAlternateNames()
        {
            return "Java iButton, Cryptographic iButton";
        }   // getAlternateNames

        /** Gets a short description of the function of this Java iButton.
         *
         *  @return string represetation of the function description
         */
        public override string GetDescription()
        {
            return "JavaCard 2.0 compliant device.";
        }   // getDescription()


        /**
         * Provides an adapter object to access this Java iButton.
         *
         * @param  sourceAdapter     adapter object required to communicate with
         *                           this Java iButton
         * @param  newAddress        address of this One-Wire device as a byte array.
         *
         * @see Address
         */
        protected override void SetupContainer(PortAdapter sourceAdapter, byte[] newAddress)
        {
            base.SetupContainer(sourceAdapter, newAddress);
            SetupJibComm(adapter, address);
        }

        /**
         * Provides an <code>JibComm</code> object to communicate
         * with this Java iButton.
         *
         * @param  sourceAdapter     adapter object required to communicate with
         *                           this Java iButton
         * @param  newAddress        address of this One-Wire device as a byte array
         *
         * @see Address
         */
        public void SetupJibComm(PortAdapter sourceAdapter, byte[] newAddress)
        {
            // JibComm has to communicate at OVERDRIVE speed.
            speed = OWSpeed.SPEED_OVERDRIVE;
            speedFallBackOK = false;
            jibComm = new JibComm(this, sourceAdapter, newAddress);
        }

        /**
         * Gets the maximum speed this One-Wire device can communicate at.
         *
         * @return maximum speed of this One-Wire device
         */
        public override OWSpeed GetMaxSpeed()
        {
            return OWSpeed.SPEED_OVERDRIVE;
        }

        /**
        * Gets the current <code>CommandAPDU</code> sent to this Java iButton
        *
        * @return current <code>CommandAPDU</code> sent to this Java iButton
        */
        public CommandAPDU GetCommandAPDUInfo()
        {
            return capdu;
        }

        /**
        * Gets the current <code>ResponseAPDU</code> received from this Java iButton
        *
        * @return current <code>ResponseAPDU</code> received from this Java iButton
        */
        public ResponseAPDU GetResponseAPDUInfo()
        {
            return rapdu;
        }

        /**
        * Gets the run time value set for this Java iButton
        *
        * @return run time value of this Java iButton
        *
        * @see    #setRunTime
        */
        public int GetRunTime()
        {
            return runTime;
        }

        /**
        * Gets the select run time value set for this Java iButton
        *
        * @return run time value of this Java iButton on select operations
        *
        * @see    #setSelectRunTime
        */
        public int GetSelectRunTime()
        {
            return selectRunTime;
        }

        /**
        * Gets the load run time value set for this Java iButton
        *
        * @return run time value of this Java iButton on load operations
        *
        * @see    #setLoadRunTime
        */
        public int GetLoadRunTime()
        {
            return loadRunTime;
        }

        /**
         * Sets the run time value for this Java iButton
         *
         * @param  runTime a <code>4</code> bit value (<code>0 -15</code>) that
         *                 represents the expected run time of the device
         *
         *  Actual run time is calculated as followed:
         *  <code><pre>
         *    runTime * 250 + 62.5 mS <BR>
         *  Therefore, 0 -> 0 * 250 + 62.5 =  62.5 mS
         *             1 -> 1 * 250 + 62.5 = 312.5 mS </pre></code>
         *  and so on.
         *
         * @throws IllegalArgumentException Invalid run time value
         *
         * @see #getRunTime
         */
        public void SetRunTime(int newRunTime)
        {
            if ((newRunTime > 15) || (newRunTime < 0))
                throw new ArgumentOutOfRangeException("Run Time value should be between 0 and 15.");

            runTime = newRunTime;
        }

        /**
         * Sets the select operation run time value for this Java iButton.
         * The select-applet operation may require special consideration
         * because APDU processing is done on a select.
         *
         * @param  runTime a <code>4</code> bit value (<code>0 -15</code>) that
         *                 represents the expected run time of the device on a 
         *                 select operation. This value should not be less than 
         *                 the normal <code>runTime</codE> value.
         *
         *  Actual run time is calculated as followed:
         *  <code><pre>
         *    runTime * 250 + 62.5 mS <BR>
         *  Therefore, 0 -> 0 * 250 + 62.5 =  62.5 mS
         *             1 -> 1 * 250 + 62.5 = 312.5 mS </pre></code>
         *  and so on.
         *
         * @throws IllegalArgumentException Invalid run time value
         *
         * @see #getSelectRunTime
         */
        public void SetSelectRunTime(int newRunTime)
        {
            if ((newRunTime > 15) || (newRunTime < 0))
                throw new ArgumentOutOfRangeException("Run Time value should be between 0 and 15.");

            selectRunTime = newRunTime;
        }

        /**
         * Sets the load operation run time value for this Java iButton.
         * The last packet of a load-applet operation may require special consideration
         * because additional processing is performed once the applet is loaded.
         * All non-final load packets run based on the normal <code>runTime</code>
         * value.
         *
         * @param  runTime a <code>4</code> bit value (<code>0 -15</code>) that
         *                 represents the expected run time of the device on a 
         *                 final load packet.  This value should not be less than
         *                 the normal <code>runTime</code> value.
         *
         *  Actual run time is calculated as followed:
         *  <code><pre>
         *    runTime * 250 + 62.5 mS <BR>
         *  Therefore, 0 -> 0 * 250 + 62.5 =  62.5 mS
         *             1 -> 1 * 250 + 62.5 = 312.5 mS </pre></code>
         *  and so on.
         *
         * @throws IllegalArgumentException Invalid run time value
         *
         * @see #getLoadRunTime
         */
        public void SetLoadRunTime(int newRunTime)
        {
            if ((newRunTime > 15) || (newRunTime < 0))
                throw new ArgumentOutOfRangeException("Run Time value should be between 0 and 15.");

            loadRunTime = newRunTime;
        }

        /**
         * Sets the run time value for reading bytes from the output buffer of this Java iButton.
         * Generally, output data is read giving the button the minimal amount of runtime
         * needed.  However, for larger amounts of data, this may fail, causing the operation
         * to be retried.  To avoid the retry, this value can be altered if something is known about
         * the size of the output data.
         *
         * @param  runTime a <code>4</code> bit value (<code>0-15</code>) that
         *                 represents the run time of the device for reading
         *                 data from the output buffer of this Java Powered iButton
         *
         * @throws IllegalArgumentException Invalid run time value
         *
         * @see #getLoadRunTime
         */
        public void SetReadingRunTime(int newRunTime)
        {
            if ((newRunTime > 15) || (newRunTime < 0))
                throw new ArgumentOutOfRangeException("Run Time value should be between 0 and 15.");

            jibComm.min_read_runtime = newRunTime;
        }

        /**
         * Sets the size of the packets sent on load.
         * The range can be from <code>64</code> to <code>112</code> bytes.
         *
         * @param  size new packet size
         *
         * @return <code>true</code> if packet size is set to the new value <BR>
         *         <code>false</code> if invalid packet size is requested
         *
         * @see #getLoadPacketSize
         * @see #loadApplet
         */
        public Boolean SetLoadPacketSize(int size)
        {

            // maximum number of bytes send is 124,
            // 4 for CommandAPDU header, 1 for data length,
            // 3 to append at sendAPDU(),  4 for JibComm setData header
            // and 112 for packet size
            if (size < 64 || size > 112)
                return false;

            APDU_PACKET_LENGTH = size;

            return true;
        }   // setLoadPacketSize()

        /**
         * Gets the size of the packets sent on load.
         *
         * @return  size of the packets sent on load
         *
         * @see #setLoadPacketSize
         * @see #loadApplet
         *
         */
        public int GetLoadPacketSize()
        {
            return APDU_PACKET_LENGTH;
        }   // getLoadPacketSize()

        /**
         * Sets the PIN used to communicate with this Java iButton.
         * Once this method has been called, the PIN will be sent to every method
         * that requires a PIN.
         *
         * @param  passwd PIN to be set and sent to this Java iButton for each command
         *         that requires a PIN
         *
         * @see    #getCommandPINMode
         * @see    #setCommandPINMode
         * @see    #setCommonPIN
         */
        public void SetPIN(String passwd)
        {
            password = passwd;
        }   // setPIN()

        //-------------------------------------------------------------------------
        //-------- Firmware Command methods
        //-------------------------------------------------------------------------

        /**
         * Gets the amount of free RAM in this Java iButton.  No PIN required.
         *
         * @return <code>ResponseAPDU</code> with a data field containing the amount
         *         of free RAM in this Java iButton.  Value returned is in little endian
         *         format.
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         */
        public ResponseAPDU GetFreeRAM()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x01);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getFreeRAM()

        /**
         * Gets requested number of bytes of random data generated by this Java
         * iButton. No PIN required.
         *
         * @param  numBytes the number of bytes requested.  <code>numByes</code>
         *         should not exceed <code>119</code>.  If the number is greater
         *         than <code>119</code>, the API only returns <code>119</code>.
         *
         * @return <code>ResponseAPDU</code> with a data field containing random
         *         bytes generated
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         */
        public ResponseAPDU GetRandomBytes(int numBytes)
        {

            // maximum number of bytes return is 124,
            // 2 for status word, 3 to discard and 119 for data
            if (numBytes > 119)
                numBytes = 119;

            byte[] data = new byte[2];

            data[0] = (byte)(numBytes & 0xFF);
            data[1] = (byte)((numBytes >> 8) & 0xFF);
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x0D,
                                       data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getRandomBytes()

        /**
         * Gets the firmware version string. No PIN required.
         *
         * @return <code>ResponseAPDU</code> with a data field containing the firmware
         *         version string of this Java iButton
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         */
        public ResponseAPDU GetFirmwareVersionString()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x00);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getFirmwareVersionString()

        /**
         * Gets the last error value.
         * If the error reporting mode is set (<code>1</code>), then this method
         * will return the value of the last exception. No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the last exception value
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getErrorReportingMode
         * @see    #setErrorReportingMode
         */
        public ResponseAPDU GetLastError()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x0F);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getLastError()

        /**
         * Gets the Error Reporting Mode.  This function is not supported in
         * the <code>.033</code> version of the firmware. No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the error reporting mode
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6A02. Command not supported by this Java iButton.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getLastError
         * @see    #setErrorReportingMode
         */
        public ResponseAPDU GetErrorReportingMode()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x09);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getErrorReportingMode()

        /**
         * Sets the Error Reporting mode.
         * If the error reporting mode is set (<code>1</code>), this Java iButton
         * stores the last exception value code.  This code can be retreived by
         * calling <code>getLastError()</code>.
         *
         *
         * @param mode <code>1</code> to turn on  Error Reporting Mode.
         *             <code>0</code> to turn off.
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getLastError
         * @see    #getErrorReportingMode
         */
        public ResponseAPDU SetErrorReportingMode(int mode)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[2 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)mode;
            }
            else
            {
                data = new byte[2];
                data[0] = (byte)0;
                data[1] = (byte)mode;
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x00, (byte)0x09, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // setErrorReportingMode()

        /**
         * Gets the AID of the applet by its installed number. No PIN required.
         *
         * @param index installed number of the applet on this Java iButton to get
         *              the AID of
         *
         * @return <code>ResponseAPDU</code> containing the AID of the applet
         *         requested
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x8453.  Applet not found.
         *             Valid applet index are in the range of <code>0-15</code>.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         */
        public ResponseAPDU GetAIDByNumber(int index)
        {
            byte[] data = new byte[1];

            data[0] = (byte)(index & 0xFF);
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x0E,
                                       data);
            rapdu = SendAPDU(capdu, runTime);

            if (rapdu.GetSW() == 0x8453)   // applet not found
                return rapdu;

            // bug fix for Firmware 0.33
            int i = rapdu.GetData().Length;

            data = new byte[(i + 4)];

            Array.Copy(rapdu.GetData(), 0, data, 0, i);

            // return a "SUCCESS" ResponseAPDU
            data[i] = rapdu.GetSW1();
            data[i + 1] = rapdu.GetSW2();
            data[i + 2] = (byte)0x90;
            data[i + 3] = (byte)0x00;

            return (new ResponseAPDU(data));
        }   // getAIDByNumber()

        /**
         * Deletes the currently selected applet.
         * If PIN protection is enabled, a PIN must be supplied.
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #deleteAppletByAID
         * @see    #deleteAppletByNumber
         */
        public ResponseAPDU DeleteSelectedApplet()
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[1 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);
            }
            else
            {
                data = new byte[2];
                data[0] = (byte)0;
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x03, (byte)0x00, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // deleteSelectedApplet()

        /**
         * Deletes an applet by its installed number.
         *
         * @param  index installed number of the applet to delete
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>Failure SW 0x8453. Applet not found.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #deleteAppletByAID
         * @see    #deleteSelectedApplet
         */
        public ResponseAPDU DeleteAppletByNumber(int index)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[2 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)index;
            }
            else
            {
                data = new byte[2];
                data[0] = (byte)0;
                data[1] = (byte)index;
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x03, (byte)0x01, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // deleleAppletByNumber()

        /**
         * Deletes an applet by its AID.
         *
         * @param  aid AID of applet to be deleted
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>Failure SW 0x8453. Applet not found.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #deleteSelectedApplet
         * @see    #deleteAppletByNumber
         */
        public ResponseAPDU DeleteAppletByAID(String aid)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[2 + password.Length + aid.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)aid.Length;

                Array.Copy(aid.ToCharArray(), 0, data, password.Length + 2,
                                 aid.Length);
            }
            else
            {
                data = new byte[2 + aid.Length];
                data[0] = (byte)0;
                data[1] = (byte)aid.Length;

                Array.Copy(aid.ToCharArray(), 0, data, 2, aid.Length);
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x03, (byte)0x02, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // deleteAppletByAID()


        /**
         * Loads an applet onto this Java iButton.
         * This method takes an input stream
         * and an AID.  The AID length must NOT exceed <code>AID_SIZE</code>.
         *
         * @param  in Stream to read the applet from
         * @param  aid AID of the applet to be loaded
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         Possible return values are
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6400. Insufficient Memory
         *         <li>Failure SW 0x6901. Invalid AID Length
         *         <li>Failure SW 0x6902. Invalid API Version
         *         <li>Failure SW 0x6903. Invalid Password
         *         <li>Failure SW 0x6904. Invalid Signature Length
         *         <li>Failure SW 0x6905. Hash Corruption
         *         <li>Failure SW 0x6906. Hash Failure
         *         <li>Failure SW 0x6982. Invalid Signature
         *         <li>Failure SW 0x6A84. Class Length Overrun
         *         <li>Failure SW 0x6A86. Invalid Loader Command
         *         <li>Failure SW 0x6A87. Invalid Packet
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IOException              Problem reading applet from the stream
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getLoadPINMode
         * @see    #setLoadPINMode
         */
        public ResponseAPDU LoadApplet(System.IO.Stream /* InputStream */ appletInputStream, String aid)
        {
            //ByteArrayOutputStream baos = new ByteArrayOutputStream(1024);
            System.IO.MemoryStream baos = new System.IO.MemoryStream(1024);
            byte[] appletBuffer = new byte[1024];

            // Password FIX THIS TO HANDLE LOADING WITH A PASSWORD.
            if (password != null)
            {
                appletBuffer[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, appletBuffer, 1,
                                    password.Length);
            }

            // append AID to appletBuffer
            for (int offset = 0; offset < AID_SIZE; offset++)
            {
                if (offset < aid.Length)
                    appletBuffer[offset + AID_NAME_OFFSET] = (byte)aid.ToCharArray()[(int)offset];
                else   // leave the rest in their default value (0)
                    break;
            }

            appletBuffer[AID_LENGTH_OFFSET] = (byte)aid.Length; //AID_SIZE;   // AID Length

            //  now write the whole enchilada to the ByteArrayOutputStream
            baos.Write(appletBuffer, 0, APPLET_FILE_HEADER_SIZE);

            int amount = appletInputStream.Read(appletBuffer, 0, appletBuffer.Length);
            while (amount != -1)
            {
                baos.Write(appletBuffer, 0, amount);
                amount = appletInputStream.Read(appletBuffer, 0, appletBuffer.Length);
            }

            appletInputStream.Close();

            int bytesSent = 0;
            Boolean firstPacket = true;
            ResponseAPDU rapdu = null;
            byte[] apduBuffer = new byte[APDU_PACKET_LENGTH];

            appletBuffer = baos.ToArray();
            int appletLength = appletBuffer.Length - APPLET_FILE_HEADER_SIZE;
            int totalLength = (appletLength + APPLET_FILE_HEADER_SIZE - APDU_PACKET_LENGTH);

            if (JibComm.doDebugMessages)
            {
                //System.out.println("TOTAL APPLET LENGTH: "+appletBuffer.length);
                Debug.WriteLine("TOTAL APPLET LENGTH: " + appletBuffer.Length);
            }
            while (bytesSent < totalLength)
            {
                if (JibComm.doDebugMessages)
                {
                    //System.out.println("byteSent: "+bytesSent);
                    Debug.WriteLine("byteSent: " + bytesSent);
                }
                Array.Copy(appletBuffer, bytesSent, apduBuffer, 0, APDU_PACKET_LENGTH);

                bytesSent += APDU_PACKET_LENGTH;

                if (firstPacket == true)
                {
                    capdu = new CommandAPDU(CLA, (byte)0xA6, (byte)0x01,
                                                    (byte)0x00, apduBuffer);
                    firstPacket = false;
                }
                else
                {
                    capdu = new CommandAPDU(CLA, (byte)0xA6, (byte)0x02,
                                            (byte)0x00, apduBuffer);
                }

                rapdu = SendAPDU(capdu, runTime);

                if (bytesSent < (appletLength + APPLET_FILE_HEADER_SIZE))
                {
                    if (rapdu.GetSW() != 0x6301)  //6301 == Success Packet
                    {
                        bytesSent = appletLength + APPLET_FILE_HEADER_SIZE + 1;
                    }
                }
            }
            if (bytesSent < (appletLength + APPLET_FILE_HEADER_SIZE))
            {
                if (JibComm.doDebugMessages)
                {
                    //System.out.println("One last packet...byteSent: "+bytesSent);
                    Debug.WriteLine("One last packet...byteSent: " + bytesSent);
                    //System.out.println("Applet length: "+appletBuffer.length);
                    Debug.WriteLine("Applet length: " + appletBuffer.Length);
                }

                apduBuffer = new byte[(int)(appletLength + APPLET_FILE_HEADER_SIZE - bytesSent)];

                Array.Copy(appletBuffer, bytesSent, apduBuffer, 0, apduBuffer.Length);

                if (firstPacket == true)
                {
                    capdu = new CommandAPDU(CLA, (byte)0xA6, (byte)0x01,
                                                    (byte)0x00, apduBuffer);
                    firstPacket = false;
                }
                else
                {
                    capdu = new CommandAPDU(CLA, (byte)0xA6, (byte)0x02,
                                            (byte)0x00, apduBuffer);
                }

                rapdu = SendAPDU(capdu, loadRunTime);
            }

            return rapdu;
        }

        /**
         * Loads an applet onto this Java iButton.
         * This method takes an applet filename, directory
         * and AID.  The AID length must NOT exceed <code>AID_SIZE</code>.
         *
         * @param  fileName file name of the applet to be loaded into this Java iButton
         * @param  directoryName path to the applet to be loaded
         * @param  aid AID of the applet to be loaded
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         Possible return values are
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6400. Insufficient Memory
         *         <li>Failure SW 0x6901. Invalid AID Length
         *         <li>Failure SW 0x6902. Invalid API Version
         *         <li>Failure SW 0x6903. Invalid Password
         *         <li>Failure SW 0x6904. Invalid Signature Length
         *         <li>Failure SW 0x6905. Hash Corruption
         *         <li>Failure SW 0x6906. Hash Failure
         *         <li>Failure SW 0x6982. Invalid Signature
         *         <li>Failure SW 0x6A84. Class Length Overrun
         *         <li>Failure SW 0x6A86. Invalid Loader Command
         *         <li>Failure SW 0x6A87. Invalid Packet
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws FileNotFoundException    Applet file not found
         * @throws IOException              Problem reading applet file
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getLoadPINMode
         * @see    #setLoadPINMode
         */
        public ResponseAPDU LoadApplet(String fileName, String directoryName,
                                        String aid)
        {
            //File            appletFile        = new File(directoryName, fileName);
            //FileInputStream appletInputStream = new FileInputStream(appletFile);
            System.IO.FileStream appletInputStream = System.IO.File.OpenRead(directoryName + "\\" + fileName);
            return LoadApplet(appletInputStream, aid);

        }

        /**
         * Clears all memory in this Java iButton.  The erase will occur at the
         * beginning of the next command.
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         */
        public ResponseAPDU MasterErase()
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[1 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);
            }
            else
            {
                data = new byte[1];
                data[0] = (byte)0;
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x00, (byte)0x00, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // masterErase()

        /**
         * Gets the number of times this Java iButton has been power-on-reset (POR)
         * since the last master erase.  Return value is in little endian
         * format. No PIN required.
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000. Data field contains the POR count.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #masterErase
         */
        public ResponseAPDU GetPORCount()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x02, (byte)0x00);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getPORCount()

        /**
         * Gets the Real Time Clock.  No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the value of the real
         *         time clock
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         */
        public ResponseAPDU GetRealTimeClock()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x0C);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getRealTimeClock()

        /**
         * Gets the Answer To Reset (ATR) from this Java iButton. No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the ATR in the data field
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         */
        public ResponseAPDU GetATR()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x0B);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getATR()

        /**
         * Gets the Ephemeral Gabage Collection Mode.
         * A value of <code>1</code> indicates ephemeral garbage collection
         * is turned on, <code>0</code> if turned off. No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the Ephemeral Garbage
         *         Collector Mode
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #setEphemeralGCMode
         */
        public ResponseAPDU GetEphemeralGCMode()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x02);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getEphemeralGCMode()

        /**
         * Gets the Applet Garbage Collection Mode.
         * A value of <code>1</code> indicates applet garbage collection
         * is turned on, <code>0</code> if turned off. No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the Applet Garbage
         *         Collector Mode
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #setAppletGCMode
         */
        public ResponseAPDU GetAppletGCMode()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x03);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getAppletGCMode()

        /**
         * Gets the Command PIN Mode.
         * A value of <code>1</code> indicates that a PIN is required for
         * all Administrative and AID commands, <code>0</code> indicates
         * free access. No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the Command PIN Mode
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #setCommandPINMode
         * @see    #setCommonPIN
         * @see    #setPIN
         */
        public ResponseAPDU GetCommandPINMode()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x04);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getCommandPINMode()

        /**
         * Gets the Load PIN Mode.
         * A value of <code>1</code> indicates that a PIN is required for Applet
         * loading. No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the Load PIN Mode
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #loadApplet
         * @see    #setLoadPINMode
         */
        public ResponseAPDU GetLoadPINMode()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x05);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getLoadPINMode()

        /**
         * Gets the Restore Mode.
         * When Restore Mode is enabled (<code>1</code>), all field updates and
         * <code>javacard.framework.System</code> transactions are considered atomic.
         * If a tear occurs in the middle of these updates, values just
         * prior to the update are restored. No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the Restore Mode
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #setRestoreMode
         */
        public ResponseAPDU GetRestoreMode()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x06);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getRestoreMode()

        /**
         * Gets the Exception Mode.
         * When Exception Mode is enabled (<code>1</code>), Java API exceptions
         * are thrown.  All uncaught exceptions return 0x6F00 in the SW.
         * When disabled, an error is returned from the VM. No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the Exception Mode value
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #setExceptionMode
         */
        public ResponseAPDU GetExceptionMode()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x07);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getExceptionMode()

        /**
         * Gets the size of the Commit Buffer.
         * Committing one field to the buffer requires <code>9</code> bytes.
         * Therefore the default size of <code>72</code> bytes allows <code>8</code>
         * field updates. The minimum size allowed is <code>72</code> bytes and the
         * maximum is restricted by the amount of free RAM. All values will be rounded
         * up to the next multiple of <code>9</code>. No PIN required.
         *
         * @return <code>ResponseAPDU</code> containing the Commit Buffer size in little
         *         endian format
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getFreeRAM
         * @see    #setCommitBufferSize
         */
        public ResponseAPDU GetCommitBufferSize()
        {
            capdu = new CommandAPDU(CLA, INS, (byte)0x01, (byte)0x0A);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // getCommitBufferSize()

        /**
         * Sets the Common PIN.
         * This method is used to change the value of the Common PIN on the Java
         * iButton. If this Java iButton currently has a PIN, the <code>setPIN()</code>
         * method should be called prior to this method with the 'oldPIN' value.
         *
         * @param newPIN the value of the PIN to be set in this Java iButton
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getCommandPINMode
         * @see    #setCommandPINMode
         * @see    #setPIN
         */
        public ResponseAPDU SetCommonPIN(String newPIN)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[newPIN.Length + password.Length + 2];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)newPIN.Length;

                Array.Copy(newPIN.ToCharArray(), 0, data, password.Length + 2,
                                 newPIN.Length);
            }
            else
            {
                data = new byte[2 + newPIN.Length];
                data[0] = (byte)0;
                data[1] = (byte)newPIN.Length;

                Array.Copy(newPIN.ToCharArray(), 0, data, 2, newPIN.Length);
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x00, (byte)0x01, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // setCommonPin()

        /**
         * Sets the Ephemeral Garbage Collection Mode.
         * A value of <code>1</code> turns the Ephemeral Garbage Collection on
         * and a value of <code>0</code> turns it off.
         *
         * @param mode <code>1</code> to turn on  Ephemeral Garbage Collection Mode.
         *             <code>0</code> to turn off.
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getEphemeralGCMode
         */
        public ResponseAPDU SetEphemeralGCMode(int mode)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[2 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)mode;
            }
            else
            {
                data = new byte[2];
                data[0] = (byte)0;
                data[1] = (byte)mode;
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x00, (byte)0x02, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // set EphemeralGCMode()

        /**
         * Sets the Applet Garbage Collection Mode.
         * A value of <code>1</code> turns the Applet Garbage Collection on
         * and a value of <code>0</code> turns it off.
         *
         * @param mode <code>1</code> to turn on  Applet Garbage Collection Mode.
         *             <code>0</code> to turn off.
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getAppletGCMode
         */
        public ResponseAPDU SetAppletGCMode(int mode)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[2 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)mode;
            }
            else
            {
                data = new byte[2];
                data[0] = (byte)0;
                data[1] = (byte)mode;
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x00, (byte)0x03, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // setAppletGCMode()

        /**
         * Sets the Command PIN Mode.
         * If command PIN mode is set to <code>1</code>, a PIN is required for all
         * Administrative commands.
         *
         * @param mode <code>1</code> to turn on  Command PIN Mode.
         *             <code>0</code> to turn off.
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getCommandPINMode
         * @see    #setCommonPIN
         * @see    #setPIN
         */
        public ResponseAPDU SetCommandPINMode(int mode)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[2 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)mode;
            }
            else
            {
                data = new byte[2];
                data[0] = (byte)0;
                data[1] = (byte)mode;
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x00, (byte)0x04, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // setCommandPINMode()

        /**
         * Sets the Load PIN Mode.
         * If the load PIN mode is set (<code>1</code>), then PINs are required
         * on applet loading.
         *
         * @param mode <code>1</code> to turn on  Load PIN Mode.
         *             <code>0</code> to turn off.
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getLoadPINMode
         * @see    #loadApplet
         */
        public ResponseAPDU SetLoadPINMode(int mode)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[2 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)mode;
            }
            else
            {
                data = new byte[2];
                data[0] = (byte)0;
                data[1] = (byte)mode;
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x00, (byte)0x05, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // setLoadPINMode()

        /**
         * Sets the Restore Mode.
         * When Restore Mode is enabled (<code>1</code>), all field updates and
         * <code>javacard.framework.System</code> transactions are considered atomic.
         * If a tear occurs in the middle of these updates, values just prior to the
         * update are restored.
         *
         * @param mode <code>1</code> to turn on  Restore Mode.
         *             <code>0</code> to turn off.
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getRestoreMode
         */
        public ResponseAPDU SetRestoreMode(int mode)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[2 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)mode;
            }
            else
            {
                data = new byte[2];
                data[0] = (byte)0;
                data[1] = (byte)mode;
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x00, (byte)0x06, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // setRestoreMode()

        /**
         * Sets the Exception Mode.
         * When Exception Mode is enabled (<code>1</code>), Java API exceptions
         * are thrown. All uncaught exceptions return 0x6F00 in the SW. When
         * disabled, an error is returned from the VM.
         *
         * @param mode <code>1</code> to turn on  Exception Mode.
         *             <code>0</code> to turn off.
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getExceptionMode
         */
        public ResponseAPDU SetExceptionMode(int mode)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[2 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)mode;
            }
            else
            {
                data = new byte[2];
                data[0] = (byte)0;
                data[1] = (byte)mode;
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x00, (byte)0x07, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // setExceptionMode()

        /**
         * Sets the Commit Buffer size.
         * Committing one field to the buffer requires <code>9</code> bytes.
         * Therefore the default size of <code>72</code> bytes allows <code>8</code>
         * field updates. The minimum size allowed is <code>72</code> bytes and the
         * maximum is restricted by the amount of free RAM. All values will be rounded
         * up to the next multiple of <code>9</code>.
         *
         * @param  size size of the desired commit buffer
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6681. Invalid PIN.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getCommitBufferSize
         */
        public ResponseAPDU SetCommitBufferSize(int size)
        {
            byte[] data;

            if (password != null)
            {
                data = new byte[3 + password.Length];
                data[0] = (byte)password.Length;

                Array.Copy(password.ToCharArray(), 0, data, 1, password.Length);

                data[password.Length + 1] = (byte)(size & 0xFF);
                data[password.Length + 2] = (byte)(size >> 8);
            }
            else
            {
                data = new byte[3];
                data[0] = (byte)0;
                data[1] = (byte)(size & 0xFF);
                data[2] = (byte)(size >> 8);
            }

            capdu = new CommandAPDU(CLA, INS, (byte)0x00, (byte)0x0A, data);
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }   // setCommitBufferSize()

        /**
         * Selects an applet by AID.
         * Lenght of AID should not exceed <code>AID_SIZE</code>. No PIN required.
         *
         * @param  aid the AID of the applet to be selected
         *
         * @return <code>ResponseAPDU</code> indicating success or failure
         *         <ul>
         *         <li>Success SW 0x9000.
         *         <li>Failure SW 0x6A82. Unable to Select Applet
         *         <li>Failure SW 0x8453. Applet not found.
         *         <li>For additional error codes, please see:
         *             <A HREF="http://www.ibutton.com/jibkit/documents/sw.html">
         *                      http://www.ibutton.com/jibkit/documents/sw.html</A>
         *         </ul>
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #deleteAppletByAID
         * @see    #deleteAppletByNumber
         * @see    #deleteSelectedApplet
         * @see    #getAIDByNumber
         * @see    #process
         */
        public ResponseAPDU Select(String aid)
        {
            if (aid.Length > AID_SIZE)
                throw new ArgumentOutOfRangeException("AID length should not exceed "
                                                   + AID_SIZE);

            byte[] data = new byte[AID_SIZE];

            Array.Copy(aid.ToCharArray(), 0, data, 0, aid.Length);

            capdu = new CommandAPDU((byte)0x00, (byte)0xA4, (byte)0x04,
                                    (byte)0x00, data);
            rapdu = SendAPDU(capdu, selectRunTime);

            return rapdu;
        }   // select

        /**
         * Sends a generic process command to this JavaiButton.
         * This method can be used to send any <code>CommandAPDU</code>.
         * No PIN required.
         *
         * @param capdu <code>CommandAPDU</code> to be sent to this Java iButton
         *
         * @return <code>ResponseAPDU</code> the response from this Java iButton
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #select
         */
        public ResponseAPDU Process(CommandAPDU capdu)
        {
            this.capdu = capdu;
            rapdu = SendAPDU(capdu, runTime);

            return rapdu;
        }

        /**
         * Sends a <code>CommandAPDU</code> to this Java iButton.
         * No PIN required.
         *
         * @param  capdu the <code>CommandAPDU</code> to be sent to this Java iButton
         *
         * @param  runTime a <code>4</code> bit value (<code>0 -15</code>) that
         *                 represents the expected run time of the device
         *
         *  <code><pre>
         *    runTime * 250 + 62.5 mS <BR>
         *  Therefore, 0 -> 0 * 250 + 62.5 =  62.5 mS
         *             1 -> 1 * 250 + 62.5 = 312.5 mS </pre></code>
         *  and so on.
         *
         * @return <code>ResponseAPDU</code> the <code>ResponseAPDU</code> from
         *         this Java iButton
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         */
        public ResponseAPDU SendAPDU(CommandAPDU capdu, int runTime)
        {

            if ((runTime > 15) || (runTime < 0))
                throw new ArgumentOutOfRangeException("Run Time value should be between 0 and 15.");

            int capduLength = capdu.GetLength();
            byte[] apdu = new byte[capduLength + 3];

            // append extra bytes for legacy reasons
            apdu[0] = (byte)(2 + capduLength);
            apdu[1] = (byte)0x89;
            apdu[2] = (byte)0x00;

            Array.Copy(capdu.GetBytes(), 0, apdu, 3, capduLength);
            // Attempt to put adapter into OverDrive Mode.
            DoSpeed();
            rapdu = new ResponseAPDU(jibComm.TransferJibData(apdu, runTime));

            return rapdu;
        }


    }
}