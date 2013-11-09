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
    /// Generic IO routines.  Supports printing and reading arrays of bytes.
    /// Also, using the setReader and setWriter methods, the source of the
    /// bytes can come from any stream as well as the destination for
    /// written bytes.  All routines are static and final and handle all
    /// exceptional cases by returning a default value.
    /// </summary>
    public sealed class IOHelper
    {

        #region Ralph Maas / RM

        private static object lockingObject = new Object();

        #endregion

        /** Do not instantiate this class */
        private IOHelper() { ;}

        private static System.IO.TextWriter /* PrintWriter */ pw = null;


        private static System.IO.TextReader /* BufferedReader */ br = null;
        // default the buffered reader to read from STDIN
        // default the print writer to write to STDOUT
        static IOHelper()
        {
            try
            {
                br = Console.In; // new BufferedReader(new InputStreamReader(System.in));
                pw = Console.Out; //  new PrintWriter(new OutputStreamWriter(System.out));
            }
            catch (Exception)
            {
                //System.err.println("IOHelper: Catastrophic Failure!");
                Console.Error.WriteLine("IOHelper: Catastrophic Failure!");
                //System.exit(1);
                Environment.Exit(1);
            }
        }



        /*----------------------------------------------------------------*/
        /*   Reading Helper Methods                                       */
        /*----------------------------------------------------------------*/



        public static void SetReader(System.IO.TextReader r)
        {
            lock (lockingObject)
            {
                br = r; //  new BufferedReader(r);
            }
        }

        public static string ReadLine()
        {
            lock (lockingObject)
            {
                try
                {
                    return br.ReadLine();
                }
                catch (System.IO.IOException)
                {
                    return "";
                }
            }
        }

        public static byte[] ReadBytes(int count, int pad, Boolean hex)
        {
            lock (lockingObject)
            {
                if (hex)
                    return ReadBytesHex(count, pad);
                else
                    return ReadBytesAsc(count, pad);
            }
        }

        public static byte[] ReadBytesHex(int count, int pad)
        {
            lock (lockingObject)
            {
                try
                {
                    String s = br.ReadLine();
                    int len = s.Length > count ? count
                                                    : s.Length;
                    byte[] ret;

                    if (count > 0)
                        ret = new byte[count];
                    else
                        ret = new byte[s.Length];

                    byte[] temp = ParseHex(s, 0);

                    if (count == 0)
                        return temp;

                    len = temp.Length;

                    Array.Copy(temp, 0, ret, 0, len);

                    for (; len < count; len++)
                        ret[len] = (byte)pad;

                    return ret;
                }
                catch (Exception)
                {
                    return new byte[count];
                }
            }
        }

        public static byte[] ReadBytesAsc(int count, int pad)
        {
            lock (lockingObject)
            {
                try
                {
                    String s = br.ReadLine();
                    int len = s.Length > count ? count
                                                    : s.Length;
                    byte[] ret;

                    if (count > 0)
                        ret = new byte[count];
                    else
                        ret = new byte[s.Length];

                    if (count == 0)
                    {
                        Array.Copy(s.ToCharArray(), 0, ret, 0, s.Length);

                        return ret;
                    }

                    Array.Copy(s.ToCharArray(), 0, ret, 0, len);

                    for (; len < count; len++)
                        ret[len] = (byte)pad;

                    return ret;
                }
                catch (System.IO.IOException)
                {
                    return new byte[count];
                }
            }
        }

        private static byte[] ParseHex(String s, int size)
        {
            byte[] temp;
            int index = 0;
            char[] x = s.ToLower().ToCharArray();

            if (size > 0)
                temp = new byte[size];
            else
                temp = new byte[x.Length];

            try
            {
                for (int i = 0; i < x.Length && index < temp.Length; index++)
                {
                    int digit = -1;

                    while (i < x.Length && digit == -1)
                        digit = int.Parse(x[i++].ToString(), System.Globalization.NumberStyles.HexNumber);
                    if (digit != -1)
                        temp[index] = (byte)((digit << 4) & 0xF0);

                    digit = -1;

                    while (i < x.Length && digit == -1)
                        digit = int.Parse(x[i++].ToString(), System.Globalization.NumberStyles.HexNumber);
                    if (digit != -1)
                        temp[index] |= (byte)(digit & 0x0F);
                }
            }
            catch (Exception) { ;}

            byte[] t;

            if (size == 0 && temp.Length != index)
            {
                t = new byte[index];
                Array.Copy(temp, 0, t, 0, t.Length);
            }
            else
                t = temp;

            return t;
        }

        public static int ReadInt()
        {
            lock (lockingObject)
            {
                return ReadInt(-1);
            }
        }

        public static int ReadInt(int def)
        {
            lock (lockingObject)
            {
                try
                {
                    return int.Parse(br.ReadLine());
                }
                catch (FormatException)
                {
                    return def;
                }
                catch (System.IO.IOException)
                {
                    return def;
                }
            }
        }

        /*----------------------------------------------------------------*/
        /*   Writing Helper Methods                                       */
        /*----------------------------------------------------------------*/

        public static void SetWriter(System.IO.TextWriter w)
        {
            lock (lockingObject)
            {
                pw = w; //  new PrintWriter(w);
            }
        }

        public static void WriteBytesHex(String delim, byte[] b, int offset, int cnt)
        {
            lock (lockingObject)
            {
                int i = offset;
                for (; i < (offset + cnt); )
                {
                    if (i != offset && ((i - offset) & 15) == 0)
                        pw.WriteLine();
                    pw.Write(ByteStr(b[i++]));
                    pw.Write(delim);
                }
                pw.WriteLine();
                pw.Flush();
            }
        }

        public static void WriteBytesHex(byte[] b, int offset, int cnt)
        {
            lock (lockingObject)
            {
                WriteBytesHex(".", b, offset, cnt);
            }
        }
        public static void WriteBytesHex(byte[] b)
        {
            lock (lockingObject)
            {
                WriteBytesHex(".", b, 0, b.Length);
            }
        }

        /**
         * Writes a <code>byte[]</code> to the specified output stream.  This method
         * writes a combined hex and ascii representation where each line has
         * (at most) 16 bytes of data in hex followed by three spaces and the ascii
         * representation of those bytes.  To write out just the Hex representation,
         * use <code>writeBytesHex(byte[],int,int)</code>.
         * 
         * @param b the byte array to print out.
         * @param offset the starting location to begin printing
         * @param cnt the number of bytes to print.
         */
        public static void WriteBytes(String delim, byte[] b, int offset, int cnt)
        {
            lock (lockingObject)
            {
                int last, i;
                last = i = offset;
                for (; i < (offset + cnt); )
                {
                    if (i != offset && ((i - offset) & 15) == 0)
                    {
                        pw.Write("  ");
                        for (; last < i; last++)
                            pw.Write((char)b[last]);
                        pw.WriteLine();
                    }
                    pw.Write(ByteStr(b[i++]));
                    pw.Write(delim);
                }
                for (int k = i; ((k - offset) & 15) != 0; k++)
                {
                    pw.Write("  ");
                    pw.Write(delim);
                }
                pw.Write("  ");
                for (; last < i; last++)
                    pw.Write((char)b[last]);
                pw.WriteLine();
                pw.Flush();
            }
        }

        /**
         * Writes a <code>byte[]</code> to the specified output stream.  This method
         * writes a combined hex and ascii representation where each line has
         * (at most) 16 bytes of data in hex followed by three spaces and the ascii
         * representation of those bytes.  To write out just the Hex representation,
         * use <code>writeBytesHex(byte[],int,int)</code>.
         * 
         * @param b the byte array to print out.
         */
        public static void WriteBytes(byte[] b)
        {
            lock (lockingObject)
            {
                WriteBytes(".", b, 0, b.Length);
            }
        }

        public static void WriteBytes(byte[] b, int offset, int cnt)
        {
            lock (lockingObject)
            {
                WriteBytes(".", b, offset, cnt);
            }
        }

        public static void Write(String s)
        {
            lock (lockingObject)
            {
                pw.Write(s);
                pw.Flush();
            }
        }

        public static void Write(Object o)
        {
            lock (lockingObject)
            {
                pw.Write(o);
                pw.Flush();
            }
        }

        public static void Write(Boolean b)
        {
            lock (lockingObject)
            {
                pw.Write(b);
                pw.Flush();
            }
        }
        public static void Write(int i)
        {
            lock (lockingObject)
            {
                pw.Write(i);
                pw.Flush();
            }
        }

        public static void WriteLine()
        {
            lock (lockingObject)
            {
                pw.WriteLine();
                pw.Flush();
            }
        }
        public static void WriteLine(String s)
        {
            lock (lockingObject)
            {
                pw.WriteLine(s);
                pw.Flush();
            }
        }

        public static void WriteLine(Object o)
        {
            lock (lockingObject)
            {
                pw.WriteLine(o);
                pw.Flush();
            }
        }

        public static void WriteLine(Boolean b)
        {
            lock (lockingObject)
            {
                pw.WriteLine(b);
                pw.Flush();
            }
        }

        public static void WriteLine(int i)
        {
            lock (lockingObject)
            {
                pw.WriteLine(i);
                pw.Flush();
            }
        }

        public static void WriteHex(byte b)
        {
            lock (lockingObject)
            {
                pw.Write(ByteStr(b));
                pw.Flush();
            }
        }

        public static void WriteHex(long l)
        {
            lock (lockingObject)
            {
                pw.Write(l.ToString("X"));
                pw.Flush();
            }
        }

        public static void WriteLineHex(byte b)
        {
            lock (lockingObject)
            {
                pw.WriteLine(ByteStr(b));
                pw.Flush();
            }
        }
        public static void WriteLineHex(long l)
        {
            lock (lockingObject)
            {
                pw.WriteLine(l.ToString("X"));
                pw.Flush();
            }
        }

        private static readonly char[] hex = "0123456789ABCDEF".ToCharArray();

        private static string ByteStr(byte b)
        {
            return "" + hex[((b >> 4) & 0x0F)] + hex[(b & 0x0F)];
        }

    }
}
