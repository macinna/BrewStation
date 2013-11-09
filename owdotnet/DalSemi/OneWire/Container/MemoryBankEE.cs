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
     * Memory bank class for the DS1971/DS2430.
     *
     *  @version    0.00, 28 Aug 2000
     *  @author     DS
     */

    public class MemoryBankEE : PagedMemoryBank
    {


        //--------
        //--------Static Final Variables
        //--------

        /**
          * Read Memory Command
          */
        public const byte READ_MEMORY_COMMAND = (byte)0xF0;

        /**
          * Write Scratchpad Command
          */
        public const byte WRITE_SCRATCHPAD_COMMAND = (byte)0x0F;

        /**
          * Read Scratchpad Command
          */
        public const byte READ_SCRATCHPAD_COMMAND = (byte)0xAA;

        /**
          * Copy Scratchpad Command
          */
        public const byte COPY_SCRATCHPAD_COMMAND = (byte)0x55;

        /**
          * Page length
          */
        public const int PAGE_LENGTH = 32;

        //--------
        //-------- Variables
        //--------

        /**
         * Reference to the OneWireContainer this bank resides on.
         */
        protected OneWireContainer ib;

        /**
         * block of 0xFF's used for faster read pre-fill of 1-Wire blocks
         */
        protected byte[] ffBlock;

        /**
         * Flag if read back verification is enabled in 'write()'.
         */
        protected Boolean writeVerification;

        /**
         * Flag to indicate that speed needs to be set
         */
        protected Boolean doSetSpeed;

        //--------
        //-------- Constructor
        //--------

        /**
         * Memory bank contstuctor.  Requires reference to the OneWireContainer
         * this memory bank resides on.
         */
        public MemoryBankEE(OneWireContainer ibutton)
        {
            lock (this)
            {

                // keep reference to ibutton where memory bank is
                ib = ibutton;

                // create the ffblock (used for faster 0xFF fills)
                ffBlock = new byte[50];

                for (int i = 0; i < 50; i++)
                    ffBlock[i] = (byte)0xFF;

                // defaults
                writeVerification = true;

                // indicate speed has not been set
                doSetSpeed = true;
            }
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
            return "Main Memory";
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
            return true;
        }

        /**
         * Query to see if current memory bank is read/write.
         *
         * @return  'true' if current memory bank is read/write
         */
        public Boolean IsReadWrite()
        {
            return true;
        }

        /**
         * Query to see if current memory bank is write write once such
         * as with EPROM technology.
         *
         * @return  'true' if current memory bank can only be written once
         */
        public Boolean IsWriteOnce()
        {
            return false;
        }

        /**
         * Query to see if current memory bank is read only.
         *
         * @return  'true' if current memory bank can only be read
         */
        public Boolean IsReadOnly()
        {
            return false;
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
            return 0;
        }

        /**
         * Query to get the memory bank size in bytes.
         *
         * @return  memory bank size in bytes.
         */
        public int GetSize()
        {
            return PAGE_LENGTH;
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
            return 1;
        }

        /**
         * Query to get  page length in bytes in current memory bank.
         *
         * @return   page length in bytes in current memory bank
         */
        public int GetPageLength()
        {
            return PAGE_LENGTH;
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
            return PAGE_LENGTH - 3;
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
            return false;
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
            return false;
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
            return 0;
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
            return null;
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
            int i, cnt = 0;
            byte[] raw_buf = new byte[2];

            // attempt to put device at max desired speed
            if (!readContinue)
                CheckSpeed();

            // check if read exceeds memory
            if ((startAddr + len) > PAGE_LENGTH)
                throw new OneWireException("Read exceeds memory bank end");

            // loop until get a non-0xFF value, or max 6 reads  
            do
            {

                // select the device
                if (!ib.Adapter.SelectDevice(ib.Address))
                {
                    ForceVerify();

                    throw new OneWireIOException("Device select failed");
                }

                // build start reading memory block
                raw_buf[0] = READ_MEMORY_COMMAND;
                raw_buf[1] = (byte)(startAddr & 0xFF);

                // do the first block for command, address
                ib.Adapter.DataBlock(raw_buf, 0, 2);

                // pre-fill readBuf with 0xFF 
                Array.Copy(ffBlock, 0, readBuf, offset, len);

                // send the block
                ib.Adapter.DataBlock(readBuf, offset, len);

                // see if result is non-0xFF
                for (i = 0; i < len; i++)
                    if (readBuf[offset + i] != (byte)(0xFF))
                        return;

                //try
                {
                    System.Threading.Thread.Sleep(10);
                }
                //catch (InterruptedException e){}
                //;


                if (!ib.IsPresent())
                {
                    ForceVerify();

                    throw new OneWireIOException("Device not present on 1-Wire");
                }
            }
            while (++cnt < 6);

            // still present, assume data really is 0xFF's
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
            int i;

            // return if nothing to do
            if (len == 0)
                return;

            // check if power delivery is available
            if (!ib.Adapter.CanDeliverPower)
                throw new OneWireException(
                   "Power delivery required but not available");

            // attempt to put device at max desired speed
            CheckSpeed();

            // check if write exceeds memory
            if ((startAddr + len) > PAGE_LENGTH)
                throw new OneWireException("Write exceeds memory bank end");

            // write the page of data to scratchpad
            WriteScratchpad(startAddr, writeBuf, offset, len);

            // read to verify ok
            byte[] raw_buf = new byte[PAGE_LENGTH];

            ReadScratchpad(raw_buf, startAddr, 0, PAGE_LENGTH);

            // check to see if the same
            for (i = 0; i < len; i++)
                if (raw_buf[i] != writeBuf[i + offset])
                {
                    ForceVerify();

                    throw new OneWireIOException(
                       "Read back scratchpad verify had incorrect data");
                }

            // do the copy
            CopyScratchpad();

            // check on write verification
            if (writeVerification)
            {

                // read back to verify
                Read(startAddr, false, raw_buf, 0, len);

                for (i = 0; i < len; i++)
                    if (raw_buf[i] != writeBuf[i + offset])
                    {
                        ForceVerify();

                        throw new OneWireIOException(
                           "Read back verify had incorrect data");
                    }
            }
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

            // check if read exceeds memory
            if (page != 0)
                throw new OneWireException("Page read exceeds memory bank end");

            Read(0, readContinue, readBuf, offset, PAGE_LENGTH);
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

            // only needs to be implemented if supported by hardware
            throw new OneWireException(
               "Read page with extra-info not supported by this memory bank");
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
            byte[] raw_buf = new byte[PAGE_LENGTH];

            // attempt to put device at speed
            CheckSpeed();

            // read the scratchpad, discard extra information
            ReadPage(page, readContinue, raw_buf, 0);

            // check if length is realistic
            if (raw_buf[0] > (PAGE_LENGTH - 3))
            {
                ForceVerify();

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
                ForceVerify();

                throw new OneWireIOException("Invalid CRC16 in packet read");
            }
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

            // only needs to be implemented if supported by hardware
            throw new OneWireException(
               "Read page packet with extra-info not supported by this memory bank");
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
            if (len > (PAGE_LENGTH - 3))
                throw new OneWireIOException(
                   "Length of packet requested exceeds page size");

            // construct the packet to write
            byte[] raw_buf = new byte[len + 3];

            raw_buf[0] = (byte)len;

            Array.Copy(writeBuf, offset, raw_buf, 1, len);

            uint crc = CRC16.Compute(raw_buf, 0, len + 1, (uint)page);

            raw_buf[len + 1] = (byte)(~crc & 0xFF);
            raw_buf[len + 2] = (byte)(((~crc & 0xFFFF) >> 8) & 0xFF);

            // write the packet, return result
            Write(page * PAGE_LENGTH, raw_buf, 0, len + 3);
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

            // only needs to be implemented if supported by hardware
            throw new OneWireException(
               "Read page with CRC not supported by this memory bank");
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

            // only needs to be implemented if supported by hardware
            throw new OneWireException(
               "Read page with CRC and extra-info not supported by this memory bank");
        }

        //--------
        //-------- Bank specific methods
        //--------

        /**
         * Read the scratchpad page of memory from the device
         * This method reads and returns the entire scratchpad after the byte
         * offset regardless of the actual ending offset
         *
         * @param  readBuf       byte array to place read data into
         *                       length of array is always pageLength.
         * @param  startAddr     address to start to read from scratchPad
         * @param  offset        offset into readBuf to pug data
         * @param  len           length in bytes to read
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        protected void ReadScratchpad(byte[] readBuf, int startAddr, int offset,
                                       int len)
        {
            // select the device
            if (!ib.Adapter.SelectDevice(ib.Address))
            {
                ForceVerify();

                throw new OneWireIOException("Device select failed");
            }

            // build first block
            byte[] raw_buf = new byte[2];

            raw_buf[0] = READ_SCRATCHPAD_COMMAND;
            raw_buf[1] = (byte)startAddr;

            // do the first block for address
            ib.Adapter.DataBlock(raw_buf, 0, 2);

            // build the next block
            Array.Copy(ffBlock, 0, readBuf, offset, len);

            // send second block to read data, return result
            ib.Adapter.DataBlock(readBuf, offset, len);
        }

        /**
         * Write to the scratchpad page of memory device.
         *
         * @param  startAddr     starting address
         * @param  writeBuf      byte array containing data to write
         * @param  offset        offset into readBuf to place data
         * @param  len           length in bytes to write
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        protected void WriteScratchpad(int startAddr, byte[] writeBuf,
                                        int offset, int len)
        {

            // select the device
            if (!ib.Adapter.SelectDevice(ib.Address))
            {
                ForceVerify();

                throw new OneWireIOException("Device select failed");
            }

            // build block to send
            byte[] raw_buf = new byte[len + 2];

            raw_buf[0] = WRITE_SCRATCHPAD_COMMAND;
            raw_buf[1] = (byte)(startAddr & 0xFF);

            Array.Copy(writeBuf, offset, raw_buf, 2, len);

            // send block, return result 
            ib.Adapter.DataBlock(raw_buf, 0, len + 2);
        }

        /**
         * Copy the scratchpad page to memory.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        protected void CopyScratchpad()
        {

            // select the device
            if (!ib.Adapter.SelectDevice(ib.Address))
            {
                ForceVerify();

                throw new OneWireIOException("Device select failed");
            }

            //try
            {

                // copy scratch
                ib.Adapter.PutByte(COPY_SCRATCHPAD_COMMAND);

                // setup strong pullup
                ib.Adapter.SetPowerDuration(OWPowerTime.DELIVERY_INFINITE);
                ib.Adapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);

                // send validation and start strong power delivery
                ib.Adapter.PutByte((byte)0xA5);

                // delay for 10ms
                System.Threading.Thread.Sleep(10);

                // disable power
                ib.Adapter.SetPowerNormal();
            }
            //catch (InterruptedException e){}
            //;
        }

        //--------
        //-------- checkSpeed methods
        //--------

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
