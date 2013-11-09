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
using DalSemi.OneWire.Utils; // IOHelper

namespace DalSemi.OneWire.Container
{

    /**
      * Memory bank class for the DS1961S/DS2432.
      *
      *  @version 	0.00, 19 Dec 2000
      *  @author 	DS
      */

    public class MemoryBankSHAEE : PagedMemoryBank
    {



        //--------
        //--------Static Final Variables
        //--------

        /** Read Memory Command */
        public const byte READ_MEMORY = (byte)0xF0;

        /** Read Authenticate Page */
        public const byte READ_AUTH_PAGE = (byte)0xA5;

        //--------
        //-------- Protected Variables for MemoryBank implementation
        //--------

        /**
          * Check the status of the memory page.
          */
        public Boolean checked_;

        /**
          * Size of memory bank in bytes
          */
        public int size;

        /**
         * Memory bank descriptions
         */
        public String bankDescription;

        /**
         * Memory bank usage flags
         */
        public Boolean generalPurposeMemory;

        /**
         * Flag if memory bank is read/write
         */
        public Boolean readWrite;

        /**
         * Flag if memory bank is write once (EPROM)
         */
        public Boolean writeOnce;

        /**
         * Flag if memory bank is read only
         */
        public Boolean readOnly;

        /**
         * Starting physical address in memory bank.  Needed for different
         * types of memory in the same logical memory bank.  This can be
         * used to seperate them into two virtual memory banks.  Example:
         * DS2406 status page has mixed EPROM and Volatile RAM.
         */
        public int startPhysicalAddress;

        /**
         * Flag if read back verification is enabled in 'write()'.
         */
        protected Boolean writeVerification;

        /**
          * Number of pages in memory bank
          */
        public int numberPages;

        /**
         *  page length in memory bank
         */
        public int pageLength;

        /**
         * Flag if reading a page in memory bank provides optional
         * extra information (counter, tamper protection, SHA-1...)
         */
        public Boolean extraInfo;

        /**
         * Extra information length in bytes
         */
        public int extraInfoLength;

        /**
         * Max data length in page packet in memory bank
         */
        public int maxPacketDataLength;

        /**
         * Flag if memory bank has page CRC.
         */
        public Boolean pageCRC;

        //--------
        //-------- Variables
        //--------

        /**
         * Reference to the OneWireContainer this bank resides on.
         */
        protected OneWireContainer33 ib = null;

        /**
         * Reference to the adapter the OneWireContainer resides on.
         */
        protected PortAdapter adapter = null;

        /**
         * Flag to indicate that speed needs to be set
         */
        protected Boolean doSetSpeed;

        /**
         * block of 0xFF's used for faster read pre-fill of 1-Wire blocks
         * Comes from OneWireContainer33 that this MemoryBank references.
         */
        protected static readonly byte[] ffBlock = OneWireContainer33.ffBlock;

        /**
         * block of 0x00's used for faster read pre-fill of 1-Wire blocks
         * Comes from OneWireContainer33 that this MemoryBank references.
         */
        //protected static readonly byte[] zeroBlock = OneWireContainer33.zeroBlock;

        protected MemoryBankScratchSHAEE scratchpad;

        /**
         * Memory bank constructor.  Requires reference to the OneWireContainer
         * this memory bank resides on.
         */
        public MemoryBankSHAEE(OneWireContainer33 ibutton, MemoryBankScratchSHAEE scratch)
        {
            // keep reference to ibutton where memory bank is
            ib = ibutton;

            scratchpad = scratch;

            // keep reference to adapter that button is on
            adapter = ib.Adapter;

            // indicate speed has not been set
            doSetSpeed = true;
        }

        //--------
        //-------- Memory Bank methods
        //--------

        /**
         * Query to see get a string description of the current memory bank.
         *
         * @return  String containing the memory bank description
         */
        public string GetBankDescription()
        {
            return bankDescription;
        }

        /**
         * Query to see if the current memory bank is general purpose
         * user memory.  If it is NOT then it is Memory-Mapped and writing
         * values to this memory will affect the behavior of the 1-Wire
         * device.
         *
         * @return  'true' if current memory bank is general purpose
         */
        public Boolean IsGeneralPurposeMemory()
        {
            return generalPurposeMemory;
        }

        /**
          * Query to see if current memory bank is read/write.
          *
          * @return  'true' if current memory bank is read/write
          */
        public Boolean IsReadWrite()
        {
            return readWrite;
        }

        /**
         * Query to see if current memory bank is write write once such
         * as with EPROM technology.
         *
         * @return  'true' if current memory bank can only be written once
         */
        public Boolean IsWriteOnce()
        {
            return writeOnce;
        }

        /**
         * Query to see if current memory bank is read only.
         *
         * @return  'true' if current memory bank can only be read
         */
        public Boolean IsReadOnly()
        {
            return readOnly;
        }

        /**
         * Query to see if current memory bank non-volatile.  Memory is
         * non-volatile if it retains its contents even when removed from
         * the 1-Wire network.
         *
         * @return  'true' if current memory bank non volatile.
         */
        public Boolean IsNonVolatile()
        {
            return true;
        }

        /**
          * Query to see if current memory bank pages need the adapter to
          * have a 'ProgramPulse' in order to write to the memory.
          *
          * @return  'true' if writing to the current memory bank pages
          *                 requires a 'ProgramPulse'.
          */
        public Boolean NeedsProgramPulse()
        {
            return false;
        }

        /**
         * Query to see if current memory bank pages need the adapter to
         * have a 'PowerDelivery' feature in order to write to the memory.
         *
         * @return  'true' if writing to the current memory bank pages
         *                 requires 'PowerDelivery'.
         */
        public Boolean NeedsPowerDelivery()
        {
            return true;
        }

        /**
         * Query to get the starting physical address of this bank.  Physical
         * banks are sometimes sub-divided into logical banks due to changes
         * in attributes.
         *
         * @return  physical starting address of this logical bank.
         */
        public int GetStartPhysicalAddress()
        {
            return startPhysicalAddress;
        }

        /**
         * Query to get the memory bank size in bytes.
         *
         * @return  memory bank size in bytes.
         */
        public int GetSize()
        {
            return size;
        }

        /**
         * Set the write verification for the 'write()' method.
         *
         * @param  doReadVerf   true (default) verify write in 'write'
         *                      false, don'thread verify write (used on
         *                      Write-Once bit manipulation)
         */
        public void SetWriteVerification(Boolean doReadVerf)
        {
            writeVerification = doReadVerf;
        }

        /**
         * Query to get the number of pages in current memory bank.
         *
         * @return  number of pages in current memory bank
         */
        public int GetNumberPages()
        {
            return numberPages;
        }

        /**
         * Query to get  page length in bytes in current memory bank.
         *
         * @return   page length in bytes in current memory bank
         */
        public int GetPageLength()
        {
            return pageLength;
        }

        /**
         * Query to see if current memory bank pages can be read with
         * the contents being verified by a device generated CRC.
         * This is used to see if the 'ReadPageCRC()' can be used.
         *
         * @return  'true' if current memory bank can be read with self
         *          generated CRC.
         */
        public Boolean HasPageAutoCRC()
        {
            return pageCRC;
        }

        /**
         * Query to see if current memory bank pages when read deliver
         * extra information outside of the normal data space.  Examples
         * of this may be a redirection byte, counter, tamper protection
         * bytes, or SHA-1 result.  If this method returns true then the
         * methods 'ReadPagePacket()' and 'readPageCRC()' with 'extraInfo'
         * parameter can be used.
         *
         * @return  'true' if reading the current memory bank pages
         *                 provides extra information.
         *
         * @deprecated  As of 1-Wire API 0.01, replaced by {@link #hasExtraInfo()}
         */
        public Boolean HaveExtraInfo()
        {
            return extraInfo;
        }

        /**
         * Checks to see if this memory bank's pages deliver extra
         * information outside of the normal data space,  when read.  Examples
         * of this may be a redirection byte, counter, tamper protection
         * bytes, or SHA-1 result.  If this method returns true then the
         * methods with an 'extraInfo' parameter can be used:
         * {@link #readPage(int,boolean,byte[],int,byte[]) readPage},
         * {@link #readPageCRC(int,boolean,byte[],int,byte[]) readPageCRC}, and
         * {@link #readPagePacket(int,boolean,byte[],int,byte[]) readPagePacket}.
         *
         * @return  <CODE> true </CODE> if reading the this memory bank's
         *                 pages provides extra information
         *
         * @see #readPage(int,boolean,byte[],int,byte[]) readPage(extra)
         * @see #readPageCRC(int,boolean,byte[],int,byte[]) readPageCRC(extra)
         * @see #readPagePacket(int,boolean,byte[],int,byte[]) readPagePacket(extra)
         * @since 1-Wire API 0.01
         */
        public Boolean HasExtraInfo()
        {
            return extraInfo;
        }

        /**
         * Query to get the length in bytes of extra information that
         * is read when read a page in the current memory bank.  See
         * 'hasExtraInfo()'.
         *
         * @return  number of bytes in Extra Information read when reading
         *          pages in the current memory bank.
         */
        public int GetExtraInfoLength()
        {
            return extraInfoLength;
        }

        /**
         * Query to get a string description of what is contained in
         * the Extra Informationed return when reading pages in the current
         * memory bank.  See 'hasExtraInfo()'.
         *
         * @return string describing extra information.
         */
        public string GetExtraInfoDescription()
        {
            return "The MAC for the SHA Engine";
        }

        /**
         * Query to get Maximum data page length in bytes for a packet
         * read or written in the current memory bank.  See the 'ReadPagePacket()'
         * and 'WritePagePacket()' methods.  This method is only usefull
         * if the current memory bank is general purpose memory.
         *
         * @return  max packet page length in bytes in current memory bank
         */
        public int GetMaxPacketDataLength()
        {
            return maxPacketDataLength;
        }

        //--------
        //-------- MemoryBank I/O methods
        //--------

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
         * @param  startAddr     starting address, relative to physical address for
         *                       this memory bank.
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
        public void Read(int startAddr, Boolean readContinue, byte[] readBuf,
                          int offset, int len)
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
#if DEBUG_MemoryBankSHAEE
            Debug.DebugStr("-----------------------------------------------------------");
            Debug.DebugStr("MemoryBankSHAEE.read(int, boolean, byte[], int, int) called");
            Debug.DebugStr("  startAddr=0x" + DalSemi.OneWire.Utils.Convert.ToHexString((byte)startAddr));
            Debug.DebugStr("  readContinue=" + readContinue);
            Debug.DebugStr("  offset=" + offset);
            Debug.DebugStr("  len=" + len);
            Debug.DebugStr("  this.startPhysicalAddress=0x" + DalSemi.OneWire.Utils.Convert.ToHexString((byte)startPhysicalAddress));
            Debug.DebugStr("  this.pageLength=" + this.pageLength);
            Debug.DebugStr("  this.numberPages=" + this.numberPages);
            Debug.StackTrace();
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

            // attempt to put device at max desired speed
            if (!readContinue && ib.AdapterSet())
                CheckSpeed();

            // check if read exceeds memory
            if ((startAddr + len) > (pageLength * numberPages))
                throw new OneWireException("Read exceeds memory bank end.");

            // see if need to access the device
            if (!readContinue)
            {
                // select the device
                if (!adapter.SelectDevice(ib.Address))
                    throw new OneWireIOException("Device select failed.");

                // build start reading memory block
                int addr = startAddr + startPhysicalAddress;
                byte[] raw_buf = new byte[3];

                raw_buf[0] = READ_MEMORY;
                raw_buf[1] = (byte)(addr & 0xFF);
                raw_buf[2] = (byte)(((addr & 0xFFFF) >> 8) & 0xFF);

                // do the first block for command, address
                adapter.DataBlock(raw_buf, 0, 3);
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
#if DEBUG_MemoryBankSHAEE
                Debug.DebugStr("  raw_buf", raw_buf, 0, 3);
#endif
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            }

            // pre-fill readBuf with 0xFF
            int pgs = len / pageLength;
            int extra = len % pageLength;

            for (int i = 0; i < pgs; i++)
                Array.Copy(ffBlock, 0, readBuf, offset + i * pageLength,
                                 pageLength);
            Array.Copy(ffBlock, 0, readBuf, offset + pgs * pageLength, extra);

            // send second block to read data, return result
            adapter.DataBlock(readBuf, offset, len);

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
#if DEBUG_MemoryBankSHAEE
            Debug.DebugStr("  readBuf", readBuf, offset, len);
            Debug.DebugStr("-----------------------------------------------------------");
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
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
         * @param  startAddr     starting address, relative to the starting physical address
         *                       of this memory bank
         * @param  writeBuf      byte array containing data to write
         * @param  offset        offset into writeBuf to get data
         * @param  len           length in bytes to write
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public void Write(int startAddr, byte[] writeBuf, int offset, int len)
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_MemoryBankSHAEE
            Debug.DebugStr("-----------------------------------------------------------");
            Debug.DebugStr("MemoryBankSHAEE.write(int,byte[],int,int) called");
            Debug.DebugStr("  startAddr=0x" + DalSemi.OneWire.Utils.Convert.ToHexString((byte)startAddr));
            Debug.DebugStr("  writeBuf", writeBuf, offset, len);
            Debug.DebugStr("  startPhysicalAddress=0x" + DalSemi.OneWire.Utils.Convert.ToHexString((byte)startPhysicalAddress));
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            int room_left;

            if (!checked_)
                checked_ = ib.CheckStatus();

            // return if nothing to do
            if (len == 0)
                return;

            // attempt to put device at speed
            CheckSpeed();

            // check to see if secret is set
            if (!ib.IsContainerSecretSet())
                throw new OneWireException("Secret is not set.");

            // check if write exceeds memory
            if ((startAddr + len) > size)
                throw new OneWireException("Write exceeds memory bank end");

            // check if trying to write read only bank
            if (IsReadOnly())
                throw new OneWireException("Trying to write read-only memory bank");

            // loop while still have pages to write
            int startx = 0, nextx = 0;   // (start and next index into writeBuf)
            byte[] raw_buf = new byte[8];
            byte[] memory = new byte[size];
            int abs_addr = startPhysicalAddress + startAddr;
            int pl = 8;

            Read(startAddr & 0xE0, false, memory, 0, size);

            if (abs_addr >= 128)
            {
                ib.GetContainerSecret(memory, 0);
            }

            do
            {
                // calculate room left in current page
                room_left = pl - ((abs_addr + startx) % pl);

                // check if block left will cross end of page
                if ((len - startx) > room_left)
                    nextx = startx + room_left;
                else
                    nextx = len;

                // bug fix, if updating pages two and three in the same write op
                // this used to fail, was (startAddr>=pageLength)
                if ((startx + startAddr) >= pageLength)
                    Array.Copy(memory, (((startx + startAddr) / 8) * 8) - 32, raw_buf, 0, 8);
                else
                    Array.Copy(memory, (((startx + startAddr) / 8) * 8), raw_buf, 0, 8);

                if ((nextx - startx) == 8)
                    Array.Copy(writeBuf, offset + startx, raw_buf, 0, 8);
                else
                    if (((startAddr + nextx) % 8) == 0)
                        Array.Copy(writeBuf, offset + startx, raw_buf, ((startAddr + startx) % 8), 8 - ((startAddr + startx) % 8));
                    else
                        Array.Copy(writeBuf, offset + startx, raw_buf, ((startAddr + startx) % 8),
                                       ((startAddr + nextx) % 8) - ((startAddr + startx) % 8));

                // write the page of data to scratchpad
                scratchpad.WriteScratchpad(abs_addr + startx + room_left - 8, raw_buf, 0, 8);

                // Copy data from scratchpad into memory
                scratchpad.CopyScratchpad(abs_addr + startx + room_left - 8, raw_buf, 0, memory, 0);

                // bug fix, if updating pages two and three in the same write op
                // this used to fail, was (startAddr>=pageLength)
                if ((startx + startAddr) >= pageLength)
                    Array.Copy(raw_buf, 0, memory, (((startx + startAddr) / 8) * 8) - 32, 8);
                else
                    Array.Copy(raw_buf, 0, memory, (((startx + startAddr) / 8) * 8), 8);

                // point to next index
                startx = nextx;
            }
            while (nextx < len);

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_MemoryBankSHAEE
            Debug.DebugStr("-----------------------------------------------------------");
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        }

        //--------
        //-------- PagedMemoryBank I/O methods
        //--------

        /**
         * Read  page in the current bank with no
         * CRC checking (device or data). The resulting data from this API
         * may or may not be what is on the 1-Wire device.  It is recommends
         * that the data contain some kind of checking (CRC) like in the
         * readPagePacket() method or have the 1-Wire device provide the
         * CRC as in readPageCRC().  readPageCRC() however is not
         * supported on all memory types, see 'hasPageAutoCRC()'.
         * If neither is an option then this method could be called more
         * then once to at least verify that the same thing is read consistantly.
         *
         * @param  page          page number to read packet from
         * @param  readContinue  if 'true' then device read is continued without
         *                       re-selecting.  This can only be used if the new
         *                       readPage() continious where the last one
         *                       led off and it is inside a
         *                       'beginExclusive/endExclusive' block.
         * @param  readBuf       byte array to place read data into
         * @param  offset        offset into readBuf to place data
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public void ReadPage(int page, Boolean readContinue, byte[] readBuf,
                              int offset)
        {
            Read(page * pageLength, readContinue, readBuf, offset, pageLength);
        }

        /**
         * Read  page with extra information in the current bank with no
         * CRC checking (device or data). The resulting data from this API
         * may or may not be what is on the 1-Wire device.  It is recommends
         * that the data contain some kind of checking (CRC) like in the
         * readPagePacket() method or have the 1-Wire device provide the
         * CRC as in readPageCRC().  readPageCRC() however is not
         * supported on all memory types, see 'hasPageAutoCRC()'.
         * If neither is an option then this method could be called more
         * then once to at least verify that the same thing is read consistantly.
         * See the method 'hasExtraInfo()' for a description of the optional
         * extra information some devices have.
         *
         * @param  page          page number to read packet from
         * @param  readContinue  if 'true' then device read is continued without
         *                       re-selecting.  This can only be used if the new
         *                       readPage() continious where the last one
         *                       led off and it is inside a
         *                       'beginExclusive/endExclusive' block.
         * @param  readBuf       byte array to place read data into
         * @param  offset        offset into readBuf to place data
         * @param  extraInfo     byte array to put extra info read into
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public void ReadPage(int page, Boolean readContinue, byte[] readBuf,
                              int offset, byte[] extraInfo)
        {
            byte[] pg = new byte[32];

            if (!checked_)
                checked_ = ib.CheckStatus();

            if (!HasPageAutoCRC())
                throw new OneWireException("This memory bank doesn'thread have crc capabilities.");

            // attempt to put device at max desired speed
            if (!readContinue)
                CheckSpeed();

            if (!ReadAuthenticatedPage(page, pg, 0, extraInfo, 0))
                throw new OneWireException("Read didn'thread work.");

            Array.Copy(pg, 0, readBuf, offset, 32);
        }

        /**
         * Read a Universal Data Packet.
         *
         * The Universal Data Packet always starts on page boundaries but
         * can end anywhere in the page.  The structure specifies the length of
         * data bytes not including the length byte and the CRC16 bytes.
         * There is one length byte. The CRC16 is first initialized to
         * the page number.  This provides a check to verify the page that
         * was intended is being read.  The CRC16 is then calculated over
         * the length and data bytes.  The CRC16 is then inverted and stored
         * low byte first followed by the high byte.  This is structure is
         * used by this method to verify the data but is not returned, only
         * the data payload is returned.
         *
         * @param  page          page number to read packet from
         * @param  readContinue  if 'true' then device read is continued without
         *                       re-selecting.  This can only be used if the new
         *                       readPagePacket() continious where the last one
         *                       stopped and it is inside a
         *                       'beginExclusive/endExclusive' block.
         * @param  readBuf       byte array to put data read. Must have at least
         *                       'getMaxPacketDataLength()' elements.
         * @param  offset        offset into readBuf to place data
         *
         * @return  number of data bytes read from the device and written to
         *          readBuf at the offset.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public int ReadPagePacket(int page, Boolean readContinue, byte[] readBuf,
                                   int offset)
        {
            byte[] raw_buf = new byte[pageLength];

            // read the  page
            ReadPage(page, readContinue, raw_buf, 0);

            // check if length is realistic
            if ((raw_buf[0] & 0x00FF) > maxPacketDataLength)
                throw new OneWireIOException("Invalid length in packet.");

            // verify the CRC is correct
            if (CRC16.Compute(raw_buf, 0, raw_buf[0] + 3, (uint)page) == 0x0000B001)
            {

                // extract the data out of the packet
                Array.Copy(raw_buf, 1, readBuf, offset, raw_buf[0]);

                // return the length
                return raw_buf[0];
            }
            else
                throw new OneWireIOException("Invalid CRC16 in packet read.");
        }

        /**
         * Read a Universal Data Packet and extra information.  See the
         * method 'readPagePacket()' for a description of the packet structure.
         * See the method 'hasExtraInfo()' for a description of the optional
         * extra information some devices have.
         *
         * @param  page          page number to read packet from
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
         * @return  number of data bytes read from the device and written to
         *          readBuf at the offset.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public int ReadPagePacket(int page, Boolean readContinue, byte[] readBuf,
                                   int offset, byte[] extraInfo)
        {
            byte[] raw_buf = new byte[pageLength];

            if (!checked_)
                checked_ = ib.CheckStatus();

            if (!HasPageAutoCRC())
                throw new OneWireException("This memory bank page doesn'thread have CRC capabilites.");

            // read the  page
            ReadAuthenticatedPage(page, raw_buf, 0, extraInfo, 0);

            // check if length is realistic
            if ((raw_buf[0] & 0x00FF) > maxPacketDataLength)
                throw new OneWireIOException("Invalid length in packet.");

            // verify the CRC is correct
            if (CRC16.Compute(raw_buf, 0, raw_buf[0] + 3, (uint)page) == 0x0000B001)
            {

                // extract the data out of the packet
                Array.Copy(raw_buf, 1, readBuf, offset, raw_buf[0]);

                // return the length
                return raw_buf[0];
            }
            else
                throw new OneWireIOException("Invalid CRC16 in packet read.");
        }

        /**
         * Write a Universal Data Packet.  See the method 'readPagePacket()'
         * for a description of the packet structure.
         *
         * @param  page          page number to write packet to
         * @param  writeBuf      data byte array to write
         * @param  offset        offset into writeBuf where data to write is
         * @param  len           number of bytes to write
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public void WritePagePacket(int page, byte[] writeBuf, int offset,
                                     int len)
        {

            // make sure length does not exceed max
            if (len > maxPacketDataLength)
                throw new OneWireIOException(
                   "Length of packet requested exceeds page size.");

            // see if this bank is general read/write
            if (!generalPurposeMemory)
                throw new OneWireException(
                   "Current bank is not general purpose memory.");

            // construct the packet to write
            byte[] raw_buf = new byte[len + 3];

            raw_buf[0] = (byte)len;

            Array.Copy(writeBuf, offset, raw_buf, 1, len);

            uint crc = CRC16.Compute(raw_buf, 0, len + 1, (uint)page);

            raw_buf[len + 1] = (byte)(~crc & 0xFF);
            raw_buf[len + 2] = (byte)(((~crc & 0xFFFF) >> 8) & 0xFF);

            // write the packet, return result
            Write(page * pageLength, raw_buf, 0, len + 3);
        }

        /**
         * Read a complete memory page with CRC verification provided by the
         * device.  Not supported by all devices.  See the method
         * 'hasPageAutoCRC()'.
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
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public void ReadPageCRC(int page, Boolean readContinue, byte[] readBuf,
                                 int offset)
        {
            byte[] extra = new byte[20];
            byte[] pg = new byte[32];

            if (!checked_)
                checked_ = ib.CheckStatus();

            if (!HasPageAutoCRC())
                throw new OneWireException("This memory bank doesn'thread have CRC capabilites.");

            if (!ReadAuthenticatedPage(page, pg, 0, extra, 0))
                throw new OneWireException("Read didn'thread work.");

            Array.Copy(pg, 0, readBuf, offset, pageLength);
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
        public void ReadPageCRC(int page, Boolean readContinue, byte[] readBuf,
                                 int offset, byte[] extraInfo)
        {
            byte[] pg = new byte[32];

            if (!checked_)
                checked_ = ib.CheckStatus();

            if (!HasPageAutoCRC())
                throw new OneWireException("This memory bank doesn'thread have CRC capabilities.");

            if (!ReadAuthenticatedPage(page, pg, 0, extraInfo, 0))
                throw new OneWireException("Read didn'thread work.");

            Array.Copy(pg, 0, readBuf, offset, pageLength);
        }

        // ------------------------
        // Setting status
        // ------------------------

        /**
         * Write protect the memory bank.
         */
        public void WriteProtect()
        {
            readOnly = true;
            readWrite = false;
        }

        /**
         * Sets the EPROM mode for this page.
         */
        public void SetEPROM()
        {
            writeOnce = true;
        }


        // ------------------------
        // Extras
        // ------------------------

        /**
         * Check the device speed if has not been done before or if
         * an error was detected.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public void CheckSpeed()
        {
            lock (this)
            {
                // only check the speed
                if (doSetSpeed)
                {

                    // attempt to set the correct speed and verify device present
                    ib.DoSpeed();

                    // no execptions so clear flag
                    doSetSpeed = false;
                }
            }
        }

        /**
         *  Reads authenticated page.
         *
         * @param page          the page number in this bank to read from.
         * @param data          the data read from the address
         * @param extra_info    the MAC calculated for this function
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean ReadAuthenticatedPage(int page,
                                             byte[] data, int dataStart,
                                             byte[] extra_info, int extraStart)
        {
            byte[] send_block = new byte[40];
            byte[] challenge = new byte[8];

            int addr = (page * pageLength) + startPhysicalAddress;

            ib.GetChallenge(challenge, 4);
            scratchpad.WriteScratchpad(addr, challenge, 0, 8);

            // access the device
            if (!adapter.SelectDevice(ib.Address))
                throw new OneWireIOException("Device select failed.");

            // Read Authenticated Command
            send_block[0] = READ_AUTH_PAGE;
            // address 1
            send_block[1] = (byte)(addr & 0xFF);
            // address 2
            send_block[2] = (byte)(((addr & 0xFFFF) >> 8) & 0xFF);

            // data + FF byte
            Array.Copy(ffBlock, 0, send_block, 3, 35);

            // now send the block
            adapter.DataBlock(send_block, 0, 38);

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_MemoryBankSHAEE
            IOHelper.WriteLine("-------------------------------------------------------------");
            IOHelper.WriteLine("ReadAuthPage - send_block:");
            IOHelper.WriteBytesHex(send_block, 0, 38);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

            // verify CRC16 is correct
            if (CRC16.Compute(send_block, 0, 38, 0) != 0x0000B001)
            {
                throw new OneWireException("First CRC didn'thread pass.");
            }

            Array.Copy(send_block, 3, data, dataStart, 32);

            Array.Copy(ffBlock, 0, send_block, 0, 22);

            //adapter.startPowerDelivery(DSPortAdapter.CONDITION_NOW);
            //try
            {
                System.Threading.Thread.Sleep(2);
            }
            //catch(InterruptedException ie)
            //{;}

            adapter.DataBlock(send_block, 0, 22);

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_MemoryBankSHAEE
            IOHelper.WriteLine("ReadAuthPage - MAC:");
            IOHelper.WriteBytesHex(send_block, 0, 20);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

            // verify CRC16 is correct
            if (CRC16.Compute(send_block, 0, 22, 0) != 0x0000B001)
            {
                throw new OneWireException("Second CRC didn'thread pass.");
            }

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_MemoryBankSHAEE
            IOHelper.WriteLine("next read:");
            IOHelper.WriteBytesHex(send_block, 0, 22);
            IOHelper.WriteLine("-------------------------------------------------------------");
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

            Array.Copy(send_block, 0, extra_info, extraStart, 20);
            return true;
        }

        /**
         * Set the flag to indicate the next 'checkSpeed()' will force
         * a speed set and verify 'doSpeed()'.
         */
        public void ForceVerify()
        {
            lock (this)
            {
                doSetSpeed = true;
            }
        }


    }
}
