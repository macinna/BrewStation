/*---------------------------------------------------------------------------
 * Copyright (C) 2004 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace DalSemi
{
    /// <summary>
    /// A class used to print debug data from the 1-Wire library.
    /// To enable debugging, both Enabled and Output properties must be set.
    /// </summary>
    public class Debug
    {
        private static bool enabled = true;


        /// <summary>
        /// Sets or gets the debugging status.
        /// </summary>
        public static bool Enabled
        {
            set
            {
                enabled = value;
            }
            get
            {
                return enabled && output != null;
            }
        }

        private static System.IO.TextWriter output = null;
        /// <summary>
        /// Sets the output, to which all debugging texts are directed.
        /// </summary>
        /// <value>The output.</value>
        public static System.IO.TextWriter Output
        {
            set
            {
                output = value;
            }
        }

        public static void WriteLine()
        {
            if (Enabled)
            {
                output.WriteLine();
            }
        }

        public static void Write(Object o)
        {
            if (Enabled)
            {
                output.Write(o);
            }
        }

        public static void WriteLine(Object o)
        {
            if (Enabled)
            {
                output.WriteLine(o);
            }
        }

        public static void Write(string s)
        {
            if (Enabled)
            {
                output.Write(s);
            }
        }

        public static void WriteLine(string s)
        {
            if (Enabled)
            {
                output.WriteLine(s);
            }
        }

        public static void Write(string s, params object[] args)
        {
            if (Enabled)
            {
                output.Write(s, args);
            }
        }

        public static void WriteLine(string s, params object[] args)
        {
            if (Enabled)
            {
                output.WriteLine(s, args);
            }
        }

        public static void WriteLineHex(string lbl, byte[] data, int offset, int length)
        {
            if (Enabled)
            {
                output.Write(lbl);
                for (int i = 0; i < length; i++)
                {
                    output.Write(" ");
                    output.Write(data[i].ToString("X2"));
                }
                output.WriteLine();
            }
        }

        public static void WriteInfo()
        {
            if (Enabled)
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                WriteLine("OS: " + Environment.OSVersion);
                WriteLine("Version: " + Environment.Version);
                WriteLine("Assembly: " + assembly.FullName);
                WriteLine("Assembly location: " + assembly.Location);
            }
        }


        /// <summary>
        /// Prints the specified string object if debug mode is enabled.  This method pre-pends the string ">> " to the message.
        /// </summary>
        /// <param name="x">string to print</param>
        public static void DebugStr(string x)
        {
            if (Enabled)
                output.WriteLine(">> " + x);
        }

        /// <summary>
        /// Prints the specified array of bytes with a given label id debuf is enabled and pre-pends the string ">> " to the message.
        /// The resulting output looks like:
        ///     >> the label
        ///     >>  FF 12 45 AE 45 EF FE
        /// </summary>
        /// <param name="lbl">The label</param>
        /// <param name="bytes">The byte array to print</param>
        /// <param name="offset">Offset into arry to start to print from</param>
        /// <param name="length">Number of bytes to print from the array</param>
        public static void DebugStr(string lbl, byte[] bytes, int offset, int length)
        {
            if (Enabled)
            {
                output.Write(">> " + lbl + ", offset=" + offset + ", length=" + length);
                length = Math.Min(bytes.Length - offset, length); // Don'thread let the loop pass the end of the array

                int inc = 8;
                bool printHead = true;
                for (int i = offset; i < length; i += inc)
                {
                    if (printHead)
                    {
                        output.WriteLine();
                        output.Write(">>    ");
                    }
                    else
                    {
                        output.WriteLine(" : ");
                    }
                    int len = Math.Min(inc, length - i);
                    output.Write(DalSemi.OneWire.Utils.Convert.ToHexString(bytes, i, len, " "));
                    printHead = !printHead;
                }
                output.WriteLine();
            }
        }

        /// <summary>
        /// Prints the specified array of bytes with a given label if debug mode is enabled.
        /// This method calls pre-pends the string ">> " to the message
        /// The resulting output looks like:
        ///     >> the label
        ///     >>  FF 12 45 AE 45 EF FE
        /// </summary>
        /// <param name="lbl">The labed</param>
        /// <param name="bytes">the byte array to print</param>
        public static void DebugStr(string lbl, byte[] bytes)
        {
            if (Enabled)
                DebugStr(lbl, bytes, 0, bytes.Length);

        }

        /// <summary>
        /// Prints the specified exception with a given label if debug mode is enabled.
        /// This method calls pre-pends the string ">> " to the message.
        ///     >> my label
        ///     >>   OneWireIOException: Device Not Present
        /// </summary>
        /// <param name="lbl">The label</param>
        /// <param name="ex">The exception to print</param>
        public static void DebugStr(string lbl, Exception ex)
        {
            if (Enabled)
            {
                output.WriteLine(">> " + lbl);
                output.WriteLine(">>    " + ex.Message);
                output.WriteLine(ex.StackTrace);
            }
        }


        /// <summary>
        /// Prints out an exception stack trace for debugging purposes.
        /// This is useful to figure out which functions are calling
        /// a particular function at runtime.
        /// </summary>
        public static void StackTrace()
        {
            if (Enabled)
            {
                try
                {
                    throw new Exception("DEBUG STACK TRACE");
                }
                catch (Exception e)
                {
                    output.WriteLine(e.StackTrace);
                }
            }
        }
    }
}
