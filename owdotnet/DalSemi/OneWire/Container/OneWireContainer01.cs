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
     * <P>1-Wire&#174 container that encapsulates the functionality of the 1-Wire
     * family type <B>01</B> (hex), Dallas Semiconductor part number: <B>DS1990A,
     * Serial Number</B>.</P>
     *
     * <P> This 1-Wire device is used as a unique serial number. </P>
     *
     * <H2> Features </H2>
     * <UL>
     *   <LI> 64 bit unique serial number
     *   <LI> Operating temperature range from -40&#176C to
     *        +85&#176C
     * </UL>
     *
     * <H2> Alternate Names </H2>
     * <UL>
     *   <LI> DS2401
     *   <LI> DS1420 (Family 81)
     * </UL>
     *
     * <H2> DataSheets </H2>
     *
     *   <A HREF="http://pdfserv.maxim-ic.com/arpdf/DS1990A.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS1990A.pdf</A><br>
     *   <A HREF="http://pdfserv.maxim-ic.com/arpdf/DS2401.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS2401.pdf</A><br>
     *   <A HREF="http://pdfserv.maxim-ic.com/arpdf/DS1420.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS1420.pdf</A>
     *
     *  @version    0.00, 28 Aug 2000
     *  @author     DS
     */


    public class OneWireContainer01 : OneWireContainer
    {

        public static byte GetFamilyCode()
        {
            return 0x01;
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
         * @see #OneWireContainer01()
         * @see com.dalsemi.onewire.utils.Address
         */
        public OneWireContainer01(PortAdapter sourceAdapter, byte[] newAddress)
            : base(sourceAdapter, newAddress)
        {
        }

        public override string GetName()
        {
            return "DS1990A";
        }

        public override string GetAlternateNames()
        {
            return "DS2401,DS2411";
        }

        public override string GetDescription()
        {
            return "64-bit unique serial number";
        }

    }
}
