/*---------------------------------------------------------------------------
* Copyright (C) 1999,2000 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace DalSemi.Utils
{

	/// <summary> CRC16 is a class containing an implementation of the
	/// Cyclic-Redundency-Check (CRC) CRC16.  The CRC16 is used in
	/// iButton memory packet structure.
	/// <p>
	/// CRC16 is based on the polynomial = X^16 + X^15 + X^2 + 1.
	/// 
	/// </summary>
	/// <version>     0.00, 28 Aug 2000
	/// </version>
	/// <author>      DS
	/// </author>
	public class CRC16
	{

		//--------
		//-------- Variables
		//--------

		/// <summary> used in CRC16 calculation</summary>
		private static readonly uint[] ODD_PARITY
		 = new uint[] { 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 1, 1, 0 };

		//--------
		//-------- Constructor
		//--------

		/// <summary> Private constructor to prevent instantiation.</summary>
		private CRC16()
		{
		}

		//--------
		//-------- Methods
		//--------

		/// <summary>
		/// Perform the CRC16 on the data element based on a zero seed.
		/// CRC16 is based on the polynomial = X^16 + X^15 + X^2 + 1.
		/// </summary>
		/// <param name="dataToCrc">data element on which to perform the CRC16</param>
		/// <returns>CRC16 value</returns>
		public static uint Compute( uint dataToCrc )
		{
			return Compute( dataToCrc, 0 );
		}

		/// <summary>
		/// Perform the CRC16 on the data element based on the provided seed.
		/// CRC16 is based on the polynomial = X^16 + X^15 + X^2 + 1.
		/// </summary>
		/// <param name="dataToCrc">data element on which to perform the CRC16</param>
		/// <returns>CRC16 value</returns>
		public static uint Compute( uint dataToCrc, uint seed )
		{
			uint dat = ( ( dataToCrc ^ ( seed & 0x0FF ) ) & 0x0FF );

			seed = ( seed & 0x0FFFF ) >> 8;

			uint indx1 = ( dat & 0x0F );
			uint indx2 = ( dat >> 4 );

			if( ( ODD_PARITY[indx1] ^ ODD_PARITY[indx2] ) == 1 )
				seed = seed ^ 0xC001;

			dat = ( dat << 6 );
			seed = seed ^ dat;
			dat = ( dat << 1 );
			seed = seed ^ dat;

			return seed;
		}

		/// <summary>
		/// Perform the CRC16 on an array of data elements based on a zero seed.
		/// CRC16 is based on the polynomial = X^16 + X^15 + X^2 + 1.
		/// </summary>
		/// <param name="dataToCrc">array of data elements on which to perform the CRC16</param>
		/// <returns>CRC16 value</returns>
		public static uint Compute( byte[] dataToCrc )
		{
			return Compute( dataToCrc, 0, dataToCrc.Length, 0 );
		}

		/// <summary>
		/// Perform the CRC16 on an array of data elements based on a zero seed.
		/// CRC16 is based on the polynomial = X^16 + X^15 + X^2 + 1.
		/// </summary>
		/// <param name="dataToCrc">array of data elements on which to perform the CRC16</param>
		/// <param name="off">offset into the data array</param>
		/// <param name="len">length of data to CRC16</param>
		/// <returns>CRC16 value</returns>
		public static uint Compute( byte[] dataToCrc, int off, int len )
		{
			return Compute( dataToCrc, off, len, 0 );
		}

		/// <summary>
		/// Perform the CRC16 on an array of data elements based on the provided seed.
		/// CRC16 is based on the polynomial = X^16 + X^15 + X^2 + 1.
		/// </summary>
		/// <param name="dataToCrc">array of data elements on which to perform the CRC16</param>
		/// <param name="off">offset into the data array</param>
		/// <param name="len">length of data to CRC16</param>
		/// <param name="seed">seed to use for CRC16</param>
		/// <returns>CRC16 value</returns>
		public static uint Compute( byte[] dataToCrc, int off, int len, uint seed )
		{
			// loop to do the crc on each data element
			for( int i = 0; i < len; i++ )
				seed = Compute( dataToCrc[i + off], seed );

			return seed;
		}

		/// <summary>
		/// Perform the CRC16 on an array of data elements based on the provided seed.
		/// CRC16 is based on the polynomial = X^16 + X^15 + X^2 + 1.
		/// </summary>
		/// <param name="dataToCrc">array of data elements on which to perform the CRC16</param>
		/// <param name="seed">seed to use for CRC16</param>
		/// <returns>CRC16 value</returns>
		public static uint Compute( byte[] dataToCrc, uint seed )
		{
			return Compute( dataToCrc, 0, dataToCrc.Length, seed );
        }


        #region Ralph Maas / RM

        /// <summary>
        /// Perform the CRC16 on an array of data elements based on the provided seed.
        /// CRC16 is based on the polynomial = X^16 + X^15 + X^2 + 1.
        /// </summary>
        /// <param name="dataToCrc">array of data elements on which to perform the CRC16</param>
        /// <param name="seed">seed to use for CRC16</param>
        /// <returns>CRC16 value</returns>
        public static uint Compute(string dataToCrc, uint seed)
        {
            byte[] ba = new byte[dataToCrc.Length];
            char[] ca = dataToCrc.ToCharArray();
            for (int i = 0; i < dataToCrc.Length; i++)
                ba[i] = (byte)ca[i];
            return Compute(ba, 0, ba.Length, seed);
        }

        #endregion

    }
}