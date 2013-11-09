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

/* JibComm.java
   revisions
   0.01 - March 27, 2001 Captain K
          Updated to speed up several operations.  Changed minimum run time from
          96 to 63 milliseconds, which is the spec.  Combined multiple datablock
          writes wherever possible.  Changed behaviour on finding the button in
          a FIRST_BIRTHDAY state, sleeping much less than before.  Tried to take
          out as many malloc's as possible, opting to make methods synchronized and
          share a byte[] buffer.
   0.02   March 30, 2001 Captain K again
          1 bug workaround and 1 bug fix.  The fix was that we were sometimes trying
          to read the registers when the registers weren'thread written yet.  Now we check to see
          if the registers are done being written to yet, then if they aren'thread ready,
          give the micro the minimum amount of runtime to finish up.  The workaround is a
          long standing bug on the part where it won'thread respond to 1-Wire communication
          sometimes.  Its an ugly workaround - if an APDU send fails, we POR and try again.
          POR is accomplished by going to normal speed, resetting, kicking to overdrive,
          selecting, and making sure the part is there.  See OneWireContainer.doSpeed()
   0.03   October 15, 2001
          Could it be that the Bad CRC problem is solved?  Thanks to Marc Palmer for working
          with me on this one, and Dave S. for helping root through the debug data...
          It looks like at some point in a communication, the adapters are dropping down
          from overdrive speed (I don'thread know where, when, or why yet...a project for another day...
          or life) but anyhow, doing an Overdrive-Match-ROM makes all the parts on the bus
          give presence pulses if you ain'thread in overdrive.  By switching to Regular-Speed-Match-ROMs
          you don'thread sacrifice anything (whlie at overdrive speed) and you are OK when you rudely
          get kicked back to regular speed.
   0.04   January, 2002
          Lots of updates/fixes.  Put the min runtime back to 96, turns out the JiB likes to run
          over and this causes lots of communications failures.  Improved the try schemes.  Fixed a bug when
          reading a large amount of data from the button.  Added CRCCorrection table at Tom's request.

 */

namespace DalSemi.OneWire.Container
{

    /** JibComm - an object that implements the necessary communication protocol
     * to access the Java iButtons.  Note that many methods are now synchronized
     * because they access global byte arrays.  This should not affect performance,
     * however, since applications <i>should</i> be using the <code>DSPortAdapter</code>
     * methods <code>beginExclusive(boolean)</code> and <code>endExclusive()</code>
     * to synchronize their 1-Wire operations.
     *
     * <H3> Usage </H3>
     *
     * <DL>
     * <DD> Used primarily by
     * {@link com.dalsemi.onewire.container.OneWireContainer16 OneWireContainer16}
     * </DL>
     *
     *
     * @see com.dalsemi.onewire.container.OneWireContainer16
     *
     * @version    0.04, 23 Jan 2002
     * @author     K, JK
     *
     *
     */

    public class JibComm
    {

        //////////////////////////////////////////////////////////
        //  Masks for checking various bits in registers        //
        //////////////////////////////////////////////////////////

        /** accelerator status mask */
        private const byte COPROCESSOR_ACCELERATOR_RUNNING = (byte)0x01;

        /** power-on-reset (POR) mask */
        private const byte POWER_ON_RESET = (byte)0x40;

        /** command complete status mask */
        private const byte COMMAND_COMPLETE = (byte)0x0B;

        /** command not complete status mask */
        private const byte COMMAND_NOT_COMPLETE = (byte)0x20;

        private const byte CE_RESPONSE_INCOMPLETE = (byte)0x0c;

        private const byte CE_JAVAVM_INCOMPLETE = (byte)0x0e;

        /** first birthday condition mask */
        private const byte FIRST_BIRTHDAY = (byte)0x1D;

        /** master Erase problems mask */
        private const byte MASTER_ERASE_PROBLEM = (byte)0x1F;

        //////////////////////////////////////////////////////////
        //  Device Specific Commands for Java iButton.          //
        //////////////////////////////////////////////////////////

        /** write IPR commmand */
        private const byte WRITE_IPR_COMMAND = (byte)0x0F;

        /** read IPR Command */
        private const byte READ_IPR_COMMAND = (byte)0xAA;

        /** write I/O buffer command */
        private const byte WRITE_IO_BUFFER_COMMAND = (byte)0x2D;

        /** read I/O buffer command */
        private const byte READ_IO_BUFFER_COMMAND = (byte)0x22;

        /** interrupt Micro command */
        private const byte INTERRUPT_MICRO_COMMAND = (byte)0x77;

        /** run Micro command */
        private const byte RUN_MICRO_COMMAND = (byte)0x87;

        /** reset Micro command */
        private const byte RESET_MICRO_COMMAND = (byte)0xDD;

        /** read Status command */
        private const byte READ_STATUS_COMMAND = (byte)0xE1;

        /** write Status command */
        private const byte WRITE_STATUS_COMMAND = (byte)0xD2;

        /** header size of data block */
        private const int HEADER_SIZE = 8;

        /** maximum data block size to send or receive */

        // there are 8 bytes of overhead in each block of data send
        private const int MAX_BLOCK_SIZE = 128 - 8;

        /** minimum run time for this Java iButton in milliseconds.
        * This number has been increased from the value specified in the
        * data sheet.  This longer time to sleep attempts to keep the host
        * from interrupting this Java iButton.
        */
        private static int MIN_RUNTIME_IN_MILLIS = 96;

        private static int RUNTIME_MULTIPLIER = 290;

        /** maximum run time for this Java iButton in milliseconds */
        private const int MAX_RUNTIME_IN_MILLIS = 3813;

        /** minimum run time value for the status register (OWUS) */
        private const int MIN_RUNTIME = 0;

        /** maximum run time value for the status register (OWUS) */
        private const int MAX_RUNTIME = 15;

        /** sending bytes to this Java iButton */
        private const int SEND = 0x01;

        /** receiving bytes from this Java iButton */
        private const int RECEIVE = 0x02;

        /** adapter used to communicate with this Java iButton */
        private PortAdapter adapter;

        /** byte array containing iButtonAddress(ID) */
        private byte[] address = new byte[8];

        /** enable/disable debug messages */
        public static Boolean doDebugMessages = false;

        /** power-on-reset (POR) correction status.
        The POR will always be corrected at the begining of a
        transfer.
        */
        private Boolean shouldCorrectPOR = false;

        /** 0xFF arrays for quick array initialization */
        private byte[] ffBlock;

        /* used in lieu of an adapter.select() call */
        private byte[] select_buffer = new byte[9];

        public int min_read_runtime = 0;

        /* byte[ buffers used by specific methods */
        private byte[] transfer_jib_status = new byte[4];
        private byte[] transfer_jib_header = new byte[HEADER_SIZE];
        private byte[] get_header_buffer = new byte[14];
        private byte[] command_buffer = new byte[16];
        private byte[] check_status = new byte[4];
        private byte[] set_status_command_buffer = new byte[6];
        private byte[] set_header_buffer = new byte[4 + HEADER_SIZE + 2];
        private byte[] run_release_buffer = { RUN_MICRO_COMMAND, (byte)0x73, (byte)0x5D };
        private byte[] interrupt_release_buffer = { INTERRUPT_MICRO_COMMAND, (byte)0x43, (byte)0x6D };

        /* used only in the SendCommand method */
        private byte[] buffer = new byte[200];

        /* keeps us from having to create a new object on every apdu sent */
        private BlockDataFragmenter fragger = new BlockDataFragmenter();

        private OneWireContainer16 container = null;

        private static uint[] CRCCorrectionTable = new uint[140];

        static JibComm()
        {
            CRCCorrectionTable[2] = CRC16.Compute(1, 0);
            for (int i = 3; i < CRCCorrectionTable.Length; i++)
            {
                CRCCorrectionTable[i] = CRC16.Compute(0, CRCCorrectionTable[i - 1]);
            }
        }

        private static uint GetCRC16Correction(int p_Length)
        {
            if (p_Length >= CRCCorrectionTable.Length)
                return 0;

            if (p_Length < 1)
                return 0;

            return CRCCorrectionTable[p_Length - 1];
        }



        /**
         * Sets the time given by a host for the Java Powered <u>i</u>Button to
         * perform its task. In some cases, increases in this value may help
         * avoid communications errors.  63 milliseconds is the absolute lowest
         * rated value, but 96 milliseconds makes for fewer errors.
         *
         * @param runtime minimum runtime the host gives a Java <u>i</u>Button to perform its task (in ms)
         *
         * @throws IllegalArgumentException on illegal run time values (must be at least 63)
         */
        public static void SetMinRuntime(int runtime)
        {
            if (runtime > 62)
                MIN_RUNTIME_IN_MILLIS = runtime;
            else throw new ArgumentOutOfRangeException("Minimum runtime must be at least 63 milliseconds");
        }

        /**
         * Sets the incremental increase in runtime a host will give a
         * Java Powered <u>i</u>Button to perform its task. Beyond the initial
         * minimum runtime, the Java <u>i</u>Button only understands increments
         * of 250 milliseconds.  However, due to clock and timing variations,
         * this value may need to be altered.  CRC errors are common if this value
         * is too low for a given operation. 250 milliseconds is the absolute lowest
         * rated value.  290 milliseconds is the default.
         *
         * @param multiplier new incremental runtime increase value to be used
         *                   for this Java <u>i</u>Button to perform its task (in ms)
         *
         * @throws IllegalArgumentException on illegal multiplier values (must be at least 250)
         */
        public static void SetRuntimeMultiplier(int multiplier)
        {
            if (multiplier > 249)
                RUNTIME_MULTIPLIER = multiplier;
            else throw new ArgumentOutOfRangeException("Minimum multiplier is 250 milliseconds");

        }

        //-------------------------------------------------------------------------
        /** Constructs a <code>JibComm</code> object to communicate with
        * this Java iButton.
        *
        * @param newAdapter adapter used to communicate with this Java iButton
        * @param newAddress address of this Java iButton
        *
        * @throws IllegalArgumentException Invalid Java iButton address
        *
        * @see com.dalsemi.onewire.utils.Address
        */
        public JibComm(OneWireContainer16 owc, PortAdapter newAdapter, byte[] newAddress)
        {
            container = owc;

            // Check to see if the length of the address is correct.
            if (newAddress.Length != 8)
                throw new ArgumentOutOfRangeException("iButton Address must be of length 8.");

            Array.Copy(newAddress, 0, address, 0, address.Length);
            Array.Copy(newAddress, 0, select_buffer, 1, 8);
            //select_buffer[0] = (byte)0x69;
            select_buffer[0] = (byte)0x55;
            adapter = newAdapter;

            // create an array of 0xFF for quick array fill
            ffBlock = new byte[130];

            for (int i = 0; i < 130; i++)
                ffBlock[i] = (byte)0xFF;
        }

        private void DataBlock(byte[] buf, int start, int off)
        {
            try
            {
                if ((doDebugMessages) && (buf[0] != 0x55))
                {
                    //System.out.println("Found a non-55 beginning..."+Integer.toHexString(buf[0]&0x0ff));
                    Debug.WriteLine("Found a non-55 beginning..." + (buf[0] & 0x0ff).ToString("X2"));
                }
                buf[0] = (byte)0x55;
                adapter.Reset();
                adapter.DataBlock(buf, start, off);
            }
            catch (OneWireException)
            {
                if (doDebugMessages)
                    //System.out.println("Retrying a datablock...");
                    Debug.WriteLine("Retrying a datablock...");

                /* Note!!! This is a hack for the older, crappy USB fobs that
                 * shipped with the first Java iButton 2-in-1's.  They don'thread like
                 * about 50% of the first datablocks that happen after
                 * a strong pull-up, which is a bad thing with the JiB,
                 * since everything it does (just about) needs a strong
                 * pull up.  So if we get a failure, try to put the part
                 * at normal speed, then back to overdrive and see if the part is
                 * present (that's done in the doSpeed method)
                 */
                adapter.Speed = OWSpeed.SPEED_REGULAR;
                container.DoSpeed();
                adapter.Reset();
                buf[0] = (byte)0x55;
                adapter.DataBlock(buf, start, off);

                /* Still, even with these heroic effots, some operations
                 * may need a retry
                 */
            }
        }


        int POR_ADJUST = 0;
        private int my_min_read_runtime = 0;

        long time1 = 0;
        long time2 = 0;
        /**
         * Transfers data to and from this Java iButton.
         *
         * @param data    byte array with data to be written to this Java iButton
         * @param runTime a <code>4</code> bit value <code>(0 -15)</code>
         *                that represents the expected run time of the device.
         *
         *  Actual run time is calculated as followed:
         *  <code><pre>
         *    runTime * 250 + 62.5 mS <BR>
         *  Therefore, 0 -> 0 * 250 + 62.5 =  62.5 mS
         *             1 -> 1 * 250 + 62.5 = 312.5 mS </pre></code>
         *  and so on.
         *
         * @return byte array containing data read back from this Java iButton.
         *
         * @throws IllegalArgumentException Invalid run time value
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         */
        public byte[] TransferJibData(byte[] data, int runTime)
        {
            lock (this)
            {
                POR_ADJUST = 0;
                OneWireException exc = null;
                Boolean reading = false;
                my_min_read_runtime = min_read_runtime;
                //try this only twice.  If it fails the first time, we
                // POR the part through a normal speed reset, kick it up to
                // overdrive, then try to re-send the APDU.  This usually works.
                //We have to do this because of a bug in the 1-Wire micro (not
                // the JiB micro) that won'thread let us communicate in certain
                // situations, so we can'thread even get to the status registers.
                if (doDebugMessages)
                {
                    //System.out.println("TRANSFER JIB DATA:\r\n"+toHexString(data));
                    Debug.WriteLine("TRANSFER JIB DATA:\r\n" + ToHexString(data));
                    time1 = DateTime.Now.Ticks; // System.currentTimeMillis();
                }
                for (int i = 0; i < 3; i++)
                {
                    reading = false;
                    try
                    {
                        if (runTime > 15) runTime = 15;
                        if (runTime < 0) runTime = 0;

                        //BlockDataFragmenter frag = new BlockDataFragmenter(data);
                        fragger.Initialize(data);

                        // Set correction of the POR to true to clear the first POR.
                        shouldCorrectPOR = true;

                        if (adapter.Speed != OWSpeed.SPEED_OVERDRIVE)
                            throw new OneWireIOException("Adapter not in overdrive mode.");

                        if (doDebugMessages)
                            //System.out.println("Sending data to Java iButton.");
                            Debug.WriteLine("Sending data to Java iButton.");

                        // Send all data and headers to the iButton.
                        while (true)
                        {
                            CheckStatus(MIN_RUNTIME, SEND, transfer_jib_status, 0);
                            SetHeader(fragger.GetNextHeaderToSend());   // Send Header
                            SetData(fragger.GetNextDataToSend());       // Send Data

                            if (doDebugMessages)
                                //System.out.println("Data has been written.");
                                Debug.WriteLine("Data has been written.");
                            // NOTE:
                            // The 'more' Variable in the BlockDataFragmenter is updated in
                            // the getNextHeaderToSend function.
                            // If this is the final block of data, then the full requested
                            // runtime needs to be sent to this Java iButton instead of the
                            // minimum one.
                            if (fragger.HasMore())
                            {
                                SetStatus(MIN_RUNTIME);
                                Interrupt(MIN_RUNTIME);
                            }
                            else
                            {
                                // all data sent
                                SetStatus(runTime);
                                Interrupt(runTime);
                                break;
                            }
                        }                                           // End of Data loading loop.

                        // retrieve the data.
                        do
                        {
                            reading = true;
                            // wait for iButton reply
                            CheckStatus(runTime, RECEIVE, transfer_jib_status, 0);
                            GetHeader(transfer_jib_header, 0);                   // retrieve header
                            byte[] datatemp = GetData((int)transfer_jib_header[1]);   // retrieve data

                            if (doDebugMessages)
                                //System.out.println("Data read back successfully.");
                                Debug.WriteLine("Data read back successfully.");
                            // Check data CRC and build the return data array.
                            fragger.CheckBlock(transfer_jib_header, datatemp);

                            // for Java iButton, just checking the COMMAND_NOT_COMPLETE flag is
                            // not sufficient as it may send data back in multiple blocks.
                            // Block number 0x80 does not necessary means the last block.
                            if (transfer_jib_status[2] == COMMAND_COMPLETE)
                                break;                               // last block received
                            SetStatus(my_min_read_runtime);
                            Run(my_min_read_runtime);
                        }
                        while (true);
                        reading = false;

                        // Retrieve the data array from the BlockDataFragmenter.
                        if (doDebugMessages)
                        {
                            time2 = DateTime.Now.Ticks; // System.currentTimeMillis();
                            //System.out.println("Full time for that transaction: "+(time2-time1));
                            Debug.WriteLine("Full time (ticks) for that transaction: " + (time2 - time1));
                        }
                        return fragger.GetDataFromRead();
                    }
                    catch (OneWireException owe)
                    {
                        //DEBUG!!! TAKE THIS OUT!!!
                        //System.out.println("This exception occurred: "+owe);

                        exc = owe;
                        if (doDebugMessages)
                        {
                            //System.out.println("This exception occurred: "+owe+", Retrying to send the apdu after POR...");
                            Debug.WriteLine("This exception occurred: " + owe + ", Retrying to send the apdu after POR...");
                        }
                        container.DoSpeed();


                        runTime++;
                        POR_ADJUST++;

                        if (reading && (owe is OneWireIOException))
                        {
                            //System.out.println("FAILED WHILE READING DATA OUT...QUE LATA! INCREASING TIME TO READ");
                            my_min_read_runtime++;
                        }
                    }
                }
                throw exc;
            }
        }

        /**
         * Corrects the device from a Power-On-Reset (POR) error.
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         */
        public void CorrectPOR()
        {

            if (doDebugMessages)
                //System.out.println("Attempting to correct the POR!");
                Debug.WriteLine("Attempting to correct the POR!");
            Reset();
            SetStatus(MIN_RUNTIME + POR_ADJUST);
            Run(MIN_RUNTIME + POR_ADJUST);
        }

        /**
         * Gets the status from this Java iButton.
         *
         * @return status byte array read from this Java iButton.
         * The contents of the status array is as follows:
         *
         * <pre>
         *      Byte 0 - number of free bytes in the Input Buffer.
         *      Byte 1 - number of used bytes in the Output Buffer.
         *      Byte 2 - contents of the OWMS Register.
         *      Byte 3 - contents of the CPST register.</pre>
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #checkStatus
         * @see    #setStatus
         */
        public void GetStatus(byte[] status, int start)
        {
            lock (this)
            {
                if (adapter.Speed != OWSpeed.SPEED_OVERDRIVE)
                {
                    if (doDebugMessages)
                        //System.out.println("FIXING SPEED!");
                        Debug.WriteLine("FIXING SPEED!");
                    container.DoSpeed();
                }
                // Set up the command block

                //first put in the select command
                Array.Copy(select_buffer, 0, command_buffer, 0, 9);

                //now put in the read status command, and the ff's for reading
                command_buffer[9] = READ_STATUS_COMMAND;
                Array.Copy(ffBlock, 0, command_buffer, 10, 6);

                DataBlock(command_buffer, 0, 16);

                // Fix Andreas Bug in read Status with cap on One-Wire.
                // Mask off the last bit of the Free bytes
                command_buffer[10] &= (byte)0xFE;

                uint crc = CRC16.Compute(command_buffer, 9, 7, 0);
                if (crc != 0xB001)
                {
                    uint correction = GetCRC16Correction(7);
                    if ((correction ^ crc) != 0xB001)
                    {
                        if (doDebugMessages)
                        {
                            for (int i = 0; i < 16; i++)
                                //System.out.print(Integer.toHexString(command_buffer[i] & 0x0ff)+" ");
                                Debug.WriteLine(((byte)(command_buffer[i] & 0x0ff)).ToString("X2") + " ");
                            //System.out.println();
                            Debug.WriteLine();
                        }
                        throw new OneWireIOException("Bad CRC on data returned in Read Status method.");
                    }
                }

                // retrieve status
                Array.Copy(command_buffer, 10, status, start, 4);
            }
        }

        /**
         * Checks the status of this Java iButton.
         *
         * @param runTime a <code>4</code> bit value <code>(0 -15)</code>
         *                that represents the expected run time of the device.
         *  Actual run time is calculated as followed:
         *  <code><pre>
         *    runTime * 250 + 62.5 mS <BR>
         *  Therefore, 0 -> 0 * 250 + 62.5 =  62.5 mS
         *             1 -> 1 * 250 + 62.5 = 312.5 mS </pre></code>
         *  and so on.
         * @param dir <code>SEND</code> if sending data to this Java iButton,
         *            <code>RECEIVE</code> if receiving
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #getStatus
         * @see    #setStatus
         */
        public void CheckStatus(int runTime, int dir, byte[] status, int start)
        {
            lock (this)
            {
                int loopCount = 0;
                // loop until the conditions are satisfied which implies that the
                // coprocesser is not running, and the previous command has finished.
                int myRunTime = runTime;
                while (true)
                {
                    loopCount++;

                    // Keeps from having a infinate loop.
                    if (loopCount > 200)    // Value can be ajusted.
                        throw new OneWireException("Unrecoverable error.  Fail.");

                    GetStatus(check_status, 0);              // get current status
                    int owms_code = check_status[2] & 0x1f;

                    /* means that the button wants more time...so let it run.  This could
                       be a keyset generation */
                    if (owms_code == 0x1f)
                        loopCount = 0;

                    if (doDebugMessages)
                    {
                        //System.out.print("<"+Integer.toHexString(check_status[0]&0x0ff)+" "+
                        //                     Integer.toHexString(check_status[1]&0x0ff)+" "+
                        //                     Integer.toHexString(check_status[2]&0x0ff)+" "+
                        //                     Integer.toHexString(check_status[3]&0x0ff)+">");
                        Debug.Write("<" + ((byte)(check_status[0] & 0x0ff)).ToString("X2") + " " +
                                        ((byte)(check_status[1] & 0x0ff)).ToString("X2") + " " +
                                        ((byte)(check_status[2] & 0x0ff)).ToString("X2") + " " +
                                        ((byte)(check_status[3] & 0x0ff)).ToString("X2") + ">");
                        //System.out.print("["+owms_code+"]");
                        Debug.Write("[" + owms_code + "]");
                    }

                    // First error checking POR bit.
                    if ((check_status[2] & POWER_ON_RESET) != 0)
                    {
                        if (shouldCorrectPOR)
                            CorrectPOR();
                        else
                            throw new OneWireException("POR of Device is set.");

                        shouldCorrectPOR = false;
                    }
                    else
                    {

                        // check CPST bit for Co-procsesser status.
                        if ((check_status[3] & COPROCESSOR_ACCELERATOR_RUNNING) != 0)
                        {

                            if (doDebugMessages)
                                //System.out.println("Coprocessor still running.");
                                Debug.WriteLine("Coprocessor still running.");
                            // give it time to finish.
                            myRunTime = myRunTime + 6;
                            if (myRunTime > 15)
                                myRunTime = 15;
                            Run(myRunTime);
                        }
                        else if (((check_status[2] & MASTER_ERASE_PROBLEM) == FIRST_BIRTHDAY)
                                 || ((check_status[2] & MASTER_ERASE_PROBLEM) == MASTER_ERASE_PROBLEM))
                        {
                            // Device is in first-birthday initialization
                            // Occurrence: if a master erase command has been sent
                            // that has not yet been completed

                            //if its not first birthday, it indicates that a load just occurred,
                            //and we don'thread need to wait that long for it to come back,
                            //so we save ourselves 400+ ms by doing this shortcut
                            if ((check_status[2] & MASTER_ERASE_PROBLEM) != FIRST_BIRTHDAY)
                            {
                                myRunTime++;
                                if (myRunTime > 15)
                                    myRunTime = 15;
                            }
                            else
                            {
                                myRunTime = 6; //optimum time for first birthday
                            }

                            if (doDebugMessages)
                            {
                                if ((check_status[2] & MASTER_ERASE_PROBLEM) == FIRST_BIRTHDAY)
                                    //System.out.println("FirstBirth identified.");
                                    Debug.WriteLine("FirstBirth identified.");
                                else
                                    //System.out.println("FirstBirth-like occurrance");
                                    Debug.WriteLine("FirstBirth-like occurrance");
                            }

                            SetStatus(myRunTime);
                            Run(myRunTime);
                        }
                        else if ((check_status[2] & COMMAND_NOT_COMPLETE) != 0)
                        {
                            if (myRunTime == MIN_RUNTIME)
                                myRunTime = 2;
                            else if (myRunTime < 8)
                                myRunTime *= 2;
                            else
                                myRunTime = 15;
                            if (doDebugMessages)
                                //System.out.println("Command Not Complete.");
                                Debug.WriteLine("Command Not Complete.");

                            SetStatus(myRunTime);
                            Run(myRunTime);
                        }
                        else if (owms_code == CE_RESPONSE_INCOMPLETE)
                        {
                            //means the response message isn'thread ready yet
                            //this fixes the very common "No header in output buffer" problem
                            if (doDebugMessages)
                                //System.out.println("Clayton response = 12");
                                Debug.WriteLine("Clayton response = 12");
                            Run(0);
                        }
                        else if ((check_status[1] == 0) && (owms_code == CE_JAVAVM_INCOMPLETE))
                        {
                            //this fixes another 'No header in output buffer' problem where there is a lot of
                            //data coming out, so the button wants to transmit but doesn'thread have any data ready to go,
                            //so we let it run a little bit longer so it can get all the data to its output buffer
                            if (doDebugMessages)
                                //System.out.println("Not quite ready yet...run a little bit more");
                                Debug.WriteLine("Not quite ready yet...run a little bit more");
                            Run(0);
                            my_min_read_runtime++;
                        }
                        else
                        {
                            // Check to see if send or receive called, procceed acordingly.
                            if (dir == SEND)
                            {
                                // Check the number of free bytes in the input buffer
                                if (check_status[0] < HEADER_SIZE)
                                    throw new OneWireIOException("JibComm Error - No room in header input buffer.");
                                else
                                    break;      // If all is good then break out of loop.
                            }
                            else
                            {
                                // Check number of free bytes in output buffer.
                                if (check_status[1] == 0)
                                {
                                    throw new OneWireIOException("JibComm Error - No header in output buffer.");
                                }
                                else if (check_status[1] != HEADER_SIZE)
                                    throw new OneWireIOException("JibComm Error - Bad header in output buffer.");
                                else
                                    break;      // If all is good then break out of loop.
                            }
                        }
                    }                       // if(POR)
                }                          // while(true)

                Array.Copy(check_status, 0, status, start, 4);
            }
        }   // checkStatus

        /**
         * Sets status register of this Java iButton.
         *
         * @param runTime a <code>4</code> bit value <code>(0 -15)</code>
         *                that represents the expected run time of the device
         *  Actual run time is calculated as followed:
         *  <code><pre>
         *    runTime * 250 + 62.5 mS <BR>
         *  Therefore, 0 -> 0 * 250 + 62.5 =  62.5 mS
         *             1 -> 1 * 250 + 62.5 = 312.5 mS </pre></code>
         *  and so on.
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #checkStatus
         * @see    #getStatus
         */
        public void SetStatus(int runTime)
        {
            lock (this)
            {
                if (adapter.Speed != OWSpeed.SPEED_OVERDRIVE)
                {
                    if (doDebugMessages)
                        //System.out.println("FIXING SPEED!");
                        Debug.WriteLine("FIXING SPEED!");
                    container.DoSpeed();
                }

                if (runTime > MAX_RUNTIME)
                    runTime = MAX_RUNTIME;

                // Set up the command block

                set_status_command_buffer[0] = WRITE_STATUS_COMMAND;
                set_status_command_buffer[1] = (byte)(runTime & 0x0F);
                set_status_command_buffer[2] = (byte)0xFF;   // Fill with 0xFF's to read the CRC.
                set_status_command_buffer[3] = (byte)0xFF;

                // Set up the release code block

                set_status_command_buffer[4] = (byte)0x7F;   // Release byte required by the iButton.
                set_status_command_buffer[5] = (byte)0x51;   // Rest of the Release byte.

                //this is a trick, we have to send the buffer as the release buffer
                //so that SendCommand knows we want to use pull-up
                SendCommand(null, set_status_command_buffer, false, 0);

                return;
            }
        }

        int mycksum = 0;
        /**
         * Sets header to be written to this Java iButton.
         *
         * @param header byte array with the header information
         *
         * @throws IllegalArgumentException Invalid header length
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #checkStatus
         * @see    #getHeader
         */
        public void SetHeader(byte[] header)
        {
            lock (this)
            {
                if (header.Length != HEADER_SIZE)
                    throw new ArgumentOutOfRangeException("The header must be of length "
                                                       + HEADER_SIZE);

                if (doDebugMessages)
                {
                    mycksum = 0;
                    for (int i = 0; i < header.Length; i++)
                    {
                        mycksum += (header[i] & 0x0ff);
                    }
                }

                // Set up the command block
                // set to command length + CRC + HEADER_SIZE.

                set_header_buffer[0] = WRITE_IO_BUFFER_COMMAND;
                set_header_buffer[1] = (byte)HEADER_SIZE;

                Array.Copy(header, 0, set_header_buffer, 2, HEADER_SIZE);

                set_header_buffer[10] = (byte)0xFF;   // Fill with 0xFF's to read the CRC.
                set_header_buffer[11] = (byte)0xFF;

                // Set up the release code block
                set_header_buffer[4 + HEADER_SIZE] = (byte)0xB3;   // Release byte required by the iButton.
                set_header_buffer[4 + HEADER_SIZE + 1] = (byte)0x9D;   // Rest of the Release byte.

                //this is a trick, we have to send the buffer as the release buffer
                //so that SendCommand knows we want to use pull-up
                SendCommand(null, set_header_buffer, false, 0);

                return;
            }
        }

        /**
         * Gets header from this Java iButton.
         *
         * @return header read from this Java iButton.
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #checkStatus
         * @see    #setHeader
         */
        public void GetHeader(byte[] header, int start)
        {
            lock (this)
            {
                // Set up the command block
                get_header_buffer[0] = READ_IO_BUFFER_COMMAND;
                get_header_buffer[1] = (byte)HEADER_SIZE;

                Array.Copy(ffBlock, 0, get_header_buffer, 2, 10);

                // Set up the release code block
                get_header_buffer[12] = (byte)0x4C;   // Release byte required by the iButton.
                get_header_buffer[13] = (byte)0x62;   // Rest of the Release byte.

                //this is a trick, we pass the buffer as the 'release buffer' to make
                //sure SendCommand does the right thing
                SendCommand(null, get_header_buffer, false, 0);

                Array.Copy(get_header_buffer, 2, header, start, HEADER_SIZE);
            }
        }

        /**
         * Sets data to be written to this Java iButton.
         *
         * @param data data to be written to this Java iButton
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #checkStatus
         * @see    #getData
         */
        public void SetData(byte[] data)
        {

            // Set up the command block, we can'thread make this a buffer
            // without a good deal more work
            byte[] commandBuffer = new byte[4 + data.Length];

            commandBuffer[0] = WRITE_IPR_COMMAND;
            commandBuffer[1] = (byte)data.Length;
            if (doDebugMessages)
            {
                //System.out.println("Setting data length "+data.length + "\r\n" + toHexString(data));
                Debug.WriteLine("Setting data length " + data.Length + "\r\n" + ToHexString(data));
                for (int i = 0; i < data.Length; i++)
                {
                    mycksum += (data[i] & 0x0ff);
                }
                //System.out.println("My checksum is "+Integer.toHexString(mycksum));
                Debug.WriteLine("My checksum is 0x" + mycksum.ToString("X"));
            }
            Array.Copy(data, 0, commandBuffer, 2, data.Length);

            commandBuffer[data.Length + 2] = (byte)0xFF;   // Fill with 0xFF's to read the CRC.
            commandBuffer[data.Length + 3] = (byte)0xFF;

            SendCommand(commandBuffer, null, false, 0);

            return;
        }

        char[] hex = "0123456789abcdef".ToCharArray();
        string ToHexString(byte[] b)
        {
            char[] c = new char[b.Length * 3];
            for (int i = 0; i < b.Length; i++)
            {
                c[i * 3] = hex[(b[i] >> 4) & 0x0f];
                c[i * 3 + 1] = hex[(b[i]) & 0x0f];
                c[i * 3 + 2] = ' ';
            }
            return new string(c);
        }

        /**
         * Gets data from this Java iButton.
         *
         * @param length expected number of bytes of data to be read from the IPR
         *
         * @return data from this Java iButton.
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         * @see    #checkStatus
         * @see    #setData
         */
        public byte[] GetData(int length)
        {

            // Set up the command block
            byte[] commandBuffer = new byte[length + 4];

            commandBuffer[0] = READ_IPR_COMMAND;
            commandBuffer[1] = (byte)(length & 0xFF);

            // Fill the rest with 0xFF.
            Array.Copy(ffBlock, 0, commandBuffer, 2, length + 2);
            SendCommand(commandBuffer, null, false, 0);

            byte[] data = new byte[length];

            Array.Copy(commandBuffer, 2, data, 0, length);

            return data;
        }

        /**
         * Runs the Micro in this Java iButton.
         *
         * @param runTime a <code>4</code> bit value <code>(0 -15)</code>
         *                that represents the expected run time of the device
         *  Actual run time is calculated as followed:
         *  <code><pre>
         *    runTime * 250 + 62.5 mS <BR>
         *  Therefore, 0 -> 0 * 250 + 62.5 =  62.5 mS
         *             1 -> 1 * 250 + 62.5 = 312.5 mS </pre></code>
         *  and so on.
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         */
        public void Run(int runTime)
        {
            lock (this)
            {
                long sleepTime = (runTime * RUNTIME_MULTIPLIER) + MIN_RUNTIME_IN_MILLIS;

                // Set up the release code block
                run_release_buffer[0] = RUN_MICRO_COMMAND;
                run_release_buffer[1] = (byte)0x73;   // Release code for runing micro.
                run_release_buffer[2] = (byte)0x5D;

                SendCommand(null, run_release_buffer, true, sleepTime);

                return;
            }
        }

        /**
         * Interrupts the Micro in this Java iButton.
         *
         * @param runTime a <code>4</code> bit value <code>(0 -15)</code>
         *                that represents the expected run time of the device
         *  Actual run time is calculated as followed:
         *  <code><pre>
         *    runTime * 250 + 62.5 mS <BR>
         *  Therefore, 0 -> 0 * 250 + 62.5 =  62.5 mS
         *             1 -> 1 * 250 + 62.5 = 312.5 mS </pre></code>
         *  and so on.
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         */
        public void Interrupt(int runTime)
        {
            lock (this)
            {
                long sleepTime = (runTime * RUNTIME_MULTIPLIER) + MIN_RUNTIME_IN_MILLIS;

                // Set up the release code block
                interrupt_release_buffer[0] = INTERRUPT_MICRO_COMMAND;
                interrupt_release_buffer[1] = (byte)0x43;   // Release code for intterupt function.
                interrupt_release_buffer[2] = (byte)0x6D;   // Release code.

                SendCommand(null, interrupt_release_buffer, true, sleepTime);

                return;
            }
        }

        /**
         * Resets the Micro in this Java iButton.
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         */
        public void Reset()
        {
            // Set up the release code block
            byte[] releaseBuffer = new byte[3];

            releaseBuffer[0] = RESET_MICRO_COMMAND;
            releaseBuffer[1] = (byte)0xBC;   // Release code for reset function.
            releaseBuffer[2] = (byte)0x92;   // Rest of the Release byte.

            SendCommand(null, releaseBuffer, false, 0);

            return;
        }

        /**
         * Sends command to this Java iButton.
         *
         * @param commandBuffer byte array containing the command
         * @param releaseBuffer byte array containing the release code
         * @param powerMode <code>true</code> if power supply is to be toggled
         * @param sleepTime sleep time for the program while the Micro runs.
         *                  Applicable only if <code>powerMode</code> is true.
         *
         * @throws OneWireException         Part could not be found [ fatal ]
         * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
         *
         */
        public void SendCommand(byte[] commandBuffer, byte[] releaseBuffer,
                                 Boolean powerMode, long sleepTime)
        {
            lock (this)
            {
                if (adapter.Speed != OWSpeed.SPEED_OVERDRIVE)
                {
                    if (doDebugMessages)
                        //System.out.println("FIXING SPEED!");
                        Debug.WriteLine("FIXING SPEED!");
                    container.DoSpeed();
                }

                Array.Copy(select_buffer, 0, buffer, 0, 9);
                int index = 9;
                if (commandBuffer != null)
                {
                    Array.Copy(commandBuffer, 0, buffer, index, commandBuffer.Length);
                    index += commandBuffer.Length;
                }
                if (releaseBuffer != null)
                {
                    Array.Copy(releaseBuffer, 0, buffer, index, releaseBuffer.Length);
                    index += releaseBuffer.Length;
                }

                // Select this Java iButton and send the command.
                DataBlock(buffer, 0, index);

                if (releaseBuffer != null)
                {                                 // send release code
                    if (powerMode)
                    {
                        if (adapter.CanDeliverSmartPower)
                        {
                            //              System.out.println("Can deliver smart power");
                            adapter.SetPowerDuration(OWPowerTime.DELIVERY_SMART_DONE);   // Pull line up for Power Delivery
                        }
                        else
                            adapter.SetPowerDuration(OWPowerTime.DELIVERY_INFINITE);   // Pull line up for Power Delivery

                        // Deliver after checking bit.
                        adapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BIT);

                        if (adapter.GetBit())
                            throw new OneWireIOException("Command not understood by Java iButton.");

                        if (!adapter.CanDeliverSmartPower)
                        {
                            //try
                            {
                                // Wait for power delivery to complete the conversion
                                if (doDebugMessages)
                                    //System.out.print("["+sleepTime);
                                    Debug.WriteLine("[" + sleepTime);
                                System.Threading.Thread.Sleep((int)sleepTime); // TODO: maybe convert long to TimeSpan ???
                                if (doDebugMessages)
                                    //System.out.print("]");
                                    Debug.WriteLine("]");
                            }
                            //catch (InterruptedException e)
                            //{
                            //    if (doDebugMessages) 
                            //        //System.out.println("How rude...I was interrupted");
                            //        Debug.WriteLine("How rude...I was interrupted");
                            //}
                        }

                        adapter.SetPowerNormal();   // Turn power off.
                    }
                    else
                    {
                        // Check final byte of buffer to see if the command was understood.
                        if (adapter.GetBit())
                            throw new OneWireIOException("Command not understood by Java iButton.");
                    }
                }                                 // if releaseBuffer

                //must copy the data back to their buffers, because its possible
                //there were 'reads' embedded in that data
                index = 9;
                if (commandBuffer != null)
                {
                    Array.Copy(buffer, index, commandBuffer, 0, commandBuffer.Length);
                    index += commandBuffer.Length;
                }
                if (releaseBuffer != null)
                {
                    Array.Copy(buffer, index, releaseBuffer, 0, releaseBuffer.Length);
                    index += releaseBuffer.Length;
                }
                return;
            }
        }

        /* TODO ???
            static JibComm()
            {
                //System.out.println("Debugging version January 29, 2002, 10:19");
                String s = System.GetProperty("JIBDEBUG");
                if (s!=null)
                    if (s.toUpperCase().equals("ENABLED"))
                    {
                        doDebugMessages = true;
                    }
            }
        */

        //------------------------------------------------------------------------------
        //------------- Inner class Block Data Fragmenter
        //------------------------------------------------------------------------------
        //----------------------------------------------------------------------------

        /**
         *  This class is used to handle all calculations and interpretations of
         *  block data fragmentation for this Java iButton.  It is Synchronized so
         *  that it can only be accessed by one thread at a time.  Otherwise errors
         *  could develop because of the need to hold the data over several accesses.
         *
         *  @version    0.00, 10 July 2000
         *  @author     JK
         */
        internal class BlockDataFragmenter
        {

            /**(128 arrays of size 128) */
            private long MAX_DATA_LENGTH = 128 * 128;

            /** last block mask. */
            private int FINAL_BLOCK = 0x80;

            /** data to be fragmented into blocks. */
            private byte[] dataBuffer;

            /** length of data to be fragmented. */
            private int dataBufferLength;

            /** data read back from this Java iButton. */
            private System.IO.MemoryStream /* ByteArrayOutputStream */ dataRead = new System.IO.MemoryStream();

            /** true if there are more blocks to send. */
            private Boolean more = true;

            /** header of the current block. */
            private byte[] currentHeader = new byte[HEADER_SIZE];

            /** data of the current block. */
            private byte[] currentDataBlock;

            /** length of the current block. */
            private int currentBlockLength;

            /** current block number. */
            private int currentBlockNumber = 0;

            /** number of bytes already sent to this Java iButton. */
            private int byteSent = 0;

            /** check sum on data and header sent. */
            private long checkSum = 0;

            /**
             * Constructs a block data fragmenter.
             *
             * @param data data byte array to be fragmented
             */
            public BlockDataFragmenter()
            {
                //hopefully, this won'thread ever need to be increased
                dataBuffer = new byte[200];
            }

            public void Initialize(byte[] data)
            {
                dataBufferLength = data.Length;

                if (dataBufferLength == 0)
                    throw new ArgumentOutOfRangeException("Data array cannot be empty.");
                else if (dataBufferLength > MAX_DATA_LENGTH)
                    throw new ArgumentOutOfRangeException(
                       "Data array size cannot exceed " + MAX_DATA_LENGTH);

                // Set up internal Data Holder array with passed data array.
                if (dataBuffer.Length < dataBufferLength)
                    dataBuffer = new byte[dataBufferLength];

                Array.Copy(data, 0, dataBuffer, 0, dataBufferLength);

                //initialize everything else
                dataRead.Position = 0; // TODO: Reset();
                more = true;
                currentBlockNumber = 0;
                byteSent = 0;
                checkSum = 0;
            }

            /**
             * Check if there are more blocks to send.
             *
             * @return true if there are more blocks, false otherwise.
             */
            public Boolean HasMore()
            {
                return more;
            }

            /**
             * Gets the next header to send.
             *
             * @return header for the next transfer
             */
            public byte[] GetNextHeaderToSend()
            {

                // copy instance variables to local variables
                int bSent = byteSent;
                int blkLen = currentBlockLength;
                int blkNum = currentBlockNumber;
                int dataLen = dataBufferLength;
                long chkSum = checkSum;
                int remainingDataLen = dataLen - bSent;

                // Determine length of current block of data.
                blkLen = (remainingDataLen > MAX_BLOCK_SIZE) ? MAX_BLOCK_SIZE
                                                             : remainingDataLen;

                if (remainingDataLen == blkLen)
                {
                    more = false;
                    blkNum |= FINAL_BLOCK;   // set most significant bit to one.
                }

                // Initalize to the correct size and copy over the needed data
                byte[] data = new byte[blkLen];

                Array.Copy(dataBuffer, bSent, data, 0, blkLen);

                // Compute CRC for data block
                uint crc = CRC16.Compute((uint)blkLen, 0);

                crc = CRC16.Compute(data, 0, blkLen, (uint)crc);

                byte[] header = currentHeader;

                // Build the header.
                header[0] = (byte)blkNum;
                header[1] = (byte)blkLen;
                header[2] = (byte)(remainingDataLen & 0xFF);   // low byte
                header[3] = (byte)((remainingDataLen >> 8) & 0xFF);   // high byte
                header[4] = (byte)(crc & 0xFF);                // low byte of crc
                header[5] = (byte)((crc >> 8) & 0xFF);         // high byte of crc

                // cycle through the header adding up for the check sum.
                // This step adds the block number, block length, remaing length bytes,
                // and the CRC bytes.
                chkSum += (((int)header[0]) & 0xFF);
                chkSum += (((int)header[1]) & 0xFF);
                chkSum += (((int)header[2]) & 0xFF);
                chkSum += (((int)header[3]) & 0xFF);
                chkSum += (((int)header[4]) & 0xFF);
                chkSum += (((int)header[5]) & 0xFF);

                // add in the data values.
                for (int i = 0; i < blkLen; i++)
                    chkSum += (((int)data[i]) & 0xFF);

                // place check sum into header and add the check sum bytes to itself
                header[6] = (byte)(chkSum & 0xFF);
                header[7] = (byte)((chkSum >> 8) & 0xFF);

                //bad jeremy!!!  make sure to turn 'header[6]' into a positive integer!!!
                chkSum += (header[6] & 0xff);
                chkSum += (header[7] & 0xff);

                blkNum++;   // update block number

                bSent += blkLen;   // update number of bytes sent

                // copy local variables to instance variables
                byteSent = bSent;
                currentDataBlock = data;
                currentBlockLength = blkLen;
                currentBlockNumber = blkNum;
                checkSum = chkSum;
                if (doDebugMessages)
                    //System.out.println("Checksum: "+Long.toHexString(checkSum));
                    Debug.WriteLine("Checksum: 0x" + checkSum.ToString("X"));

                return header;
            }

            /**
             * Gets the data to be sent.
             *
             * @return data that will be sent to this Java iButton.
             */
            public byte[] GetNextDataToSend()
            {
                return currentDataBlock;
            }

            /**
             * Checks if the header and data are correct.
             *
             * @param header header from this Java iButton
             * @param data   data from this Java iButton
             *
             * @throws OneWireIOException       Data wasn'thread transferred properly [ recoverable ]
             */
            public void CheckBlock(byte[] header, byte[] data)
            {

                // Compute CRC16 of the data passed.
                uint crc = CRC16.Compute(header[1], 0);

                crc = CRC16.Compute(data, 0, data.Length, crc);


                // compare this CRC16 value to the value returned in the header.
                int crcFromHeader = (((int)header[5]) & 0xFF);

                crcFromHeader <<= 8;
                crcFromHeader |= (((int)header[4]) & 0xFF);

                if (crc != crcFromHeader)
                {
                    uint correction = GetCRC16Correction(data.Length);
                    if ((crc ^ correction) != crcFromHeader)
                        throw new OneWireIOException("CRC passed in header does not match CRC computed from passed data.");
                }

                // put data into the byte array output steam for keeping, then return.
                // Here it is nessessary to dump the three trash bytes that are left over
                // from the old CIB API that are not used with this Java iButton.
                dataRead.Write(data, 3, data.Length - 3);

                return;
            }

            /**
             * Gets the data read from this Java iButton.
             *
             * @return data read from this Java iButton.
             */
            public byte[] GetDataFromRead()
            {
                return dataRead.ToArray();
            }
        }   // BlockDataFragmenter



    } // JibComm
}
