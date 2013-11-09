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
using DalSemi.OneWire.Utils; // SHA

namespace DalSemi.OneWire.Container
{

    /**
     * <P>1-Wire&#174 container for the '1K-Bit protected 1-Wire EEPROM with SHA-1
     *  Engine' family type <B>33</B> (hex), Dallas Semiconductor part number:
     * <B>DS1961S,DS2432</B>.
     *
     * <H3> Features </H3>
     * <UL>
     *   <LI> 1128 bits of 5V EEPROM memory partitioned into four pages of 256 bits,
     *        a 64-bit write-only secret and up to 5 general purpose read/write
     *        registers.
     *   <LI> On-chip 512-bit SHA-1 engine to Compute 160-bit Message Authentication
     *        Codes (MAC) and to generate secrets.
     *   <LI> Write access requires knowledge of the secret and the capability of
     *        computing and transmitting a 160-bit MAC as authorization.
     *   <LI> Secret and data memory can be write-protected (all or page 0 only) or
     *        put in EPROM-emulation mode ("write to 0", page0)
     *   <LI> unique, fatory-lasered and tested 64-bit registration number (8-bit
     *        family code + 48-bit serial number + 8-bit CRC tester) assures
     *        absolute traceablity because no two parts are alike.
     *   <LI> Built-in multidrop controller ensures compatibility with other 1-Wire
     *        net products.
     *   <LI> Reduces control, address, data and power to a single data pin.
     *   <LI> Directly connects to a single port pin of a microprocessor and
     *        communicates at up to 16.3k bits per second.
     *   <LI> Overdrive mode boosts communication speed to 142k bits per second.
     *   <LI> 8-bit family code specifies DS2432 communication requirements to reader.
     *   <LI> Presence detector acknowledges when reader first applies voltage.
     *   <LI> Low cost 6-lead TSOC surface mount package, or solder-bumped chip scale
     *        package.
     *   <LI> Reads and writes over a wide voltage range of 2.8V to 5.25V from -40C
     *        to +85C.
     * </UL>
     *
     * <P> The memory can also be accessed through the objects that are returned
     * from the {@link #getMemoryBanks() getMemoryBanks} method. </P>
     *
     * The following is a list of the MemoryBank instances that are returned:
     *
     * <UL>
     *   <LI> <B> Page Zero with write protection</B>
     *      <UL>
     *         <LI> <I> Implements </I> {@link com.dalsemi.onewire.container.MemoryBank MemoryBank},
     *                  {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     *         <LI> <I> Size </I> 32 starting at physical address 0
     *         <LI> <I> Features</I> Read/Write general-purpose non-volatile
     *         <LI> <I> Page</I> 1 page of length 32 bytes giving 29 bytes Packet data payload
     *         <LI> <I> Page Features </I> page-device-CRC and write protection.
     *      </UL>
     *   <LI> <B> Page One with EPROM mode and write protection </B>
     *      <UL>
     *         <LI> <I> Implements </I> {@link com.dalsemi.onewire.container.MemoryBank MemoryBank},
     *                  {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     *         <LI> <I> Size </I> 32 starting at physical address 32
     *         <LI> <I> Features</I> Read/Write general-purpose non-volatile
     *         <LI> <I> Page</I> 1 page of length 32 bytes giving 29 bytes Packet data payload
     *         <LI> <I> Page Features </I> page-device-CRC, EPROM mode and write protection.
     *      </UL>
     *   <LI> <B> Page Two and Three with write protection </B>
     *      <UL>
     *         <LI> <I> Implements </I> {@link com.dalsemi.onewire.container.MemoryBank MemoryBank},
     *                  {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     *         <LI> <I> Size </I> 64 starting at physical address 64
     *         <LI> <I> Features</I> Read/Write general-purpose non-volatile
     *         <LI> <I> Pages</I> 2 pages of length 32 bytes giving 29 bytes Packet data payload
     *         <LI> <I> Page Features </I> page-device-CRC and write protection.
     *      </UL>
     *   <LI> <B> Status Page that contains the secret and the status. </B>
     *      <UL>
     *         <LI> <I> Implements </I> {@link com.dalsemi.onewire.container.MemoryBank MemoryBank},
     *                  {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     *         <LI> <I> Size </I> 24 starting at physical address 128
     *         <LI> <I> Page Features </I> Contains secret and status for the iButton.
     *      </UL>
     * </UL>
     *
     * <DD> <H4> Example 1</H4>
     * Display some features of isMACValid where owd is an instanceof OneWireContainer33 and
     * bank is an instanceof PagedMemoryBank:
     * <PRE> <CODE>
     *  byte[] read_buf  = new byte [bank.getPageLength()];
     *  byte[] extra_buf = new byte [bank.getExtraInfoLength()];
     *  byte[] challenge = new byte [8];
     *
     *  // read a page (use the most verbose and secure method)
     *  if (bank.hasPageAutoCRC())
     *  {
     *     System.out.println("Using device generated CRC");
     *
     *     if (bank.hasExtraInfo())
     *     {
     *        bank.readPageCRC(pg, false, read_buf, 0, extra_buf);
     *
     *        owd.getChallenge(challenge,0);
     *        owd.getContainerSecret(secret, 0);
     *        sernum = owd.getAddress();
     *        macvalid = owd.isMACValid(bank.getStartPhysicalAddress()+pg*bank.getPageLength(),
     *                                  sernum,read_buf,extra_buf,challenge,secret);
     *     }
     *     else
     *        bank.readPageCRC(pg, false, read_buf, 0);
     *  }
     *  else
     *  {
     *     if (bank.hasExtraInfo())
     *        bank.readPage(pg, false, read_buf, 0, extra_buf);
     *     else
     *        bank.readPage(pg, false, read_buf, 0);
     *  }
     * </CODE> </PRE>
     *
     * <H3> DataSheet </H3>
     * <DL>
     * <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS2432.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS2432.pdf</A>
     * </DL>
     *
     * @see com.dalsemi.onewire.application.sha.SHAiButtonUser33
     * @version 	0.00, 19 Dec 2000
     * @author JPE
     */

    public class OneWireContainer33 : OneWireContainer
    {

        public static byte GetFamilyCode()
        {
            return 0x33;
        }

        //turns on extra debugging output in all 1-wire containers
        private const Boolean DEBUG = false;

        //--------
        //-------- Static Final Variables
        //--------

        /** Private Secret */
        private byte[] secret = new byte[8];

        /** Challenge to use for the Read Authenticate Methods */
        private byte[] challenge = new byte[8];

        /** The different memory banks for the container. */
        private MemoryBankScratchSHAEE mbScratchpad;
        private MemoryBankSHAEE memstatus;
        private MemoryBankSHAEE[] memoryPages = new MemoryBankSHAEE[4];

        /** Buffer used to hold MAC for certain calls */
        private byte[] MAC_buffer = new byte[20];

        /** Flag to indicate if the secret has been set. */
        protected Boolean secretSet;

        /** Flag to indicate if the secret is write protected. */
        protected Boolean secretProtected;

        /** Flag to indicate if the adapter has been specified. */
        protected Boolean setAdapter;

        /** Flag to indicate if the status has been checked. */
        protected Boolean container_check;

        /** block of 0xFF's used for faster read pre-fill of 1-Wire blocks */
        // RM: length was 36... is 96 now...
        public static readonly byte[] ffBlock = MemoryBankScratch.ffBlock;
        /*
                             {
                             (byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,
                             (byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,
                             (byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,
                             (byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,
                             (byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF, 
                             (byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,
                             (byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,
                             (byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,
                             (byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF }; 
        */

        /** block of 0xFF's used for faster erase of blocks */
        //private static readonly byte[] zeroBlock = {
        //                0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00 };

        /** This byte is used to set flags in the status register */
        private readonly byte[] ACTIVATION_BYTE = { 0xAA };

        /**
         * Create a container with a provided adapter object
         * and the address of the iButton or 1-Wire device.
         *
         * @param  sourceAdapter     adapter object required to communicate with
         *                           this iButton.
         * @param  newAddress        address of this 1-Wire device
         */
        public OneWireContainer33(PortAdapter sourceAdapter, byte[] newAddress)
            : base(sourceAdapter, newAddress)
        {
            Array.Copy(ffBlock, 0, secret, 0, 8);
            Array.Copy(ffBlock, 0, challenge, 0, 8);

            setAdapter = true;
            container_check = false;
            InitMem();
        }

        //--------
        //-------- Methods
        //--------

        /**
         * Tells whether an adapter has been set.
         *
         * @return boolean telling weather an adapter has been set.
         */
        public Boolean AdapterSet()
        {
            return setAdapter;
        }

        /**
         * Provide this container the adapter object used to access this device
         * and provide the address of this iButton or 1-Wire device.
         *
         * @param  sourceAdapter     adapter object required to communicate with
         *                           this iButton.
         * @param  newAddress        address of this 1-Wire device
         */
        protected override void SetupContainer(PortAdapter sourceAdapter, byte[] newAddress)
        {
            base.SetupContainer(sourceAdapter, newAddress);

            if (!setAdapter)
                InitMem();

            setAdapter = true;
        }

        /**
         * Retrieve the Dallas Semiconductor part number of the iButton
         * as a string.  For example 'DS1992'.
         *
         * @return string represetation of the iButton name.
         */
        public override string GetName()
        {
            return "DS1961S";
        }

        /**
         * Retrieve the alternate Dallas Semiconductor part numbers or names.
         * A 'family' of MicroLAN devices may have more than one part number
         * depending on packaging.
         *
         * @return  the alternate names for this iButton or 1-Wire device
         */
        public override string GetAlternateNames()
        {
            return "DS2432";
        }

        /**
         * Retrieve a short description of the function of the iButton type.
         *
         * @return string represetation of the function description.
         */
        public override string GetDescription()
        {
            return "1K-Bit protected 1-Wire EEPROM with SHA-1 Engine.";
        }

        /**
         * Returns the maximum speed this iButton can communicate at.
         *
         * @return  max. communication speed.
         */
        public override OWSpeed GetMaxSpeed()
        {
            return OWSpeed.SPEED_OVERDRIVE;
        }

        /**
         * Get an enumeration of memory bank instances that implement one or more
         * of the following interfaces:
         * {@link com.dalsemi.onewire.container.MemoryBank MemoryBank},
         * {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank},
         * and {@link com.dalsemi.onewire.container.OTPMemoryBank OTPMemoryBank}.
         * @return <CODE>Enumeration</CODE> of memory banks
         */
        public override MemoryBankList GetMemoryBanks()
        {
            MemoryBankList bank_vector = new MemoryBankList();

            bank_vector.Add(mbScratchpad);
            bank_vector.Add(memoryPages[0]);
            bank_vector.Add(memoryPages[1]);
            bank_vector.Add(memoryPages[2]);
            bank_vector.Add(memstatus);

            return bank_vector;
        }



        /**
         * Returns the instance of the Scratchpad memory bank.  Contains
         * methods for reading/writing the Scratchpad contents.  Also,
         * methods for Load First Secret, Compute Next Secret, and
         * Refresh Scratchpad
         *
         * @return the instance of the Scratchpad memory bank
         */
        public MemoryBankScratchSHAEE GetScratchpadMemoryBank()
        {
            return mbScratchpad;
        }

        /**
         * Returns the instance of the Status page memory bank.
         *
         * @return the instance of the Status page memory bank
         */
        public MemoryBankSHAEE GetStatusPageMemoryBank()
        {
            return memstatus;
        }

        /**
         * Returns the instance of the memory bank for a particular page
         *
         * @param page the page for the requested memory bank;
         *
         * @return the instance of the memory bank for the specified page
         */
        public MemoryBankSHAEE GetMemoryBankForPage(int page)
        {
            if (page == 3)
                page = 2;
            return memoryPages[page];
        }

        /**
         * Sets the bus master secret for this DS2432.
         *
         * @param newSecret Secret for this DS2432.
         * @param offset index into array to copy the secret from
         */
        public void SetContainerSecret(byte[] newSecret, int offset)
        {
            Array.Copy(newSecret, offset, secret, 0, 8);
            secretSet = true;
        }
        /**
         * Get the secret of this device as an array of bytes.
         *
         * @param secretBuf array of bytes for holding the container secret
         * @param offset index into array to copy the secret to
         */
        public void GetContainerSecret(byte[] secretBuf, int offset)
        {
            Array.Copy(secret, 0, secretBuf, offset, 8);
        }
        /**
         * Get the current status of the secret.
         *
         * @return  boolean telling if the secret is set
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean IsContainerSecretSet()
        {
            //if(!container_check)
            //   container_check = this.checkStatus();

            return (secretSet);
        }
        /**
         * Get the status of the secret, if it is write protected.
         *
         * @return  boolean telling if the secret is write protected.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean IsSecretWriteProtected()
        {
            if (!container_check)
                container_check = this.CheckStatus();

            return secretProtected;
        }

        /**
         * Sets the challenge for the Read Authenticate Page
         *
         * @param challengeset  Challenge for all the memory banks.
         */
        public void SetChallenge(byte[] challengeset, int offset)
        {
            Array.Copy(challengeset, 0, challenge, offset, 3);
        }
        /**
         * Get the challenge of this device as an array of bytes.
         *
         * @param get  array of bytes containing the iButton challenge
         */
        public void GetChallenge(byte[] get, int offset)
        {
            Array.Copy(challenge, 0, get, offset, 3);
        }

        /**
         * Get the status of all the pages, if they are write protected.
         *
         * @return  boolean telling if all the pages are write protected.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean IsWriteProtectAllSet()
        {
            if (!container_check)
                container_check = this.CheckStatus();

            return memoryPages[2].IsReadOnly();
        }

        /**
         *  Write protects the secret for the DS2432.
         */
        public void WriteProtectSecret()
        {
            if (!container_check)
                container_check = this.CheckStatus();

            // write protect secret
            //  - write status byte to address
            //    ((8h) + physical) = 88h
            memstatus.Write(8, ACTIVATION_BYTE, 0, 1);

            secretProtected = true;
        }

        /**
         * Write protect pages 0 to 3
         */
        public void WriteProtectAll()
        {
            if (!container_check)
                container_check = this.CheckStatus();

            // write protect all pages
            //  - write status byte to address
            //    ((9h) + physical) = 89h
            memstatus.Write(9, ACTIVATION_BYTE, 0, 1);

            memoryPages[0].WriteProtect();
            memoryPages[1].WriteProtect();
            memoryPages[2].WriteProtect();
        }

        /**
         * Sets the EPROM mode for page 1.  After setting, Page One can only be written to once.
         */
        public void WetEPROMModePageOne()
        {
            if (!container_check)
                container_check = this.CheckStatus();

            // Enable EPROM mode for page 1.
            //  - write status byte to address
            //    ((12h) + physical) = 8Ch
            memstatus.Write(12, ACTIVATION_BYTE, 0, 1);

            memoryPages[1].SetEPROM();
        }

        /**
         * Tells if page one is in EPROM mode.
         *
         * @return  boolean telling if page one is in EPROM mode.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean IsPageOneEPROMmode()
        {
            if (!container_check)
                container_check = this.CheckStatus();

            return memoryPages[1].IsWriteOnce();
        }

        /**
         * Write protect page zero only.
         */
        public void WriteProtectPageZero()
        {
            if (!container_check)
                container_check = this.CheckStatus();

            // Enable Write protection for page zero.
            //  - write status byte to address
            //    ((13h) + physical) = 8Dh
            memstatus.Write(13, ACTIVATION_BYTE, 0, 1);

            memoryPages[0].WriteProtect();
        }
        /**
         * Get the status of page zero, if it is write protected.
         *
         * @return  boolean telling if page zero is write protected.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean isWriteProtectPageZeroSet()
        {
            if (!container_check)
                container_check = this.CheckStatus();

            return memoryPages[0].IsReadOnly();
        }

        /**
         * Compute Next Secret
         *
         * @param addr address of page to use for the next secret computation.
         * @param parialsecret the data to put into the scrathpad in computing next secret.
         */
        public void ComputeNextSecret(int pageNum, byte[] partialsecret, int offset)
        {
            if (!container_check)
                container_check = this.CheckStatus();

            mbScratchpad.ComputeNextSecret(pageNum * 32, partialsecret, offset);
        }

        /**
         * Compute Next Secret using the current contents of data page and scratchpad.
         *
         * @param addr address of page to use for the next secret computation.
         */
        public void ComputeNextSecret(int pageNum)
        {
            if (!container_check)
                container_check = this.CheckStatus();

            mbScratchpad.ComputeNextSecret(pageNum * 32);
        }

        /**
         * Load First Secret
         *
         * @return              boolean saying if first secret was loaded
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean LoadFirstSecret(byte[] data, int offset)
        {
            if (!container_check)
                container_check = this.CheckStatus();

            mbScratchpad.LoadFirstSecret(0x080, data, offset);
            return true;
        }

        /**
         * Refreshes a particular 8-byte set of data on a given page.
         * This will correct any weakly-programmed EEPROM bits.  This
         * feature is only available on the DS1961S, but is safely
         * ignored on the DS2432.  The refresh consists of a Refresh Scratchpad
         * command followed by a Load First Secret to the same offset.
         *
         * @param page the page number that contains the 8-bytes to refresh.
         * @param offset the offset into the page for the 8-bytes to refresh.
         *
         * @return <code>true</code> if refresh is successful.
         */
        public Boolean RefreshPage(int page, int offset)
        {
            if (!container_check)
                container_check = this.CheckStatus();

            int addr = page * 32 + offset;
            try
            {
                mbScratchpad.RefreshScratchpad(addr);
            }
            catch (OneWireException)
            {
                // only return false for the DS2432 devices
                // which do not support this command
                return false;
            }

            mbScratchpad.LoadFirstSecret(addr);
            return true;
        }

        /**
         * Refreshes all 32 bytes of data on a given page.
         * This will correct any weakly-programmed EEPROM bits.  This
         * feature is only available on the DS1961S, but is safely
         * ignored on the DS2432.  The refresh consists of a Refresh Scratchpad
         * command followed by a Load First Secret to the same offset, for
         * all 8-byte offsets on the page.
         *
         * @param page the page number that will be refreshed.
         *
         * @return <code>true</code> if refresh is successful.
         */
        public Boolean RefreshPage(int page)
        {
            return RefreshPage(page, 0) &&
                   RefreshPage(page, 8) &&
                   RefreshPage(page, 16) &&
                   RefreshPage(page, 24);
        }

        /**
         * To check the status of the part.
         *
         * @return boolean saying the part has been checked or was checked.
         */
        public Boolean CheckStatus()
        {
            if (!container_check)
            {
                byte[] mem = new byte[8];

                memstatus.Read(8, false, mem, 0, 8);

                if ((mem[0] == (byte)0xAA) || (mem[0] == (byte)0x55))
                    secretProtected = true;
                else
                    secretProtected = false;

                if (((mem[1] == (byte)0xAA) || (mem[1] == (byte)0x55)) ||
                    ((mem[5] == (byte)0xAA) || (mem[5] == (byte)0x55)))
                {
                    memoryPages[0].readWrite = false;
                    memoryPages[0].readOnly = true;
                }
                else
                {
                    memoryPages[0].readWrite = true;
                    memoryPages[0].readOnly = false;
                }


                if ((mem[4] == (byte)0xAA) || (mem[4] == (byte)0x55))
                    memoryPages[1].writeOnce = true;
                else
                    memoryPages[1].writeOnce = false;

                if ((mem[5] == (byte)0xAA) || (mem[5] == (byte)0x55))
                {
                    memoryPages[1].readWrite = false;
                    memoryPages[1].readOnly = true;
                }
                else
                {
                    memoryPages[1].readWrite = true;
                    memoryPages[1].readOnly = false;
                }

                if ((mem[5] == (byte)0xAA) || (mem[5] == (byte)0x55))
                {
                    memoryPages[2].readWrite = false;
                    memoryPages[2].readOnly = true;
                }
                else
                {
                    memoryPages[2].readWrite = true;
                    memoryPages[2].readOnly = false;
                }

                memstatus.checked_ = true;
                memoryPages[0].checked_ = true;
                memoryPages[1].checked_ = true;
                memoryPages[2].checked_ = true;
                container_check = true;
            }

            return container_check;
        }

        /**
         * Initialize the memory banks and data associated with each.
         */
        private void InitMem()
        {
            secretSet = false;

            mbScratchpad = new MemoryBankScratchSHAEE(this);

            // Set memory bank variables for the status and secret page
            memstatus = new MemoryBankSHAEE(this, mbScratchpad);
            memstatus.bankDescription = "Status Page that contains the secret and the status.";
            memstatus.generalPurposeMemory = true;
            memstatus.startPhysicalAddress = 128;
            memstatus.size = 32;
            memstatus.numberPages = 1;
            memstatus.pageLength = 32;
            memstatus.maxPacketDataLength = 32 - 3;
            memstatus.extraInfo = false;
            memstatus.extraInfoLength = 20;
            memstatus.readWrite = false;
            memstatus.writeOnce = false;
            memstatus.pageCRC = false;
            memstatus.readOnly = false;
            memstatus.checked_ = false;

            // Set memory bank variables
            memoryPages[0] = new MemoryBankSHAEE(this, mbScratchpad);
            memoryPages[0].bankDescription = "Page Zero with write protection.";
            memoryPages[0].generalPurposeMemory = true;
            memoryPages[0].startPhysicalAddress = 0;
            memoryPages[0].size = 32;
            memoryPages[0].numberPages = 1;
            memoryPages[0].pageLength = 32;
            memoryPages[0].maxPacketDataLength = 32 - 3;
            memoryPages[0].extraInfo = true;
            memoryPages[0].extraInfoLength = 20;
            memoryPages[0].writeOnce = false;
            memoryPages[0].pageCRC = true;
            memoryPages[0].checked_ = false;

            // Set memory bank varialbes
            memoryPages[1] = new MemoryBankSHAEE(this, mbScratchpad);
            memoryPages[1].bankDescription = "Page One with EPROM mode and write protection.";
            memoryPages[1].generalPurposeMemory = true;
            memoryPages[1].startPhysicalAddress = 32;
            memoryPages[1].size = 32;
            memoryPages[1].numberPages = 1;
            memoryPages[1].pageLength = 32;
            memoryPages[1].maxPacketDataLength = 32 - 3;
            memoryPages[1].extraInfo = true;
            memoryPages[1].extraInfoLength = 20;
            memoryPages[1].pageCRC = true;
            memoryPages[1].checked_ = false;

            // Set memory bank varialbes
            memoryPages[2] = new MemoryBankSHAEE(this, mbScratchpad);
            memoryPages[2].bankDescription = "Page Two and Three with write protection.";
            memoryPages[2].generalPurposeMemory = true;
            memoryPages[2].startPhysicalAddress = 64;
            memoryPages[2].size = 64;
            memoryPages[2].numberPages = 2;
            memoryPages[2].pageLength = 32;
            memoryPages[2].maxPacketDataLength = 32 - 3;
            memoryPages[2].extraInfo = true;
            memoryPages[2].extraInfoLength = 20;
            memoryPages[2].writeOnce = false;
            memoryPages[2].pageCRC = true;
            memoryPages[2].checked_ = false;

            memoryPages[3] = memoryPages[2];
        }

        /**
         *  Authenticates page data given a MAC.
         *
         * @param addr          address of the data to be read
         * @param memory        the memory read from the page
         * @param mac           the MAC calculated for this function given back as the extra info
         * @param challenge     the 3 bytes written to the scratch pad used in calculating the mac
         *
         */
        public static Boolean IsMACValid(int addr, byte[] SerNum, byte[] memory, byte[] mac,
                                         byte[] challenge, byte[] secret)
        {
            byte[] MT = new byte[64];

            Array.Copy(secret, 0, MT, 0, 4);
            Array.Copy(memory, 0, MT, 4, 32);
            Array.Copy(ffBlock, 0, MT, 36, 4);

            MT[40] = (byte)(0x40 |
                              (((addr) << 3) & 0x08) |
                              (((addr) >> 5) & 0x07));

            Array.Copy(SerNum, 0, MT, 41, 7);
            Array.Copy(secret, 4, MT, 48, 4);
            Array.Copy(challenge, 0, MT, 52, 3);

            // finish up with proper padding
            MT[55] = (byte)0x80;
            for (int i = 56; i < 62; i++)
                MT[i] = (byte)0x00;
            MT[62] = (byte)0x01;
            MT[63] = (byte)0xB8;

            uint[] AtoE = new uint[5];
            SHA.ComputeSHA(MT, AtoE);

            int cnt = 0;
            for (int i = 0; i < 5; i++)
            {
                uint temp = AtoE[4 - i];
                for (int j = 0; j < 4; j++)
                {
                    if (mac[cnt++] != (byte)(temp & 0x0FF))
                    {
                        return false;
                    }
                    temp >>= 8;
                }
            }

            return true;
        }

        /**
         * <p>Installs a secret on a DS1961S/DS2432.  The secret is written in partial phrases
         * of 47 bytes (32 bytes to a memory page, 8 bytes to the scratchpad, 7 bytes are
         * discarded (but included for compatibility with DS193S)) and
         * is cumulative until the entire secret is processed.</p>
         *
         * <p>On TINI, this method will be slightly faster if the secret's length is divisible by 47.
         * However, since secret key generation is a part of initialization, it is probably
         * not necessary.</p>
         *
         * @param page the page number used to write the partial secrets to
         * @param secret the entire secret, in partial phrases, to be installed
         *
         * @return <code>true</code> if successful
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         *
         * @see #bindSecretToiButton(int,byte[])
         */
        public Boolean InstallMasterSecret(int page, byte[] newSecret)
        {
            for (int i = 0; i < secret.Length; i++)
                secret[i] = 0x00;
            if (LoadFirstSecret(secret, 0))
            {
                if (newSecret.Length == 0)
                    return false;

                byte[] input_secret = null;
                byte[] sp_buffer = new byte[8];
                int secret_mod_length = newSecret.Length % 47;

                if (secret_mod_length == 0)   //if the length of the secret is divisible by 40
                    input_secret = newSecret;
                else
                {
                    input_secret = new byte[newSecret.Length + (47 - secret_mod_length)];

                    Array.Copy(newSecret, 0, input_secret, 0, newSecret.Length);
                }

                for (int i = 0; i < input_secret.Length; i += 47)
                {
                    WriteDataPage(page, input_secret, i);
                    Array.Copy(input_secret, i + 36, sp_buffer, 0, 8);
                    mbScratchpad.ComputeNextSecret(page * 32, sp_buffer, 0);
                }
                return true;
            }
            else
                throw new OneWireException("Load first secret failed");
        }

        /**
         * <p>Binds an installed secret to a DS1961S/DS2432 by using
         * well-known binding data and the DS1961S/DS2432's unique
         * address.  This makes the secret unique
         * for this iButton.</p>
         *
         * @param page the page number that has the master secret already installed
         * @param bind_data 32 bytes of binding data used to bind the iButton to the system
         *
         * @return <code>true</code> if successful
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         *
         * @see #installMasterSecret(int,byte[])
         */
        public Boolean BindSecretToiButton(int pageNum, byte[] bindData)
        {
            if (!WriteDataPage(pageNum, bindData))
                return false;

            byte[] bind_code = new byte[8];
            bind_code[0] = (byte)pageNum;
            Array.Copy(address, 0, bind_code, 1, 7);
            mbScratchpad.ComputeNextSecret(pageNum * 32, bind_code, 0);

            return true;
        }

        /**
         * <p>Writes a data page to the DS1961S/DS2432.</p>
         *
         * @param page_number page number to write
         * @param page_data page data to write (must be at least 32 bytes long)
         *
         * @return <code>true</code> if successful, <code>false</code> if the operation
         *         failed
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         */
        public Boolean WriteDataPage(int targetPage, byte[] pageData)
        {
            return WriteDataPage(targetPage, pageData, 0);
        }

        /**
         * <p>Writes a data page to the DS1961S/DS2432.</p>
         *
         * @param page_number page number to write
         * @param page_data page data to write (must be at least 32 bytes long)
         * @param offset the offset to start copying the 32-bytes of page data.
         *
         * @return <code>true</code> if successful, <code>false</code> if the operation
         *         failed
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         */
        public Boolean WriteDataPage(int targetPage, byte[] pageData, int offset)
        {
            int addr = 0;
            if (targetPage == 3)
                addr = 32;
            memoryPages[targetPage].Write(addr, pageData, offset, 32);
            return true;
        }

        /**
         * <p>Writes data to the scratchpad.  In order to write to a data page using this method,
         * next call <code>readScratchPad()</code>, and then <code>copyScratchPad()</code>.
         * Note that the addresses passed to this method will be the addresses the data is
         * copied to if the <code>copyScratchPad()</code> method is called afterward.</p>
         *
         * <p>Also note that if too many bytes are written, this method will truncate the
         * data so that only a valid number of bytes will be sent.</p>
         *
         * @param targetPage the page number this data will eventually be copied to
         * @param targetPageOffset the offset on the page to copy this data to
         * @param inputbuffer the data that will be copied into the scratchpad
         * @param start offset into the input buffer for the data to write
         * @param length number of bytes to write
         *
         * @return <code>true</code> if successful, <code>false</code> on a CRC error
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         */
        public Boolean WriteScratchpad(int targetPage,
                int targetPageOffset, byte[] inputbuffer, int start, int length)
        {
            int addr = (targetPage << 5) + targetPageOffset;
            mbScratchpad.WriteScratchpad(addr, inputbuffer, start, length);
            return true;
        }

        /**
         * Read from the Scratch Pad, which is a max of 8 bytes.
         *
         * @param  scratchpad    byte array to place read data into
         *                       length of array is always pageLength.
         * @param  offset        offset into readBuf to pug data
         * @param  extraInfo     byte array to put extra info read into
         *                       (TA1, TA2, e/s byte)
         *                       Can be 'null' if extra info is not needed.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public void ReadScratchpad(byte[] scratchpad, int offset,
                                    byte[] extraInfo)
        {
            mbScratchpad.ReadScratchpad(scratchpad, offset, 8, extraInfo);
        }

        /**
         * Copy all 8 bytes of the Sratch Pad to a certain page and offset in memory.
         *
         * @param targetPage the page to copy the data to
         * @param targetPageOffset the offset into the page to copy to
         * @param copy_auth byte[] containing write authorization
         * @param authStart the offset into the copy_auth array where the authorization begins.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean CopyScratchpad(int targetPage, int targetPageOffset,
                                       byte[] copy_auth, int authStart)
        {
            int addr = (targetPage << 5) + targetPageOffset;
            mbScratchpad.CopyScratchpadWithMAC(addr, copy_auth, authStart);
            return true;
        }

        /**
         * Copy all 8 bytes of the Sratch Pad to a certain page and offset in memory.
         *
         * The container secret must be set so that the container can produce the
         * correct MAC.
         *
         * @param targetPage the page to copy the data to
         * @param targetPageOffset the offset into the page to copy to
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean CopyScratchpad(int targetPage, int targetPageOffset)
        {
            int addr = (targetPage << 5) + targetPageOffset;
            mbScratchpad.CopyScratchpad(addr, 8);

            return true;
        }

        /**
         * Reads a page of memory..
         *
         * @param  page          page number to read packet from
         * @param  pageData       byte array to place read data into
         * @param  offset        offset into readBuf to place data
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean ReadMemoryPage(int page, byte[] pageData, int offset)
        {
            int addr = 0;
            if (page == 3)
                addr = 32;
            memoryPages[page].Read(addr, false, pageData, offset, 32);
            return true;
        }

        /**
         * <p>Reads and authenticates a page.  See <code>readMemoryPage()</code> for a description
         * of page numbers and their contents.  This method will also generate a signature for the
         * selected page, used in the authentication of roving (User) iButtons.</p>
         *
         * @param pageNum page number to read and authenticate
         * @param pagedata array for the page data.
         * @param offset offset to copy into the array
         * @param computed_mac array for the MAC returned by the device.
         * @param macStart offset to copy into the mac array
         *
         * @return <code>true</code> if successful, <code>false</code> if the operation
         *         failed while waiting for the DS1963S's output to alternate
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         reading an incorrect CRC from a 1-Wire device.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter
         */
        public Boolean ReadAuthenticatedPage(int page,
                                             byte[] pagedata, int offset,
                                             byte[] computed_mac, int macStart)
        {
            int mbPage = 0;
            if (page == 3)
            {
                mbPage = 1;
                page = 2;
            }
            return memoryPages[page].ReadAuthenticatedPage(mbPage,
                                                           pagedata, offset,
                                                           computed_mac, macStart);
        }


    }
}
