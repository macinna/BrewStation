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

namespace DalSemi.OneWire.Utils
{

    /// <summary>
    /// Utility methods for performing SHA calculations.
    /// </summary>
    public class SHA
    {
        // SHA constants
        private static readonly uint[] KTN = new uint[] { 0x5a827999, 0x6ed9eba1, 0x8f1bbcdc, 0xca62c1d6 };

        private const uint H0 = 0x67452301;
        private const uint H1 = 0xEFCDAB89;
        private const uint H2 = 0x98BADCFE;
        private const uint H3 = 0x10325476;
        private const uint H4 = 0xC3D2E1F0;

        // some local variables in the Compute SHA function.
        // can'thread 'static final' methods with no locals be
        // inlined easier?  I think so, but I need to remember
        // to look into that.
        private static uint word;
        private static int i, j;
        private static uint ShftTmp, Temp;

        private static uint[] MTword = new uint[80];
        private static uint[] H = new uint[5];

        private SHA()
        { /* you can'thread instantiate this class */ }


        /// <summary>
        /// Does Dallas SHA, as specified in DS1963S datasheet.
        /// result is in intel Endian format, starting with the
        /// LSB of E to the MSB of E followed by the LSB of D.
        /// result array should be at least 20 bytes long, after
        /// the offset.
        /// </summary>
        /// <param name="MT">The message block (padded if necessary).</param>
        /// <param name="result">The byte[] into which the result will be copied.</param>
        /// <param name="offset">The starting location in 'result' to start copying.</param>
        /// <returns>Computed SHA</returns>
        public static byte[] ComputeSHA(byte[] MT, byte[] result, int offset)
        {
            lock (typeof(SHA))
            {
                ComputeSHA(MT, H);

                //split up the result into a byte array, LSB first
                for (i = 0; i < 5; i++)
                {
                    word = H[4 - i];
                    j = (i << 2) + offset;
                    result[j + 0] = (byte)((word) & 0x00FF);
                    result[j + 1] = (byte)((word >> 8) & 0x00FF);
                    result[j + 2] = (byte)((word >> 16) & 0x00FF);
                    result[j + 3] = (byte)((word >> 24) & 0x00FF);
                }

                return result;
            }
        }




        /// <summary>
        /// Does Dallas SHA, as specified in DS1963S datasheet.
        /// result is in intel Endian format, starting with the
        /// LSB of E to the MSB of E followed by the LSB of D.
        /// </summary>
        /// <param name="MT">The message block (padded if necessary).</param>
        /// <param name="ABCDE">The result will be copied into this 5-int array.</param>
        public static void ComputeSHA(byte[] MT, uint[] ABCDE)
        {
            lock (typeof(SHA))
            {
                for (i = 0; i < 16; i++)
                    MTword[i] = (uint)(((MT[i * 4 + 0] & 0x00FF) << 24) | ((MT[i * 4 + 1] & 0x00FF) << 16) |
                                       ((MT[i * 4 + 2] & 0x00FF) << 8) | (MT[i * 4 + 3] & 0x00FF));

                for (i = 16; i < 80; i++)
                {
                    ShftTmp = MTword[i - 3] ^ MTword[i - 8] ^ MTword[i - 14] ^ MTword[i - 16];
                    MTword[i] = ((ShftTmp << 1) & 0xFFFFFFFE) |
                                ((ShftTmp >> 31) & 0x00000001);
                }

                ABCDE[0] = H0; //A
                ABCDE[1] = H1; //B
                ABCDE[2] = H2; //C
                ABCDE[3] = H3; //D
                ABCDE[4] = H4; //E

                for (i = 0; i < 80; i++)
                {
                    ShftTmp = ((ABCDE[0] << 5) & 0xFFFFFFE0) | ((ABCDE[0] >> 27) & 0x0000001F);
                    Temp = NLF(ABCDE[1], ABCDE[2], ABCDE[3], i) + ABCDE[4] + KTN[i / 20] + MTword[i] + ShftTmp;
                    ABCDE[4] = ABCDE[3];
                    ABCDE[3] = ABCDE[2];
                    ABCDE[2] = ((ABCDE[1] << 30) & 0xC0000000) | ((ABCDE[1] >> 2) & 0x3FFFFFFF);
                    ABCDE[1] = ABCDE[0];
                    ABCDE[0] = Temp;
                }
            }
        }



        /// <summary>
        /// calculation used for SHA.
        /// static final methods with no locals should definitely be inlined by the compiler.
        /// </summary>
        /// <param name="B">The B.</param>
        /// <param name="C">The C.</param>
        /// <param name="D">The D.</param>
        /// <param name="n">The n.</param>
        /// <returns>NLF</returns>
        private static uint NLF(uint B, uint C, uint D, int n)
        {
            if (n < 20)
                return ((B & C) | ((~B) & D));
            else if (n < 40)
                return (B ^ C ^ D);
            else if (n < 60)
                return ((B & C) | (B & D) | (C & D));
            else
                return (B ^ C ^ D);
        }


    }
}
