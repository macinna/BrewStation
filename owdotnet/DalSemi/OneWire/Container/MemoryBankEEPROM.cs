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
using System.Collections.Generic;
using System.Text;

using DalSemi.OneWire.Adapter; // OneWireIOException
using DalSemi.Utils; // CRC16

namespace DalSemi.OneWire.Container
{
    /**
     * Memory bank class for the EEPROM section of iButtons and 1-Wire devices on the DS2431.
     *
     *  @version    1.00, 20 February 2004
     *  @author     DS
     */

    public class MemoryBankEEPROM : OTPMemoryBank
    {


        //--------
        //-------- Static Final Variables
        //--------

        /**
          * Read Memory Command
          */
        public const byte READ_MEMORY_COMMAND = (byte)0xF0;

        /**
          * Page Lock Flag
          */
        public const byte LOCKED_FLAG = (byte)0x55;

        /**
          * EPROM Mode Flag
          */
        public const byte EPROM_MODE_FLAG = (byte)0xAA;

        //--------
        //-------- Variables
        //--------

        /**
         * ScratchPad memory bank
         */
        protected ScratchPad sp;

        /**
         * Reference to the OneWireContainer this bank resides on.
         */
        protected OneWireContainer ib;

        /**
         * block of 0xFF's used for faster read pre-fill of 1-Wire blocks
         */
        protected byte[] ffBlock = new byte[150];

        /**
         * Flag to indicate that speed needs to be set
         */
        public Boolean doSetSpeed;

        //--------
        //-------- Protected Variables for MemoryBank implementation
        //--------

        /**
         * Size of memory bank in bytes
         */
        public int size;

        /**
         * Memory bank descriptions
         */
        public string bankDescription;

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
         * Flag if memory bank is non volatile
         * (will not erase when power removed)
         */
        public Boolean nonVolatile;

        /**
         * Flag if memory bank needs program Pulse to write
         */
        public Boolean programPulse;

        /**
         * Flag if memory bank needs power delivery to write
         */
        public Boolean powerDelivery;

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
        public Boolean writeVerification;

        //--------
        //-------- Protected Variables for PagedMemoryBank implementation
        //--------

        /**
         * Number of pages in memory bank
         */
        public int numberPages;

        /**
         *  page length in memory bank
         */
        public int pageLength;

        /**
         * Max data length in page packet in memory bank
         */
        public int maxPacketDataLength;

        /**
         * Flag if memory bank has page auto-CRC generation
         */
        public Boolean pageAutoCRC;

        /**
         * Flag if reading a page in memory bank provides optional
         * extra information (counter, tamper protection, SHA-1...)
         */
        public Boolean extraInfo;

        /**
         * Length of extra information when reading a page in memory bank
         */
        public int extraInfoLength;

        /**
         * Extra information descriptoin when reading page in memory bank
         */
        public string extraInfoDescription;


        //--------
        //-------- Protected Variables for OTPMemoryBank implementation
        //--------

        /**
         * Flag if memory bank can have pages locked
         */
        public Boolean lockPage;

        /**
         * Memory bank to lock pages in 'this' memory bank
         */
        public MemoryBankEEPROM mbLock;

        //--------
        //-------- Constructor
        //--------

        /**
         * Memory bank contstuctor.  Requires reference to the OneWireContainer
         * this memory bank resides on.  Requires reference to memory banks used
         * in OTP operations.
         */
        public MemoryBankEEPROM(OneWireContainer ibutton, ScratchPad scratch)
        {

            // keep reference to ibutton where memory bank is
            ib = ibutton;

            // keep reference to scratchPad bank
            sp = scratch;

            // get references to MemoryBanks used in OTP operations, assume locking
            mbLock = null;
            lockPage = true;

            // initialize attributes of this memory bank - DEFAULT: Main memory DS2431
            generalPurposeMemory = true;
            bankDescription = "Main memory";
            numberPages = 4;
            size = 128;
            pageLength = 32;
            maxPacketDataLength = 29;
            readWrite = true;
            writeOnce = false;
            readOnly = false;
            nonVolatile = true;
            pageAutoCRC = false;
            lockPage = true;
            programPulse = false;
            powerDelivery = true;
            extraInfo = false;
            extraInfoLength = 0;
            extraInfoDescription = null;
            writeVerification = false;
            startPhysicalAddress = 0;
            doSetSpeed = true;

            // create the ffblock (used for faster 0xFF fills)
            for (int i = 0; i < 150; i++)
                ffBlock[i] = (byte)0xFF;
        }

        //--------
        //-------- MemoryBank query methods
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
            return nonVolatile;
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
            return programPulse;
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
            return powerDelivery;
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

        //--------
        //-------- PagedMemoryBank query methods
        //--------

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
            return pageAutoCRC;
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
            return false;
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
            return extraInfoDescription;
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

        //--------
        //-------- OTPMemoryBank query methods
        //--------

        /**
         * Query to see if current memory bank pages can be redirected
         * to another pages.  This is mostly used in Write-Once memory
         * to provide a means to update.
         *
         * @return  'true' if current memory bank pages can be redirected
         *          to a new page.
         */
        public Boolean CanRedirectPage()
        {
            return false;
        }

        /**
         * Query to see if current memory bank pages can be locked.  A
         * locked page would prevent any changes to the memory.
         *
         * @return  'true' if current memory bank pages can be redirected
         *          to a new page.
         */
        public Boolean CanLockPage()
        {
            return lockPage;
        }

        /**
         * Query to see if current memory bank pages can be locked from
         * being redirected.  This would prevent a Write-Once memory from
         * being updated.
         *
         * @return  'true' if current memory bank pages can be locked from
         *          being redirected to a new page.
         */
        public Boolean CanLockRedirectPage()
        {
            return false;
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
        public void Read(int startAddr, Boolean readContinue, byte[] readBuf,
                          int offset, int len)
        {
            byte[] buff = new byte[150];

            Array.Copy(ffBlock, 0, buff, 0, 150);

            // check if read exceeds memory
            if ((startAddr + len) > (pageLength * numberPages))
                throw new OneWireException("Read exceeds memory bank end");

            if (len < 0)
                throw new OneWireException("Invalid length");


            // attempt to put device at max desired speed
            if (!readContinue)
            {
                sp.CheckSpeed();

                // select the device
                if (ib.Adapter.SelectDevice(ib.Address))
                {
                    buff[0] = READ_MEMORY_COMMAND;

                    // address 1
                    buff[1] = (byte)((startAddr + startPhysicalAddress) & 0xFF);
                    // address 2
                    buff[2] = (byte)((((startAddr + startPhysicalAddress) & 0xFFFF) >> 8) & 0xFF);

                    ib.Adapter.DataBlock(buff, 0, len + 3);

                    // extract the data
                    Array.Copy(buff, 3, readBuf, offset, len);
                }
                else
                    throw new OneWireIOException("Device select failed");
            }
            else
            {
                ib.Adapter.DataBlock(buff, 0, len);

                // extract the data
                Array.Copy(buff, 0, readBuf, offset, len);
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
        public void Write(int startAddr, byte[] writeBuf, int offset, int len)
        {
            int room_left;

            // return if nothing to do
            if (len == 0)
                return;

            // attempt to put device at speed
            sp.CheckSpeed();

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
            //byte[] scratchpad = new byte[8];
            //byte[] es_data    = new byte[3];
            int abs_addr = startAddr + startPhysicalAddress;
            int pl = 8;

            // check to see if we need to read memory for the beginning of the block
            if ((startAddr & 0x07) != 0)
                Read((startAddr & 0x00F8), false, memory, (startAddr & 0x00F8),
                      startAddr - (startAddr & 0x00F8) + 1);

            // check to see if we need to read memory for the end of the block
            if (((startAddr + len - 1) & 0x07) != 0x07)
                Read((startAddr + len), false, memory, (startAddr + len),
                      ((startAddr + len) | 0x07) - (startAddr + len) + 1);

            do
            {
                // calculate room left in current page
                room_left = pl - ((abs_addr + startx) % pl);

                // check if block left will cross end of page
                if ((len - startx) > room_left)
                    nextx = startx + room_left;
                else
                    nextx = len;

                Array.Copy(memory, (((startx + startAddr) / 8) * 8), raw_buf, 0, 8);

                if ((nextx - startx) == 8)
                {
                    Array.Copy(writeBuf, offset + startx, raw_buf, 0, 8);
                }
                else
                {
                    if (((startAddr + nextx) % 8) == 0)
                    {
                        Array.Copy(writeBuf, offset + startx, raw_buf, ((startAddr + startx) % 8),
                                         8 - ((startAddr + startx) % 8));
                    }
                    else
                    {
                        Array.Copy(writeBuf, offset + startx, raw_buf, ((startAddr + startx) % 8),
                                        ((startAddr + nextx) % 8) - ((startAddr + startx) % 8));
                    }
                }

                // write the page of data to scratchpad (always do full scratchpad)
                sp.WriteScratchpad(abs_addr + startx + room_left - 8, raw_buf, 0, 8);

                // Copy data from scratchpad into memory
                sp.CopyScratchpad(abs_addr + startx + room_left - 8, 8);

                if (startAddr >= pageLength)
                    Array.Copy(raw_buf, 0, memory, (((startx + startAddr) / 8) * 8) - 32, 8);
                else
                    Array.Copy(raw_buf, 0, memory, (((startx + startAddr) / 8) * 8), 8);

                // point to next index
                startx = nextx;
            }
            while (nextx < len);
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
            throw new OneWireException(
                  "Read extra information not supported on this memory bank");
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
         * @return  number of data bytes written to readBuf at the offset.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public int ReadPagePacket(int page, Boolean readContinue, byte[] readBuf,
                                   int offset, byte[] extraInfo)
        {
            throw new OneWireException(
                  "Read extra information not supported on this memory bank");
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
         *
         * @return  number of data bytes written to readBuf at the offset.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public int ReadPagePacket(int page, Boolean readContinue, byte[] readBuf,
                                   int offset)
        {
            byte[] raw_buf = new byte[pageLength];

            // read entire page with read page CRC
            Read((page * pageLength), readContinue, raw_buf, 0, pageLength);

            // check if length is realistic
            if ((raw_buf[0] & 0x00FF) > maxPacketDataLength)
            {
                sp.ForceVerify();

                throw new OneWireIOException("Invalid length in packet");
            }

            // verify the CRC is correct
            if (CRC16.Compute(raw_buf, 0, raw_buf[0] + 3, (uint)page) == 0x0000B001)
            {

                // extract the data out of the packet
                Array.Copy(raw_buf, 1, readBuf, offset, raw_buf[0]);

                // return the length
                return raw_buf[0];
            }
            else
            {
                sp.ForceVerify();

                throw new OneWireIOException("Invalid CRC16 in packet read");
            }
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
                   "Length of packet requested exceeds page size");

            // see if this bank is general read/write
            if (!generalPurposeMemory)
                throw new OneWireException(
                   "Current bank is not general purpose memory");

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
            throw new OneWireException(
                  "Read page with CRC not supported in this memory bank");
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
            throw new OneWireException(
                  "Read page with CRC not supported in this memory bank");
        }

        //--------
        //-------- OTPMemoryBank I/O methods
        //--------

        /**
         * Lock the specifed page in the current memory bank.  Not supported
         * by all devices.  See the method 'canLockPage()'.
         *
         * @param  page   number of page to lock
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public void LockPage(int page)
        {
            byte[] wr_byte = new byte[1];

            wr_byte[0] = LOCKED_FLAG;

            mbLock.Write(page, wr_byte, 0, 1);

            // read back to verify
            if (!IsPageLocked(page))
            {
                sp.ForceVerify();

                throw new OneWireIOException(
                   "Read back from write incorrect, could not lock page");
            }
        }

        /**
         * Query to see if the specified page is locked.
         * See the method 'canLockPage()'.
         *
         * @param  page  number of page to see if locked
         *
         * @return  'true' if page locked.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean IsPageLocked(int page)
        {
            byte[] rd_byte = new byte[1];

            mbLock.Read(page, false, rd_byte, 0, 1);

            return (rd_byte[0] == LOCKED_FLAG);
        }

        /**
         * Redirect the specifed page in the current memory bank to a new page.
         * Not supported by all devices.  See the method 'canRedirectPage()'.
         *
         * @param  page      number of page to redirect
         * @param  newPage   new page number to redirect to
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public void RedirectPage(int page, int newPage)
        {
            throw new OneWireException("This memory bank does not support redirection.");
        }

        /**
         * Query to see if the specified page is redirected.
         * Not supported by all devices.  See the method 'canRedirectPage()'.
         *
         * @param  page      number of page check for redirection
         *
         * @return  return the new page number or 0 if not redirected
         *
         * @throws OneWireIOException
         * @throws OneWireException
         *
         * @deprecated  As of 1-Wire API 0.01, replaced by {@link #getRedirectedPage(int)}
         */
        public int IsPageRedirected(int page)
        {
            throw new OneWireException("This memory bank does not support redirection.");
        }

        /**
         * Gets the page redirection of the specified page.
         * Not supported by all devices.
         *
         * @param  page  page to check for redirection
         *
         * @return  the new page number or 0 if not redirected
         *
         * @throws OneWireIOException on a 1-Wire communication error such as
         *         no device present or a CRC read from the device is incorrect.  This could be
         *         caused by a physical interruption in the 1-Wire Network due to
         *         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'.
         * @throws OneWireException on a communication or setup error with the 1-Wire
         *         adapter.
         *
         * @see #canRedirectPage() canRedirectPage
         * @see #redirectPage(int,int) redirectPage
         * @since 1-Wire API 0.01
         */
        public int GetRedirectedPage(int page)
        {
            throw new OneWireException("This memory bank does not support redirection.");
        }

        /**
         * Lock the redirection option for the specifed page in the current
         * memory bank. Not supported by all devices.  See the method
         * 'canLockRedirectPage()'.
         *
         * @param  page      number of page to redirect
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public void LockRedirectPage(int page)
        {
            throw new OneWireException("This memory bank does not support redirection.");
        }

        /**
         * Query to see if the specified page has redirection locked.
         * Not supported by all devices.  See the method 'canRedirectPage()'.
         *
         * @param  page      number of page check for locked redirection
         *
         * @return  return 'true' if redirection is locked for this page
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public Boolean IsRedirectPageLocked(int page)
        {
            throw new OneWireException("This memory bank does not support redirection.");
        }

    }
}
