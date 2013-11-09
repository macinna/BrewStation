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
 * Note that the main meat of this source was taken right from an MSDN article
 * on Serial I/O with .NET Compact Framework, which went on to become a serial
 * I/O package in the OpenNETCF.org distribution.  Significant portions of 
 * this file (which contains most of the win32 api calls) and the accompanying
 * file (SerialPort.cs, which contains all of the high level functions) were 
 * modified by Dallas Semiconductor.  Note that due to the significant 
 * functionality changes, the namespace was changed to prevent confusion with 
 * the original codebase.
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
using System.Text;

namespace DalSemi.Serial
{
    #region API structs and enums
    [Flags]
    internal enum CommEventFlags : uint
    {
        NONE = 0x0000, //
        RXCHAR = 0x0001, // Any Character received
        RXFLAG = 0x0002, // Received specified flag character
        TXEMPTY = 0x0004, // Tx buffer Empty
        CTS = 0x0008, // CTS changed
        DSR = 0x0010, // DSR changed
        RLSD = 0x0020, // RLSD changed
        BREAK = 0x0040, // BREAK received
        ERR = 0x0080, // Line status error
        RING = 0x0100, // ring detected
        PERR = 0x0200, // printer error
        RX80FULL = 0x0400, // rx buffer is at 80%
        EVENT1 = 0x0800, // provider event
        EVENT2 = 0x1000, // provider event
        POWER = 0x2000, // wince power notification
        ALL = 0x3FFF  // mask of all flags 
    }

    internal enum EventFlags
    {
        EVENT_PULSE = 1,
        EVENT_RESET = 2,
        EVENT_SET = 3
    }

    [Flags]
    internal enum CommErrorFlags : uint
    {
        RXOVER = 0x0001,
        OVERRUN = 0x0002,
        RXPARITY = 0x0004,
        FRAME = 0x0008,
        BREAK = 0x0010,
        TXFULL = 0x0100,
        PTO = 0x0200,
        IOE = 0x0400,
        DNS = 0x0800,
        OOP = 0x1000,
        MODE = 0x8000
    }

    [Flags]
    internal enum CommModemStatusFlags : uint
    {
        MS_CTS_ON = 0x0010,	// The CTS (Clear To Send) signal is on. 
        MS_DSR_ON = 0x0020,	// The DSR (Data Set Ready) signal is on. 
        MS_RING_ON = 0x0040,	// The ring indicator signal is on. 
        MS_RLSD_ON = 0x0080	// The RLSD (Receive Line Signal Detect) signal is on. 
    }

    internal enum CommEscapes : uint
    {
        SETXOFF = 1,
        SETXON = 2,
        SETRTS = 3,
        CLRRTS = 4,
        SETDTR = 5,
        CLRDTR = 6,
        RESETDEV = 7,
        SETBREAK = 8,
        CLRBREAK = 9
    }

    internal enum CommPurge : uint
    {
        TXABORT = 0x01,
        RXABORT = 0x02,
        TXCLEAR = 0x04,
        RXCLEAR = 0x08,
    }

    internal enum APIErrors : int
    {
        ERROR_INVALID_FUNCTION = 1,
        ERROR_FILE_NOT_FOUND = 2,
        ERROR_PATH_NOT_FOUND = 3,
        ERROR_ACCESS_DENIED = 5,
        ERROR_INVALID_HANDLE = 6,
        ERROR_DEV_NOT_EXIST = 55,
        ERROR_CALL_NOT_IMPLEMENTED = 120,
        ERROR_INVALID_NAME = 123,
        ERROR_OPERATION_ABORTED = 995,
        ERROR_IO_INCOMPLETE = 996,
        ERROR_IO_PENDING = 997
    }

    internal enum APIConstants : uint
    {
        WAIT_OBJECT_0 = 0x00000000,
        WAIT_ABANDONED = 0x00000080,
        WAIT_ABANDONED_0 = 0x00000080,
        WAIT_FAILED = 0xffffffff,
        INFINITE = 0xffffffff
    }


    #endregion

    internal class CommAPI
    {
        internal static string GetPortName(int index)
        {
            if (IsWin32)
                return "COM" + index;
            else if (IsWinCE)
                return "COM" + index + ":";
            else
                return "/dev/ttyS" + index;
        }

        internal static string PortHeader
        {
            get
            {
                if (IsWin32 || IsWinCE)
                    return "COM";
                else
                    return "/dev/ttyS";
            }
        }
        internal static IntPtr CreateFile(string FileName)
        {
            if (IsWin32)
            {
                // overlapped default on win32
                return WinCreateFile(FileName, GENERIC_WRITE | GENERIC_READ, 0,
                   IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);
            }
            else
            {
                // overlapped not supported on CE
                return CECreateFileW(FileName, GENERIC_WRITE | GENERIC_READ, 0,
                   IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            }
        }

        internal static IntPtr CreateFile(string FileName, bool Overlapped)
        {
            if (IsWin32)
            {
                return WinCreateFile(FileName, GENERIC_WRITE | GENERIC_READ, 0,
                   IntPtr.Zero, OPEN_EXISTING,
                   (Overlapped ? FILE_FLAG_OVERLAPPED : 0), IntPtr.Zero);
            }
            else
            {
                // overlapped not supported on CE
                return CECreateFileW(FileName, GENERIC_WRITE | GENERIC_READ, 0,
                   IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            }
        }

        internal static bool WaitCommEvent(IntPtr hPort, ref CommEventFlags flags,
           IntPtr lpOverlapped)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinWaitCommEvent(hPort, ref flags, lpOverlapped));
            }
            else
            {
                // overlapped not supported on CE
                return Convert.ToBoolean(CEWaitCommEvent(hPort, ref flags, IntPtr.Zero));
            }
        }

        internal static bool WaitCommEvent(IntPtr hPort, ref CommEventFlags flags)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinWaitCommEvent(hPort, ref flags, IntPtr.Zero));
            }
            else
            {
                return Convert.ToBoolean(CEWaitCommEvent(hPort, ref flags, IntPtr.Zero));
            }
        }

        internal static bool ClearCommError(IntPtr hPort, ref CommErrorFlags flags, CommStat stat)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinClearCommError(hPort, ref flags, stat));
            }
            else
            {
                return Convert.ToBoolean(CEClearCommError(hPort, ref flags, stat));
            }
        }

        internal static bool SetCommMask(IntPtr hPort, CommEventFlags dwEvtMask)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinSetCommMask(hPort, dwEvtMask));
            }
            else
            {
                return Convert.ToBoolean(CESetCommMask(hPort, dwEvtMask));
            }
        }

        internal static bool ReadFile(IntPtr hPort, IntPtr buffer,
           uint cbToRead, out Int32 cbRead)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(
                   WinReadFile(hPort, buffer, cbToRead, out cbRead, IntPtr.Zero));
            }
            else
            {
                return Convert.ToBoolean(
                   CEReadFile(hPort, buffer, cbToRead, out cbRead, IntPtr.Zero));
            }
        }
        internal static bool ReadFile(IntPtr hPort, IntPtr buffer,
           uint cbToRead, out Int32 cbRead, IntPtr lpOverLapped)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(
                   WinReadFile(hPort, buffer, cbToRead, out cbRead, lpOverLapped));
            }
            else
            {
                // overlapped not supported on WinCE
                return Convert.ToBoolean(
                   CEReadFile(hPort, buffer, cbToRead, out cbRead, IntPtr.Zero));
            }
        }
        internal static bool ReadFile(IntPtr hPort, byte[] buffer,
           uint cbToRead, out Int32 cbRead, ref OVERLAPPED lpOverLapped)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(
                   WinReadFile(hPort, buffer, cbToRead, out cbRead, ref lpOverLapped));
            }
            else
            {
                // overlapped not supported on WinCE
                return Convert.ToBoolean(
                   CEReadFile(hPort, buffer, cbToRead, out cbRead, IntPtr.Zero));
            }
        }
        internal static bool ReadFile(IntPtr hPort, byte[] buffer,
           uint cbToRead, out Int32 cbRead, IntPtr lpOverLapped)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(
                   WinReadFile(hPort, buffer, cbToRead, out cbRead, lpOverLapped));
            }
            else
            {
                // overlapped not supported on WinCE
                return Convert.ToBoolean(
                   CEReadFile(hPort, buffer, cbToRead, out cbRead, IntPtr.Zero));
            }
        }

        internal static bool WriteFile(IntPtr hPort, IntPtr buffer,
           UInt32 cbToWrite, out Int32 cbWritten)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(
                   WinWriteFile(hPort, buffer, cbToWrite, out cbWritten, IntPtr.Zero));
            }
            else
            {
                return Convert.ToBoolean(
                   CEWriteFile(hPort, buffer, cbToWrite, out cbWritten, IntPtr.Zero));
            }
        }
        internal static bool WriteFile(IntPtr hPort, IntPtr buffer,
           UInt32 cbToWrite, out Int32 cbWritten, IntPtr lpOverLapped)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(
                   WinWriteFile(hPort, buffer, cbToWrite, out cbWritten, lpOverLapped));
            }
            else
            {
                // overlapped not supported on WinCE
                return Convert.ToBoolean(
                   CEWriteFile(hPort, buffer, cbToWrite, out cbWritten, IntPtr.Zero));
            }
        }
        internal static bool WriteFile(IntPtr hPort, byte[] buffer,
           UInt32 cbToWrite, out Int32 cbWritten, ref OVERLAPPED lpOverLapped)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(
                   WinWriteFile(hPort, buffer, cbToWrite, out cbWritten, ref lpOverLapped));
            }
            else
            {
                // overlapped not supported on WinCE
                return Convert.ToBoolean(
                   CEWriteFile(hPort, buffer, cbToWrite, out cbWritten, IntPtr.Zero));
            }
        }
        internal static bool WriteFile(IntPtr hPort, byte[] buffer,
           UInt32 cbToWrite, out Int32 cbWritten, IntPtr lpOverLapped)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(
                   WinWriteFile(hPort, buffer, cbToWrite, out cbWritten, lpOverLapped));
            }
            else
            {
                // overlapped not supported on WinCE
                return Convert.ToBoolean(
                   CEWriteFile(hPort, buffer, cbToWrite, out cbWritten, IntPtr.Zero));
            }
        }

        internal static bool CloseHandle(IntPtr hPort)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinCloseHandle(hPort));
            }
            else
            {
                return Convert.ToBoolean(CECloseHandle(hPort));
            }
        }

        internal static bool SetupComm(IntPtr hPort, UInt32 dwInQueue, UInt32 dwOutQueue)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinSetupComm(hPort, dwInQueue, dwOutQueue));
            }
            else
            {
                return Convert.ToBoolean(CESetupComm(hPort, dwInQueue, dwOutQueue));
            }
        }

        internal static bool SetCommState(IntPtr hPort, DCB dcb)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinSetCommState(hPort, dcb));
            }
            else
            {
                return Convert.ToBoolean(CESetCommState(hPort, dcb));
            }
        }

        internal static bool GetCommState(IntPtr hPort, DCB dcb)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinGetCommState(hPort, dcb));
            }
            else
            {
                return Convert.ToBoolean(CEGetCommState(hPort, dcb));
            }
        }

        internal static bool SetCommTimeouts(IntPtr hPort, CommTimeouts timeouts)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinSetCommTimeouts(hPort, timeouts));
            }
            else
            {
                return Convert.ToBoolean(CESetCommTimeouts(hPort, timeouts));
            }
        }

        internal static bool EscapeCommFunction(IntPtr hPort, CommEscapes escape)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinEscapeCommFunction(hPort, (uint)escape));
            }
            else
            {
                return Convert.ToBoolean(CEEscapeCommFunction(hPort, (uint)escape));
            }
        }

        internal static bool PurgeComm(IntPtr hPort, CommPurge purge)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinPurgeComm(hPort, (uint)purge));
            }
            else
            {
                return Convert.ToBoolean(CEPurgeComm(hPort, (uint)purge));
                //            return true;
            }
        }

        internal static bool FlushFileBuffers(IntPtr hPort)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinFlushFileBuffers(hPort));
            }
            else
            {
                //return Convert.ToBoolean(CEFlushFileBuffers(hPort));
                return true;
            }
        }

        internal static IntPtr CreateEvent(bool bManualReset, bool bInitialState, string lpName)
        {
            if (IsWin32)
            {
                return WinCreateEvent(IntPtr.Zero, Convert.ToInt32(bManualReset), Convert.ToInt32(bInitialState), lpName);
            }
            else
            {
                return CECreateEvent(IntPtr.Zero, Convert.ToInt32(bManualReset), Convert.ToInt32(bInitialState), lpName);
            }
        }

        internal static bool SetEvent(IntPtr hEvent)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinSetEvent(hEvent));
            }
            else
            {
                return Convert.ToBoolean(CEEventModify(hEvent, (uint)EventFlags.EVENT_SET));
            }
        }

        internal static bool ResetEvent(IntPtr hEvent)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinResetEvent(hEvent));
            }
            else
            {
                return Convert.ToBoolean(CEEventModify(hEvent, (uint)EventFlags.EVENT_RESET));
            }
        }

        internal static bool PulseEvent(IntPtr hEvent)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(WinPulseEvent(hEvent));
            }
            else
            {
                return Convert.ToBoolean(CEEventModify(hEvent, (uint)EventFlags.EVENT_PULSE));
            }
        }

        internal static int WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds)
        {
            if (IsWin32)
            {
                return WinWaitForSingleObject(hHandle, dwMilliseconds);
            }
            else
            {
                return CEWaitForSingleObject(hHandle, dwMilliseconds);
            }
        }

        internal static bool GetOverlappedResult(
           IntPtr hHandle, IntPtr lpOverlapped,
           out int lpNumberOfBytesTransferred, bool bWait)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(
                   WinGetOverlappedResult(hHandle, lpOverlapped,
                   out lpNumberOfBytesTransferred, (uint)(bWait ? 1 : 0)));
            }
            else
            {
                return Convert.ToBoolean(
                   CEGetOverlappedResult(hHandle, lpOverlapped,
                   out lpNumberOfBytesTransferred, (uint)(bWait ? 1 : 0)));
            }
        }
        internal static bool GetOverlappedResult(
           IntPtr hHandle, ref OVERLAPPED lpOverlapped,
           out int lpNumberOfBytesTransferred, bool bWait)
        {
            if (IsWin32)
            {
                return Convert.ToBoolean(
                   WinGetOverlappedResult(hHandle, ref lpOverlapped,
                   out lpNumberOfBytesTransferred, (uint)(bWait ? 1 : 0)));
            }
            else
            {
                return Convert.ToBoolean(
                   CEGetOverlappedResult(hHandle, ref lpOverlapped,
                   out lpNumberOfBytesTransferred, (uint)(bWait ? 1 : 0)));
            }
        }

        internal static APIErrors GetLastError()
        {
            return (APIErrors)Marshal.GetLastWin32Error();
        }


        #region Helper Properties
        internal static bool IsWin32
        {
            get
            {
                return (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                   || (Environment.OSVersion.Platform == PlatformID.Win32NT)
                   || (Environment.OSVersion.Platform == PlatformID.Win32S);
            }
        }
        internal static bool IsWinCE
        {
            get
            {
                return (Environment.OSVersion.Platform == PlatformID.WinCE);
            }
        }
        internal static bool IsLinux
        {
            get
            {
                return !IsWinCE && !IsWin32; // TODO: Figure out how to determine if platform is linux
            }
        }
        #endregion

        #region API Constants
        internal const Int32 INVALID_HANDLE_VALUE = -1;
        internal const UInt32 OPEN_EXISTING = 3;
        internal const UInt32 GENERIC_READ = 0x80000000;
        internal const UInt32 GENERIC_WRITE = 0x40000000;
        internal const UInt32 FILE_FLAG_OVERLAPPED = 0x40000000;
        #endregion

        #region Windows CE API imports
        [DllImport("coredll.dll", EntryPoint = "WaitForSingleObject", SetLastError = true)]
        private static extern int CEWaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("coredll.dll", EntryPoint = "EventModify", SetLastError = true)]
        private static extern int CEEventModify(IntPtr hEvent, uint function);

        [DllImport("coredll.dll", EntryPoint = "CreateEvent", SetLastError = true)]
        private static extern IntPtr CECreateEvent(IntPtr lpEventAttributes, int bManualReset, int bInitialState, string lpName);

        [DllImport("coredll.dll", EntryPoint = "EscapeCommFunction", SetLastError = true)]
        private static extern int CEEscapeCommFunction(IntPtr hFile, UInt32 dwFunc);

        [DllImport("coredll.dll", EntryPoint = "SetCommTimeouts", SetLastError = true)]
        private static extern int CESetCommTimeouts(IntPtr hFile, CommTimeouts timeouts);

        [DllImport("coredll.dll", EntryPoint = "GetCommState", SetLastError = true)]
        private static extern int CEGetCommState(IntPtr hFile, DCB dcb);

        [DllImport("coredll.dll", EntryPoint = "SetCommState", SetLastError = true)]
        private static extern int CESetCommState(IntPtr hFile, DCB dcb);

        [DllImport("coredll.dll", EntryPoint = "SetupComm", SetLastError = true)]
        private static extern int CESetupComm(IntPtr hFile, UInt32 dwInQueue, UInt32 dwOutQueue);

        [DllImport("coredll.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        private static extern int CECloseHandle(IntPtr hObject);

        [DllImport("coredll.dll", EntryPoint = "WriteFile", SetLastError = true)]
        extern private static int CEWriteFile(IntPtr hFile, IntPtr lpBuffer,
           UInt32 nNumberOfBytesToRead, out Int32 lpNumberOfBytesWritten, IntPtr lpOverlapped);
        [DllImport("coredll.dll", EntryPoint = "WriteFile", SetLastError = true)]
        extern private static int CEWriteFile(IntPtr hFile, byte[] lpBuffer,
           UInt32 nNumberOfBytesToRead, out Int32 lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("coredll.dll", EntryPoint = "ReadFile", SetLastError = true)]
        private static extern int CEReadFile(IntPtr hFile, IntPtr lpBuffer,
           UInt32 nNumberOfBytesToRead, out Int32 lpNumberOfBytesRead, IntPtr lpOverlapped);
        [DllImport("coredll.dll", EntryPoint = "ReadFile", SetLastError = true)]
        private static extern int CEReadFile(IntPtr hFile, byte[] lpBuffer,
           UInt32 nNumberOfBytesToRead, out Int32 lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("coredll.dll", EntryPoint = "SetCommMask", SetLastError = true)]
        private static extern int CESetCommMask(IntPtr handle, CommEventFlags dwEvtMask);

        [DllImport("coredll.dll", EntryPoint = "GetCommModemStatus", SetLastError = true)]
        extern private static int CEGetCommModemStatus(IntPtr hFile, ref uint lpModemStat);

        [DllImport("coredll.dll", EntryPoint = "ClearCommError", SetLastError = true)]
        extern private static int CEClearCommError(IntPtr hFile, ref CommErrorFlags lpErrors, CommStat lpStat);

        [DllImport("coredll.dll", EntryPoint = "WaitCommEvent", SetLastError = true)]
        private static extern int CEWaitCommEvent(IntPtr hFile, ref CommEventFlags lpEvtMask, IntPtr lpOverlapped);

        [DllImport("coredll.dll", EntryPoint = "CreateFileW", SetLastError = true)]
        private static extern IntPtr CECreateFileW(
           String lpFileName, UInt32 dwDesiredAccess, UInt32 dwShareMode,
           IntPtr lpSecurityAttributes, UInt32 dwCreationDisposition, UInt32 dwFlagsAndAttributes,
           IntPtr hTemplateFile);

        [DllImport("coredll.dll", EntryPoint = "PurgeComm", SetLastError = true)]
        private static extern int CEPurgeComm(IntPtr hFile, UInt32 dwFlags);

        [DllImport("coredll.dll", EntryPoint = "FlushFileBuffers", SetLastError = true)]
        private static extern bool CEFlushFileBuffers(IntPtr hFile);

        [DllImport("coredll.dll", EntryPoint = "GetOverlappedResult", SetLastError = true)]
        private static extern int CEGetOverlappedResult(IntPtr hFile,
           IntPtr lpOverlapped, out Int32 lpNumberOfBytesTransferred, uint bWait);
        [DllImport("coredll.dll", EntryPoint = "GetOverlappedResult", SetLastError = true)]
        private static extern int CEGetOverlappedResult(IntPtr hFile,
           ref OVERLAPPED lpOverlapped, out Int32 lpNumberOfBytesTransferred, uint bWait);
        #endregion

        #region Desktop Windows API imports

        [DllImport("kernel32.dll", EntryPoint = "WaitForSingleObject", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinWaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", EntryPoint = "ResetEvent", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinResetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", EntryPoint = "SetEvent", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinSetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", EntryPoint = "PulseEvent", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinPulseEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", EntryPoint = "CreateEvent", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr WinCreateEvent(IntPtr lpEventAttributes, int bManualReset, int bInitialState, string lpName);

        [DllImport("kernel32.dll", EntryPoint = "EscapeCommFunction", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinEscapeCommFunction(IntPtr hFile, UInt32 dwFunc);

        [DllImport("kernel32.dll", EntryPoint = "SetCommTimeouts", SetLastError = true)]
        private static extern int WinSetCommTimeouts(IntPtr hFile, CommTimeouts timeouts);

        [DllImport("kernel32.dll", EntryPoint = "GetCommState", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinGetCommState(IntPtr hFile, DCB dcb);

        [DllImport("kernel32.dll", EntryPoint = "SetCommState", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinSetCommState(IntPtr hFile, DCB dcb);

        [DllImport("kernel32.dll", EntryPoint = "SetupComm", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinSetupComm(IntPtr hFile, UInt32 dwInQueue, UInt32 dwOutQueue);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinCloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", EntryPoint = "WriteFile", SetLastError = true, CharSet = CharSet.Auto)]
        extern private static int WinWriteFile(IntPtr hFile, IntPtr lpBuffer,
           UInt32 nNumberOfBytesToRead, out Int32 lpNumberOfBytesWritten, IntPtr lpOverlapped);
        [DllImport("kernel32.dll", EntryPoint = "WriteFile", SetLastError = true, CharSet = CharSet.Auto)]
        extern private static int WinWriteFile(IntPtr hFile, byte[] lpBuffer,
           UInt32 nNumberOfBytesToRead, out Int32 lpNumberOfBytesWritten, ref OVERLAPPED lpOverlapped);
        [DllImport("kernel32.dll", EntryPoint = "WriteFile", SetLastError = true, CharSet = CharSet.Auto)]
        extern private static int WinWriteFile(IntPtr hFile, byte[] lpBuffer,
           UInt32 nNumberOfBytesToRead, out Int32 lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", EntryPoint = "ReadFile", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinReadFile(IntPtr hFile, IntPtr lpBuffer,
           UInt32 nNumberOfBytesToRead, out Int32 lpNumberOfBytesRead, IntPtr lpOverlapped);
        [DllImport("kernel32.dll", EntryPoint = "ReadFile", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinReadFile(IntPtr hFile, byte[] lpBuffer,
           UInt32 nNumberOfBytesToRead, out Int32 lpNumberOfBytesRead, ref OVERLAPPED lpOverlapped);
        [DllImport("kernel32.dll", EntryPoint = "ReadFile", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinReadFile(IntPtr hFile, byte[] lpBuffer,
           UInt32 nNumberOfBytesToRead, out Int32 lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", EntryPoint = "SetCommMask", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinSetCommMask(IntPtr handle, CommEventFlags dwEvtMask);

        [DllImport("kernel32.dll", EntryPoint = "ClearCommError", SetLastError = true, CharSet = CharSet.Auto)]
        extern private static int WinClearCommError(IntPtr hFile, ref CommErrorFlags lpErrors, CommStat lpStat);

        [DllImport("kernel32.dll", EntryPoint = "CreateFile", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr WinCreateFile(String lpFileName, UInt32 dwDesiredAccess, UInt32 dwShareMode,
           IntPtr lpSecurityAttributes, UInt32 dwCreationDisposition, UInt32 dwFlagsAndAttributes,
           IntPtr hTemplateFile);

        [DllImport("kernel32.dll", EntryPoint = "WaitCommEvent", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinWaitCommEvent(IntPtr hFile, ref CommEventFlags lpEvtMask, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", EntryPoint = "PurgeComm", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinPurgeComm(IntPtr hFile, UInt32 dwFlags);

        [DllImport("kernel32.dll", EntryPoint = "FlushFileBuffers", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WinFlushFileBuffers(IntPtr hFile);

        [DllImport("kernel32.dll", EntryPoint = "GetOverlappedResult", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinGetOverlappedResult(IntPtr hFile,
           IntPtr lpOverlapped, out Int32 lpNumberOfBytesTransferred, uint bWait);
        [DllImport("kernel32.dll", EntryPoint = "GetOverlappedResult", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int WinGetOverlappedResult(IntPtr hFile,
           ref OVERLAPPED lpOverlapped, out Int32 lpNumberOfBytesTransferred, uint bWait);

        #endregion

        #region Allocate unmanaged memory
        [DllImport("coredll.dll", EntryPoint = "LocalAlloc", SetLastError = true)]
        private static extern IntPtr CEAllocMem(Int32 wFlags, UInt32 wBytes);

        [DllImport("coredll.dll", EntryPoint = "LocalFree", SetLastError = true)]
        private static extern void CEFreeMem(IntPtr lpMem);

        [DllImport("kernel32.dll", EntryPoint = "LocalAlloc", SetLastError = true)]
        private static extern IntPtr WinAllocMem(Int32 wFlags, UInt32 wBytes);

        [DllImport("kernel32.dll", EntryPoint = "LocalFree", SetLastError = true)]
        private static extern void WinFreeMem(IntPtr lpMem);

        internal static IntPtr AllocMem(uint size)
        {
            if (IsWin32)
            {
                // 0 = allocates memory, does not initialize
                return WinAllocMem(0, size);
            }
            else
            {
                // 0 = allocates memory, does not initialize
                return CEAllocMem(0, size);
            }
        }

        internal static void FreeMem(IntPtr lpMem)
        {
            if (IsWin32)
            {
                WinFreeMem(lpMem);
            }
            else
            {
                CEFreeMem(lpMem);
            }
        }
        #endregion
    }


    /// <summary>
    /// The OVERLAPPED structure contains information used in asynchronous I/O.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct OVERLAPPED
    {
        /// <summary>
        /// Reserved for operating system use. 
        /// </summary>
        internal UIntPtr internalLow;
        /// <summary>
        /// Reserved for operating system use.
        /// </summary>
        internal UIntPtr internalHigh;
        /// <summary>
        /// Specifies a file position at which to start the transfer. 
        /// The file position is a byte offset from the start of the file. 
        /// The calling process sets this member before calling the ReadFile 
        /// or WriteFile function. This member is ignored when reading from 
        /// or writing to named pipes and communications devices and should be zero.
        /// </summary>
        internal UInt32 offset;
        /// <summary>
        /// Specifies the high word of the byte offset at which to start the transfer. 
        /// This member is ignored when reading from or writing to named pipes and 
        /// communications devices and should be zero.
        /// </summary>
        internal UInt32 offsetHigh;
        /// <summary>
        /// Handle to an event set to the signaled state when the operation has 
        /// been completed. The calling process must set this member either to 
        /// zero or a valid event handle before calling any overlapped functions. 
        /// To create an event object, use the CreateEvent function. Functions 
        /// such as WriteFile set the event to the nonsignaled state before they 
        /// begin an I/O operation.
        /// </summary>
        internal IntPtr hEvent;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class CommTimeouts
    {
        internal UInt32 ReadIntervalTimeout;
        internal UInt32 ReadTotalTimeoutMultiplier;
        internal UInt32 ReadTotalTimeoutConstant;
        internal UInt32 WriteTotalTimeoutMultiplier;
        internal UInt32 WriteTotalTimeoutConstant;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class CommStat
    {
        //
        // typedef struct _COMSTAT {
        //     DWORD fCtsHold : 1;
        //     DWORD fDsrHold : 1;
        //     DWORD fRlsdHold : 1;
        //     DWORD fXoffHold : 1;
        //     DWORD fXoffSent : 1;
        //     DWORD fEof : 1;
        //     DWORD fTxim : 1;
        //     DWORD fReserved : 25;
        //     DWORD cbInQue;
        //     DWORD cbOutQue;
        // } COMSTAT, *LPCOMSTAT;
        //

        //
        // Since the structure contains a bit-field, use a UInt32 to contain
        // the bit field and then use properties to expose the individual
        // bits as a bool.
        //
        private UInt32 bitfield;
        internal UInt32 cbInQue = 0;
        internal UInt32 cbOutQue = 0;

        // Helper constants for manipulating the bit fields.
        private readonly UInt32 fCtsHoldMask = 0x00000001;
        private readonly Int32 fCtsHoldShift = 0;
        private readonly UInt32 fDsrHoldMask = 0x00000002;
        private readonly Int32 fDsrHoldShift = 1;
        private readonly UInt32 fRlsdHoldMask = 0x00000004;
        private readonly Int32 fRlsdHoldShift = 2;
        private readonly UInt32 fXoffHoldMask = 0x00000008;
        private readonly Int32 fXoffHoldShift = 3;
        private readonly UInt32 fXoffSentMask = 0x00000010;
        private readonly Int32 fXoffSentShift = 4;
        private readonly UInt32 fEofMask = 0x00000020;
        private readonly Int32 fEofShift = 5;
        private readonly UInt32 fTximMask = 0x00000040;
        private readonly Int32 fTximShift = 6;

        internal bool fCtsHold
        {
            get { return ((bitfield & fCtsHoldMask) != 0); }
            set { bitfield |= (Convert.ToUInt32(value) << fCtsHoldShift); }
        }
        internal bool fDsrHold
        {
            get { return ((bitfield & fDsrHoldMask) != 0); }
            set { bitfield |= (Convert.ToUInt32(value) << fDsrHoldShift); }
        }
        internal bool fRlsdHold
        {
            get { return ((bitfield & fRlsdHoldMask) != 0); }
            set { bitfield |= (Convert.ToUInt32(value) << fRlsdHoldShift); }
        }
        internal bool fXoffHold
        {
            get { return ((bitfield & fXoffHoldMask) != 0); }
            set { bitfield |= (Convert.ToUInt32(value) << fXoffHoldShift); }
        }
        internal bool fXoffSent
        {
            get { return ((bitfield & fXoffSentMask) != 0); }
            set { bitfield |= (Convert.ToUInt32(value) << fXoffSentShift); }
        }
        internal bool fEof
        {
            get { return ((bitfield & fEofMask) != 0); }
            set { bitfield |= (Convert.ToUInt32(value) << fEofShift); }
        }
        internal bool fTxim
        {
            get { return ((bitfield & fTximMask) != 0); }
            set { bitfield |= (Convert.ToUInt32(value) << fTximShift); }
        }
    }

    //
    // The Win32 DCB structure is implemented below in a C# class.
    //
    [StructLayout(LayoutKind.Sequential)]
    internal class DCB
    {
        //
        // Note the layout of the Win32 DCB structure in native code and that
        // it contains bitfields. I use a UInt32 to contain the bit field
        // and then use properties to expose the individual bits at bools or
        // appropriate flags (as in the case of fDtrControl and fRtsControl).
        // 

        //
        // typedef struct _DCB { 
        //     DWORD DCBlength; 
        //     DWORD BaudRate; 
        //     DWORD fBinary: 1; 
        //     DWORD fParity: 1; 
        //     DWORD fOutxCtsFlow:1; 
        //     DWORD fOutxDsrFlow:1; 
        //     DWORD fDtrControl:2; 
        //          #define DTR_CONTROL_DISABLE    0x00
        //          #define DTR_CONTROL_ENABLE     0x01
        //          #define DTR_CONTROL_HANDSHAKE  0x02
        //     DWORD fDsrSensitivity:1; 
        //     DWORD fTXContinueOnXoff:1; 
        //     DWORD fOutX: 1; 
        //     DWORD fInX: 1; 
        //     DWORD fErrorChar: 1; 
        //     DWORD fNull: 1; 
        //     DWORD fRtsControl:2; 
        //          #define RTS_CONTROL_DISABLE    0x00
        //          #define RTS_CONTROL_ENABLE     0x01
        //          #define RTS_CONTROL_HANDSHAKE  0x02
        //          #define RTS_CONTROL_TOGGLE     0x03
        //     DWORD fAbortOnError:1; 
        //     DWORD fDummy2:17; 
        //     WORD wReserved; 
        //     WORD XonLim; 
        //     WORD XoffLim; 
        //     BYTE ByteSize; 
        //     BYTE Parity; 
        //     BYTE StopBits; 
        //     char XonChar; 
        //     char XoffChar; 
        //     char ErrorChar; 
        //     char EofChar; 
        //     char EvtChar; 
        //     WORD wReserved1; 
        // } DCB; 
        //

        //
        // Enumeration for fDtrControl bit field. Underlying type only needs
        // to be a byte since we only have 2-bits of information.
        //
        internal enum DtrControlFlags : byte
        {
            Disable = 0,
            Enable = 1,
            Handshake = 2
        }

        //
        // Enumeration for fRtsControl bit field. Underlying type only needs
        // to be a byte since we only have 2-bits of information.
        //
        internal enum RtsControlFlags : byte
        {
            Disable = 0,
            Enable = 1,
            Handshake = 2,
            Toggle = 3
        }

        internal DCB()
        {
            //
            // Initialize the length of the structure. Marshal.SizeOf returns
            // the size of the unmanaged object (basically the object that
            // gets marshalled).
            //
            this.DCBlength = (uint)Marshal.SizeOf(this);
        }

        private UInt32 DCBlength;
        internal UInt32 BaudRate;
        internal UInt32 Control;
        internal UInt16 wReserved;
        internal UInt16 XonLim;
        internal UInt16 XoffLim;
        internal byte ByteSize;
        internal byte Parity;
        internal byte StopBits;
        internal sbyte XonChar;
        internal sbyte XoffChar;
        internal sbyte ErrorChar;
        internal sbyte EofChar;
        internal sbyte EvtChar;
        internal UInt16 wReserved1;

        //
        // We need to have reserved fields to preserve the size of the 
        // underlying structure to match the Win32 structure when it is 
        // marshaled. Use these fields to suppress compiler warnings.
        //
        internal void _SuppressCompilerWarnings()
        {
            wReserved += 0;
            wReserved1 += 0;
        }

        // Helper constants for manipulating the bit fields.
        private readonly UInt32 fBinaryMask = 0x00000001;
        private readonly Int32 fBinaryShift = 0;
        private readonly UInt32 fParityMask = 0x00000002;
        private readonly Int32 fParityShift = 1;
        private readonly UInt32 fOutxCtsFlowMask = 0x00000004;
        private readonly Int32 fOutxCtsFlowShift = 2;
        private readonly UInt32 fOutxDsrFlowMask = 0x00000008;
        private readonly Int32 fOutxDsrFlowShift = 3;
        private readonly UInt32 fDtrControlMask = 0x00000030;
        private readonly Int32 fDtrControlShift = 4;
        private readonly UInt32 fDsrSensitivityMask = 0x00000040;
        private readonly Int32 fDsrSensitivityShift = 6;
        private readonly UInt32 fTXContinueOnXoffMask = 0x00000080;
        private readonly Int32 fTXContinueOnXoffShift = 7;
        private readonly UInt32 fOutXMask = 0x00000100;
        private readonly Int32 fOutXShift = 8;
        private readonly UInt32 fInXMask = 0x00000200;
        private readonly Int32 fInXShift = 9;
        private readonly UInt32 fErrorCharMask = 0x00000400;
        private readonly Int32 fErrorCharShift = 10;
        private readonly UInt32 fNullMask = 0x00000800;
        private readonly Int32 fNullShift = 11;
        private readonly UInt32 fRtsControlMask = 0x00003000;
        private readonly Int32 fRtsControlShift = 12;
        private readonly UInt32 fAbortOnErrorMask = 0x00004000;
        private readonly Int32 fAbortOnErrorShift = 14;

        internal bool fBinary
        {
            get { return ((Control & fBinaryMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fBinaryShift); }
        }
        internal bool fParity
        {
            get { return ((Control & fParityMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fParityShift); }
        }
        internal bool fOutxCtsFlow
        {
            get { return ((Control & fOutxCtsFlowMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fOutxCtsFlowShift); }
        }
        internal bool fOutxDsrFlow
        {
            get { return ((Control & fOutxDsrFlowMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fOutxDsrFlowShift); }
        }
        internal DtrControlFlags fDtrControl
        {
            get { return (DtrControlFlags)((Control & fDtrControlMask) >> fDtrControlShift); }
            set { Control |= (Convert.ToUInt32(value) << fDtrControlShift); }
        }
        internal bool fDsrSensitivity
        {
            get { return ((Control & fDsrSensitivityMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fDsrSensitivityShift); }
        }
        internal bool fTXContinueOnXoff
        {
            get { return ((Control & fTXContinueOnXoffMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fTXContinueOnXoffShift); }
        }
        internal bool fOutX
        {
            get { return ((Control & fOutXMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fOutXShift); }
        }
        internal bool fInX
        {
            get { return ((Control & fInXMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fInXShift); }
        }
        internal bool fErrorChar
        {
            get { return ((Control & fErrorCharMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fErrorCharShift); }
        }
        internal bool fNull
        {
            get { return ((Control & fNullMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fNullShift); }
        }
        internal RtsControlFlags fRtsControl
        {
            get { return (RtsControlFlags)((Control & fRtsControlMask) >> fRtsControlShift); }
            set { Control |= (Convert.ToUInt32(value) << fRtsControlShift); }
        }
        internal bool fAbortOnError
        {
            get { return ((Control & fAbortOnErrorMask) != 0); }
            set { Control |= (Convert.ToUInt32(value) << fAbortOnErrorShift); }
        }

        //
        // Method to dump the DCB to take a look and help debug issues.
        //
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("DCB:\r\n");
            sb.AppendFormat(null, "  BaudRate:     {0}\r\n", BaudRate);
            sb.AppendFormat(null, "  Control:      0x{0:x}\r\n", Control);
            sb.AppendFormat(null, "    fBinary:           {0}\r\n", fBinary);
            sb.AppendFormat(null, "    fParity:           {0}\r\n", fParity);
            sb.AppendFormat(null, "    fOutxCtsFlow:      {0}\r\n", fOutxCtsFlow);
            sb.AppendFormat(null, "    fOutxDsrFlow:      {0}\r\n", fOutxDsrFlow);
            sb.AppendFormat(null, "    fDtrControl:       {0}\r\n", fDtrControl);
            sb.AppendFormat(null, "    fDsrSensitivity:   {0}\r\n", fDsrSensitivity);
            sb.AppendFormat(null, "    fTXContinueOnXoff: {0}\r\n", fTXContinueOnXoff);
            sb.AppendFormat(null, "    fOutX:             {0}\r\n", fOutX);
            sb.AppendFormat(null, "    fInX:              {0}\r\n", fInX);
            sb.AppendFormat(null, "    fNull:             {0}\r\n", fNull);
            sb.AppendFormat(null, "    fRtsControl:       {0}\r\n", fRtsControl);
            sb.AppendFormat(null, "    fAbortOnError:     {0}\r\n", fAbortOnError);
            sb.AppendFormat(null, "  XonLim:       {0}\r\n", XonLim);
            sb.AppendFormat(null, "  XoffLim:      {0}\r\n", XoffLim);
            sb.AppendFormat(null, "  ByteSize:     {0}\r\n", ByteSize);
            sb.AppendFormat(null, "  Parity:       {0}\r\n", Parity);
            sb.AppendFormat(null, "  StopBits:     {0}\r\n", StopBits);
            sb.AppendFormat(null, "  XonChar:      {0}\r\n", XonChar);
            sb.AppendFormat(null, "  XoffChar:     {0}\r\n", XoffChar);
            sb.AppendFormat(null, "  ErrorChar:    {0}\r\n", ErrorChar);
            sb.AppendFormat(null, "  EofChar:      {0}\r\n", EofChar);
            sb.AppendFormat(null, "  EvtChar:      {0}\r\n", EvtChar);

            return sb.ToString();
        }
    }
}