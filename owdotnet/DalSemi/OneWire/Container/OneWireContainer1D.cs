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




    /// <summary>
    /// 1-Wire container for 512 byte memory with external counters, DS2423.  This container
    /// encapsulates the functionality of the 1-Wire family type 1D (hex)
    /// 
    /// This 1-Wire device is primarily used as a counter with memory.
    /// Each counter is assosciated with a memory page.  The counters for pages
    /// 12 and 13 are incremented with a write to the memory on that page. The counters
    /// for pages 14 and 15 are externally triggered. See the method ReadCounter(uint)
    /// to read a counter directly.  Note that the the counters may also be read with the
    /// PagedMemoryBank interface as 'extra' information on a page read.
    /// 
    /// Memory
    /// The memory can be accessed through the objects that are returned from the GetMemoryBanks() method.
    /// The following is a list of the MemoryBank instances that are returned:
    /// 
    /// Scratchpad Ex
    /// Implements Com.DalSemi.OneWire.Container.MemoryBank,
    /// Com.DalSemi.OneWire.Container.PagedMemoryBank
    /// Size: 32 starting at physical address 0
    /// Features: Read/Write not-general-purpose volatile
    /// Pages: 1 pages of length 32 bytes
    /// Extra information for each page: Target address, offset, length 3
    /// 
    /// Main Memory
    /// Implements Com.DalSemi.OneWire.Container.MemoryBank,
    /// Com.DalSemi.OneWire.Container.PagedMemoryBank
    /// Size: 384 starting at physical address 0
    /// Features: Read/Write general-purpose non-volatile
    /// Pages: 12 pages of length 32 bytes giving 29 bytes Packet data payload
    /// Page Features: page-device-CRC
    /// 
    /// Memory with write cycle counter
    /// Implements Com.DalSemi.OneWire.Container.MemoryBank,
    /// Com.DalSemi.OneWire.Container.PagedMemoryBank
    /// Size: 64 starting at physical address 384
    /// Features: Read/Write general-purpose non-volatile
    /// Pages: 2 pages of length 32 bytes giving 29 bytes Packet data payload
    /// Page Features: page-device-CRC
    /// Extra information for each page: Write cycle counter, length 8
    /// 
    /// Memory with externally triggered counter
    /// Implements Com.DalSemi.OneWire.Container.MemoryBank,
    /// Com.DalSemi.OneWire.Container.PagedMemoryBank}
    /// Size: 64 starting at physical address 448
    /// Features: Read/Write general-purpose non-volatile
    /// Pages: 2 pages of length 32 bytes giving 29 bytes Packet data payload
    /// Page Features: page-device-CRC
    /// Extra information for each page: Externally triggered counter, length 8
    /// 
    /// DataSheet
    /// http://pdfserv.maxim-ic.com/arpdf/DS2422-DS2423.pdf
    /// </summary>
    public class OneWireContainer1D : OneWireContainer, ICounterContainer
    {
        /// <summary>
        /// The available pages in this device
        /// </summary>
        private readonly uint[] counterPages = new uint[] { 12, 13, 14, 15 };

        /// <summary>
        /// Gets the family code.
        /// </summary>
        /// <returns></returns>
        public static byte GetFamilyCode()
        {
            return 0x1D;
        }

        /// <summary>
        /// DS2423 read memory command
        /// </summary>
        private const byte READ_MEMORY_COMMAND = (byte)0xA5;

        /// <summary>
        /// Internal buffer
        /// </summary>
        private byte[] buffer = new byte[14];


        /// <summary>
        /// Initializes a new instance of the <see cref="OneWireContainer1D"/> class.
        /// </summary>
        /// <param name="sourceAdapter">adapter object required to communicate with this device.</param>
        /// <param name="newAddress">address of this 1-Wire device</param>
        /// <see cref="OneWireContainer()"/>
        /// <see cref="DalSemi.OneWire.Utils.Address"/>
        public OneWireContainer1D( PortAdapter sourceAdapter, byte[] newAddress )
            : base( sourceAdapter, newAddress )
        {

        }

        /// <summary>
        /// Retrieves the Dallas Semiconductor part number of the 1-Wire device as a string.  For example 'Crypto iButton' or 'DS1992'.
        /// </summary>
        /// <returns>1-Wire device name</returns>
        /// Get the Dallas Semiconductor part number of the iButton
        /// or 1-Wire Device as a string.  For example 'DS1992'.
        public override string GetName()
        {
            return "DS2423";
        }

        /// <summary>
        /// Retrieves a short description of the function of the 1-Wire device type.
        /// </summary>
        /// <returns>Device functional description</returns>
        public override string GetDescription()
        {
            return "1-Wire counter with 4096 bits of read/write, nonvolatile "
                   + "memory.  Memory is partitioned into sixteen pages of 256 bits each.  "
                   + "256 bit scratchpad ensures data transfer integrity.  "
                   + "Has overdrive mode.  Last four pages each have 32 bit "
                   + "read-only non rolling-over counter.  The first two counters "
                   + "increment on a page write cycle and the second two have "
                   + "active-low external triggers.";
        }


        /// <summary>
        /// Returns the maximum speed this iButton or 1-Wire device can communicate at.
        /// Override this method if derived iButton type can go faster than SPEED_REGULAR(0).
        /// </summary>
        /// <returns>The maxumin speed for the this device</returns>
        /// <see cref="DSPortAdapter.SetSpeed"/>
        public override OWSpeed GetMaxSpeed()
        {
            return OWSpeed.SPEED_OVERDRIVE;
        }


        /// <summary>
        /// Returns an a MemoryBankList of MemoryBanks.  Default is no memory banks.
        /// </summary>
        /// <returns>
        /// enumeration of memory banks to read and write memory on this iButton or 1-Wire device
        /// </returns>
        /// <see cref="MemoryBank"/>
        public override MemoryBankList GetMemoryBanks()
        {
            MemoryBankList memoryBanks = new MemoryBankList();
            
            // scratchpad
            MemoryBankScratchEx scratch = new MemoryBankScratchEx( this );
            memoryBanks.Add( scratch );

            // NVRAM 0000H - 17Fh
            MemoryBankNVCRC nv = new MemoryBankNVCRC( this, scratch );

            nv.numberPages = 12;
            nv.size = 384; // 180h
            nv.extraInfoLength = 8;
            nv.readContinuePossible = false;
            nv.numVerifyBytes = 8;

            memoryBanks.Add( nv );

            // NVRAM (with write cycle counters) 180h - 1BFh, page 12-13
            nv = new MemoryBankNVCRC( this, scratch );
            nv.numberPages = 2;
            nv.size = 64;
            nv.bankDescription = "Memory with write cycle counter";
            nv.startPhysicalAddress = 384; // 180h
            nv.extraInfo = true;
            nv.extraInfoDescription = "Write cycle counter";
            nv.extraInfoLength = 8;
            nv.readContinuePossible = false;
            nv.numVerifyBytes = 8;

            memoryBanks.Add( nv );

            // NVRAM (with external counters) 1C0h - 1FFh, page 14 & 15
            nv = new MemoryBankNVCRC( this, scratch );
            nv.numberPages = 2;
            nv.size = 64;
            nv.bankDescription = "Memory with externally triggered counter";
            nv.startPhysicalAddress = 448;
            nv.extraInfo = true;
            nv.extraInfoDescription = "Externally triggered counter";
            nv.extraInfoLength = 8;

            memoryBanks.Add( nv );
            
            return memoryBanks;
        }


        #region ICounterContainer Members

        /// <summary>
        /// Gets the page numbers of the of counters in this device.
        /// </summary>
        /// <returns></returns>
        public uint[] GetCounterPages()
        {
            return counterPages;
        }

        /// <summary>
        /// Gets the count for the specified counter.
        /// </summary>
        /// <param name="counterPage">The counter page.</param>
        /// <param name="state">The state, as returned from OneWireContainer.ReadDevice()</param>
        /// <returns></returns>
        /// <exception cref="OneWireIOException">Thrown if a IO error occures</exception>
        /// <exception cref="OneWireException">Thrown on a communication or setup error with the 1-Wire adapter</exception>
        public uint ReadCounter( uint counterPage )
        {
            // check if counter page provided is valid
            if( counterPage < 12 || counterPage > 15 )
                throw new OneWireException( "OneWireContainer1D-invalid counter page" );

            // select the device 
            if( adapter.SelectDevice( address ) ) {
                
                // Read the counter only by starting at the last byte of the
                // page associated with that counter
                buffer[0] = READ_MEMORY_COMMAND;
                
                // Address of last data byte before counter
                // Shifting the page number left 5 bits gives the start of the memory page
                // Adding 31 gives the last of the page (when reading past this boundary
                // the device will transmit the counter for the current page)
                // 
                uint addressOfByteBeforeCounter = ( counterPage << 5 ) + 31;

                // Insert address into the datablock to send
                buffer[1] = (byte)addressOfByteBeforeCounter; // Low byte of start offset (TA1)
                buffer[2] = (byte)( addressOfByteBeforeCounter >> 8 ); // High byte of start offset (TA2)

                // Now add the read-slot bytes for 1 data byte, 4 counter bytes, 4 zero bytes and (inverted) crc16
                for( int i = 3; i < 14; i++ )
                    buffer[i] = (byte)0xFF;

                // Send the block
                adapter.DataBlock( buffer, 0, 14 );

                // The buffer now contains:
                // command | TA1 | TA2 | data byte | 4 counter bytes | 4 zero bytes | 2 bytes inverted crc16
                
                // Calculate the checksum of the buffer up to the inverted crc16
                Int16 crc16 = (Int16)CRC16.Compute( buffer, 0, 12, 0 );

                // Extract the crc16 from the buffer
                Int16 inverted = buffer[13];    // High byte
                inverted <<= 8;
                inverted += buffer[12];             

                // Compare the two CRC16
                if( ( ~inverted ) == crc16 ) {
                    // Packet verified, extract the counter
                    uint count = 0;

                    for( int i = 4; i >= 1; i-- ) {
                        count <<= 8;
                        count |= buffer[i + 3];
                    }

                    return count;
                }
                else {
                    throw new OneWireException( "CRC16 validation error while reading counter from page " + counterPage );
                }

                // calculate the CRC16 on the result and check if correct
                //if( CRC16.Compute( buffer, 3, 11, crc16 ) == 0xB001 ) {
                    
                //}
            }

            // device must not have been present
            throw new OneWireIOException( "OneWireContainer1D-device not present" );

        }

        #endregion
    }
}
