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

using DalSemi.OneWire.Adapter; // OneWireIOException
using DalSemi.Utils; // CRC16

namespace DalSemi.OneWire.Container
{

    /**
     * Memory bank class for the NVRAM with built-in CRC generation and Password
     * protected memory read/write iButtons and 1-Wire Devices.  An example of
     * such a devices is the DS1922 Thermochron with 8k or password protected
     * log memory.
     *
     *  @version    1.00, 11 Feb 2002
     *  @author     SH
     */

    public class MemoryBankNVCRCPW : MemoryBankNVCRC
    {

        /**
         * Read Memory (with CRC and Password) Command
         */
        public const byte READ_MEMORY_CRC_PW_COMMAND = (byte)0x69;

        /**
         * Scratchpad with Password.  Used as container for password.
         */
        protected MemoryBankScratchCRCPW scratchpadPW = null;

        /**
         * Password Container to access the passwords for the memory bank.
         */
        protected PasswordContainer ibPass = null;

        /**
         * Enable Provided Power for some Password checking.
         */
        public Boolean enablePower = false;

        /**
         * Memory bank contstuctor.  Requires reference to the OneWireContainer
         * this memory bank resides on.
         */
        public MemoryBankNVCRCPW(PasswordContainer ibutton, MemoryBankScratchCRCPW scratch)
            : base((OneWireContainer)ibutton, scratch)
        {
            ibPass = ibutton;

            // initialize attributes of this memory bank
            pageAutoCRC = true;
            readContinuePossible = true;
            numVerifyBytes = 0;

            scratchpadPW = scratch;
        }

        /**
         * Read a complete memory page with CRC verification provided by the
         * device with extra information.  Not supported by all devices.
         * See the method 'hasPageAutoCRC()'.
         * See the method 'hasExtraInfo()' for a description of the optional
         * extra information.
         *
         * @param  page          page number to read
         * @param  readContinue  if 'true' then device read is continued without
         *                       re-selecting.  This can only be used if the new
         *                       readPagePacket() continious where the last one
         *                       stopped and it is inside a
         *                       'beginExclusive/endExclusive' block.
         * @param  readBuf       byte array to put data read. Must have at least
         *                       'getMaxPacketDataLength()' elements.
         * @param  offset        offset into readBuf to place data
         * @param  extraInfo     byte array to put extra info read into
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public override void ReadPageCRC(int page, Boolean readContinue, byte[] readBuf,
                                 int offset, byte[] extraInfo)
        {
            ReadPageCRC(page, readContinue, readBuf, offset, extraInfo,
                        extraInfoLength);
        }

        /**
         * Read a complete memory page with CRC verification provided by the
         * device with extra information.  Not supported by all devices.
         * If not extra information available then just call with extraLength=0.
         *
         * @param  page          page number to read
         * @param  readContinue  if 'true' then device read is continued without
         *                       re-selecting.  This can only be used if the new
         *                       readPagePacket() continious where the last one
         *                       stopped and it is inside a
         *                       'beginExclusive/endExclusive' block.
         * @param  readBuf       byte array to put data read. Must have at least
         *                       'getMaxPacketDataLength()' elements.
         * @param  offset        offset into readBuf to place data
         * @param  extraInfo     byte array to put extra info read into
         * @param  extraLength   length of extra information
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        protected override void ReadPageCRC(int page, Boolean readContinue,
                                    byte[] readBuf, int offset, byte[] extraInfo,
                                    int extraLength)
        {
            uint last_crc = 0;
            byte[] raw_buf;

            // only needs to be implemented if supported by hardware
            if (!pageAutoCRC)
                throw new OneWireException(
                   "Read page with CRC not supported in this memory bank");

            // attempt to put device at max desired speed
            if (!readContinue)
                sp.CheckSpeed();

            // check if read exceeds memory
            if (page > numberPages)
                throw new OneWireException("Read exceeds memory bank end");

            // see if need to access the device
            if (!readContinue || !readContinuePossible)
            {

                // select the device
                if (!ib.Adapter.SelectDevice(ib.Address))
                {
                    sp.ForceVerify();

                    throw new OneWireIOException("Device select failed");
                }

                // build start reading memory block
                raw_buf = new byte[11];
                raw_buf[0] = READ_MEMORY_CRC_PW_COMMAND;

                int addr = page * pageLength + startPhysicalAddress;

                raw_buf[1] = (byte)(addr & 0xFF);
                raw_buf[2] = (byte)(((addr & 0xFFFF) >> 8) & 0xFF);

                if (ibPass.IsContainerReadWritePasswordSet())
                    ibPass.GetContainerReadWritePassword(raw_buf, 3);
                else
                    ibPass.GetContainerReadOnlyPassword(raw_buf, 3);

                // perform CRC16 on first part (without the password)
                last_crc = CRC16.Compute(raw_buf, 0, 3, (uint)last_crc);

                // do the first block for command, TA1, TA2, and password

                if (enablePower)
                {
                    ib.Adapter.DataBlock(raw_buf, 0, 10);

                    ib.Adapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);

                    ib.Adapter.PutByte(raw_buf[10]);

                    System.Threading.Thread.Sleep(10);

                    ib.Adapter.SetPowerNormal();
                }
                else
                    ib.Adapter.DataBlock(raw_buf, 0, 11);
            }

            // pre-fill with 0xFF
            raw_buf = new byte[pageLength + extraLength + 2 + numVerifyBytes];

            Array.Copy(ffBlock, 0, raw_buf, 0, raw_buf.Length);

            // send block to read data + extra info? + crc
            ib.Adapter.DataBlock(raw_buf, 0, raw_buf.Length);

            // check the CRC
            if (CRC16.Compute(raw_buf, 0, raw_buf.Length - numVerifyBytes, last_crc)
                    != 0x0000B001)
            {
                sp.ForceVerify();
                throw new OneWireIOException("Invalid CRC16 read from device.  Password may be incorrect.");
            }

            // extract the page data
            Array.Copy(raw_buf, 0, readBuf, offset, pageLength);

            // optional extract the extra info
            if (extraInfo != null)
                Array.Copy(raw_buf, pageLength, extraInfo, 0, extraLength);
        }

        /**
         * Read  memory in the current bank with no CRC checking (device or
         * data). The resulting data from this API may or may not be what is on
         * the 1-Wire device.  It is recommends that the data contain some kind
         * of checking (CRC) like in the readPagePacket() method or have
         * the 1-Wire device provide the CRC as in readPageCRC().  readPageCRC()
         * however is not supported on all memory types, see 'hasPageAutoCRC()'.
         * If neither is an option then this method could be called more
         * then once to at least verify that the same thing is read consistantly.
         *
         * @param  startAddr     starting physical address
         * @param  readContinue  if 'true' then device read is continued without
         *                       re-selecting.  This can only be used if the new
         *                       read() continious where the last one led off
         *                       and it is inside a 'beginExclusive/endExclusive'
         *                       block.
         * @param  readBuf       byte array to place read data into
         * @param  offset        offset into readBuf to place data
         * @param  len           length in bytes to read
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public override void Read(int startAddr, Boolean readContinue, byte[] readBuf,
                          int offset, int len)
        {
            int i;
            byte[] raw_buf = new byte[11];

            // attempt to put device at max desired speed
            if (!readContinue)
            {
                sp.CheckSpeed();
            }

            // check if read exceeds memory
            if ((startAddr + len) > size)
                throw new OneWireException("Read exceeds memory bank end");

            // see if need to access the device
            if (!readContinue)
            {

                // select the device
                if (!ib.Adapter.SelectDevice(ib.Address))
                {
                    sp.ForceVerify();

                    throw new OneWireIOException("Device select failed");
                }

                // build start reading memory block
                int addr = startAddr + startPhysicalAddress;

                raw_buf[0] = READ_MEMORY_CRC_PW_COMMAND;

                raw_buf[1] = (byte)(addr & 0xFF);
                raw_buf[2] = (byte)(((addr & 0xFFFF) >> 8) & 0xFF);

                if (ibPass.IsContainerReadWritePasswordSet())
                    ibPass.GetContainerReadWritePassword(raw_buf, 3);
                else
                    ibPass.GetContainerReadOnlyPassword(raw_buf, 3);

                // do the first block for command, address. password
                if (enablePower)
                {
                    ib.Adapter.DataBlock(raw_buf, 0, 10);
                    ib.Adapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);
                    ib.Adapter.PutByte(raw_buf[10]);

                    System.Threading.Thread.Sleep(10);

                    ib.Adapter.SetPowerNormal();
                }
                else
                {
                    ib.Adapter.DataBlock(raw_buf, 0, 11);
                }
            }

            // pre-fill readBuf with 0xFF
            Boolean finished = false;
            int pgs = len / pageLength;
            int extra = len % pageLength;
            int add = pageLength - (startAddr % pageLength);

            if ((len < pageLength) && enablePower)
            {
                if (((startAddr % pageLength) > ((startAddr + len) % pageLength)) &&
                    (((startAddr + len) % pageLength) != 0))
                {
                    Array.Copy(ffBlock, 0, readBuf, offset, add);
                    Array.Copy(ffBlock, 0, raw_buf, 0, 2);

                    ib.Adapter.DataBlock(readBuf, offset, add);
                    raw_buf[0] = (byte)ib.Adapter.GetByte();
                    ib.Adapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);
                    raw_buf[1] = (byte)ib.Adapter.GetByte();

                    System.Threading.Thread.Sleep(10);

                    ib.Adapter.SetPowerNormal();

                    Array.Copy(ffBlock, 0, readBuf, offset + add,
                                     len - add);

                    ib.Adapter.DataBlock(readBuf, (offset + add), extra);

                    finished = true;
                }
                else
                {
                    Array.Copy(ffBlock, 0, readBuf, offset, len);

                    ib.Adapter.DataBlock(readBuf, offset, len);

                    finished = true;
                }
            }

            for (i = 0; i < pgs; i++)
            {
                if (!enablePower)
                {
                    if (i == 0)
                    {
                        Array.Copy(ffBlock, 0, readBuf, offset, add);
                        ib.Adapter.DataBlock(readBuf, offset, add);
                        raw_buf[0] = (byte)ib.Adapter.GetByte();
                        raw_buf[1] = (byte)ib.Adapter.GetByte();
                    }
                    else
                    {
                        Array.Copy(ffBlock, 0, readBuf,
                                        offset + add + ((i - 1) * pageLength), pageLength);

                        ib.Adapter.DataBlock(readBuf, offset + add + ((i - 1) * pageLength),
                                             pageLength);
                        raw_buf[0] = (byte)ib.Adapter.GetByte();
                        raw_buf[1] = (byte)ib.Adapter.GetByte();
                    }
                }
                else
                {
                    if (i == 0)
                    {
                        Array.Copy(ffBlock, 0, readBuf, offset, add);
                        Array.Copy(ffBlock, 0, raw_buf, 0, 2);

                        ib.Adapter.DataBlock(readBuf, offset, add);
                        raw_buf[0] = (byte)ib.Adapter.GetByte();
                        ib.Adapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);
                        raw_buf[1] = (byte)ib.Adapter.GetByte();

                        System.Threading.Thread.Sleep(10);

                        ib.Adapter.SetPowerNormal();
                    }
                    else
                    {
                        Array.Copy(ffBlock, 0, readBuf,
                                        offset + add + ((i - 1) * pageLength), pageLength);
                        Array.Copy(ffBlock, 0, raw_buf, 0, 2);

                        ib.Adapter.DataBlock(readBuf, offset + add + ((i - 1) * pageLength),
                                             pageLength);
                        raw_buf[0] = (byte)ib.Adapter.GetByte();
                        ib.Adapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);
                        raw_buf[1] = (byte)ib.Adapter.GetByte();

                        System.Threading.Thread.Sleep(10);

                        ib.Adapter.SetPowerNormal();
                    }
                }
            }

            if (!enablePower)
            {
                Array.Copy(ffBlock, 0, readBuf, offset + pgs * pageLength, extra);

                // send second block to read data, return result
                ib.Adapter.DataBlock(readBuf, offset + pgs * pageLength, extra);
            }
            else
            {
                if (!finished)
                {
                    Array.Copy(ffBlock, 0, readBuf,
                                    offset + add + ((i - 1) * pageLength),
                                    len - add - ((i - 1) * pageLength));

                    ib.Adapter.DataBlock(readBuf, offset + add + ((i - 1) * pageLength),
                                         len - add - ((i - 1) * pageLength));
                }
            }

        }

        /**
         * Write  memory in the current bank.  It is recommended that
         * when writing  data that some structure in the data is created
         * to provide error free reading back with read().  Or the
         * method 'writePagePacket()' could be used which automatically
         * wraps the data in a length and CRC.
         *
         * When using on Write-Once devices care must be taken to write into
         * into empty space.  If write() is used to write over an unlocked
         * page on a Write-Once device it will fail.  If write verification
         * is turned off with the method 'setWriteVerification(false)' then
         * the result will be an 'AND' of the existing data and the new data.
         *
         * @param  startAddr     starting address
         * @param  writeBuf      byte array containing data to write
         * @param  offset        offset into writeBuf to get data
         * @param  len           length in bytes to write
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public override void Write(int startAddr, byte[] writeBuf, int offset, int len)
        {
            // find the last (non-inclusive) address for this write
            int endingOffset = (startAddr + len);
            if (((endingOffset & 0x1F) > 0) && (!enablePower))
            {
                // find the number of bytes left until the end of the page
                int numBytes = pageLength - (endingOffset & 0x1F);
                if ( // endingOffset == 0x250 ???? why??
                   (
                      ibPass.HasReadWritePassword() &&
                      (0xFFE0 & endingOffset) == (0xFFE0 & ibPass.GetReadWritePasswordAddress()) &&
                      endingOffset < (ibPass.GetReadWritePasswordAddress() + ibPass.GetReadWritePasswordLength())
                   ) || (
                      ibPass.HasReadOnlyPassword() &&
                      (0xFFE0 & endingOffset) == (0xFFE0 & ibPass.GetReadOnlyPasswordAddress()) &&
                      endingOffset < (ibPass.GetReadOnlyPasswordAddress() + ibPass.GetReadOnlyPasswordLength())
                   ) || (
                      ibPass.HasWriteOnlyPassword() &&
                      (0xFFE0 & endingOffset) == (0xFFE0 & ibPass.GetWriteOnlyPasswordAddress()) &&
                      endingOffset < (ibPass.GetWriteOnlyPasswordAddress() + ibPass.GetWriteOnlyPasswordLength())
                   )
                  )
                {

                    // password block would be written to with potentially bad data
                    throw new OneWireException(
                       "Executing write would overwrite password control registers with "
                    + "potentially invalid data.  Please ensure write does not occur over"
                    + "password control register page, or the password control data is "
                    + "specified exactly in the write buffer.");
                }

                byte[] tempBuf = new byte[len + numBytes];
                Array.Copy(writeBuf, offset, tempBuf, 0, len);
                Read(endingOffset, false, tempBuf, len, numBytes);

                base.Write(startAddr, tempBuf, 0, tempBuf.Length);
            }
            else
            {
                // write does extend to end of page
                base.Write(startAddr, writeBuf, offset, len);
            }

        }

    }
}
