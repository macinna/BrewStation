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
     * Memory bank class for the Scratchpad section of EEPROM iButtons and
     * 1-Wire devices.
     *
     *  @version    0.00, 28 Aug 2000
     *  @author     DS
     */

    public class MemoryBankScratchEE : MemoryBankScratch
    {

        /**
         * Copy Scratchpad Delay length
         */
        public byte COPY_DELAY_LEN;

        /**
         * Mask for ES byte during copy scratchpad
         */
        public byte ES_MASK;

        /**
         * Number of bytes to read for verification (only last one will be checked).
         */
        public int numVerificationBytes = 1;

        //--------
        //-------- Constructor
        //--------

        /**
         * Memory bank contstuctor.  Requires reference to the OneWireContainer
         * this memory bank resides on.
         */
        public MemoryBankScratchEE(OneWireContainer ibutton)
            : base(ibutton)
        {

            // default copy scratchpad delay
            COPY_DELAY_LEN = (byte)5;

            // default ES mask for copy scratchpad 
            ES_MASK = 0;
        }

        //--------
        //-------- ScratchPad methods
        //--------

        /**
         * Write to the scratchpad page of memory a NVRAM device.
         *
         * @param  startAddr     starting address
         * @param  writeBuf      byte array containing data to write
         * @param  offset        offset into readBuf to place data
         * @param  len           length in bytes to write
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public override void WriteScratchpad(int startAddr, byte[] writeBuf, int offset,
                                     int len)
        {
            Boolean calcCRC = false;

            // select the device
            if (!ib.Adapter.SelectDevice(ib.Address))
            {
                ForceVerify();

                throw new OneWireIOException("Device select failed");
            }

            // build block to send
            byte[] raw_buf = new byte[37];

            raw_buf[0] = WRITE_SCRATCHPAD_COMMAND;
            raw_buf[1] = (byte)(startAddr & 0xFF);
            raw_buf[2] = (byte)(((startAddr & 0xFFFF) >> 8) & 0xFF);

            Array.Copy(writeBuf, offset, raw_buf, 3, len);

            // check if full page (can utilize CRC)
            if (((startAddr + len) % pageLength) == 0)
            {
                Array.Copy(ffBlock, 0, raw_buf, len + 3, 2);

                calcCRC = true;
            }

            // send block, return result 
            ib.Adapter.DataBlock(raw_buf, 0, len + 3 + ((calcCRC) ? 2
                                                                  : 0));

            // check crc
            if (calcCRC)
            {
                if (CRC16.Compute(raw_buf, 0, len + 5, 0) != 0x0000B001)
                {
                    ForceVerify();

                    throw new OneWireIOException("Invalid CRC16 read from device");
                }
            }
        }

        /**
         * Copy the scratchpad page to memory.
         *
         * @param  startAddr     starting address
         * @param  len           length in bytes that was written already
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public override void CopyScratchpad(int startAddr, int len)
        {

            // select the device
            if (!ib.Adapter.SelectDevice(ib.Address))
            {
                ForceVerify();

                throw new OneWireIOException("Device select failed");
            }

            // build block to send
            byte[] raw_buf = new byte[3];

            raw_buf[0] = COPY_SCRATCHPAD_COMMAND;
            raw_buf[1] = (byte)(startAddr & 0xFF);
            raw_buf[2] = (byte)(((startAddr & 0xFFFF) >> 8) & 0xFF);

            // send block (command, address)
            ib.Adapter.DataBlock(raw_buf, 0, 3);

            //try
            {

                // setup strong pullup
                ib.Adapter.SetPowerDuration(OWPowerTime.DELIVERY_INFINITE);
                ib.Adapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);

                // send the offset and start power delivery
                ib.Adapter.PutByte((byte)(((startAddr + len - 1) & 0x1F)) | ES_MASK);

                // delay for 5ms
                System.Threading.Thread.Sleep(COPY_DELAY_LEN);

                // disable power
                ib.Adapter.SetPowerNormal();

                // check if complete
                // OLD: BEGIN
                //byte rslt = (byte)ib.Adapter.GetByte();
                // OLD: END

                // NEW: BEGIN
                byte rslt = 0;
                if (numVerificationBytes == 1)
                    rslt = (byte)ib.Adapter.GetByte();
                else
                {
                    raw_buf = new byte[numVerificationBytes];
                    ib.Adapter.GetBlock(raw_buf, 0, numVerificationBytes);
                    rslt = raw_buf[numVerificationBytes - 1];
                }
                // NEW: END

                if (((byte)(rslt & 0x0F0) != (byte)0xA0)
                        && ((byte)(rslt & 0x0F0) != (byte)0x50))
                {
                    ForceVerify();

                    throw new OneWireIOException(
                       "Copy scratchpad complete not found");
                }
            }
            //catch (InterruptedException e){}
            //;
        }


    }
}
