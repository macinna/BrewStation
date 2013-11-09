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
using System.Text;

namespace DalSemi.OneWire.Utils
{
	/// <summary>
	/// Utilities for conversion between miscellaneous datatypes.
	/// </summary>
	public class Convert
	{
		/** returns hex character for each digit, 0-15 */
		private readonly char[] lookup_hex = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

		/**
		 * Not to be instantiated
		 */
		private Convert()
		{ ;}

		// ----------------------------------------------------------------------
		// Temperature conversions
		// ----------------------------------------------------------------------

		// ??? does this optimization help out on TINI, where double-division is
		// ??? potentially slower?  If not, feel free to delete it.
		// ???
		/** cache the value of five divided by nine, which is irrational */
		private const double FIVE_NINTHS = ( 5.0d / 9.0d );

		/// <summary>
		/// Converts a temperature reading from Celsius to Fahrenheit.
		/// </summary>
		/// <param name="celsiusTemperature">temperature value in Celsius</param>
		/// <returns>Fahrenheit conversion of the supplied temperature</returns>
		public static double ToFahrenheit( double celsiusTemperature )
		{
			// (9/5)=1.8
			return celsiusTemperature * 1.8d + 32.0d;
		}

		/// <summary>
		/// Converts a temperature reading from Fahrenheit to Celsius.
		/// </summary>
		/// <param name="fahrenheitTemperature">temperature value in Fahrenheit</param>
		/// <returns>conversion of the supplied temperature</returns>
		public static double ToCelsius( double fahrenheitTemperature )
		{
			return ( fahrenheitTemperature - 32.0d ) * FIVE_NINTHS;
		}

		// ----------------------------------------------------------------------
		// Long <-> ByteArray conversions
		// ----------------------------------------------------------------------

		/// <summary>
		/// This method constructs a long from a LSByte byte array of specified length..
		/// </summary>
		/// <param name="byteArray">The byte array.</param>
		/// <param name="offset">byte array to convert to a long (LSByte first)</param>
		/// <param name="len">number of bytes to use to convert to a long</param>
		/// <returns>value constructed from bytes</returns>
		public static long ToLong( byte[] byteArray, int offset, int len )
		{
			long val = 0;

			len = Math.Min( len, 8 );

			// Concatanate the byte array into one variable.
			for( int i = ( len - 1 ); i >= 0; i-- ) {
				val <<= 8;
				val |= (byte)( byteArray[offset + i] & 0x00FF );
			}

			return val;
		}

		/// <summary>
		/// This method constructs a long from a LSByte byte array of specified length.
		/// Uses 8 bytes starting at the first index.
		/// </summary>
		/// <param name="byteArray">byte array to convert to a long (LSByte first)</param>
		/// <returns>value constructed from bytes</returns>
		public static long ToLong( byte[] byteArray )
		{
			return ToLong( byteArray, 0, Math.Min( 8, byteArray.Length ) );
		}

		/// <summary>
		/// This method constructs a LSByte byte array with specified length from a long.
		/// </summary>
		/// <param name="longVal">the long value to convert to a byte array.</param>
		/// <param name="byteArray">LSByte first byte array, holds bytes from long</param>
		/// <param name="offset">byte offset into the array</param>
		/// <param name="len">value constructed from bytes</param>
		public static void ToByteArray( long longVal, byte[] byteArray, int offset, int len )
		{
			int max = offset + len;

			// Concatanate the byte array into one variable.
			for( int i = offset; i < max; i++ ) {
				byteArray[i] = (byte)longVal;
				longVal >>= 8; // >> was >>>
			}
		}

		/// <summary>
		/// This method constructs a LSByte byte array with 8 bytes from a long.
		/// </summary>
		/// <param name="longVal">the long value to convert to a byte array.</param>
		/// <param name="byteArray">LSByte first byte array, holds bytes from long</param>
		public static void ToByteArray( long longVal, byte[] byteArray )
		{
			ToByteArray( longVal, byteArray, 0, 8 );
		}

		/// <summary>
		/// This method constructs a LSByte byte array with 8 bytes from a long.
		/// </summary>
		/// <param name="longVal">the long value to convert to a byte array.</param>
		/// <returns>value constructed from bytes</returns>
		public static byte[] ToByteArray( long longVal )
		{
			byte[] byteArray = new byte[8];
			ToByteArray( longVal, byteArray, 0, 8 );
			return byteArray;
		}

		// ----------------------------------------------------------------------
		// Int <-> ByteArray conversions
		// ----------------------------------------------------------------------

		/// <summary>
		/// This method constructs an int from a LSByte byte array of specified length.
		/// </summary>
		/// <param name="byteArray">byte array to convert to an int (LSByte first)</param>
		/// <param name="offset">byte offset into the array where to start to convert</param>
		/// <param name="len">number of bytes to use to convert to an int</param>
		/// <returns>value constructed from bytes</returns>
		public static int ToInt( byte[] byteArray, int offset, int len )
		{
			int val = 0;

			len = Math.Min( len, 4 );

			// Concatanate the byte array into one variable.
			for( int i = ( len - 1 ); i >= 0; i-- ) {
				val <<= 8;
				val |= ( byteArray[offset + i] & 0x00FF );
			}

			return val;
		}

		/// <summary>
		/// This method constructs an int from a LSByte byte array of specified length.
		/// Uses 4 bytes starting at the first index.
		/// </summary>
		/// <param name="byteArray">byte array to convert to an int (LSByte first)</param>
		/// <returns>value constructed from bytes</returns>
		public static int ToInt( byte[] byteArray )
		{
			return ToInt( byteArray, 0, Math.Min( 4, byteArray.Length ) );
		}

		/// <summary>
		/// This method constructs a LSByte byte array with specified length from an int.
		/// </summary>
		/// <param name="intVal">the int value to convert to a byte array.</param>
		/// <param name="byteArray">LSByte first byte array, holds bytes from int</param>
		/// <param name="offset">byte offset into the array</param>
		/// <param name="len">number of bytes to get</param>
		public static void ToByteArray( int intVal,
											 byte[] byteArray, int offset, int len )
		{
			int max = offset + len;

			// Concatanate the byte array into one variable.
			for( int i = offset; i < max; i++ ) {
				byteArray[i] = (byte)intVal;
				intVal >>= 8; // >> was >>>
			}
		}

		/// <summary>
		/// This method constructs a LSByte byte array with 4 bytes from an int.
		/// </summary>
		/// <param name="intVal">the int value to convert to a byte array.</param>
		/// <param name="byteArray">LSByte first byte array, holds bytes from long</param>
		public static void ToByteArray( int intVal, byte[] byteArray )
		{
			ToByteArray( intVal, byteArray, 0, 4 );
		}

		/// <summary>
		/// This method constructs a LSByte byte array with 4 bytes from an int.
		/// </summary>
		/// <param name="intVal">the int value to convert to a byte array.</param>
		/// <returns>value constructed from bytes</returns>
		public static byte[] ToByteArray( int intVal )
		{
			byte[] byteArray = new byte[4];
			ToByteArray( intVal, byteArray, 0, 4 );
			return byteArray;
		}

		// ----------------------------------------------------------------------
		// String <-> ByteArray conversions
		// ----------------------------------------------------------------------

		/// <summary>
		/// Converts a hex-encoded string into an array of bytes.
		/// To illustrate the rules for parsing, the following string:
		/// "FF 0 1234 567"
		/// becomes:
		/// byte[] { 0xFF, 0x00, 0x12, 0x34, 0x56, 0x07 }
		/// </summary>
		/// <param name="strData">hex-encoded numerical string</param>
		/// <returns>the decoded bytes</returns>
		public static byte[] ToByteArray( string strData )
		{
			byte[] bDataTmp = new byte[strData.Length * 2];
			int len = ToByteArray( strData, bDataTmp, 0, bDataTmp.Length );
			byte[] bData = new byte[len];
			Array.Copy( bDataTmp, 0, bData, 0, len );
			return bData;
		}

		/// <summary>
		/// Converts a hex-encoded string into an array of bytes.
		/// To illustrate the rules for parsing, the following String:
		/// "FF 0 1234 567"
		/// becomes:
		/// byte[] { 0xFF, 0x00, 0x12, 0x34, 0x56, 0x07 }
		/// </summary>
		/// <param name="strData">hex-encoded numerical string</param>
		/// <param name="bData">byte[] which will hold the decoded bytes</param>
		/// <returns>The number of bytes converted</returns>
		public static int ToByteArray( String strData, byte[] bData )
		{
			return ToByteArray( strData, bData, 0, bData.Length );
		}



		/// <summary>
		/// Converts a hex-encoded string into an array of bytes.
		/// To illustrate the rules for parsing, the following string:
		/// "FF 0 1234 567"
		/// becomes:
		/// byte[] { 0xFF, 0x00, 0x12, 0x34, 0x56, 0x07 }
		/// </summary>
		/// <param name="strData">hex-encoded numerical string</param>
		/// <param name="bData">byte[] which will hold the decoded bytes</param>
		/// <param name="offset">the offset into bData to start placing bytes</param>
		/// <param name="length">the maximum number of bytes to convert</param>
		/// <returns>The number of bytes converted</returns>
		public static int ToByteArray( string strData,
											byte[] bData, int offset, int length )
		{
			int strIndex = 0, strLength = strData.Length;
			int index = offset;
			int end = length + offset;

			while( index < end && strIndex < strLength ) {
				char lower = '0', upper;
				do {
					upper = strData.ToCharArray()[strIndex++];
				}
				while( strIndex < strLength && ( upper == ' ' ) );

				if( strIndex < strLength ) {
					lower = strData.ToCharArray()[strIndex++];
					if( lower == ' ' ) {
						lower = upper;
						upper = '0';
					}
				}
				else {
					if( !( upper == ' ' ) )
						lower = upper;
					upper = '0';
				}

				bData[index++] = byte.Parse( upper.ToString() + lower.ToString(), System.Globalization.NumberStyles.HexNumber );

				//             (byte)((Character.digit(upper, 16) << 4)
				//                            | Character.digit(lower, 16));
			}
			return ( index - offset );
		}

		/// <summary>
		/// Converts a byte array into a hex-encoded String, using the provided delimeter.
		/// </summary>
		/// <param name="data">The byte[] to convert to a hex-encoded string</param>
		/// <returns>Hex-encoded string</returns>
		public static string ToHexString( byte[] data )
		{
			return ToHexString( data, 0, data.Length, "" );
		}

		/// <summary>
		/// Converts a byte array into a hex-encoded String, using the provided delimeter.
		/// </summary>
		/// <param name="data">The byte[] to convert to a hex-encoded string</param>
		/// <param name="offset">the offset to start converting bytes</param>
		/// <param name="length">the number of bytes to convert</param>
		/// <returns>Hex-encoded string</returns>
		public static string ToHexString( byte[] data, int offset, int length )
		{
			return ToHexString( data, offset, length, "" );
		}


		/// <summary>
		/// Converts a byte array into a hex-encoded String, using the provided delimeter.
		/// </summary>
		/// <param name="data">The byte[] to convert to a hex-encoded string</param>
		/// <param name="delimeter">the delimeter to place between each byte of data</param>
		/// <returns>Hex-encoded string</returns>
		public static string ToHexString( byte[] data, String delimeter )
		{
			return ToHexString( data, 0, data.Length, delimeter );
		}

		/// <summary>
		/// Converts a byte array into a hex-encoded String, using the provided delimeter.
		/// </summary>
		/// <param name="data">The byte[] to convert to a hex-encoded string</param>
		/// <param name="offset">the offset to start converting bytes</param>
		/// <param name="length">the number of bytes to convert</param>
		/// <param name="delimeter">the delimeter to place between each byte of data</param>
		/// <returns>Hex-encoded String</returns>
		public static string ToHexString( byte[] data, int offset, int length, string delimeter )
		{
			StringBuilder value = new StringBuilder( length * ( 2 + delimeter.Length ) );
			int max = length + offset;
			int lastDelim = max - 1;
			for( int i = offset; i < max; i++ ) {
				//byte bits = data[i];
				//value.Append(lookup_hex[(bits>>4)&0x0F]);
				//value.Append(lookup_hex[bits&0x0F]);

				value.Append( string.Format( "{0:X2}", data[i] ) );

				if( i < lastDelim )
					value.Append( delimeter );
			}
			return value.ToString();
		}

		/// <summary>
		/// Converts a single byte into a hex-encoded string.
		/// </summary>
		/// <param name="bValue">the byte to encode</param>
		/// <returns>Hex-encoded string</returns>
		public static string ToHexString( byte bValue )
		{
			return string.Format( "{0:X2}", bValue );
		}

		/// <summary>
		/// Converts a char array into a hex-encoded String, using the provided
		/// delimeter.
		/// </summary>
		/// <param name="data">The char[] to convert to a hex-encoded string</param>
		/// <returns>Hex-encoded string</returns>
		public static string ToHexString( char[] data )
		{
			return ToHexString( data, 0, data.Length, "" );
		}

		/// <summary>
		/// Converts a byte array into a hex-encoded String, using the provided delimeter.
		/// </summary>
		/// <param name="data">The char[] to convert to a hex-encoded string</param>
		/// <param name="offset">the offset to start converting bytes</param>
		/// <param name="length">the number of bytes to convert</param>
		/// <returns>Hex-encoded string</returns>
		public static string ToHexString( char[] data, int offset, int length )
		{
			return ToHexString( data, offset, length, "" );
		}


		/// <summary>
		/// Converts a char array into a hex-encoded String, using the provided
		/// delimeter.
		/// </summary>
		/// <param name="data">The char[] to convert to a hex-encoded string</param>
		/// <param name="delimeter">the delimeter to place between each byte of data</param>
		/// <returns>Hex-encoded string</returns>
		public static string ToHexString( char[] data, String delimeter )
		{
			return ToHexString( data, 0, data.Length, delimeter );
		}

		/// <summary>
		/// Converts a char array into a hex-encoded string, using the provided delimeter.
		/// </summary>
		/// <param name="data">The char[] to convert to a hex-encoded string</param>
		/// <param name="offset">the offset to start converting bytes</param>
		/// <param name="length">the number of bytes to convert</param>
		/// <param name="delimeter">the delimeter to place between each byte of data</param>
		/// <returns>Hex-encoded string</returns>
		public static string ToHexString( char[] data, int offset, int length, string delimeter )
		{
			StringBuilder value = new StringBuilder( length * ( 2 + delimeter.Length ) );
			int max = length + offset;
			int lastDelim = max - 1;
			for( int i = offset; i < max; i++ ) {
				value.Append( string.Format( "{0:X2}", (byte)data[i] ) );
				if( i < lastDelim )
					value.Append( delimeter );
			}
			return value.ToString();
		}

		/// <summary>
		/// Converts a single character into a hex-encoded string.
		/// </summary>
		/// <param name="bValue">the byte to encode</param>
		/// <returns>Hex-encoded String</returns>
		public static string ToHexString( char bValue )
		{
			return ToHexString( (byte)bValue );
		}


		// ----------------------------------------------------------------------
		// String <-> Long conversions
		// ----------------------------------------------------------------------




		/// <summary>
		/// Converts a hex-encoded string (LSByte) into a long.
		/// To illustrate the rules for parsing, the following string:
		/// "FF 0 1234 567 12 03"
		/// becomes:
		/// long 0x03120756341200ff
		/// </summary>
		/// <param name="strData">hex-encoded numerical string</param>
		/// <returns>the decoded long</returns>
		public static long ToLong( string strData )
		{
			return ToLong( ToByteArray( strData ) );
		}

		/// <summary>
		/// Converts a long into a hex-encoded string (LSByte).
		/// </summary>
		/// <param name="lValue">the long integer to encode</param>
		/// <returns>Hex-encoded string</returns>
		public static string ToHexString( long lValue )
		{
			return ToHexString( ToByteArray( lValue ), "" );
		}

		// ----------------------------------------------------------------------
		// String <-> Int conversions
		// ----------------------------------------------------------------------

		/// <summary>
		/// Converts a hex-encoded string (LSByte) into an int.
		/// To illustrate the rules for parsing, the following string:
		/// "FF 0 1234 567 12 03"
		/// becomes:
		/// long 0x03120756341200ff
		/// </summary>
		/// <param name="strData">hex-encoded numerical string</param>
		/// <returns>the decoded int</returns>
		public static int ToInt( String strData )
		{
			return ToInt( ToByteArray( strData ) );
		}

		/// <summary>
		/// Converts an integer into a hex-encoded string (LSByte).
		/// </summary>
		/// <param name="iValue">the integer to encode</param>
		/// <returns>Hex-encoded string</returns>
		public static string ToHexString( int iValue )
		{
			return ToHexString( ToByteArray( iValue ), "" );
		}

		// ----------------------------------------------------------------------
		// Double conversions
		// ----------------------------------------------------------------------

		/** Field Double NEGATIVE_INFINITY */
		private const double d_POSITIVE_INFINITY = 1.0d / 0.0d;
		/** Field Double NEGATIVE_INFINITY */
		private const double d_NEGATIVE_INFINITY = -1.0d / 0.0d;

		/// <summary>
		/// Converts a double value into a string with the specified number of
		/// digits after the decimal place.
		/// </summary>
		/// <param name="dubbel">the double value to convert to a string</param>
		/// <param name="nFrac">the number of digits to display after the decimal point</param>
		/// <returns>String representation of the double value with the specified
		/// number of digits after the decimal place.</returns>
		public static string ToString( double dubbel, int nFrac )
		{
			// check for special case
			if( dubbel == d_POSITIVE_INFINITY )
				return "Infinity";
			else if( dubbel == d_NEGATIVE_INFINITY )
				return "-Infinity";
			else if( (double)dubbel != dubbel )
				return "NaN";

			// check for fast out (no fractional digits)
			if( nFrac <= 0 )
				// round the whole portion
				return ( (long)( dubbel + 0.5d ) ).ToString();

			// extract the non-fractional portion
			long dWhole = (long)dubbel;

			// figure out if it's positive or negative.  We need to remove
			// the sign from the fractional part
			double sign = ( dWhole < 0 ) ? -1d : 1d;

			// figure out how many places to shift fractional portion
			double shifter = 1;
			for( int ii = 0; ii < nFrac; ii++ )
				shifter *= 10;

			// extract, unsign, shift, and round the fractional portion
			long dFrac = (long)( ( dubbel - dWhole ) * sign * shifter + 0.5d );

			// convert the fractional portion to a string
			string fracString = dFrac.ToString();
			int fracLength = fracString.Length;

			// ensure that rounding the fraction didn'thread carry into the whole portion
			if( fracLength > nFrac ) {
				dWhole += 1;
				fracLength = 0;
			}

			// convert the whole portion to a string
			String wholeString = dWhole.ToString();
			int wholeLength = wholeString.Length;

			// create the string buffer
			char[] dubbelChars = new char[wholeLength + 1 + nFrac];

			// append the non-fractional portion
			//wholeString.getChars(0, wholeLength, dubbelChars, 0);
			Array.Copy( wholeString.ToCharArray(), dubbelChars, wholeLength );

			// and the decimal place
			dubbelChars[wholeLength] = '.';

			// append any necessary leading zeroes
			int i = wholeLength + 1;
			int max = i + nFrac - fracLength;
			for( ; i < max; i++ )
				dubbelChars[i] = '0';

			// append the fractional portion
			if( fracLength > 0 )
				//fracString.getChars(0, fracLength, dubbelChars, max);
				Array.Copy( fracString.ToCharArray(), 0, dubbelChars, max, fracLength );

			return new String( dubbelChars, 0, dubbelChars.Length );
		}


		// ----------------------------------------------------------------------
		// Float conversions
		// ----------------------------------------------------------------------

		/** Field Float NEGATIVE_INFINITY */
		private const float f_POSITIVE_INFINITY = 1.0f / 0.0f;
		/** Field Float NEGATIVE_INFINITY */
		private const float f_NEGATIVE_INFINITY = -1.0f / 0.0f;

		/// <summary>
		/// Converts a float value into a string with the specified number of
		/// digits after the decimal place.
		/// Note: this function does not properly handle special case float
		/// values such as Infinity and NaN.
		/// </summary>
		/// <param name="flote">the float value to convert to a string</param>
		/// <param name="nFrac">representation of the float value with the specified
		/// number of digits after the decimal place.</param>
		/// <returns>ring representation of the float value with the specified
		/// number of digits after the decimal place.</returns>
		public static string ToString( float flote, int nFrac )
		{
			// check for special case
			if( flote == f_POSITIVE_INFINITY )
				return "Infinity";
			else if( flote == f_NEGATIVE_INFINITY )
				return "-Infinity";
			else if( (float)flote != flote )
				return "NaN";

			// check for fast out (no fractional digits)
			if( nFrac <= 0 )
				// round the whole portion
				return ( (long)( flote + 0.5f ) ).ToString();

			// extract the non-fractional portion
			long fWhole = (long)flote;

			// figure out if it's positive or negative.  We need to remove
			// the sign from the fractional part
			float sign = ( fWhole < 0 ) ? -1f : 1f;

			// figure out how many places to shift fractional portion
			float shifter = 1;
			for( int ii = 0; ii < nFrac; ii++ )
				shifter *= 10;

			// extract, shift, and round the fractional portion
			long fFrac = (long)( ( flote - fWhole ) * sign * shifter + 0.5f );

			// convert the fractional portion to a string
			String fracString = fFrac.ToString();
			int fracLength = fracString.Length;

			// ensure that rounding the fraction didn'thread carry into the whole portion
			if( fracLength > nFrac ) {
				fWhole += 1;
				fracLength = 0;
			}

			// convert the whole portion to a string
			String wholeString = fWhole.ToString();
			int wholeLength = wholeString.Length;

			// create the string buffer
			char[] floteChars = new char[wholeLength + 1 + nFrac];

			// append the non-fractional portion

			//wholeString.getChars(0, wholeLength, floteChars, 0);
			Array.Copy( wholeString.ToCharArray(), floteChars, wholeLength );



			// and the decimal place
			floteChars[wholeLength] = '.';

			// append any necessary leading zeroes
			int i = wholeLength + 1;
			int max = i + nFrac - fracLength;
			for( ; i < max; i++ )
				floteChars[i] = '0';

			// append the fractional portion
			if( fracLength > 0 )
				//fracString.getChars(0, fracLength, floteChars, max);
				Array.Copy( fracString.ToCharArray(), 0, floteChars, max, fracLength );


			return new string( floteChars );
		}

        public static DateTime ToDateTime(long msSinceEpoch)
		{
            return new DateTime(1970, 1, 1).AddMilliseconds(msSinceEpoch).ToLocalTime();
		}

        public static long ToMSSinceEpoch(DateTime lt)
		{
            TimeSpan ts = lt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1));
            return (long)ts.TotalMilliseconds;
		}

	}

}
