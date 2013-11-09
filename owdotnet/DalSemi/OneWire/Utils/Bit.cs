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

	/// <summary>
	/// Utilities for bit operations on an array.
	/// </summary>
	public class Bit
	{

		/// <summary>
		/// Write the bit state in a byte array.
		/// </summary>
		/// <param name="state">New state of the bit 1, 0</param>
		/// <param name="index">Bit index into byte at offset.</param>
		/// <param name="offset">Offset of the byte to modify</param>
		/// <param name="buf">byte array to manipulate</param>
		public static void ArrayWriteBit( int state, int index, int offset, byte[] buf )
		{
			int nbyt = ( index >> 3 );
			int nbit = index - ( nbyt << 3 );

			if( state == 1 )
				buf[nbyt + offset] |= (byte)( 0x01 << nbit );
			else
				buf[nbyt + offset] &= (byte)~( 0x01 << nbit );
		}

		/// <summary>
		/// Read a bit state in a byte array.
		/// </summary>
		/// <param name="index">Index of the bit to read</param>
		/// <param name="offset">Offset of the byte to read</param>
		/// <param name="buf">The byte array to read byte array </param>
		/// <returns>bit state 1 or 0</returns>
		public static int ArrayReadBit( int index, int offset, byte[] buf )
		{
			int nbyt = ( index >> 3 );
			int nbit = index - ( nbyt << 3 );

			return ( ( buf[nbyt + offset] >> nbit ) & 0x01 );
		}
	}

}
