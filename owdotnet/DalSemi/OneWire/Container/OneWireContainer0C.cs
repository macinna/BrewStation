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
    public class OneWireContainer0C : OneWireContainer
    {

        public static byte GetFamilyCode()
        {
            return 0x0C;
        }

        /**
         * Create a container with a provided adapter object
         * and the address of the iButton or 1-Wire device.<p>
         *
         * This is one of the methods to construct a container.  The other is
         * through creating a OneWireContainer with NO parameters.
         *
         * @param  sourceAdapter     adapter object required to communicate with
         * this iButton.
         * @param  newAddress        address of this 1-Wire device
         * @see #OneWireContainer()
         * @see com.dalsemi.onewire.utils.Address
         */
        public OneWireContainer0C(PortAdapter sourceAdapter, byte[] newAddress)
            : base(sourceAdapter, newAddress)
        {
        }

        /**
         * Get the Dallas Semiconductor part number of the iButton
         * or 1-Wire Device as a string.  For example 'DS1992'.
         *
         * @return iButton or 1-Wire device name
         */
        public override string GetName()
        {
            return "DS1996";
        }

        /**
         * Get a short description of the function of this iButton 
         * or 1-Wire Device type.
         *
         * @return device description
         */
        public override string GetDescription()
        {
            return "65536 bit read/write nonvolatile memory partitioned "
                   + "into two-hundred fifty-six pages of 256 bits each.";
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

            //Vector bank_vector = new Vector(2);

            // scratchpad
            MemoryBankScratch scratch = new MemoryBankScratch(this);

            bank_vector.Add(scratch);

            // NVRAM
            MemoryBankNV nv = new MemoryBankNV(this, scratch);

            nv.numberPages = 256;
            nv.size = 8192;

            bank_vector.Add(nv);

            return bank_vector;
        }

    }
}
