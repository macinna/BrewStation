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

namespace DalSemi.OneWire.Container
{

    /**
     * <P> 1-Wire container for 512 byte EEPROM memory iButton, DS1973 and 1-Wire Chip, DS2433. 
     * This container encapsulates the functionality of the 1-Wire family 
     * type <B>23</B> (hex)</P>
     *
     * <P> The iButton package for this device is primarily used as a read/write portable memory device.  
     * The 1-Wire Chip version is used for small non-volatile storage. </P>
     * 
     * <H3> Features </H3> 
     * <UL>
     *   <LI> 4096 bits (512 bytes) Electrically Erasable Programmable Read Only Memory
     *        (EEPROM)
     *   <LI> 256-bit (32-byte) scratchpad ensures integrity of data
     *        transfer
     *   <LI> Memory partitioned into 256-bit (32-byte) pages for
     *        packetizing data
     *   <LI> Overdrive mode boosts communication to
     *        142 kbits per second
     *   <LI> Reduces control, address, data and power to
     *        a single data pin
     *   <LI> Reads and writes over a wide voltage range
     *        of 2.8V to 6.0V from -40&#176C to +85&#176C environments
     * </UL>
     * 
     * <H3> Alternate Names </H3>
     * <UL>
     *   <LI> DS2433 
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
     *   <LI> <B> Scratchpad </B> 
     *      <UL> 
     *         <LI> <I> Implements </I> {@link com.dalsemi.onewire.container.MemoryBank MemoryBank}, 
     *                   {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     *         <LI> <I> Size </I> 32 starting at physical address 0
     *         <LI> <I> Features</I> Read/Write not-general-purpose volatile
     *         <LI> <I> Pages</I> 1 pages of length 32 bytes 
     *         <LI> <I> Extra information for each page</I>  Target address, offset, length 3
     *      </UL> 
     *   <LI> <B> Main Memory </B>
     *      <UL> 
     *         <LI> <I> Implements </I> {@link com.dalsemi.onewire.container.MemoryBank MemoryBank}, 
     *                  {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     *         <LI> <I> Size </I> 512 starting at physical address 0
     *         <LI> <I> Features</I> Read/Write general-purpose non-volatile
     *         <LI> <I> Pages</I> 16 pages of length 32 bytes giving 29 bytes Packet data payload
     *      </UL> 
     * </UL>
     * 
     * <H3> Usage </H3> 
     * 
     * <DL> 
     * <DD> See the usage example in 
     * {@link com.dalsemi.onewire.container.OneWireContainer OneWireContainer}
     * to enumerate the MemoryBanks.
     * <DD> See the usage examples in 
     * {@link com.dalsemi.onewire.container.MemoryBank MemoryBank} and
     * {@link com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank}
     * for bank specific operations.
     * </DL>
     *
     * <H3> DataSheets </H3> 
     * <DL>
     * <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS1973.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS1973.pdf</A>
     * <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS2433.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS2433.pdf</A>
     * </DL>
     * 
     * @see com.dalsemi.onewire.container.MemoryBank
     * @see com.dalsemi.onewire.container.PagedMemoryBank
     * @see com.dalsemi.onewire.container.OneWireContainer14
     * 
     * @version    0.00, 28 Aug 2000
     * @author     DS
     */

    public class OneWireContainer23 : OneWireContainer
    {

        public static byte GetFamilyCode()
        {
            return 0x23;
        }


        //--------
        //-------- Constructors
        //--------

        /**
         * Create a container with the provided adapter instance
         * and the address of the iButton or 1-Wire device.<p>
         *
         * This is one of the methods to construct a container.  The other is
         * through creating a OneWireContainer with NO parameters.
         *
         * @param  sourceAdapter     adapter instance used to communicate with
         * this iButton
         * @param  newAddress        {@link com.dalsemi.onewire.utils.Address Address}  
         *                           of this 1-Wire device
         *
         * @see #OneWireContainer23() OneWireContainer23() 
         * @see com.dalsemi.onewire.utils.Address utils.Address
         */
        public OneWireContainer23(PortAdapter sourceAdapter, byte[] newAddress)
            : base(sourceAdapter, newAddress)
        {
        }

        //--------
        //-------- Methods
        //--------

        /**
         * Get the Dallas Semiconductor part number of the iButton
         * or 1-Wire Device as a string.  For example 'DS1992'.
         *
         * @return iButton or 1-Wire device name
         */
        public override string GetName()
        {
            return "DS1973";
        }

        /**
         * Get the alternate Dallas Semiconductor part numbers or names.
         * A 'family' of 1-Wire Network devices may have more than one part number
         * depending on packaging.  There can also be nicknames such as
         * 'Crypto iButton'.
         *
         * @return 1-Wire device alternate names
         */
        public override string GetAlternateNames()
        {
            return "DS2433";
        }

        /**
         * Get a short description of the function of this iButton 
         * or 1-Wire Device type.
         *
         * @return device description
         */
        public override string GetDescription()
        {
            return "4096 bit Electrically Erasable Programmable Read Only Memory "
                   + "(EEPROM) organized as sixteen pages of 256 bits.";
        }

        /**
         * Get the maximum speed this iButton or 1-Wire device can
         * communicate at.
         * Override this method if derived iButton type can go faster then
         * SPEED_REGULAR(0).
         *
         * @return maximum speed
         * @see com.dalsemi.onewire.container.OneWireContainer#SetSpeed super.SetSpeed
         * @see com.dalsemi.onewire.adapter.DSPortAdapter#SPEED_REGULAR DSPortAdapter.SPEED_REGULAR
         * @see com.dalsemi.onewire.adapter.DSPortAdapter#SPEED_OVERDRIVE DSPortAdapter.SPEED_OVERDRIVE
         * @see com.dalsemi.onewire.adapter.DSPortAdapter#SPEED_FLEX DSPortAdapter.SPEED_FLEX
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

            // scratchpad
            MemoryBankScratchEE mbScratch = new MemoryBankScratchEE(this);
            bank_vector.Add(mbScratch);

            // EEPROM 
            MemoryBankNV mbNV = new MemoryBankNV(this, mbScratch);
            mbNV.powerDelivery = true;

            bank_vector.Add(mbNV);

            return bank_vector;
        }


    }
}
