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

using System.Net;
using System.Net.Sockets;

using System.Threading;

using DalSemi.Utils; // CRC16

namespace DalSemi.OneWire.Adapter
{
    /**
     * <P>NetAdapterHost is the host (or server) component for a network-based
     * DSPortAdapter.  It actually wraps the hardware DSPortAdapter and handles
     * connections from outside sources (NetAdapter) who want to access it.</P>
     *
     * <P>NetAdapterHost is designed to be run in a thread, waiting for incoming
     * connections.  You can run this in the same thread as your main program or
     * you can establish the connections yourself (presumably using some higher
     * level of security) and then call the <code>handleConnection(Socket)</code>
     * {@see #handleConnection(Socket)}.</P>
     *
     * <P>Once a NetAdapter is connected with the host, a version check is performed
     * followed by a simple authentication step.  The authentication is dependent
     * upon a secret shared between the NetAdapter and the host.  Both will use
     * a default value, that each will agree with if you don'thread provide a secret
     * of your own.  To set the secret, add the following line to your
     * onewire.properties file:
     * <ul>
     *    <li>NetAdapter.secret="This is my custom secret"</li>
     * </ul>
     * Optionally, the secret can be set by calling the <code>setSecret(String)</code>
     * {@see #setSecret(String)}</P>
     *
     * <P>The NetAdapter and NetAdapterHost support multicast broadcasts for
     * automatic discovery of compatible servers on your LAN.  To start the
     * multicast listener for this NetAdapterHost, call the
     * <code>createMulticastListener()</code> method
     * {@see #createMulticastListener()}.</P>
     *
     * <P>For information on creating the client component, see the JavaDocs
     * for the  {@link com.dalsemi.onewire.adapter.NetAdapter NetAdapter}.
     *
     * @see NetAdapter
     *
     * @author SH
     * @version    1.00, 9 Jan 2002
     */
    public class NetAdapterHost // implements Runnable, NetAdapterConstants
    {

        /** random number generator, used to issue challenges to client */
        protected readonly System.Random rand = new System.Random((int)DateTime.Now.Ticks);

        /** The adapter this NetAdapter will proxy too */
        protected PortAdapter adapter = null;

        /** The server socket for listening for connections */
        protected Socket serverSocket = null;

        /** secret for authentication with the server */
        protected string netAdapterSecret = null;

        /** boolean flags for stopping the host */
        protected volatile Boolean hostStopped = false, hostRunning = false;

        /** boolean flag to indicate whether or not the host is single or multi-threaded */
        protected Boolean singleThreaded = true;

        /** Map of all Service threads created, only for multi-threaded */
        protected System.Collections.Hashtable hashHandlers = null;

        /** Optional, listens for datagram packets from potential clients */
        protected MulticastListener multicastListener = null;

        /** timeout for socket receive, in seconds */
        protected int timeoutInSeconds = 30;

        /**
         * <P>Creates an instance of a NetAdapterHost which wraps the provided
         * adapter.  The host listens on the default port as specified by
         * NetAdapterConstants.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterHost will proxy
         * commands to.
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterHost(PortAdapter adapter)
            : this(adapter, NetAdapterConstants.DEFAULT_PORT, false)
        {
        }

        /**
         * <P>Creates a single-threaded instance of a NetAdapterHost which wraps the
         * provided adapter.  The host listens on the specified port.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterHost will proxy
         * commands to.
         * @param listenPort the TCP/IP port to listen on for incoming connections
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterHost(PortAdapter adapter, int listenPort)
            : this(adapter, listenPort, false)
        {
        }

        /**
         * <P>Creates an (optionally multithreaded) instance of a NetAdapterHost
         * which wraps the provided adapter.  The listen port is set to the
         * default port as defined in NetAdapterConstants.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterHost will proxy
         * commands to.
         * @param multiThread if true, multiple TCP/IP connections are allowed
         * to interact simulataneously with this adapter.
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterHost(PortAdapter adapter, Boolean multiThread)
            : this(adapter, NetAdapterConstants.DEFAULT_PORT, multiThread)
        {
        }

        /**
         * <P>Creates an (optionally multi-threaded) instance of a NetAdapterHost which
         * wraps the provided adapter.  The host listens on the specified port.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterHost will proxy
         * commands to.
         * @param listenPort the TCP/IP port to listen on for incoming connections
         * @param multiThread if true, multiple TCP/IP connections are allowed
         * to interact simulataneously with this adapter.
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterHost(PortAdapter adapter, int listenPort,
                              Boolean multiThread)
        {
            //save reference to adapter
            this.adapter = adapter;

            // create the server socket

            this.serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // this.serverSocket = new ServerSocket(listenPort);

            /*
        //create the listening socket...
        m_socListener = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            */
            IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, listenPort);
            //bind to local IP Address...
            this.serverSocket.Bind(ipLocal);
            //start listening...
            this.serverSocket.Listen(4);
            /*
        // create the call back for any client connections...
        m_socListener.BeginAccept(new AsyncCallback ( OnClientConnect ),null);
        cmdListen.Enabled = false;
            */








            // set multithreaded flag
            this.singleThreaded = !multiThread;
            if (multiThread)
            {
                this.hashHandlers = new System.Collections.Hashtable();
                this.timeoutInSeconds = 0;
            }

            // get the shared secret
            string secret = AccessProvider.GetProperty("NetAdapter.secret");
            if (secret != null)
                netAdapterSecret = secret;
            else
                netAdapterSecret = NetAdapterConstants.DEFAULT_SECRET;
        }

        /**
         * <P>Creates an instance of a NetAdapterHost which wraps the provided
         * adapter.  The host listens on the default port as specified by
         * NetAdapterConstants.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterHost will proxy
         * commands to.
         * @param serverSock the ServerSocket for incoming connections
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterHost(PortAdapter adapter, Socket serverSock)
            : this(adapter, serverSock, false)
        {
        }

        /**
         * <P>Creates an (optionally multi-threaded) instance of a NetAdapterHost which
         * wraps the provided adapter.  The host listens on the specified port.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterHost will proxy
         * commands to.
         * @param serverSock the ServerSocket for incoming connections
         * @param multiThread if true, multiple TCP/IP connections are allowed
         * to interact simulataneously with this adapter.
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterHost(PortAdapter adapter, Socket serverSock,
                              Boolean multiThread)
        {
            //save reference to adapter
            this.adapter = adapter;

            // create the server socket
            this.serverSocket = serverSock;

            // set multithreaded flag
            this.singleThreaded = !multiThread;
            if (multiThread)
            {
                this.hashHandlers = new System.Collections.Hashtable();
                this.timeoutInSeconds = 0;
            }

            // get the shared secret
            String secret = AccessProvider.GetProperty("NetAdapter.secret");
            if (secret != null)
                netAdapterSecret = secret;
            else
                netAdapterSecret = NetAdapterConstants.DEFAULT_SECRET;
        }

        /**
         * Sets the secret used for authenticating incoming client connections.
         *
         * @param secret The shared secret information used for authenticating
         *               incoming client connections.
         */
        public void SetSecret(string secret)
        {
            netAdapterSecret = secret;
        }

        /**
         * Creates a Multicast Listener to allow NetAdapter clients to discover
         * this NetAdapterHost automatically.  Uses defaults for Multicast group
         * and port.
         */
        public void CreateMulticastListener()
        {
            CreateMulticastListener(NetAdapterConstants.DEFAULT_MULTICAST_PORT);
        }

        /**
         * Creates a Multicast Listener to allow NetAdapter clients to discover
         * this NetAdapterHost automatically.  Uses default for Multicast group.
         *
         * @param port The port the Multicast socket will receive packets on
         */
        public void CreateMulticastListener(int port)
        {
            string group = AccessProvider.GetProperty("NetAdapter.MulticastGroup");
            if (group == null)
                group = NetAdapterConstants.DEFAULT_MULTICAST_GROUP;
            CreateMulticastListener(port, group);
        }

        /**
         * Creates a Multicast Listener to allow NetAdapter clients to discover
         * this NetAdapterHost automatically.
         *
         * @param port The port the Multicast socket will receive packets on
         * @param group The group the Multicast socket will join
         */
        public void CreateMulticastListener(int port, String group)
        {
            if (multicastListener == null)
            {
                // 4 bytes for integer versionUID
                byte[] versionBytes = DalSemi.OneWire.Utils.Convert.ToByteArray(NetAdapterConstants.versionUID);

                // this byte array is 5 because length is used to determine different
                // packet types by client
                byte[] listenPortBytes = new byte[5];
                DalSemi.OneWire.Utils.Convert.ToByteArray((serverSocket.LocalEndPoint as IPEndPoint).Port,
                                    listenPortBytes, 0, 4);
                listenPortBytes[4] = (byte)0x0FF;

                multicastListener = new MulticastListener(port, group,
                                                  versionBytes, listenPortBytes);
                new Thread(multicastListener.Run).Start();
            }
        }


        /**
         * Run method for threaded NetAdapterHost.  Maintains server socket which
         * waits for incoming connections.  Whenever a connection is received
         * launches it services the socket or (optionally) launches a new thread
         * for servicing the socket.
         */
        public void Run()
        {
            hostRunning = true;
            while (!hostStopped)
            {
                Socket sock = null;
                try
                {
                    sock = serverSocket.Accept();
                    HandleConnection(sock);
                }
                catch (Exception /* IOException ioe1 */)
                {
                    try
                    {
                        if (sock != null)
                            sock.Close();
                    }
                    catch (Exception /* IOException ioe2 */)
                    { ;}
                }
            }
            hostRunning = false;
        }

        /**
         * Handles a socket connection.  If single-threaded, the connection is
         * serviced in the current thread.  If multi-threaded, a new thread is
         * created for servicing this connection.
         */
        public void HandleConnection(Socket sock)
        {
            SocketHandler socketHandler = new SocketHandler(this, sock);
            if (singleThreaded)
            {
                // single-threaded
                socketHandler.Run();
            }
            else
            {
                // multi-threaded
                Thread thread = new Thread(socketHandler.Run);
                thread.Start();
                lock (hashHandlers)
                {
                    hashHandlers.Add(thread, socketHandler);
                }
            }
        }
        /**
         * Stops all threads and kills the server socket.
         */
        public void StopHost()
        {
            this.hostStopped = true;
            try
            {
                this.serverSocket.Close();
            }
            catch (Exception /* IOException ioe */)
            { ;}

            // wait for run method to quit, with a timeout of 1 second
            int i = 0;
            while (hostRunning && i++ < 100)
                try { Thread.Sleep(10); }
                catch (Exception) { ;}

            if (!singleThreaded)
            {
                lock (hashHandlers)
                {
                    foreach (SocketHandler e in hashHandlers)
                        e.StopHandler();
                    /*
                                Enumeration e = hashHandlers.elements();
                                while(e.hasMoreElements())
                                   ((SocketHandler)e.nextElement()).stopHandler();
                    */
                }
            }

            if (multicastListener != null)
                multicastListener.StopListener();

            // ensure that there is no exclusive use of the adapter
            adapter.EndExclusive();
        }

        /**
         * Transmits the versionUID of the current NetAdapter protocol to
         * the client connection.  If it matches the clients versionUID,
         * the client returns RET_SUCCESS.
         *
         * @param conn The connection to send/receive data.
         * @return <code>true</code> if the versionUID matched.
         */
        private Boolean SendVersionUID(OWClientConnector conn)
        {
            // write server version
            conn.WriteInt(NetAdapterConstants.versionUID);
            conn.Flush();

            byte retVal = conn.ReadByte();

            return (retVal == NetAdapterConstants.RET_SUCCESS);
        }

        /**
         * Reads in command from client and calls the appropriate handler function.
         *
         * @param conn The connection to send/receive data.
         *
         */
        private void ProcessRequests(OWClientConnector conn)
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("\n------------------------------------------");
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

            // get the next command
            byte cmd = conn.ReadByte();

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("CMD received: 0x" + cmd.ToString("X2"));
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

            try
            {
                // ... and fire the appropriate method
                switch (cmd)
                {
                    /* Connection keep-alive and close commands */
                    case NetAdapterConstants.CMD_PINGCONNECTION:
                        // no-op, might update timer of some sort later
                        conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
                        conn.Flush();
                        break;
                    case NetAdapterConstants.CMD_CLOSECONNECTION:
                        Close(conn);
                        break;
                    /* Raw Data commands */
                    case NetAdapterConstants.CMD_RESET:
                        AdapterReset(conn);
                        break;
                    case NetAdapterConstants.CMD_PUTBIT:
                        AdapterPutBit(conn);
                        break;
                    case NetAdapterConstants.CMD_PUTBYTE:
                        AdapterPutByte(conn);
                        break;
                    case NetAdapterConstants.CMD_GETBIT:
                        AdapterGetBit(conn);
                        break;
                    case NetAdapterConstants.CMD_GETBYTE:
                        AdapterGetByte(conn);
                        break;
                    case NetAdapterConstants.CMD_GETBLOCK:
                        AdapterGetBlock(conn);
                        break;
                    case NetAdapterConstants.CMD_DATABLOCK:
                        AdapterDataBlock(conn);
                        break;
                    /* Power methods */
                    case NetAdapterConstants.CMD_SETPOWERDURATION:
                        AdapterSetPowerDuration(conn);
                        break;
                    case NetAdapterConstants.CMD_STARTPOWERDELIVERY:
                        AdapterStartPowerDelivery(conn);
                        break;
                    case NetAdapterConstants.CMD_SETPROGRAMPULSEDURATION:
                        AdapterSetProgramPulseDuration(conn);
                        break;
                    case NetAdapterConstants.CMD_STARTPROGRAMPULSE:
                        AdapterStartProgramPulse(conn);
                        break;
                    case NetAdapterConstants.CMD_STARTBREAK:
                        AdapterStartBreak(conn);
                        break;
                    case NetAdapterConstants.CMD_SETPOWERNORMAL:
                        AdapterSetPowerNormal(conn);
                        break;
                    /* Speed methods */
                    case NetAdapterConstants.CMD_SETSPEED:
                        AdapterSetSpeed(conn);
                        break;
                    case NetAdapterConstants.CMD_GETSPEED:
                        AdapterGetSpeed(conn);
                        break;
                    /* Network Semaphore methods */
                    case NetAdapterConstants.CMD_BEGINEXCLUSIVE:
                        AdapterBeginExclusive(conn);
                        break;
                    case NetAdapterConstants.CMD_ENDEXCLUSIVE:
                        AdapterEndExclusive(conn);
                        break;
                    /* Searching methods */
                    case NetAdapterConstants.CMD_FINDFIRSTDEVICE:
                        AdapterFindFirstDevice(conn);
                        break;
                    case NetAdapterConstants.CMD_FINDNEXTDEVICE:
                        AdapterFindNextDevice(conn);
                        break;
                    case NetAdapterConstants.CMD_GETADDRESS:
                        AdapterGetAddress(conn);
                        break;
                    case NetAdapterConstants.CMD_SETSEARCHONLYALARMINGDEVICES:
                        AdapterSetSearchOnlyAlarmingDevices(conn);
                        break;
                    case NetAdapterConstants.CMD_SETNORESETSEARCH:
                        AdapterSetNoResetSearch(conn);
                        break;
                    case NetAdapterConstants.CMD_SETSEARCHALLDEVICES:
                        AdapterSetSearchAllDevices(conn);
                        break;
                    case NetAdapterConstants.CMD_TARGETALLFAMILIES:
                        AdapterTargetAllFamilies(conn);
                        break;
                    case NetAdapterConstants.CMD_TARGETFAMILY:
                        AdapterTargetFamily(conn);
                        break;
                    case NetAdapterConstants.CMD_EXCLUDEFAMILY:
                        AdapterExcludeFamily(conn);
                        break;
                    /* feature methods */
                    case NetAdapterConstants.CMD_CANBREAK:
                        AdapterCanBreak(conn);
                        break;
                    case NetAdapterConstants.CMD_CANDELIVERPOWER:
                        AdapterCanDeliverPower(conn);
                        break;
                    case NetAdapterConstants.CMD_CANDELIVERSMARTPOWER:
                        AdapterCanDeliverSmartPower(conn);
                        break;
                    case NetAdapterConstants.CMD_CANFLEX:
                        AdapterCanFlex(conn);
                        break;
                    case NetAdapterConstants.CMD_CANHYPERDRIVE:
                        AdapterCanHyperdrive(conn);
                        break;
                    case NetAdapterConstants.CMD_CANOVERDRIVE:
                        AdapterCanOverdrive(conn);
                        break;
                    case NetAdapterConstants.CMD_CANPROGRAM:
                        AdapterCanProgram(conn);
                        break;
                    default:
                        //Console.Out.WriteLine("Unkown command: " + cmd);
                        break;
                }
            }
            catch (OneWireException owe)
            {
                conn.WriteByte(NetAdapterConstants.RET_FAILURE);
                conn.WriteUTF(owe.ToString());
                conn.Flush();
            }
        }

        /**
         * Closes the provided connection.
         *
         * @param conn The connection to send/receive data.
         */
        private void Close(OWClientConnector conn)
        {
            try
            {
                //if(conn.sock!=null)
                {
                    //conn.sock.close();
                    conn.Dispose();
                }
            }
            catch (Exception /* IOException ioe */)
            { /*drain*/; }

            //conn.sock = null;
            //conn.input = null;
            //conn.output = null;

            // ensure that there is no exclusive use of the adapter
            adapter.EndExclusive();
        }

        //--------
        //-------- Finding iButton/1-Wire device options
        //--------

        private byte[] lastAddress = new byte[8];

        private void AdapterFindFirstDevice(OWClientConnector conn)
        {
            Boolean b = adapter.GetFirstDevice(lastAddress, 0);

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   findFirstDevice returned " + b);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        private void AdapterFindNextDevice(OWClientConnector conn)
        {
            Boolean b = adapter.GetNextDevice(lastAddress, 0);

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   findNextDevice returned " + b);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        private void AdapterGetAddress(OWClientConnector conn)
        {
            // read in the address
            byte[] address = lastAddress; //  new byte[8];
            // call getAddress
            //adapter.GetAddress(address);

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   adapter.getAddress(byte[]) called, speed=" + adapter.Speed.ToString());
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Write(address, 0, 8);
            conn.Flush();
        }

        private void AdapterSetSearchOnlyAlarmingDevices(OWClientConnector conn)
        {
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   setSearchOnlyAlarmingDevices called, speed=" + adapter.Speed.ToString());
#endif

            adapter.SetSearchOnlyAlarmingDevices();

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterSetNoResetSearch(OWClientConnector conn)
        {
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   setNoResetSearch called, speed=" + adapter.Speed.ToString());
#endif

            adapter.SetNoResetSearch();

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterSetSearchAllDevices(OWClientConnector conn)
        {
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   setSearchAllDevices called, speed=" + adapter.Speed.ToString());
#endif

            adapter.SetSearchAllDevices();

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterTargetAllFamilies(OWClientConnector conn)
        {
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   targetAllFamilies called, speed=" + adapter.Speed.ToString());
#endif

            adapter.TargetAllFamilies();

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterTargetFamily(OWClientConnector conn)
        {
            // get the number of family codes to expect
            int len = conn.ReadInt();
            // get the family codes
            byte[] family = new byte[len];
            conn.ReadFully(family, 0, len);

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   targetFamily called, speed=" + adapter.Speed.ToString());
            Console.Out.WriteLine("      families: " + DalSemi.OneWire.Utils.Convert.ToHexString(family));
#endif

            // call targetFamily
            adapter.TargetFamily(family);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterExcludeFamily(OWClientConnector conn)
        {
            // get the number of family codes to expect
            int len = conn.ReadInt();
            // get the family codes
            byte[] family = new byte[len];
            conn.ReadFully(family, 0, len); 

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   excludeFamily called, speed=" + adapter.Speed.ToString());
            Console.Out.WriteLine("      families: " + DalSemi.OneWire.Utils.Convert.ToHexString(family));
#endif

            // call excludeFamily
            adapter.ExcludeFamily(family);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        //--------
        //-------- 1-Wire Network Semaphore methods
        //--------

        private void AdapterBeginExclusive(OWClientConnector conn)
        {
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   adapter.beginExclusive called, speed=" + adapter.Speed.ToString());
#endif

            // get blocking boolean
            Boolean blocking = conn.ReadBoolean();
            // call beginExclusive
            Boolean b = adapter.BeginExclusive(blocking);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("      adapter.beginExclusive returned " + b);
#endif
        }

        private void AdapterEndExclusive(OWClientConnector conn)
        {
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   adapter.endExclusive called, speed=" + adapter.Speed.ToString());
#endif

            // call endExclusive
            adapter.EndExclusive();

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        //--------
        //-------- Primitive 1-Wire Network data methods
        //--------

        private void AdapterReset(OWClientConnector conn)
        {
            int i = (int)adapter.Reset();

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   reset, speed=" + adapter.Speed.ToString() + ", returned " + i);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteInt(i);
            conn.Flush();
        }

        private void AdapterPutBit(OWClientConnector conn)
        {
            // get the value of the bit
            Boolean bit = conn.ReadBoolean();

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   putBit called, speed=" + adapter.Speed.ToString());
            Console.Out.WriteLine("      bit=" + bit);
#endif

            adapter.PutBit(bit);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterPutByte(OWClientConnector conn)
        {
            // get the value of the byte
            byte b = conn.ReadByte();

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   putByte called, speed=" + adapter.Speed.ToString());
            Console.Out.WriteLine("      byte=" + DalSemi.OneWire.Utils.Convert.ToHexString(b));
#endif

            adapter.PutByte(b);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterGetBit(OWClientConnector conn)
        {
            Boolean bit = adapter.GetBit();

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   getBit called, speed=" + adapter.Speed.ToString());
            Console.Out.WriteLine("      bit=" + bit);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(bit);
            conn.Flush();
        }

        private void AdapterGetByte(OWClientConnector conn)
        {
            byte b = adapter.GetByte();

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   getByte called, speed=" + adapter.Speed.ToString());
            Console.Out.WriteLine("      byte=" + DalSemi.OneWire.Utils.Convert.ToHexString((byte)b));
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteByte(b);
            conn.Flush();
        }
        private void AdapterGetBlock(OWClientConnector conn)
        {
            // get the number requested
            int len = conn.ReadInt();
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   getBlock called, speed=" + adapter.Speed.ToString());
            Console.Out.WriteLine("      len=" + len);
#endif

            // get the bytes
            byte[] b = adapter.GetBlock(len);

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("      returned: " + DalSemi.OneWire.Utils.Convert.ToHexString(b));
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Write(b, 0, len);
            conn.Flush();
        }

        private void AdapterDataBlock(OWClientConnector conn)
        {
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("   DataBlock called, speed=" + adapter.Speed.ToString());
#endif
            // get the number to block
            int len = conn.ReadInt();
            // get the bytes to block
            byte[] b = new byte[len];
            conn.ReadFully(b, 0, len); 

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("      " + len + " bytes");
            Console.Out.WriteLine("      Send: " + DalSemi.OneWire.Utils.Convert.ToHexString(b));
#endif

            // do the block
            adapter.DataBlock(b, 0, len);

#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine("      Recv: " + DalSemi.OneWire.Utils.Convert.ToHexString(b));
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Write(b, 0, len);
            conn.Flush();
        }

        //--------
        //-------- 1-Wire Network power methods
        //--------

        private void AdapterSetPowerDuration(OWClientConnector conn)
        {
            // get the time factor value
            int timeFactor = conn.ReadInt();

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   setPowerDuration called, speed=" + adapter.Speed.ToString());
                Console.Out.WriteLine("      timeFactor=" + timeFactor);
#endif

            // call setPowerDuration
            adapter.SetPowerDuration((OWPowerTime)timeFactor);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterStartPowerDelivery(OWClientConnector conn)
        {
            // get the change condition value
            int changeCondition = conn.ReadInt();

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   startPowerDelivery called, speed=" + adapter.Speed.ToString());
                Console.Out.WriteLine("      changeCondition=" + changeCondition);
#endif

            // call startPowerDelivery
            Boolean success = adapter.StartPowerDelivery((OWPowerStart)changeCondition);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(success);
            conn.Flush();
        }

        private void AdapterSetProgramPulseDuration(OWClientConnector conn)
        {
            // get the time factor value
            int timeFactor = conn.ReadInt();

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   setProgramPulseDuration called, speed=" + adapter.Speed.ToString());
                Console.Out.WriteLine("      timeFactor=" + timeFactor);
#endif

            // call setProgramPulseDuration
            adapter.SetProgramPulseDuration((OWPowerTime)timeFactor);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterStartProgramPulse(OWClientConnector conn)
        {
            // get the change condition value
            int changeCondition = conn.ReadInt();

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   startProgramPulse called, speed=" + adapter.Speed.ToString());
                Console.Out.WriteLine("      changeCondition=" + changeCondition);
#endif

            // call startProgramPulse();
            Boolean success = adapter.StartProgramPulse((OWPowerStart)changeCondition);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(success);
            conn.Flush();
        }

        private void AdapterStartBreak(OWClientConnector conn)
        {
#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   startBreak called, speed=" + adapter.Speed.ToString());
#endif

            // call startBreak();
            adapter.StartBreak();

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterSetPowerNormal(OWClientConnector conn)
        {
#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   setPowerNormal called, speed=" + adapter.Speed.ToString());
#endif

            // call setPowerNormal
            adapter.SetPowerNormal();

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        //--------
        //-------- 1-Wire Network speed methods
        //--------

        private void AdapterSetSpeed(OWClientConnector conn)
        {
            // get the value of the new speed
            int speed = conn.ReadInt();

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   setSpeed called, speed=" + adapter.Speed.ToString());
                Console.Out.WriteLine("      speed=" + speed);
#endif

            // do the setSpeed
            adapter.Speed = (OWSpeed)speed;

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterGetSpeed(OWClientConnector conn)
        {
            // get the adapter speed
            int speed = (int)adapter.Speed;

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   getSpeed called, speed=" + adapter.Speed.ToString());
                Console.Out.WriteLine("      speed=" + speed);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteInt(speed);
            conn.Flush();
        }


        //--------
        //-------- Adapter feature methods
        //--------

        private void AdapterCanOverdrive(OWClientConnector conn)
        {
            Boolean b = adapter.CanOverdrive;

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   canOverdrive returned " + b);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        private void AdapterCanHyperdrive(OWClientConnector conn)
        {
            Boolean b = adapter.CanHyperdrive;

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   canHyperDrive returned " + b);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        private void AdapterCanFlex(OWClientConnector conn)
        {
            Boolean b = adapter.CanFlex;

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   canFlex returned " + b);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        private void AdapterCanProgram(OWClientConnector conn)
        {
            Boolean b = adapter.CanProgram;

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   canProgram returned " + b);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        private void AdapterCanDeliverPower(OWClientConnector conn)
        {
            Boolean b = adapter.CanDeliverPower;

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   canDeliverPower returned " + b);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        private void AdapterCanDeliverSmartPower(OWClientConnector conn)
        {
            Boolean b = adapter.CanDeliverSmartPower;

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   canDeliverSmartPower returned " + b);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        private void AdapterCanBreak(OWClientConnector conn)
        {
            Boolean b = adapter.CanBreak;

#if DEBUG_NETADAPTERHOST
                Console.Out.WriteLine("   canBreak returned " + b);
#endif

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        //--------
        //-------- Inner classes
        //--------

        /**
         * Private inner class for servicing new connections.
         * Can be run in it's own thread or in the same thread.
         */
        private class SocketHandler // implements Runnable
        {
            /**
             * The connection that is being serviced.
             */
            private OWClientConnector conn;

            /**
             * indicates whether or not the handler is currently running
             */
            private volatile Boolean handlerRunning = false;

            /**
             * Constructor for socket servicer.  Creates the input and output
             * streams and send's the version of this host to the client
             * connection.
             */
            private NetAdapterHost host;
            public SocketHandler(NetAdapterHost Host, Socket sock)
            {
                host = Host;
                conn = new OWClientConnectorTCP(sock);
                // set socket timeout to 10 seconds
                //sock.SetSoTimeout(host.timeoutInSeconds * 1000);
                conn.SetSoTimeout(host.timeoutInSeconds * 1000);

                /*
                                // create the connection object
                                conn = new Connection();
                                conn.sock = sock;
                                conn.input = new DataInputStream(conn.sock.getInputStream());
                                if (BUFFERED_OUTPUT)
                                {
                                    conn.output = new DataOutputStream(new BufferedOutputStream(
                                                                         conn.sock.getOutputStream()));
                                }
                                else
                                {
                                    conn.output = new DataOutputStream(conn.sock.getOutputStream());
                                }
                */

                // first thing transmitted should be version info
                if (!host.SendVersionUID(conn))
                {
                    throw new Exception("send version failed"); // IOException
                }

                // authenticate the client
                byte[] chlg = new byte[8];
                host.rand.NextBytes(chlg);
                conn.Write(chlg, 0, chlg.Length);
                conn.Flush();

                // compute the crc of the secret and the challenge
                uint crc = CRC16.Compute(host.netAdapterSecret, 0);
                crc = CRC16.Compute(chlg, crc);
                int answer = conn.ReadInt();
                if (answer != crc)
                {
                    conn.WriteByte(NetAdapterConstants.RET_FAILURE);
                    conn.WriteUTF("Client Authentication Failed");
                    conn.Flush();
                    throw new Exception("authentication failed"); // IOException
                }
                else
                {
                    conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
                    conn.Flush();
                }
            }

            /**
             * Run method for socket Servicer.
             */
            public void Run()
            {
                handlerRunning = true;
                try
                {
                    while (!host.hostStopped /* && conn.sock != null */)
                        host.ProcessRequests(conn);
                }
                catch (Exception
#if DEBUG_NETADAPTERHOST 
                    e 
#endif
)
                {
#if DEBUG_NETADAPTERHOST
            Console.Out.WriteLine(e.StackTrace);
#endif
                    host.Close(conn);
                }
                handlerRunning = false;

                if (!host.hostStopped && !host.singleThreaded)
                {
                    lock (host.hashHandlers)
                    {
                        // thread finished running without being stopped.
                        // politely remove it from the hashtable.
                        host.hashHandlers.Remove(Thread.CurrentThread); // TODO: check this hash
                    }
                }
            }

            /**
             * Waits for handler to finish, with a timeout.
             */
            public void StopHandler()
            {
                int i = 0;
                int timeout = 3000;
                while (handlerRunning && i++ < timeout)
                    try { Thread.Sleep(10); }
                    catch (Exception) { ;}
            }
        }

        //--------
        //-------- Default Main Method, for launching server with defaults
        //--------
        /**
         * A Default Main Method, for launching NetAdapterHost getting the
         * default adapter with the OneWireAccessProvider and listening on
         * the default port specified by DEFAULT_PORT.
         */
        public static void NetAdapterHostExampleMain(string[] args)
        {
            PortAdapter adapter = AccessProvider.DefaultAdapter;

            NetAdapterHost host = new NetAdapterHost(adapter, true);

            Console.Out.WriteLine("Starting Multicast Listener");
            host.CreateMulticastListener();

            Console.Out.WriteLine("Starting NetAdapter Host");
            new Thread(host.Run).Start();

            //if(System.in!=null)
            //{
            //   Console.Out.WriteLine("\nPress Enter to Shutdown");
            //   (new BufferedReader(new InputStreamReader(System.in))).readLine();
            //   host.stopHost();
            //   System.exit(1);
            //}
        }
    }


    /**
     * Starts the host component for NetAdapter clients on the local machine.
     * If no options are specified, the default adapter for this machine is used
     * and the host is launched as a multi-threaded server using the defaults.
     *
     */
    public class StartNetAdapterHost
    {
        static readonly string strUsage =
     "Starts the host component for NetAdapter clients on the local machine.\r\n" +
     "If no options are specified, the default adapter for this machine is used\r\n" +
     "and the host is launched as a multi-threaded server using the defaults:\r\n" +
     "\r\n" +
     "  Host Listen Port: " + NetAdapterConstants.DEFAULT_PORT + "\r\n" +
     "  Multithreaded Host: Enabled\r\n" +
     "  Shared Secret: '" + NetAdapterConstants.DEFAULT_SECRET + "'\r\n" +
     "  Multicast: Enabled\r\n" +
     "  Multicast Port: " + NetAdapterConstants.DEFAULT_MULTICAST_PORT + "\r\n" +
     "  Multicast Group: " + NetAdapterConstants.DEFAULT_MULTICAST_GROUP + "\r\n" +
     "\r\n" +
     "syntax: java StartNetAdapterHost <options>\r\n" +
     "\r\n" +
     "Options:\r\n" +
     "  -props                    Pulls all defaults from the onewire.properties\r\n" +
     "                            file rather than using the defaults set in\r\n" +
     "                            com.dalsemi.onewire.adapter.NetAdapterConstants.\r\n" +
     "  -adapterName STRING       Selects the Adapter to use for the host.\r\n" +
     "  -adapterPort STRING       Selects the Adapter port to use for the host.\r\n" +
     "  -listenPort NUM           Sets the host's listening port for incoming\r\n" +
     "                            socket connections.\r\n" +
     "  -multithread [true|false] Sets whether or not the hosts launches a new\r\n" +
     "                            thread for every incoming client.\r\n" +
     "  -secret STRING            Sets the shared secret for authenticating incoming\r\n" +
     "                            client connections.\r\n" +
     "  -multicast [true|false]   Enables/Disables the multicast listener. If\r\n" +
     "                            disabled, clients will not be able to\r\n" +
     "                            automatically discover this host.\r\n" +
     "  -multicastPort NUM        Set the port number for receiving packets.\r\n" +
     "  -multicastGroup STRING    Set the group for multicast sockets.  Must be in\r\n" +
     "                            the range of '224.0.0.0' to '239.255.255.255'.\r\n";

        public static void Usage()
        {

            Console.Out.WriteLine();
            Console.Out.WriteLine(strUsage);
            Environment.Exit(1);
        }

        public static void StartNetAdapterHostMain(string[] args)
        {

            Console.Out.WriteLine(
               "\r\n" +
               "  This program is distributed as part of the open source OWdotNET project.\r\n" +
               "  Project pages: https://sourceforge.net/projects/owdotnet\r\n" +
               "  Web Site:      http://owdotnet.sourceforge.net/\r\n");

            string adapterName = null, adapterPort = null;
            int listenPort = NetAdapterConstants.DEFAULT_PORT;
            Boolean multithread = true;
            string secret = NetAdapterConstants.DEFAULT_SECRET;
            Boolean multicast = true;
            int mcPort = NetAdapterConstants.DEFAULT_MULTICAST_PORT;
            string mcGroup = NetAdapterConstants.DEFAULT_MULTICAST_GROUP;

            Boolean useProperties = false;

            if (args.Length > 0)
            {
                try
                {
                    // check to see if they are looking for help
                    char[] ca = args[0].ToCharArray();
                    char c = (ca.Length > 1) ? ca[1] : '?';
                    char cc = (ca.Length > 0) ? ca[0] : '@'; // not being '-'
                    if (cc != '-' || c == 'h' || c == 'H' || c == '?')
                        Usage();

                    // do one pass to see if we're supposed to use the properties file
                    for (int i = 0; !useProperties && i < args.Length; i++)
                        useProperties = (args[i].ToLower().Equals("-props"));

                    // if we found -props, load all the defauls from onewire.properties
                    // that can be found there.
                    if (useProperties)
                    {
                        String test = AccessProvider.GetProperty("onewire.adapter.default");
                        if (test != null)
                            adapterName = test;

                        test = AccessProvider.GetProperty("onewire.port.default");
                        if (test != null)
                            adapterPort = test;

                        test = AccessProvider.GetProperty("NetAdapter.ListenPort");
                        if (test != null)
                            listenPort = int.Parse(test);

                        test = AccessProvider.GetProperty("NetAdapter.Multithread");
                        if (test != null)
                            multithread = Convert.ToBoolean(test);

                        test = AccessProvider.GetProperty("NetAdapter.Secret");
                        if (test != null)
                            secret = test;

                        test = AccessProvider.GetProperty("NetAdapter.Multicast");
                        if (test != null)
                            multicast = Convert.ToBoolean(test);

                        test = AccessProvider.GetProperty("NetAdapter.MulticastPort");
                        if (test != null)
                            mcPort = int.Parse(test);

                        test = AccessProvider.GetProperty("NetAdapter.MulticastGroup");
                        if (test != null)
                            mcGroup = test;
                    }

                    // get the other propertie values from the command-line
                    // these will override the defaults
                    for (int i = 0; i < args.Length; i++)
                    {
                        string arg = args[i];
                        ca = arg.ToCharArray();
                        cc = (ca.Length > 0) ? ca[0] : '@'; // not '-'
                        if (cc != '-')
                            Usage();

                        if (arg.ToLower().Equals("-adapterName"))
                        {
                            adapterName = args[++i];
                        }
                        else if (arg.ToLower().Equals("-adapterPort"))
                        {
                            adapterPort = args[++i];
                        }
                        else if (arg.ToLower().Equals("-listenPort"))
                        {
                            listenPort = int.Parse(args[++i]);
                        }
                        else if (arg.ToLower().Equals("-multithread"))
                        {
                            multithread = Boolean.Parse(args[++i]);
                        }
                        else if (arg.ToLower().Equals("-secret"))
                        {
                            secret = args[++i];
                        }
                        else if (arg.ToLower().Equals("-multicast"))
                        {
                            multicast = Boolean.Parse(args[++i]);
                        }
                        else if (arg.ToLower().Equals("-multicastPort"))
                        {
                            mcPort = int.Parse(args[++i]);
                        }
                        else if (arg.ToLower().Equals("-multicastGroup"))
                        {
                            mcGroup = args[++i];
                        }
                        else if (!arg.ToLower().Equals("-props"))
                        {
                            Console.Out.WriteLine("Invalid option: " + arg);
                            Usage();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine("Error parsing arguments: " + e.Message);
                    Console.Out.WriteLine("");
                    Usage();
                }
            }

            // get the appropriate adapter.
            PortAdapter adapter;
            if (adapterName == null || adapterPort == null)
                adapter = AccessProvider.DefaultAdapter;
            else
                adapter = AccessProvider.GetAdapter(adapterName, adapterPort);

            Console.Out.WriteLine(
               "  Adapter Name: " + adapter.AdapterName + "\r\n" +
               "  Adapter Port: " + adapter.PortName + "\r\n" +
               "  Host Listen Port: " + listenPort + "\r\n" +
               "  Multithreaded Host: " + (multithread ? "Enabled" : "Disabled") + "\r\n" +
               "  Shared Secret: '" + secret + "'\r\n" +
               "  Multicast: " + (multicast ? "Enabled" : "Disabled") + "\r\n" +
               "  Multicast Port: " + mcPort + "\r\n" +
               "  Multicast Group: " + mcGroup + "\r\n");

            // Create the NetAdapterHost
            NetAdapterHost host = new NetAdapterHost(adapter, listenPort, multithread);
            // set the shared secret
            host.SetSecret(secret);

            if (multicast)
            {
                Console.Out.WriteLine("  Starting Multicast Listener");
                host.CreateMulticastListener(mcPort, mcGroup);
            }

            Console.Out.WriteLine("  Starting NetAdapter Host");
            new Thread(host.Run).Start();

            Console.Out.Write("\r\n  Press CTRL+C to end program");

        }
    }
}
