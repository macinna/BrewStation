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

namespace DalSemi.OneWire.Utils
{
    public class Address
    {
        /// <summary>
        /// Converts a 1-Wire Network address byte array (little endian)
        /// to a hex string representation (big endian).
        /// </summary>
        /// <param name="address">address, family code first</param>
        /// <returns>address represented in a string, family code last.</returns>
        public static string ToString(byte[] address)
        {
            // When displaying, the CRC is first, family code is last so
            // that the center 6 bytes are a real serial number (not byte reversed).

            char[] barr = new char[16];
            int index = 0;
            int ch;

            for (int i = 7; i >= 0; i--)
            {
                ch = (address[i] >> 4) & 0x0F;
                ch += ((ch > 9) ? 'A' - 10 : '0');
                barr[index++] = (char)ch;
                ch = address[i] & 0x0F;
                ch += ((ch > 9) ? 'A' - 10 : '0');
                barr[index++] = (char)ch;
            }
            return new string(barr);
        }

        public static byte[] ToByteArray(string address)
        {
            if (address.Length != 16)
                throw new OneWireException("Invalid address");
            byte[] ba = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                ba[7 - i] = byte.Parse(address.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return ba;
        }
    }
}
