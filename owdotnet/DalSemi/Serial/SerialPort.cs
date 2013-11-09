// TODO: readEvent -> Handle or SafeWaitHandle.DangerousGetHandle() ???

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

/*============================================================================
 * June 6, 2004
 * Note that the structure of this source was taken right from an MSDN article
 * on Serial I/O with .NET Compact Framework, which went on to become a serial
 * I/O package in the OpenNETCF.org distribution.  Significant portions of 
 * this file which contains all of the high-level functions) and the 
 * accompanying file (CommAPI.cs, which contains all of the low level 
 * functions) were modified by Dallas Semiconductor.  Note that due to the 
 * significant functionality changes, the namespace was changed to prevent 
 * confusion with the original codebase.  The structure of this file was taken
 * from the orginal source, but the meat of the code was ported directly from
 * DalSemi's own Public Domain Kit C Code (i.e. the serial port init, close,
 * flush, read, and write functions).
 * 
 * The OpenNETCF copyright notice can be found below this message.
 * ===========================================================================
 */

//==========================================================================================
//
// OpenNETCF.IO.Serial.Port
// Copyright (c) 2003, OpenNETCF.org
//
// This library is free software; you can redistribute it and/or modify it under 
// the terms of the OpenNETCF.org Shared Source License.
//
// This library is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
// FITNESS FOR A PARTICULAR PURPOSE. See the OpenNETCF.org Shared Source License 
// for more details.
//
// You should have received a copy of the OpenNETCF.org Shared Source License 
// along with this library; if not, email licensing@opennetcf.org to request a copy.
//
// If you wish to contact the OpenNETCF Advisory Board to discuss licensing, please 
// email licensing@opennetcf.org.
//
// For general enquiries, email enquiries@opennetcf.org or visit our website at:
// http://www.opennetcf.org
//
//==========================================================================================

// This file is distributed as part of the open source OWdotNET project.
// Project pages: https://sourceforge.net/projects/owdotnet
// Web Site:      http://owdotnet.sourceforge.net/

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

namespace DalSemi.Serial
{
    public class SerialPortException : System.IO.IOException
    {
        public SerialPortException(string desc) : base(desc) { }
    }

    public class SerialPort : System.IO.Stream // , IDisposable
    {
        // setting this to false will cause Win32 to not alloc unmanaged memory
        // for serial i/o.  instead, api will depend on GC to not move the
        // managed buffers while waiting on overlapped i/o to complete.
        private const bool AllocUnmanagedMemoryBuffers = true;

        #region variable declarations
        private string portName;
        private IntPtr hPort = (IntPtr)CommAPI.INVALID_HANDLE_VALUE;
        private CommTimeouts slowCommTimeouts;
        //private CommTimeouts fastCommTimeouts;
        private DCB dcb = new DCB();
        private DetailedPortSettings portSettings;

        // default Rx buffer is 1024 bytes
        private uint rxBufferSize = 1024;
        // default Tx buffer is 1024 bytes
        private uint txBufferSize = 1024;

        private ManualResetEvent readEvent;
        private OVERLAPPED ovReadEvent;
        private IntPtr ptrOvReadEvent = IntPtr.Zero;
        private IntPtr ptrReadDataBuffer = IntPtr.Zero;

        //private ManualResetEvent writeEvent;
        private OVERLAPPED ovWriteEvent;
        private IntPtr ptrOvWriteEvent = IntPtr.Zero;
        private IntPtr ptrWriteDataBuffer = IntPtr.Zero;

        private int rts = -1;
        private bool rtsavail = false;
        private int dtr = -1;
        private bool dtravail = false;
        private int brk = -1;

        private bool isOpen = false;
        #endregion

        #region constructors
        public SerialPort(string PortName)
            : this(PortName, new DetailedPortSettings(), 1024, 1024)
        {
        }

        public SerialPort(string PortName, BasicPortSettings InitialSettings)
            : this(PortName, InitialSettings, 1024, 1024)
        {
        }

        public SerialPort(string PortName, DetailedPortSettings InitialSettings)
            : this(PortName, InitialSettings, 1024, 1024)
        {
        }

        public SerialPort(string PortName, uint RxBufferSize, uint TxBufferSize)
            : this(PortName, new DetailedPortSettings(), RxBufferSize, TxBufferSize)
        {
        }

        public SerialPort(string PortName, BasicPortSettings InitialSettings, uint RxBufferSize, uint TxBufferSize)
            : this(PortName, new DetailedPortSettings(), RxBufferSize, TxBufferSize)
        {
            //override default ettings
            portSettings.BasicSettings = InitialSettings;
        }

        public SerialPort(string PortName, DetailedPortSettings InitialSettings, uint RxBufferSize, uint TxBufferSize)
        {
            portName = PortName;

            //override default settings
            portSettings = InitialSettings;

            this.rxBufferSize = RxBufferSize;
            this.txBufferSize = TxBufferSize;

            //writeEvent = new ManualResetEvent(false);
            ovWriteEvent = new OVERLAPPED();
            //ovWriteEvent.hEvent = writeEvent.Handle;
            readEvent = new ManualResetEvent(false);
            ovReadEvent = new OVERLAPPED();
            ovReadEvent.hEvent = readEvent.SafeWaitHandle.DangerousGetHandle(); // TODO: test Handle;

            portSettings = InitialSettings;

            // set the Comm timeouts
            slowCommTimeouts = new CommTimeouts();
            slowCommTimeouts.ReadIntervalTimeout = 0;
            slowCommTimeouts.ReadTotalTimeoutConstant = 20;
            slowCommTimeouts.ReadTotalTimeoutMultiplier = 40;
            slowCommTimeouts.WriteTotalTimeoutConstant = 0;
            slowCommTimeouts.WriteTotalTimeoutMultiplier = 0;

            /*fastCommTimeouts = new CommTimeouts();
            fastCommTimeouts.ReadIntervalTimeout = uint.MaxValue;
            fastCommTimeouts.ReadTotalTimeoutConstant = 0;
            fastCommTimeouts.ReadTotalTimeoutMultiplier = 0;
            fastCommTimeouts.WriteTotalTimeoutConstant = 0;
            fastCommTimeouts.WriteTotalTimeoutMultiplier = 0;*/

        }
        #endregion

        #region destructors

        // since the event thread blocks until the port handle is closed
        // implement both a Dispose and destrucor to make sure that we
        // clean up as soon as possible
        public new void Dispose()
        {
            Dispose( true );
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream"></see> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            this.Close();
            base.Dispose(disposing);
        }

        #endregion

        #region methods

        public static System.Collections.IList PortNames
        {
            get
            {
                System.Collections.ArrayList al = new System.Collections.ArrayList(16);
                for (int i = 0; i < 16; i++)
                {
                    String pn = CommAPI.GetPortName(i);
                    IntPtr tmpPort = CommAPI.CreateFile(pn);
                    if ((tmpPort != (IntPtr)0) && (tmpPort != (IntPtr)(-1)))
                    {
                        CommAPI.CloseHandle(tmpPort);
                        al.Add(pn);
                    }
                }
                return al;
            }
        }

        public bool Open()
        {
            if (isOpen) return false;

            Debug.WriteInfo();
            Debug.WriteLine("DEBUG: SerialPort.Open() called");
            Debug.WriteLine("DEBUG: CommAPI.IsWin32: " + CommAPI.IsWin32);
            Debug.WriteLine("DEBUG: CommAPI.IsWinCE: " + CommAPI.IsWinCE);
            Debug.WriteLine("DEBUG: CommAPI.IsLinux: " + CommAPI.IsLinux);
            Debug.WriteLine("DEBUG: CommAPI.CreateFile: " + portName);
            hPort = CommAPI.CreateFile(portName);
            Debug.WriteLine("DEBUG: hPort = " + hPort.ToString());

            if (hPort == (IntPtr)CommAPI.INVALID_HANDLE_VALUE)
            //if(((int)hPort) <= 0)
            {
                APIErrors e = CommAPI.GetLastError();
                Debug.WriteLine("DEBUG: CreateFile Error = " + e);

                if (e == APIErrors.ERROR_ACCESS_DENIED)
                {
                    // port is unavailable
                    return false;
                }
                throw new SerialPortException("Createfile Failed: " + e);
            }

            if (!CommAPI.SetCommMask(hPort, Serial.CommEventFlags.RXCHAR
               | Serial.CommEventFlags.TXEMPTY
               | Serial.CommEventFlags.ERR
               | Serial.CommEventFlags.BREAK))
            {
                APIErrors e = CommAPI.GetLastError();
                isOpen = false;
                CommAPI.CloseHandle(hPort);
                throw new SerialPortException("SetCommMask failed: " + e);
            }

            // set queue sizes
            if (!CommAPI.SetupComm(hPort, rxBufferSize, txBufferSize))
            {
                APIErrors e = CommAPI.GetLastError();
                isOpen = false;
                CommAPI.CloseHandle(hPort);
                throw new SerialPortException("SetupComm failed: " + e);
            }

            // transfer the port settings to a DCB structure
            dcb.BaudRate = (uint)portSettings.BasicSettings.BaudRate;
            dcb.ByteSize = portSettings.BasicSettings.ByteSize;
            dcb.EofChar = (sbyte)portSettings.EOFChar;
            dcb.ErrorChar = (sbyte)portSettings.ErrorChar;
            dcb.EvtChar = (sbyte)portSettings.EVTChar;
            dcb.fAbortOnError = portSettings.AbortOnError;
            dcb.fBinary = true;
            dcb.fDsrSensitivity = portSettings.DSRSensitive;
            dcb.fDtrControl = (DCB.DtrControlFlags)portSettings.DTRControl;
            dcb.fErrorChar = portSettings.ReplaceErrorChar;
            dcb.fInX = portSettings.InX;
            dcb.fNull = portSettings.DiscardNulls;
            dcb.fOutX = portSettings.OutX;
            dcb.fOutxCtsFlow = portSettings.OutCTS;
            dcb.fOutxDsrFlow = portSettings.OutDSR;
            dcb.fParity = (portSettings.BasicSettings.Parity == Parity.none) ? false : true;
            dcb.fRtsControl = (DCB.RtsControlFlags)portSettings.RTSControl;
            dcb.fTXContinueOnXoff = portSettings.TxContinueOnXOff;
            dcb.Parity = (byte)portSettings.BasicSettings.Parity;
            dcb.StopBits = (byte)portSettings.BasicSettings.StopBits;
            dcb.XoffChar = (sbyte)portSettings.XoffChar;
            dcb.XonChar = (sbyte)portSettings.XonChar;

            dcb.XonLim = dcb.XoffLim = (ushort)(rxBufferSize / 10);

            if (!CommAPI.SetCommState(hPort, dcb))
            {
                APIErrors e = CommAPI.GetLastError();
                isOpen = false;
                CommAPI.CloseHandle(hPort);
                throw new SerialPortException("SetCommState failed: " + e);
            }

            // clear break, DTR, and RTS values
            brk = 0;
            dtr = dcb.fDtrControl == DCB.DtrControlFlags.Enable ? 1 : 0;
            rts = dcb.fRtsControl == DCB.RtsControlFlags.Enable ? 1 : 0;

            if (!CommAPI.SetCommTimeouts(hPort, slowCommTimeouts))
            {
                APIErrors e = CommAPI.GetLastError();
                isOpen = false;
                CommAPI.CloseHandle(hPort);
                throw new SerialPortException("SetCommTimeouts failed: " + e);
            }

            if (CommAPI.IsWin32 && AllocUnmanagedMemoryBuffers)
            {
                ptrOvWriteEvent = CommAPI.AllocMem((uint)Marshal.SizeOf(ovWriteEvent));
                Marshal.StructureToPtr(ovWriteEvent, ptrOvWriteEvent, true);
                ptrWriteDataBuffer = CommAPI.AllocMem(txBufferSize);

                ptrOvReadEvent = CommAPI.AllocMem((uint)Marshal.SizeOf(ovReadEvent));
                Marshal.StructureToPtr(ovReadEvent, ptrOvReadEvent, true);
                ptrReadDataBuffer = CommAPI.AllocMem(rxBufferSize);
            }

            isOpen = true;
            return true;
        }

        public override void Close()
        {
            if (!isOpen) return;

            Debug.WriteLine("DEBUG: SerialPort.Close Called");
            isOpen = false;
            CommAPI.SetCommMask(hPort, 0);
            CommAPI.EscapeCommFunction(hPort, CommEscapes.CLRDTR);
            CommAPI.EscapeCommFunction(hPort, CommEscapes.CLRRTS);
            if (CommAPI.CloseHandle(hPort))
            {
                hPort = (IntPtr)CommAPI.INVALID_HANDLE_VALUE;

                if (ptrOvWriteEvent != IntPtr.Zero)
                    CommAPI.FreeMem(ptrOvWriteEvent);
                if (ptrWriteDataBuffer != IntPtr.Zero)
                    CommAPI.FreeMem(ptrWriteDataBuffer);

                if (ptrOvReadEvent != IntPtr.Zero)
                    CommAPI.FreeMem(ptrOvReadEvent);
                if (ptrReadDataBuffer != IntPtr.Zero)
                    CommAPI.FreeMem(ptrReadDataBuffer);
            }
        }

        public override void Flush()
        {
            if (!isOpen) return;

            Debug.WriteLine("DEBUG: SerialPort.Flush Called");
            if (!CommAPI.FlushFileBuffers(hPort))
            {
                throw new SerialPortException("FlushFileBuffers failed: " + CommAPI.GetLastError());
            }
            if (!CommAPI.PurgeComm(hPort,
               CommPurge.RXABORT | CommPurge.TXABORT |
               CommPurge.RXCLEAR | CommPurge.TXCLEAR))
            {
                throw new SerialPortException("PurgeComm failed: " + CommAPI.GetLastError());
            }
            //while(ReadImmediate()!=-1)
            //   ;// empty
        }

        public void Write(int data)
        {
            Debug.WriteLine("DEBUG: SerialPort.Write(int) called");
            byte[] tmpBuf = new byte[1];
            tmpBuf[0] = (byte)data;
            Write(tmpBuf, 1);
        }

        public void Write(byte[] data)
        {
            Debug.WriteLine("DEBUG: SerialPort.Write(byte[]) called");
            Write(data, data.Length);

        }

        public override void Write(byte[] data, int offset, int length)
        {
            Debug.WriteLine("DEBUG: SerialPort.Write(byte[], int, int) called");
            if (offset != 0)
            {
                byte[] tmpBuf = new byte[data.Length];
                Array.Copy(data, offset, tmpBuf, 0, length);
                Write(tmpBuf, length);
            }
            else
            {
                Write(data, length);
            }
        }

        public void Write(byte[] data, int length)
        {
            if (!isOpen)
                throw new SerialPortException("Port Not Open");

            if (length > txBufferSize)
                throw new SerialPortException("Write larger than transmit buffer");

            Debug.WriteLine("DEBUG: SerialPort.Write(byte[], int) called");
            Debug.WriteLineHex("DEBUG: SerialPort.Write, Data = ", data, 0, length);

            bool writeStatus = false;
            int bytesWritten = 0;
            try
            {
                if (ptrWriteDataBuffer != IntPtr.Zero)
                {
                    Marshal.Copy(data, 0, ptrWriteDataBuffer, length);
                    writeStatus = CommAPI.WriteFile(hPort, ptrWriteDataBuffer,
                       (uint)length, out bytesWritten, ptrOvWriteEvent);
                }
                else
                {
                    writeStatus = CommAPI.WriteFile(hPort, data, (uint)length,
                       out bytesWritten, ref ovWriteEvent);
                }
            }
            catch (Exception e)
            {
                throw new SerialPortException(e.Message);
            }
            return;//bytesWritten;
        }

        /// <summary>
        /// Reads whatever is sitting in the input buffer.
        /// </summary>
        /// <returns>
        /// returns a byte of data from serial input 
        /// buffer or -1 if no bytes available
        /// </returns>
        /*public int ReadImmediate()
        {
           if(!isOpen)
              throw new SerialPortException("Port Not Open");

           Debug.WriteLine("DEBUG: SerialPort.ReadImmediate() called");
           int bytesRead = -1;
           bool readStatus = false;
           byte[] buff = new byte[1];
           try
           {
              CommAPI.SetCommTimeouts(hPort, fastCommTimeouts);
              readEvent.Reset();
              readStatus = CommAPI.ReadFile(hPort, buff, (uint)1,
                 out bytesRead, ref ovReadEvent);

              // check for an error
              if (!readStatus)
              {
                 APIErrors ler = CommAPI.GetLastError();
                 // check if IO Pending
                 if(ler == APIErrors.ERROR_IO_PENDING )
                 {
                    bool wait = readEvent.WaitOne();

                    // verify all is read correctly
                    //readStatus = CommAPI.GetOverlappedResult(
                    //   hPort, ref ovReadEvent, out bytesRead, false);
                    readStatus = CommAPI.ReadFile(hPort, buff, (uint)1,
                       out bytesRead, ref ovReadEvent);
                    return bytesRead;
                 }
              }
              CommAPI.SetCommTimeouts(hPort, slowCommTimeouts);
           }
           catch(Exception e)
           {
              throw new SerialPortException(e.Message);
           }

           return -1;
        }*/

        public byte[] Read(int numBytes)
        {
            Debug.WriteLine("DEBUG: SerialPort.Read(int) called");
            byte[] buf = new byte[numBytes];
            int cnt = Read(buf, numBytes);
            if (cnt != numBytes)
                throw new SerialPortException("Read too few bytes. expected=" + numBytes + ", got=" + cnt);
            return buf;
        }

        public int Read(byte[] data)
        {
            Debug.WriteLine("DEBUG: SerialPort.Read(byte[]) called");
            return Read(data, data.Length);
        }


        public override int Read(byte[] data, int offset, int length)
        {
            Debug.WriteLine("DEBUG: SerialPort.Read(byte[], int, int) called");
            if (offset != 0)
            {
                byte[] tmpBuf = new byte[length];
                Array.Copy(data, offset, tmpBuf, 0, length);
                return Read(tmpBuf, length);
            }
            else
            {
                return Read(data, length);
            }
        }

        public int Read(byte[] data, int length)
        {
            if (!isOpen)
                throw new SerialPortException("Port Not Open");

            if (length > rxBufferSize)
                throw new SerialPortException("Read larger than receive buffer");

            Debug.WriteLine("DEBUG: SerialPort.Read(byte[], int) called");

            bool readStatus = false;
            int bytesRead = 0;
            try
            {
                readEvent.Reset();
                if (ptrReadDataBuffer != IntPtr.Zero)
                {
                    readStatus = CommAPI.ReadFile(hPort, ptrReadDataBuffer, (uint)length,
                       out bytesRead, ptrOvReadEvent);
                }
                else
                {
                    readStatus = CommAPI.ReadFile(hPort, data, (uint)length,
                       out bytesRead, ref ovReadEvent);
                }

                // check for an error
                if (!readStatus)
                {
                    APIErrors ler = CommAPI.GetLastError();
                    // check if IO Pending
                    if (ler == APIErrors.ERROR_IO_PENDING)
                    {
                        bool wait = readEvent.WaitOne();

                        // verify all is read correctly
                        if (ptrOvReadEvent != IntPtr.Zero)
                        {
                            readStatus = CommAPI.GetOverlappedResult(
                               hPort, ptrOvReadEvent, out bytesRead, false);
                        }
                        else
                        {
                            readStatus = CommAPI.GetOverlappedResult(
                               hPort, ref ovReadEvent, out bytesRead, false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new SerialPortException(e.Message);
            }
            if (ptrReadDataBuffer != IntPtr.Zero)
            {
                if (readStatus && bytesRead > 0)
                    Marshal.Copy(ptrReadDataBuffer, data, 0, bytesRead);
            }
            Debug.Write("DEBUG: SerialPort.Read, bytesRead = ");
            Debug.WriteLine(bytesRead);
            Debug.WriteLineHex("DEBUG: SerialPort.Read, Data = ", data, 0, bytesRead);
            return bytesRead;
        }

        public void SendBreak(int milliseconds)
        {
            this.Break = true;
            Thread.Sleep(milliseconds);
            this.Break = false;
        }
        #endregion

        #region properties
        public string PortName
        {
            get
            {
                return portName;
            }
            set
            {
                portName = value;
            }
        }

        public bool IsOpen
        {
            get
            {
                return isOpen;
            }
        }

        public bool Break
        {
            get
            {
                if (!isOpen) return false;

                return (brk == 1);
            }
            set
            {
                if (!isOpen) return;
                if (brk < 0) return;
                if (hPort == (IntPtr)CommAPI.INVALID_HANDLE_VALUE) return;

                if (value)
                {
                    if (CommAPI.EscapeCommFunction(hPort, CommEscapes.SETBREAK))
                        brk = 1;
                    else
                        throw new SerialPortException("Failed to set break!");
                }
                else
                {
                    if (CommAPI.EscapeCommFunction(hPort, CommEscapes.CLRBREAK))
                        brk = 0;
                    else
                        throw new SerialPortException("Failed to clear break!");
                }
            }
        }

        public BaudRates BaudRate
        {
            set
            {
                dcb.BaudRate = (UInt32)value;
                portSettings.BasicSettings.BaudRate = value;
                if (isOpen)
                {
                    CommAPI.SetCommState(hPort, dcb);
                }
            }
            get
            {
                return portSettings.BasicSettings.BaudRate;
                //CommAPI.GetCommState(hPort, dcb);
                //return (BaudRates)dcb.BaudRate;
            }
        }

        public bool DTRAvailable
        {
            get
            {
                return dtravail;
            }
        }

        public bool DTREnable
        {
            get
            {
                return (dtr == 1);
            }
            set
            {
                if (dtr < 0) return;
                if (hPort == (IntPtr)CommAPI.INVALID_HANDLE_VALUE) return;

                if (value)
                {
                    if (CommAPI.EscapeCommFunction(hPort, CommEscapes.SETDTR))
                        dtr = 1;
                    else
                        throw new SerialPortException("Failed to set DTR!");
                }
                else
                {
                    if (CommAPI.EscapeCommFunction(hPort, CommEscapes.CLRDTR))
                        dtr = 0;
                    else
                        throw new SerialPortException("Failed to clear DTR!");
                }
            }
        }

        public bool RTSAvailable
        {
            get
            {
                return rtsavail;
            }
        }

        public bool RTSEnable
        {
            get
            {
                return (rts == 1);
            }
            set
            {
                if (rts < 0) return;
                if (hPort == (IntPtr)CommAPI.INVALID_HANDLE_VALUE) return;

                if (value)
                {
                    if (CommAPI.EscapeCommFunction(hPort, CommEscapes.SETRTS))
                        rts = 1;
                    else
                        throw new SerialPortException("Failed to set RTS!");
                }
                else
                {
                    if (CommAPI.EscapeCommFunction(hPort, CommEscapes.CLRRTS))
                        rts = 0;
                    else
                        throw new SerialPortException("Failed to clear RTS!");
                }
            }
        }

        public DetailedPortSettings DetailedSettings
        {
            get
            {
                return portSettings;
            }
            set
            {
                portSettings = value;
            }
        }

        public BasicPortSettings Settings
        {
            get
            {
                return portSettings.BasicSettings;
            }
            set
            {
                portSettings.BasicSettings = value;
            }
        }
        #endregion

        #region Stream Properties
        public override bool CanRead
        {
            get { return true; }
        }
        public override bool CanWrite
        {
            get { return true; }
        }
        public override bool CanSeek
        {
            get { return false; }
        }
        public override long Position
        {
            get { throw new System.NotSupportedException("Does not support Position property"); }
            set { throw new System.NotSupportedException("Does not support Position property"); }
        }
        public override long Length
        {
            get { throw new System.NotSupportedException("Does not support Length property"); }
        }
        public override void SetLength(long len)
        {
            throw new System.NotSupportedException("Does not support Length property");
        }
        public override System.Int64 Seek(long l, System.IO.SeekOrigin so)
        {
            throw new System.NotSupportedException("Does not support Seek");
        }
        #endregion
    }


    #region enums
    public enum ASCII : byte
    {
        NULL = 0x00, SOH = 0x01, STH = 0x02, ETX = 0x03, EOT = 0x04, ENQ = 0x05,
        ACK = 0x06, BELL = 0x07, BS = 0x08, HT = 0x09, LF = 0x0A, VT = 0x0B,
        FF = 0x0C, CR = 0x0D, SO = 0x0E, SI = 0x0F, DC1 = 0x11, DC2 = 0x12,
        DC3 = 0x13, DC4 = 0x14, NAK = 0x15, SYN = 0x16, ETB = 0x17, CAN = 0x18,
        EM = 0x19, SUB = 0x1A, ESC = 0x1B, FS = 0x1C, GS = 0x1D, RS = 0x1E,
        US = 0x1F, SP = 0x20, DEL = 0x7F
    }

    public enum Handshake
    {
        none,
        XonXoff,
        CtsRts,
        DsrDtr
    }

    public enum Parity
    {
        none = 0,
        odd = 1,
        even = 2,
        mark = 3,
        space = 4
    };

    public enum StopBits
    {
        one = 0,
        onePointFive = 1,
        two = 2
    };

    public enum DTRControlFlows
    {
        disable = 0x00,
        enable = 0x01,
        handshake = 0x02
    }

    public enum RTSControlFlows
    {
        disable = 0x00,
        enable = 0x01,
        handshake = 0x02,
        toggle = 0x03
    }

    public enum BaudRates : uint
    {
        CBR_110 = 110,
        CBR_300 = 300,
        CBR_600 = 600,
        CBR_1200 = 1200,
        CBR_2400 = 2400,
        CBR_4800 = 4800,
        CBR_9600 = 9600,
        CBR_14400 = 14400,
        CBR_19200 = 19200,
        CBR_38400 = 38400,
        CBR_56000 = 56000,
        CBR_57600 = 57600,
        CBR_115200 = 115200,
        CBR_128000 = 128000,
        CBR_256000 = 256000
    }
    #endregion

    #region Serial Port Settings
    [StructLayout(LayoutKind.Sequential)]
    public class BasicPortSettings
    {
        public BaudRates BaudRate = BaudRates.CBR_19200;
        public byte ByteSize = 8;
        public Parity Parity = Parity.none;
        public StopBits StopBits = StopBits.one;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class DetailedPortSettings
    {
        public DetailedPortSettings()
        {
            BasicSettings = new BasicPortSettings();
            Init();
        }

        // These are the default port settings
        // override Init() to create new defaults (i.e. common handshaking)
        protected virtual void Init()
        {
            BasicSettings.BaudRate = BaudRates.CBR_19200;
            BasicSettings.ByteSize = 8;
            BasicSettings.Parity = Parity.none;
            BasicSettings.StopBits = StopBits.one;

            OutCTS = false;
            OutDSR = false;
            DTRControl = DTRControlFlows.disable;
            DSRSensitive = false;
            TxContinueOnXOff = true;
            OutX = false;
            InX = false;
            ReplaceErrorChar = false;
            RTSControl = RTSControlFlows.disable;
            DiscardNulls = false;
            AbortOnError = false;
            XonChar = (char)ASCII.DC1;
            XoffChar = (char)ASCII.DC3;
            ErrorChar = (char)ASCII.NAK;
            EOFChar = (char)ASCII.EOT;
            EVTChar = (char)ASCII.NULL;
        }

        public BasicPortSettings BasicSettings;
        public bool OutCTS = false;
        public bool OutDSR = false;
        public DTRControlFlows DTRControl = DTRControlFlows.disable;
        public bool DSRSensitive = false;
        public bool TxContinueOnXOff = true;
        public bool OutX = false;
        public bool InX = false;
        public bool ReplaceErrorChar = false;
        public RTSControlFlows RTSControl = RTSControlFlows.disable;
        public bool DiscardNulls = false;
        public bool AbortOnError = false;
        public char XonChar = (char)ASCII.DC1;
        public char XoffChar = (char)ASCII.DC3;
        public char ErrorChar = (char)ASCII.NAK;
        public char EOFChar = (char)ASCII.EOT;
        public char EVTChar = (char)ASCII.NULL;
    }

    public class HandshakeNone : DetailedPortSettings
    {
        protected override void Init()
        {
            base.Init();

            OutCTS = false;
            OutDSR = false;
            OutX = false;
            InX = false;
            RTSControl = RTSControlFlows.enable;
            DTRControl = DTRControlFlows.enable;
            TxContinueOnXOff = true;
            DSRSensitive = false;
        }
    }

    public class HandshakeXonXoff : DetailedPortSettings
    {
        protected override void Init()
        {
            base.Init();

            OutCTS = false;
            OutDSR = false;
            OutX = true;
            InX = true;
            RTSControl = RTSControlFlows.enable;
            DTRControl = DTRControlFlows.enable;
            TxContinueOnXOff = true;
            DSRSensitive = false;
            XonChar = (char)ASCII.DC1;
            XoffChar = (char)ASCII.DC3;
        }
    }

    public class HandshakeCtsRts : DetailedPortSettings
    {
        protected override void Init()
        {
            base.Init();

            OutCTS = true;
            OutDSR = false;
            OutX = false;
            InX = false;
            RTSControl = RTSControlFlows.handshake;
            DTRControl = DTRControlFlows.enable;
            TxContinueOnXOff = true;
            DSRSensitive = false;
        }
    }

    public class HandshakeDsrDtr : DetailedPortSettings
    {
        protected override void Init()
        {
            base.Init();

            OutCTS = false;
            OutDSR = true;
            OutX = false;
            InX = false;
            RTSControl = RTSControlFlows.enable;
            DTRControl = DTRControlFlows.handshake;
            TxContinueOnXOff = true;
            DSRSensitive = false;
        }
    }
    #endregion
}
