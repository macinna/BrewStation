/*---------------------------------------------------------------------------
 * Copyright (C) 2002 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace DalSemi.OneWire.Adapter
{

    /**
     * Interface for holding all constants related to Network Adapter communications.
     * This interface is used by both NetAdapterHost and the NetAdapter.  In
     * addition, the common utility class <code>Connection</code> is defined here.
     *
     * @author SH
     * @version 1.00
     */
    public class NetAdapterConstants
    {

        /** version UID, used to detect incompatible host */
        public const int versionUID = 1;

        public const byte VERSION_UID_TERMINATOR = 0xFF;

        /** Indicates whether or not to buffer the output (probably always true!) */
        const Boolean BUFFERED_OUTPUT = true;

        /** Default port for NetAdapter TCP/IP connection */
        public const int DEFAULT_PORT = 6161;

        /** default secret for authentication with the server */
        public const string DEFAULT_SECRET = "Adapter Secret Default";

        /** Address for Multicast group for NetAdapter Datagram packets */
        public const string DEFAULT_MULTICAST_GROUP = "228.5.6.7";

        /** Default port for NetAdapter Datagram packets */
        public const int DEFAULT_MULTICAST_PORT = 6163;

        /*------------------------------------------------------------*/
        /*----- Method Return codes ----------------------------------*/
        /*------------------------------------------------------------*/
        public const byte RET_SUCCESS = (byte)0x0FF;
        public const byte RET_FAILURE = (byte)0x0F0;
        /*------------------------------------------------------------*/

        /*------------------------------------------------------------*/
        /*----- Method command bytes ---------------------------------*/
        /*------------------------------------------------------------*/
        public const byte CMD_CLOSECONNECTION = 0x08;
        public const byte CMD_PINGCONNECTION = 0x09;
        /*------------------------------------------------------------*/
        /* Raw Data methods ------------------------------------------*/
        public const byte CMD_RESET = 0x10;
        public const byte CMD_PUTBIT = 0x11;
        public const byte CMD_PUTBYTE = 0x12;
        public const byte CMD_GETBIT = 0x13;
        public const byte CMD_GETBYTE = 0x14;
        public const byte CMD_GETBLOCK = 0x15;
        public const byte CMD_DATABLOCK = 0x16;
        /*------------------------------------------------------------*/
        /* Power methods ---------------------------------------------*/
        public const byte CMD_SETPOWERDURATION = 0x17;
        public const byte CMD_STARTPOWERDELIVERY = 0x18;
        public const byte CMD_SETPROGRAMPULSEDURATION = 0x19;
        public const byte CMD_STARTPROGRAMPULSE = 0x1A;
        public const byte CMD_STARTBREAK = 0x1B;
        public const byte CMD_SETPOWERNORMAL = 0x1C;
        /*------------------------------------------------------------*/
        /* Speed methods ---------------------------------------------*/
        public const byte CMD_SETSPEED = 0x1D;
        public const byte CMD_GETSPEED = 0x1E;
        /*------------------------------------------------------------*/
        /* Network Semaphore methods ---------------------------------*/
        public const byte CMD_BEGINEXCLUSIVE = 0x1F;
        public const byte CMD_ENDEXCLUSIVE = 0x20;
        /*------------------------------------------------------------*/
        /* Searching methods -----------------------------------------*/
        public const byte CMD_FINDFIRSTDEVICE = 0x21;
        public const byte CMD_FINDNEXTDEVICE = 0x22;
        public const byte CMD_GETADDRESS = 0x23;
        public const byte CMD_SETSEARCHONLYALARMINGDEVICES = 0x24;
        public const byte CMD_SETNORESETSEARCH = 0x25;
        public const byte CMD_SETSEARCHALLDEVICES = 0x26;
        public const byte CMD_TARGETALLFAMILIES = 0x27;
        public const byte CMD_TARGETFAMILY = 0x28;
        public const byte CMD_EXCLUDEFAMILY = 0x29;
        /*------------------------------------------------------------*/
        /* feature methods -------------------------------------------*/
        public const byte CMD_CANBREAK = 0x2A;
        public const byte CMD_CANDELIVERPOWER = 0x2B;
        public const byte CMD_CANDELIVERSMARTPOWER = 0x2C;
        public const byte CMD_CANFLEX = 0x2D;
        public const byte CMD_CANHYPERDRIVE = 0x2E;
        public const byte CMD_CANOVERDRIVE = 0x2F;
        public const byte CMD_CANPROGRAM = 0x30;
        /*------------------------------------------------------------*/

        /**
         * An inner utility class for coupling Socket with I/O streams
         */
        //static class Connection
        //{
        //   /** socket to host */
        //   public java.net.Socket sock = null;
        //   /** input stream from socket */
        //   public java.io.DataInputStream input = null;
        //   /** output stream from socket */
        //   public java.io.DataOutputStream output = null;
        //}




        /** instance for an empty connection, basically it's a NULL object
         *  that's safe to synchronize on. */
        public static OWClientConnector EMPTY_CONNECTION = new OWClientConnectorEMPTY();
    }

    public abstract class OWClientConnector : IDisposable
    {

        public abstract byte ReadByte();
        public abstract string ReadUTF();
        public abstract int ReadInt();
        public abstract Boolean ReadBoolean();
        public abstract int Read(byte[] b, int off, int len);
        public abstract void ReadFully(byte[] b, int off, int len);

        public abstract void WriteByte(byte b);
        public abstract void WriteUTF(string s);
        public abstract void WriteInt(int v);
        public abstract void WriteBoolean(Boolean b);
        public abstract void Write(byte[] b, int off, int len);

        public abstract void Flush();

        public abstract OWClientConnector Clone();

        public abstract string PortNameForReconnect { get; } 

        ~OWClientConnector()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(Boolean disposing)
        {
        }

        public abstract OWClientConnector TransferControl(OWClientConnector newConnector);

        public abstract void SetSoTimeout(int msTimeout);

    }

    internal sealed class OWClientConnectorEMPTY : OWClientConnector
    {

        public override byte ReadByte()
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override string ReadUTF()
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override int ReadInt()
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override Boolean ReadBoolean()
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override int Read(byte[] b, int off, int len)
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override void ReadFully(byte[] b, int off, int len)
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override void WriteByte(byte b)
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override void WriteUTF(string s)
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override void WriteInt(int v)
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override void WriteBoolean(Boolean b)
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override void Write(byte[] b, int off, int len)
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override void Flush()
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override OWClientConnector Clone()
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

        public override string PortNameForReconnect
        {
            get
            {
                throw new Exception("Illegal call. This is the EMPTY connector");
            }
        }

        public override OWClientConnector TransferControl(OWClientConnector newConnector)
        {
            return newConnector;
        }

        public override void SetSoTimeout(int msTimeout)
        {
            throw new Exception("Illegal call. This is the EMPTY connector");
        }

    }

}
