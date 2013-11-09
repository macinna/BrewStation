// TODO: Disposal of Processor object

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

using System.Collections; // Hashtable

using System.Net; // DalSemi.Utils; // CRC16
using System.Net.Sockets;

using System.Threading;

using DalSemi.Utils; // CRC16

using System.IO;

namespace DalSemi.OneWire.Adapter
{

    /**
     * <P>NetAdapterSim is the host (or server) component for a network-based
     * DSPortAdapter.  It actually wraps the hardware DSPortAdapter and handles
     * connections from outside sources (NetAdapter) who want to access it.</P>
     *
     * <P>NetAdapterSim is designed to be run in a thread, waiting for incoming
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
     * <P>The NetAdapter and NetAdapterSim support multicast broadcasts for
     * automatic discovery of compatible servers on your LAN.  To start the
     * multicast listener for this NetAdapterSim, call the
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

    public class NetAdapterSim // implements Runnable, NetAdapterConstants
    {

        protected static Boolean SIM_DEBUG = false;

        /** random number generator, used to issue challenges to client */
        protected readonly System.Random rand = new System.Random((int)DateTime.Now.Ticks);

        /** Log file */
        private StreamWriter logFile;

        /** exec command, command string to start the simulator */
        protected string execCommand;

        protected Processor process;

        /** fake address, returned from all search or getAddress commands */
        protected byte[] fakeAddress = null;

        /** The server socket for listening for connections */
        protected /* Server */ Socket serverSocket = null;

        /** secret for authentication with the server */
        protected string /* byte[] */ netAdapterSecret = null;

        /** boolean flags for stopping the host */
        protected volatile Boolean hostStopped = false, hostRunning = false;

        /** boolean flag to indicate whether or not the host is single or multi-threaded */
        protected Boolean singleThreaded = true;

        /** Map of all Service threads created, only for multi-threaded */
        protected Hashtable hashHandlers = null;

        /** Optional, listens for datagram packets from potential clients */
        protected MulticastListener multicastListener = null;

        /** timeout for socket receive, in seconds */
        protected int timeoutInSeconds = 30;

        /**
         * <P>Creates an instance of a NetAdapterSim which wraps the provided
         * adapter.  The host listens on the default port as specified by
         * NetAdapterConstants.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterSim will proxy
         * commands to.
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterSim(string execCmd, string logFilename)
            : this(execCmd, logFilename, NetAdapterConstants.DEFAULT_PORT, false)
        {
        }

        /**
         * <P>Creates a single-threaded instance of a NetAdapterSim which wraps the
         * provided adapter.  The host listens on the specified port.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterSim will proxy
         * commands to.
         * @param listenPort the TCP/IP port to listen on for incoming connections
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterSim(String execCmd, byte[] fakeAddress, String logFile,
                             int listenPort)
            : this(execCmd, logFile, listenPort, false)
        {
        }

        /**
         * <P>Creates an (optionally multithreaded) instance of a NetAdapterSim
         * which wraps the provided adapter.  The listen port is set to the
         * default port as defined in NetAdapterConstants.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterSim will proxy
         * commands to.
         * @param multiThread if true, multiple TCP/IP connections are allowed
         * to interact simulataneously with this adapter.
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterSim(string execCmd, string logFilename,
                             Boolean multiThread)
            : this(execCmd, logFilename, NetAdapterConstants.DEFAULT_PORT, multiThread)
        {
        }

        /**
         * <P>Creates an (optionally multi-threaded) instance of a NetAdapterSim which
         * wraps the provided adapter.  The host listens on the specified port.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterSim will proxy
         * commands to.
         * @param listenPort the TCP/IP port to listen on for incoming connections
         * @param multiThread if true, multiple TCP/IP connections are allowed
         * to interact simulataneously with this adapter.
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterSim(String execCmd, String logFilename,
                             int listenPort, Boolean multiThread)
        {
            // save references to file and command
            this.execCommand = execCmd;
            this.process = Processor.Exec(execCmd);

            // wait until process is ready
            int complete = 0;
            while (complete < 2)
            {
                string line = process.ReadLine();
                if (complete == 0 && line.IndexOf("read ok (data=17)") >= 0)
                {
                    complete++;
                    continue;
                }
                if (complete == 1 && line.IndexOf(NetAdapterSimConstants.PROMPT) >= 0)
                {
                    complete++;
                    continue;
                }
            }

            if (logFilename != null)
                this.logFile = File.AppendText(logFilename); // new FileWriter(logFilename), true);

            // Make sure we loaded the address of the device
            SimulationGetAddress();

            // create the server socket
            this.serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // new ServerSocket(listenPort);

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
                this.hashHandlers = new Hashtable();
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
         * <P>Creates an instance of a NetAdapterSim which wraps the provided
         * adapter.  The host listens on the default port as specified by
         * NetAdapterConstants.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterSim will proxy
         * commands to.
         * @param serverSock the ServerSocket for incoming connections
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterSim(string execCmd, string logFilename,
            /* Server */ Socket serverSock)
            : this(execCmd, logFilename, serverSock, false)
        {
        }

        /**
         * <P>Creates an (optionally multi-threaded) instance of a NetAdapterSim which
         * wraps the provided adapter.  The host listens on the specified port.</P>
         *
         * <P>Note that the secret used for authentication is the value specified
         * in the onewire.properties file as "NetAdapter.secret=mySecret".
         * To set the secret to another value, use the
         * <code>setSecret(String)</code> method.</P>
         *
         * @param adapter DSPortAdapter that this NetAdapterSim will proxy
         * commands to.
         * @param serverSock the ServerSocket for incoming connections
         * @param multiThread if true, multiple TCP/IP connections are allowed
         * to interact simulataneously with this adapter.
         *
         * @throws IOException if a network error occurs or the listen socket
         * cannot be created on the specified port.
         */
        public NetAdapterSim(string execCmd, string logFilename,
            /* Server */ Socket serverSock, Boolean multiThread)
        {
            // save references to file and command
            this.execCommand = execCmd;
            this.process = Processor.Exec(execCmd);

            // wait  until process is ready
            int complete = 0;
            while (complete < 2)
            {
                String line = process.ReadLine();
                if (complete == 0 && line.IndexOf("read ok (data=17)") >= 0)
                {
                    complete++;
                    continue;
                }
                if (complete == 1 && line.IndexOf(NetAdapterSimConstants.PROMPT) >= 0)
                {
                    complete++;
                    continue;
                }
            }

            if (logFilename != null)
                this.logFile = File.AppendText(logFilename); // new FileWriter(logFilename), true);

            // Make sure we loaded the address of the device
            SimulationGetAddress();

            // save reference to the server socket
            this.serverSocket = serverSock;

            // set multithreaded flag
            this.singleThreaded = !multiThread;
            if (multiThread)
            {
                this.hashHandlers = new Hashtable();
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
            netAdapterSecret = secret; // .getBytes();
        }

        /**
         * Creates a Multicast Listener to allow NetAdapter clients to discover
         * this NetAdapterSim automatically.  Uses defaults for Multicast group
         * and port.
         */
        public void CreateMulticastListener()
        {
            CreateMulticastListener(NetAdapterConstants.DEFAULT_MULTICAST_PORT);
        }

        /**
         * Creates a Multicast Listener to allow NetAdapter clients to discover
         * this NetAdapterSim automatically.  Uses default for Multicast group.
         *
         * @param port The port the Multicast socket will receive packets on
         */
        public void CreateMulticastListener(int port)
        {
            string group
               = AccessProvider.GetProperty("NetAdapter.MulticastGroup");
            if (group == null)
                group = NetAdapterConstants.DEFAULT_MULTICAST_GROUP;
            CreateMulticastListener(port, group);
        }

        /**
         * Creates a Multicast Listener to allow NetAdapter clients to discover
         * this NetAdapterSim automatically.
         *
         * @param port The port the Multicast socket will receive packets on
         * @param group The group the Multicast socket will join
         */
        public void CreateMulticastListener(int port, string group)
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
                listenPortBytes[4] = NetAdapterConstants.VERSION_UID_TERMINATOR;

                multicastListener = new MulticastListener(port, group,
                                                  versionBytes, listenPortBytes);
                //(new Thread(multicastListener)).start();
                new Thread(new ThreadStart(multicastListener.Run)).Start();
            }
        }


        /**
         * Run method for threaded NetAdapterSim.  Maintains server socket which
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
                    // reset time of last command, so we don'thread simulate a bunch of
                    // unneccessary time
                    timeOfLastCommand = DateTime.Now.Ticks; // System.currentTimeMillis();
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
            SocketHandler sh = new SocketHandler(this, sock);
            if (singleThreaded)
            {
                // single-threaded
                sh.Run();
            }
            else
            {
                // multi-threaded
                Thread t = new Thread(new ThreadStart(sh.Run));
                t.Start();
                lock (hashHandlers)
                {
                    hashHandlers.Add(t, sh);
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
                    //Enumeration e = hashHandlers.elements();
                    //while(e.hasMoreElements())
                    // ((SocketHandler)e.nextElement()).stopHandler();
                }
            }

            if (multicastListener != null)
                multicastListener.StopListener();

            // ensure that there is no exclusive use of the adapter
            //adapter.endExclusive();
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

        protected long timeOfLastCommand = 0;
        protected const long IGNORE_TIME_MIN = 2;
        protected const long IGNORE_TIME_MAX = 1000;
        /**
         * Reads in command from client and calls the appropriate handler function.
         *
         * @param conn The connection to send/receive data.
         *
         */
        private void ProcessRequests(OWClientConnector conn)
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            if (logFile != null)
                logFile.WriteLine("\n------------------------------------------");
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

            // get the next command
            byte cmd = 0x00;

            cmd = conn.ReadByte();

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            if (logFile != null)
                logFile.WriteLine("CMD received: " + cmd.ToString("X2"));
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

            if (cmd == NetAdapterConstants.CMD_PINGCONNECTION)
            {
                // no-op, might update timer of some sort later
                SimulationPing(1000);
                conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
                conn.Flush();
            }
            else
            {
                long timeDelta = DateTime.Now.Ticks /* System.currentTimeMillis() */ - timeOfLastCommand;
                if (SIM_DEBUG && logFile != null)
                    logFile.WriteLine("general: timeDelta=" + timeDelta);
                if (timeDelta >= IGNORE_TIME_MIN && timeDelta <= IGNORE_TIME_MAX)
                {
                    // do something with timeDelta
                    SimulationPing(timeDelta);
                }

                try
                {
                    // ... and fire the appropriate method
                    switch (cmd)
                    {
                        /* Connection keep-alive and close commands */
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
                            if (SIM_DEBUG && logFile != null)
                                logFile.WriteLine("Unkown command received: " + cmd);
                            break;
                    }
                }
                catch (OneWireException owe)
                {
                    if (SIM_DEBUG && logFile != null)
                        logFile.WriteLine("Exception: " + owe.ToString());
                    conn.WriteByte(NetAdapterConstants.RET_FAILURE);
                    conn.WriteUTF(owe.ToString());
                    conn.Flush();
                }
                timeOfLastCommand = DateTime.Now.Ticks; // System.currentTimeMillis();
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
                //if (conn.sock != null)
                {
                    conn.Dispose(); // conn.sock.close();
                }
            }
            catch (Exception /* IOException ioe */ )
            { /*drain*/; }

            //conn.sock = null;
            //conn.input = null;
            //conn.output = null;

            // ensure that there is no exclusive use of the adapter
            //adapter.endExclusive();
        }

        //--------
        //-------- Finding iButton/1-Wire device options
        //--------

        private void AdapterFindFirstDevice(OWClientConnector conn)
        {
            Boolean b = true;//adapter.findFirstDevice();

            if (logFile != null)
            {
                logFile.WriteLine("   findFirstDevice returned " + b);
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        private void AdapterFindNextDevice(OWClientConnector conn)
        {
            Boolean b = false;//adapter.findNextDevice();

            if (logFile != null)
            {
                logFile.WriteLine("   findNextDevice returned " + b);
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        private void AdapterGetAddress(OWClientConnector conn)
        {
            if (logFile != null)
            {
                logFile.WriteLine("   adapter.getAddress(byte[]) called");
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Write(fakeAddress, 0, 8);
            conn.Flush();
        }

        private void AdapterSetSearchOnlyAlarmingDevices(OWClientConnector conn)
        {
            if (logFile != null)
            {
                logFile.WriteLine("   setSearchOnlyAlarmingDevices called");
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterSetNoResetSearch(OWClientConnector conn)
        {
            if (logFile != null)
            {
                logFile.WriteLine("   setNoResetSearch called");
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterSetSearchAllDevices(OWClientConnector conn)
        {
            if (logFile != null)
            {
                logFile.WriteLine("   setSearchAllDevices called");
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterTargetAllFamilies(OWClientConnector conn)
        {
            if (logFile != null)
            {
                logFile.WriteLine("   targetAllFamilies called");
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        // TODO
        private void AdapterTargetFamily(OWClientConnector conn)
        {
            // get the number of family codes to expect
            int len = conn.ReadInt();
            // get the family codes
            byte[] family = new byte[len];
            conn.ReadFully(family, 0, len);

            if (logFile != null)
            {
                logFile.WriteLine("   targetFamily called");
                logFile.WriteLine("      families: " + DalSemi.OneWire.Utils.Convert.ToHexString(family));
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        // TODO
        private void AdapterExcludeFamily(OWClientConnector conn)
        {
            // get the number of family codes to expect
            int len = conn.ReadInt();
            // get the family codes
            byte[] family = new byte[len];
            conn.ReadFully(family, 0, len);

            if (logFile != null)
            {
                logFile.WriteLine("   excludeFamily called");
                logFile.WriteLine("      families: " + DalSemi.OneWire.Utils.Convert.ToHexString(family));
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        //--------
        //-------- 1-Wire Network Semaphore methods
        //--------

        // TODO
        private void AdapterBeginExclusive(OWClientConnector conn)
        {
            if (logFile != null)
            {
                logFile.WriteLine("   adapter.beginExclusive called");
            }

            // get blocking boolean
            //boolean blocking = 
            conn.ReadBoolean();
            // call beginExclusive
            Boolean b = true;

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();

            if (logFile != null)
            {
                logFile.WriteLine("      adapter.beginExclusive returned " + b);
            }
        }

        // TODO
        private void AdapterEndExclusive(OWClientConnector conn)
        {
            if (logFile != null)
            {
                logFile.WriteLine("   adapter.endExclusive called");
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        //--------
        //-------- Primitive 1-Wire Network data methods
        //--------

        private void AdapterReset(OWClientConnector conn)
        {
            int i = 1;// return 1 for presence pulse

            if (logFile != null)
            {
                logFile.WriteLine("   reset returned " + i);
            }

            SimulationReset();

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteInt(i);
            conn.Flush();
        }

        //TODO
        private void AdapterPutBit(OWClientConnector conn)
        {
            // get the value of the bit
            Boolean bit = conn.ReadBoolean();

            if (logFile != null)
            {
                logFile.WriteLine("   putBit called");
                logFile.WriteLine("      bit=" + bit);
            }

            SimulationPutBit(bit);
            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterPutByte(OWClientConnector conn)
        {
            // get the value of the byte
            byte b = conn.ReadByte();

            if (logFile != null)
            {
                logFile.WriteLine("   putByte called");
                logFile.WriteLine("      byte=" + DalSemi.OneWire.Utils.Convert.ToHexString(b));
            }

            SimulationPutByte(b);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        private void AdapterGetBit(OWClientConnector conn)
        {
            Boolean bit = SimulationGetBit();

            if (logFile != null)
            {
                logFile.WriteLine("   getBit called");
                logFile.WriteLine("      bit=" + bit);
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(bit);
            conn.Flush();
        }

        private void AdapterGetByte(OWClientConnector conn)
        {
            byte b = SimulationGetByte();

            if (logFile != null)
            {
                logFile.WriteLine("   getByte called");
                logFile.WriteLine("      byte=" + DalSemi.OneWire.Utils.Convert.ToHexString((byte)b));
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteByte(b);
            conn.Flush();
        }

        private void AdapterGetBlock(OWClientConnector conn)
        {
            // get the number requested
            int len = conn.ReadInt();
            if (logFile != null)
            {
                logFile.WriteLine("   getBlock called");
                logFile.WriteLine("      len=" + len);
            }

            // get the bytes
            byte[] b = new byte[len];
            for (int i = 0; i < len; i++)
            {
                b[i] = SimulationGetByte();
            }

            if (logFile != null)
            {
                logFile.WriteLine("      returned: " + DalSemi.OneWire.Utils.Convert.ToHexString(b));
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Write(b, 0, len);
            conn.Flush();
        }

        private void AdapterDataBlock(OWClientConnector conn)
        {
            if (logFile != null)
            {
                logFile.WriteLine("   DataBlock called");
            }
            // get the number to block
            int len = conn.ReadInt();
            // get the bytes to block
            byte[] b = new byte[len];
            conn.ReadFully(b, 0, len);

            if (logFile != null)
            {
                logFile.WriteLine("      " + len + " bytes");
                logFile.WriteLine("      Send: " + DalSemi.OneWire.Utils.Convert.ToHexString(b));
            }

            // do the block
            for (int i = 0; i < len; i++)
            {
                if (b[i] == (byte)0x0FF)
                {
                    b[i] = SimulationGetByte();
                }
                else
                {
                    SimulationPutByte(b[i]);
                }
            }

            if (logFile != null)
            {
                logFile.WriteLine("      Recv: " + DalSemi.OneWire.Utils.Convert.ToHexString(b));
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Write(b, 0, len);
            conn.Flush();
        }

        //--------
        //-------- 1-Wire Network power methods
        //--------

        // TODO
        private void AdapterSetPowerDuration(OWClientConnector conn)
        {
            // get the time factor value
            int timeFactor = conn.ReadInt();

            if (logFile != null)
            {
                logFile.WriteLine("   setPowerDuration called");
                logFile.WriteLine("      timeFactor=" + timeFactor);
            }

            // call setPowerDuration
            //adapter.setPowerDuration(timeFactor);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        // TODO
        private void AdapterStartPowerDelivery(OWClientConnector conn)
        {
            // get the change condition value
            int changeCondition = conn.ReadInt();

            if (logFile != null)
            {
                logFile.WriteLine("   startPowerDelivery called");
                logFile.WriteLine("      changeCondition=" + changeCondition);
            }

            // call startPowerDelivery
            Boolean success = true;//adapter.startPowerDelivery(changeCondition);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(success);
            conn.Flush();
        }

        // TODO
        private void AdapterSetProgramPulseDuration(OWClientConnector conn)
        {
            // get the time factor value
            int timeFactor = conn.ReadInt();

            if (logFile != null)
            {
                logFile.WriteLine("   setProgramPulseDuration called");
                logFile.WriteLine("      timeFactor=" + timeFactor);
            }

            // call setProgramPulseDuration
            //adapter.setProgramPulseDuration(timeFactor);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        // TODO
        private void AdapterStartProgramPulse(OWClientConnector conn)
        {
            // get the change condition value
            int changeCondition = conn.ReadInt();

            if (logFile != null)
            {
                logFile.WriteLine("   startProgramPulse called");
                logFile.WriteLine("      changeCondition=" + changeCondition);
            }

            // call startProgramPulse();
            Boolean success = true;//adapter.startProgramPulse(changeCondition);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(success);
            conn.Flush();
        }

        // TODO
        private void AdapterStartBreak(OWClientConnector conn)
        {
            if (logFile != null)
            {
                logFile.WriteLine("   startBreak called");
            }

            // call startBreak();
            //adapter.startBreak();

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        // TODO
        private void AdapterSetPowerNormal(OWClientConnector conn)
        {
            if (logFile != null)
            {
                logFile.WriteLine("   setPowerNormal called");
            }

            // call setPowerNormal
            //adapter.setPowerNormal();

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        //--------
        //-------- 1-Wire Network speed methods
        //--------

        // TODO
        private void AdapterSetSpeed(OWClientConnector conn)
        {
            // get the value of the new speed
            int speed = conn.ReadInt();

            if (logFile != null)
            {
                logFile.WriteLine("   setSpeed called");
                logFile.WriteLine("      speed=" + speed);
            }

            // do the setSpeed
            //adapter.setSpeed(speed);

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.Flush();
        }

        // TODO
        private void AdapterGetSpeed(OWClientConnector conn)
        {
            // get the adapter speed
            int speed = 0;//adapter.getSpeed();

            if (logFile != null)
            {
                logFile.WriteLine("   getSpeed called");
                logFile.WriteLine("      speed=" + speed);
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteInt(speed);
            conn.Flush();
        }


        //--------
        //-------- Adapter feature methods
        //--------

        // TODO
        private void AdapterCanOverdrive(OWClientConnector conn)
        {
            Boolean b = false;//adapter.canOverdrive();

            if (logFile != null)
            {
                logFile.WriteLine("   canOverdrive returned " + b);
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        // TODO
        private void AdapterCanHyperdrive(OWClientConnector conn)
        {
            Boolean b = false;//adapter.canHyperdrive();

            if (logFile != null)
            {
                logFile.WriteLine("   canHyperDrive returned " + b);
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        // TODO
        private void AdapterCanFlex(OWClientConnector conn)
        {
            Boolean b = false;//adapter.canFlex();

            if (logFile != null)
            {
                logFile.WriteLine("   canFlex returned " + b);
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        // TODO
        private void AdapterCanProgram(OWClientConnector conn)
        {
            Boolean b = true;//adapter.canProgram();

            if (logFile != null)
            {
                logFile.WriteLine("   canProgram returned " + b);
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        // TODO
        private void AdapterCanDeliverPower(OWClientConnector conn)
        {
            Boolean b = true;//adapter.canDeliverPower();

            if (logFile != null)
            {
                logFile.WriteLine("   canDeliverPower returned " + b);
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        // TODO
        private void AdapterCanDeliverSmartPower(OWClientConnector conn)
        {
            Boolean b = true;//adapter.canDeliverSmartPower();

            if (logFile != null)
            {
                logFile.WriteLine("   canDeliverSmartPower returned " + b);
            }

            conn.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.WriteBoolean(b);
            conn.Flush();
        }

        // TODO
        private void AdapterCanBreak(OWClientConnector conn)
        {
            Boolean b = true;//adapter.canBreak();

            if (logFile != null)
            {
                logFile.WriteLine("   canBreak returned " + b);
            }

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

            private NetAdapterSim sim;

            /**
             * indicates whether or not the handler is currently running
             */
            private volatile Boolean handlerRunning = false;

            /**
             * Constructor for socket servicer.  Creates the input and output
             * streams and send's the version of this host to the client
             * connection.
             */
            public SocketHandler(NetAdapterSim Sim, Socket socket)
            {
                sim = Sim;
                conn = new OWClientConnectorTCP(socket);

                // set socket timeout to 10 seconds
                conn.SetSoTimeout(sim.timeoutInSeconds * 1000);

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
                if (!sim.SendVersionUID(conn))
                {
                    throw new Exception("send version failed"); // IOException
                }

                // authenticate the client
                byte[] chlg = new byte[8];
                sim.rand.NextBytes(chlg);
                conn.Write(chlg, 0, chlg.Length);
                conn.Flush();

                // compute the crc of the secret and the challenge
                uint crc = CRC16.Compute(sim.netAdapterSecret, 0);
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
                    while (!sim.hostStopped /* && conn.sock != null */)
                    {
                        sim.ProcessRequests(conn);
                    }
                }
                catch (Exception e) // thread
                {
                    if (sim.logFile != null)
                        sim.logFile.WriteLine(e.StackTrace);
                    sim.Close(conn);
                }
                handlerRunning = false;

                if (!sim.hostStopped && !sim.singleThreaded)
                {
                    lock (sim.hashHandlers)
                    {
                        // thread finished running without being stopped.
                        // politely remove it from the hashtable.
                        sim.hashHandlers.Remove(Thread.CurrentThread); // TODO: test this hashing
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

        private void SimulationReset()
        {
            if (SIM_DEBUG && logFile != null)
            {
                logFile.WriteLine("reset: Writing=" + NetAdapterSimConstants.OW_RESET_CMD);
                logFile.WriteLine("reset: Writing=" + NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_RESET_RUN_LENGTH);
            }
            process.Write(NetAdapterSimConstants.OW_RESET_CMD + NetAdapterSimConstants.LINE_DELIM);
            process.Write(NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_RESET_RUN_LENGTH + NetAdapterSimConstants.LINE_DELIM);
            process.Flush();

            // wait for it to complete
            int complete = 0;
            while (complete < 2)
            {
                String line = process.ReadLine();
                if (SIM_DEBUG && logFile != null)
                    logFile.WriteLine("reset: complete=" + complete + ", read=" + line);
                if (complete == 0 && line.IndexOf(NetAdapterSimConstants.OW_RESET_RESULT) >= 0)
                {
                    complete++;
                    continue;
                }
                if (complete == 1 && line.IndexOf(NetAdapterSimConstants.GENERIC_CMD_END) >= 0)
                {
                    complete++;
                    continue;
                }
            }
            if (SIM_DEBUG && logFile != null)
                logFile.WriteLine("reset: Complete");
        }

        private Boolean SimulationGetBit()
        {
            Boolean bit = true;

            if (SIM_DEBUG && logFile != null)
            {
                logFile.WriteLine("getBit: Writing=" + NetAdapterSimConstants.OW_READ_SLOT_CMD);
                logFile.WriteLine("getBit: Writing=" + NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_READ_SLOT_RUN_LENGTH);
            }
            process.Write(NetAdapterSimConstants.OW_READ_SLOT_CMD + NetAdapterSimConstants.LINE_DELIM);
            process.Write(NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_READ_SLOT_RUN_LENGTH + NetAdapterSimConstants.LINE_DELIM);
            process.Flush();

            // wait for it to complete
            int complete = 0;
            while (complete < 3)
            {
                String line = process.ReadLine();
                if (SIM_DEBUG && logFile != null)
                    logFile.WriteLine("getBit: complete=" + complete + ", read=" + line);
                if (complete == 0 && line.IndexOf("OW = 1'b0") >= 0)
                {
                    complete++;
                    continue;
                }
                if (complete == 1 && line.IndexOf("OW = 1'b0") >= 0)
                {
                    bit = false;
                    complete++;
                    continue;
                }
                if (complete == 1 && line.IndexOf("OW = 1'b1") >= 0)
                {
                    bit = true;
                    complete++;
                    continue;
                }
                if (complete == 2 && line.IndexOf(NetAdapterSimConstants.GENERIC_CMD_END) >= 0)
                {
                    complete++;
                    continue;
                }
            }
            if (SIM_DEBUG && logFile != null)
                logFile.WriteLine("getBit: Complete");
            return bit;
        }

        private byte SimulationGetByte()
        {
            byte bits = 0;

            if (SIM_DEBUG && logFile != null)
            {
                logFile.WriteLine("getByte: Writing=" + NetAdapterSimConstants.OW_READ_BYTE_CMD);
                logFile.WriteLine("getByte: Writing=" + NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_READ_BYTE_RUN_LENGTH);
            }
            process.Write(NetAdapterSimConstants.OW_READ_BYTE_CMD + NetAdapterSimConstants.LINE_DELIM);
            process.Write(NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_READ_BYTE_RUN_LENGTH + NetAdapterSimConstants.LINE_DELIM);
            process.Flush();

            // wait for it to complete
            try
            {
                int complete = 0;
                while (complete < 2)
                {
                    String line = process.ReadLine();
                    if (SIM_DEBUG && logFile != null)
                        logFile.WriteLine("getByte: complete=" + complete + ", read=" + line);
                    if (complete == 0 && line.IndexOf(NetAdapterSimConstants.OW_READ_RESULT) >= 0)
                    {
                        int i = line.IndexOf(NetAdapterSimConstants.OW_READ_RESULT) + NetAdapterSimConstants.OW_READ_RESULT.Length;
                        String bitstr = line.Substring(i, i + 2);
                        if (SIM_DEBUG && logFile != null)
                            logFile.WriteLine("getByte: bitstr=" + bitstr);
                        bits = (byte)(DalSemi.OneWire.Utils.Convert.ToInt(bitstr) & 0x0FF);
                        complete++;
                        continue;
                    }
                    if (complete == 1 && line.IndexOf(NetAdapterSimConstants.GENERIC_CMD_END) >= 0)
                    {
                        complete++;
                        continue;
                    }
                }
                if (SIM_DEBUG && logFile != null)
                    logFile.WriteLine("getByte: complete");
            }
            catch (Exception /* Convert.ConvertException */ ce)
            {
                if (SIM_DEBUG && logFile != null)
                    logFile.WriteLine("Error during hex string conversion: " + ce.ToString());
            }
            return bits;
        }

        private void SimulationPutBit(Boolean bit)
        {
            if (bit)
            {
                if (SIM_DEBUG && logFile != null)
                {
                    logFile.WriteLine("putBit: Writing=" + NetAdapterSimConstants.OW_WRITE_ONE_CMD);
                    logFile.WriteLine("putBit: Writing=" + NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_WRITE_ONE_RUN_LENGTH);
                }
                process.Write(NetAdapterSimConstants.OW_WRITE_ONE_CMD + NetAdapterSimConstants.LINE_DELIM);
                process.Write(NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_WRITE_ONE_RUN_LENGTH + NetAdapterSimConstants.LINE_DELIM);
            }
            else
            {
                if (SIM_DEBUG && logFile != null)
                {
                    logFile.WriteLine("putBit: Writing=" + NetAdapterSimConstants.OW_WRITE_ZERO_CMD);
                    logFile.WriteLine("putBit: Writing=" + NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_WRITE_ZERO_RUN_LENGTH);
                }
                process.Write(NetAdapterSimConstants.OW_WRITE_ZERO_CMD + NetAdapterSimConstants.LINE_DELIM);
                process.Write(NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_WRITE_ZERO_RUN_LENGTH + NetAdapterSimConstants.LINE_DELIM);
            }
            process.Flush();

            // wait for it to complete
            int complete = 0;
            while (complete < 1)
            {
                String line = process.ReadLine();
                if (SIM_DEBUG && logFile != null)
                    logFile.WriteLine("putBit: complete=" + complete + ", read=" + line);
                if (complete == 0 && line.IndexOf(NetAdapterSimConstants.GENERIC_CMD_END) >= 0)
                {
                    complete++;
                    continue;
                }
            }
            if (SIM_DEBUG && logFile != null)
                logFile.WriteLine("putBit: complete");
        }

        private void SimulationPutByte(byte b)
        {
            if (SIM_DEBUG && logFile != null)
            {
                logFile.WriteLine("putByte: Writing=" + NetAdapterSimConstants.OW_WRITE_BYTE_ARG + DalSemi.OneWire.Utils.Convert.ToHexString(b));
                logFile.WriteLine("putByte: Writing=" + NetAdapterSimConstants.OW_WRITE_BYTE_CMD);
                logFile.WriteLine("putByte: Writing=" + NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_WRITE_BYTE_RUN_LENGTH);
            }
            process.Write(NetAdapterSimConstants.OW_WRITE_BYTE_ARG + DalSemi.OneWire.Utils.Convert.ToHexString(b) + NetAdapterSimConstants.LINE_DELIM);
            process.Write(NetAdapterSimConstants.OW_WRITE_BYTE_CMD + NetAdapterSimConstants.LINE_DELIM);
            process.Write(NetAdapterSimConstants.RUN + NetAdapterSimConstants.OW_WRITE_BYTE_RUN_LENGTH + NetAdapterSimConstants.LINE_DELIM);
            process.Flush();

            // wait for it to complete
            int complete = 0;
            while (complete < 1)
            {
                String line = process.ReadLine();
                if (SIM_DEBUG && logFile != null)
                    logFile.WriteLine("putByte: complete=" + complete + ", read=" + line);
                if (complete == 0 && line.IndexOf(NetAdapterSimConstants.GENERIC_CMD_END) >= 0)
                {
                    complete++;
                    continue;
                }
            }
            if (SIM_DEBUG && logFile != null)
                logFile.WriteLine("putByte: complete");
        }

        private void SimulationPing(long timeDelta)
        {
            if (SIM_DEBUG && logFile != null)
            {
                logFile.WriteLine("ping: timeDelta=" + timeDelta);
                logFile.WriteLine("ping: Writing=" + NetAdapterSimConstants.RUN + (NetAdapterSimConstants.PING_MS_RUN_LENGTH * timeDelta));
            }
            process.Write(NetAdapterSimConstants.RUN + (NetAdapterSimConstants.PING_MS_RUN_LENGTH * timeDelta) + NetAdapterSimConstants.LINE_DELIM);
            process.Flush();

            // wait for it to complete
            int complete = 0;
            while (complete < 1)
            {
                String line = process.ReadLine();
                if (SIM_DEBUG && logFile != null)
                    logFile.WriteLine("ping: complete=" + complete + ", read=" + line);
                if (complete == 0 && line.IndexOf(NetAdapterSimConstants.GENERIC_CMD_END) >= 0)
                {
                    complete++;
                    continue;
                }
            }
            if (SIM_DEBUG && logFile != null)
                logFile.WriteLine("ping: complete");
        }

        private void SimulationGetAddress()
        {
            this.fakeAddress = new byte[8];
            // reset the simulated part
            SimulationReset();
            // put the Read Rom command
            SimulationPutByte((byte)0x33);
            // get the Rom ID
            for (int i = 0; i < 8; i++)
                this.fakeAddress[i] = SimulationGetByte();
        }

        //--------
        //-------- Default Main Method, for launching server with defaults
        //--------
        /**
         * A Default Main Method, for launching NetAdapterSim getting the
         * default adapter with the OneWireAccessProvider and listening on
         * the default port specified by DEFAULT_PORT.
         */
        public static void NetAdapterSimExampleMain(string[] args)
        {
            Console.Out.WriteLine("NetAdapterSim");
            if (args.Length < 1)
            {
                Console.Out.WriteLine("");
                Console.Out.WriteLine("   java com.dalsemi.onewire.adapter.NetAdapterSim <execCmd> <logFilename> <simDebug>");
                Console.Out.WriteLine("");
                Console.Out.WriteLine("   execCmd     - the command to start the simulator");
                Console.Out.WriteLine("   logFilename - the name of the file to log output to");
                Console.Out.WriteLine("   simDebug    - 'true' or 'false', turns on debug output from simulation");
                Console.Out.WriteLine("");
                Environment.Exit(1);
            }

            String execCmd = args[0];
            Console.Out.WriteLine("   Executing: " + execCmd);
            String logFilename = null;
            if (args.Length > 1)
            {
                if (!args[1].ToLower().Equals("false"))
                {
                    logFilename = args[1];
                    Console.Out.WriteLine("   Logging data to file: " + logFilename);
                }
            }
            if (args.Length > 2)
            {
                NetAdapterSim.SIM_DEBUG = args[2].ToLower().Equals("true");
                Console.Out.WriteLine("   Simulation Debugging is: "
                                   + (NetAdapterSim.SIM_DEBUG ? "enabled" : "disabled"));
            }

            NetAdapterSim host = new NetAdapterSim(execCmd, logFilename);
            Console.Out.WriteLine("Device Address=" + DalSemi.OneWire.Utils.Address.ToString(host.fakeAddress));

            Console.Out.WriteLine("Starting Multicast Listener...");
            host.CreateMulticastListener();

            Console.Out.WriteLine("Starting NetAdapter Host...");
            new Thread(host.Run).Start();
            Console.Out.WriteLine("NetAdapter Host Started");
        }


    }
}
