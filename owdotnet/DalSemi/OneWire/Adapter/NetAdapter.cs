// TODO: remove use of System.Net.Sockets.SocketException

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

using DalSemi.Utils; // CRC16

namespace DalSemi.OneWire.Adapter
{

    /**
     * <P>NetAdapter is a network-based DSPortAdapter.  It allows for the use of
     * an actual DSPortAdapter which isn'thread on the local machine, but rather is
     * connected to another device which is reachable via a TCP/IP network
     * connection.</P>
     *
     * <P>The syntax for the <code>selectPort(String)</code> command is the
     * hostname of the computer which hosts the actual DSPortAdapter and the
     * TCP/IP port that the host is listening on.  If the port number is not
     * specified, a default value of 6161 is used. Here are a few examples to
     * illustrate the syntax:
     * <ul>
     *    <li>my.host.com:6060</li>
     *    <li>180.0.2.46:6262</li>
     *    <li>my.host.com</li>
     *    <li>180.0.2.46</li>
     * </ul></P>
     *
     * <P?The use of the NetAdapter is virtually identical to the use of any
     * other DSPortAdapter.  The only significant changes are the necessity
     * of the 'host' component (see NetAdapterHost)
     * and the discovery of hosts on your network.  There are currently two
     * techniques used for discovering all of the hosts: The look-up of each host
     * from the onewire.properties file and the use of multicast sockets for
     * automatic discovery.</P>
     *
     * <P>In the onewire.properties file, you can add a host to your list of valid
     * hosts by making a NetAdapter.host with an integer to distinguish the hosts.
     * There is no limit on the number of hosts which can appear in this list, but
     * the first one must be numbered '0'.  These hosts will then be returned in
     * the list of valid 'ports' from the <code>selectPortNames()</code> method.
     * Note that there do not have to be any servers returned from
     * <code>selectPortNames()</code> for the NetAdapter to be able to connect
     * to them (so it isn'thread necessary to add these entries for it to function),
     * but applications which allow a user to automatically select an appropriate
     * adapter and a port from a given list will not function properly without it.
     * For example:
     * <ul>
     *    <li>NetAdapter.host0=my.host.com:6060</li>
     *    <li>NetAdapter.host1=180.0.2.46:6262</li>
     *    <li>NetAdapter.host2=my.host.com</li>
     *    <li>NetAdapter.host3=180.0.2.46</li>
     * </ul></P>
     *
     * <P>The multicast socket technique allows you to automatically discover
     * hosts on your subnet which are listening for multicast packets.  By
     * default, the multicast discovery of NetAdapter hosts is disabled.
     * When enabled, the NetAdapter creates a multicast socket and looks for servers
     * every time you call <code>selectPortNames()</code>.  This will add a
     * 1 second delay (due to the socket timeout) on calling the method.  If you'd
     * like to enable this feature, add the following line to your
     * onewire.properties file:
     * <ul>
     *    <li>NetAdapter.MulticastEnabled=true</li>
     * </ul>
     * The port used and the multicast group used for multicast sockets can
     * also be changed.  The group however, must fall withing a valid range.
     * For more information about multicast sockets in Java, see the Java
     * tutorial on networking at <A HREF="http://java.sun.com/docs/books/tutorial/">
     * http://java.sun.com/docs/books/tutorial/</A>.  Change the defaults in the
     * onewire.properties file with the following entries:
     * <ul>
     *    <li>NetAdapter.MulticastGroup=228.5.6.7</li>
     *    <li>NetAdapter.MulticastPort=6163</li>
     * </ul>
     * </P>
     *
     * <P>Once the NetAdapter is connected with a host, a version check is performed
     * followed by a simple authentication step.  The authentication is dependent
     * upon a secret shared between the NetAdapter and the host.  Both will use
     * a default value, that each will agree with if you don'thread provide a secret
     * of your own.  To set the secret, add the following line to your
     * onewire.properties file:
     * <ul>
     *    <li>NetAdapter.secret="This is my custom secret"</li>
     * </ul>
     * Optionally, the secret can be specified on a per-host basis by simply
     * adding the secret after the port number followed by a colon.  If no port
     * number is specified, a double-colon is required.  Here are examples:
     * <ul>
     *    <li>my.host.com:6060:my custom secret</li>
     *    <li>180.0.2.46:6262:another custom secret</li>
     *    <li>my.host.com::the custom secret without port number</li>
     *    <li>180.0.2.46::another example of a custom secret</li>
     * </ul></P>
     *
     * <P>All of the above mentioned properties can be set on the command-line
     * as well as being set in the onewire.properties file.  To set the
     * properties on the command-line, use the -D option:
     * java -DNetAdapter.Secret="custom secret" myApplication</P>
     *
     * <P>The following is a list of all parameters that can be set for the
     * NetAdapter, followed by default values where applicable.<br>
     * <ul>
     *    <li>NetAdapter.secret=Adapter Secret Default</li>
     *    <li>NetAdapter.secret[0-MaxInt]=[no default]</li>
     *    <li>NetAdapter.host[0-MaxInt]=[no default]</li>
     *    <li>NetAdapter.MulticastEnabled=false</li>
     *    <li>NetAdapter.MulticastGroup=228.5.6.7</li>
     *    <li>NetAdapter.MulticastPort=6163</li>
     * </ul></P>
     *
     * <p>If you wanted added security on the communication channel, an SSL socket
     * (or similar custom socket implementation) can be used by circumventing the
     * standard DSPortAdapter's <code>selectPort(String)</code> and using the
     * NetAdapter-specific <code>selectPort(Socket)</code>.  For example:
     * <pre>
     *    NetAdapter na = new NetAdapter();
     *
     *    Socket secureSocket = // insert fancy secure socket implementation here
     *
     *    na.selectPort(secureSocket);
     * <pre></P>
     *
     * <P>For information on setting up the host component, see the JavaDocs
     * for the <code>NetAdapterHost</code>
     *
     * @see NetAdapterHost
     *
     * @author SH
     * @version    1.00, 9 Jan 2002
     */

    public class NetAdapter : PortAdapter // NetAdapterConstants
    {

        /** Error message when neither RET_SUCCESS or RET_FAILURE are returned */
        protected const string UNSPECIFIED_ERROR = "An unspecified error occurred.";
        /** Error message when I/O failure occurs */
        protected const string COMM_FAILED = "IO Error: ";

        /** constant for no exclusive lock */
        protected const int NOT_OWNED = 0;
        /** Keeps hash of current thread for exclusive lock */
        protected int currentThreadHash = NOT_OWNED;
        private object currentThreadHashLock = new object();

        /** instance for current connection, defaults to EMPTY*/
        protected OWClientConnector conn = NetAdapterConstants.EMPTY_CONNECTION;

        /** portName For Reconnecting to Host */
        protected string portNameForReconnect = null;

        /** secret for authentication with the server */
        protected string /* byte[] */ netAdapterSecret = null;

        /** if true, the user used a custom secret */
        protected Boolean useCustomSecret = false;

        //-------
        //------- Multicast variables
        //-------

        /** indicates whether or not mulicast is enabled */
        private Boolean? multicastEnabled = null;

        public Boolean MulticastEnabled
        {
            set
            {
                multicastEnabled = value;
            }
        }

        /**
         * Creates an instance of NetAdapter that isn'thread connected.  Must call
         * selectPort(String); or selectPort(Socket);
         */
        public NetAdapter()
        {
            try
            {
                ResetSecret();
            }
            catch (Exception)
            {
                SetSecret(NetAdapterConstants.DEFAULT_SECRET);
            }
        }

        /**
         * Sets the shared secret for authenticating this NetAdapter with
         * a NetAdapterHost.
         *
         * @param secret the new secret for authenticating this client.
         */
        public void SetSecret(string secret)
        {
            if (secret != null)
            {
                this.netAdapterSecret = secret;
            }
            else
                ResetSecret();
        }

        /**
         * Resets the secret to be the default stored in the onewire.properties
         * file (if there is one), or the default as defined by NetAdapterConstants.
         */
        public void ResetSecret()
        {
            string secret = AccessProvider.GetProperty("NetAdapter.Secret");
            if (secret != null)
                this.netAdapterSecret = secret;
            else
                this.netAdapterSecret = NetAdapterConstants.DEFAULT_SECRET;
        }

        /**
         * Checks return value from input stream.  Reads one byte.  If that
         * byte is not equal to RET_SUCCESS, then it tries to create an
         * appropriate error message.  If it is RET_FAILURE, it reads a
         * string representing the error message.  If it is neither, it
         * wraps an error message indicating that an unspecified error
         * occurred and attemps a reconnect.
         */
        private void CheckReturnValue(OWClientConnector conn)
        {
            byte retVal = conn.ReadByte();
            if (retVal != NetAdapterConstants.RET_SUCCESS)
            {
                // an error occurred
                String errorMsg;
                if (retVal == NetAdapterConstants.RET_FAILURE)
                {
                    // should be a standard error message after RET_FAILURE
                    errorMsg = conn.ReadUTF();
                }
                else
                {
                    // didn'thread even get RET_FAILURE
                    errorMsg = UNSPECIFIED_ERROR;

                    // that probably means we have a major communication error.
                    // better to disconnect and reconnect.
                    FreePort();
                    OpenPort(portNameForReconnect);
                }

                throw new OneWireIOException(errorMsg);
            }
        }

        /**
         * Sends a ping to the host, just to keep the connection alive.  Although
         * it currently is not implemented on the standard NetAdapterHost, this
         * command is used as a signal to the NetAdapterSim to simulate some amount
         * of time that has run.
         */
        public void PingHost()
        {
            try
            {
                lock (conn)
                {
                    // send beginExclusive command
                    conn.WriteByte(NetAdapterConstants.CMD_PINGCONNECTION);
                    conn.Flush();

                    CheckReturnValue(conn);
                }
            }
            catch (System.Net.Sockets.SocketException ioe) // TODO (RM): change this exception to transport-independent one
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        //--------
        //-------- Methods
        //--------

        /**
         * Detects adapter presence on the selected port.
         *
         * @return  <code>true</code> if the adapter is confirmed to be connected to
         * the selected port, <code>false</code> if the adapter is not connected.
         *
         * @throws OneWireIOException
         * @throws OneWireException
         */
        public override Boolean AdapterDetected
        {
            get
            {
                lock (conn)
                {
                    return conn != NetAdapterConstants.EMPTY_CONNECTION /* && conn.sock != null */ ; // TODO: check
                }
            }
        }

        /// <summary> Finalize to Cleanup native</summary>
        protected override void Dispose(bool Disposing)
        {
            try
            {
                if (conn != NetAdapterConstants.EMPTY_CONNECTION)
                    lock (conn)
                    {
                        conn.WriteByte(NetAdapterConstants.CMD_CLOSECONNECTION);
                        conn.Flush();
                        conn.Dispose();
                        conn = NetAdapterConstants.EMPTY_CONNECTION;
                    }
            }
            catch (Exception e)
            {
                if (Disposing) // when called from the Finalizer (Destructor) the connection can be long closed... Gracefully...
                               // maybe because the GC already finalized conn, before this finalizer is called
                    throw new OneWireException(COMM_FAILED + e.Message);
            }
        }


        /**
         * Retrieves the name of the port adapter as a string.  The 'Adapter'
         * is a device that connects to a 'port' that allows one to
         * communicate with an iButton or other 1-Wire device.  As example
         * of this is 'DS9097U'.
         *
         * @return  <code>String</code> representation of the port adapter.
         */
        public override string AdapterName
        {
            get
            {
                return "NetAdapter";
            }
        }

        /**
         * Retrieves a description of the port required by this port adapter.
         * An example of a 'Port' would 'serial communication port'.
         *
         * @return  <code>String</code> description of the port type required.
         */
        public override string PortTypeDescription
        {
            get
            {
                return "Network 'Hostname:Port'";
            }
        }

        //--------
        //-------- Port Selection
        //--------

        /**
         * Retrieves a list of the platform appropriate port names for this
         * adapter.  A port must be selected with the method 'selectPort'
         * before any other communication methods can be used.  Using
         * a communcation method before 'selectPort' will result in
         * a <code>OneWireException</code> exception.
         *
         * @return  <code>Enumeration</code> of type <code>String</code> that contains the port
         * names
         */
        public override System.Collections.IList PortNames
        {
            get
            {
                List<string> v = new List<string>();

                // figure out if multicast is enabled
                if (multicastEnabled == null)
                {
                    string enabled = null;
                    try
                    {
                        enabled = AccessProvider.GetProperty("NetAdapter.MulticastEnabled");
                    }
                    catch (Exception) { ;}
                    if (enabled != null)
                        multicastEnabled = Convert.ToBoolean(enabled);
                    else
                        multicastEnabled = false;
                }

                if (multicastEnabled ?? false)
                {
                    //conn.GetPortNames(v); // this does not work (can be EMPTY_CONNECTION)
                    OWClientConnectorTCP.GetMulticastPortNames(v);
                }

                // get all servers from the properties file
                string server = "";
                try
                {
                    for (int i = 0; server != null; i++)
                    {
                        server = AccessProvider.GetProperty("NetAdapter.host" + i);
                        if (server != null)
                            v.Add(server);
                    }
                }
                catch (Exception) { ;}

                return v;
            }
        }

        /**
         * Specifies a platform appropriate port name for this adapter.  Note that
         * even though the port has been selected, it's ownership may be relinquished
         * if it is not currently held in a 'exclusive' block.  This class will then
         * try to re-aquire the port when needed.  If the port cannot be re-aquired
         * ehen the exception <code>PortInUseException</code> will be thrown.
         *
         * @param  portName  Address to connect this NetAdapter to, in the form of
         * "hostname:port".  For example, "shughes.dalsemi.com:6161", where 6161
         * is the port number to connect to.  The use of NetAdapter.DEFAULT_PORT
         * is recommended.
         *
         * @return <code>true</code> if the port was aquired, <code>false</code>
         * if the port is not available.
         *
         * @throws OneWireIOException If port does not exist, or unable to communicate with port.
         * @throws OneWireException If port does not exist
         */
        public override bool OpenPort(string portName)
        {
            // portname --> host:port:secret
            lock (conn)
            {
                OWClientConnector s = null;
                try
                {
                    int port = NetAdapterConstants.DEFAULT_PORT;
                    // should be of the format "hostname:port" or hostname
                    int index = portName.IndexOf(':');
                    if (index >= 0)
                    {
                        int index2 = portName.IndexOf(':', index + 1);
                        if (index2 < 0) // no custom secret specified
                        {
                            port = int.Parse(portName.Substring(index + 1));
                            // reset the secret to default
                            ResetSecret();
                            useCustomSecret = false;
                        }
                        else
                        {
                            // custom secret is specified
                            SetSecret(portName.Substring(index2 + 1));
                            useCustomSecret = true;
                            if (index < index2 - 1) // port number is specified
                                port = int.Parse(portName.Substring(index + 1, index2 - index - 1));
                        }
                        portName = portName.Substring(0, index);
                    }
                    else
                    {
                        // reset the secret
                        ResetSecret();
                        useCustomSecret = false;
                    }
                    s = new OWClientConnectorTCP(portName /* host */, port /* port */); // TCP -> for now...
                    // BTW: no-one knows of s's existance, so no locking is needed
                }
                catch (System.Net.Sockets.SocketException ioe)
                {
                    throw new OneWireIOException("Can'thread reach server: " + ioe.Message);
                }

                return OpenPort(s);
            }
        }

        /**
         * New method, unique to NetAdapter.  Sets the "port", i.e. the connection
         * to the server via an already established socket connection.
         *
         * @param sock Socket connection to NetAdapterHost
         *
         * @return <code>true</code> if connection to host was successful
         *
         * @throws OneWireIOException If port does not exist, or unable to communicate with port.
         * @throws OneWireException If port does not exist
         */
        private Boolean OpenPort(OWClientConnector sock)
        {
            Boolean bSuccess = false;
            lock (conn)
            {
                OWClientConnector tmpConn = sock.Clone();
                //tmpConn.sock = sock;

                try
                {
                    /*
                                tmpConn.input = new DataInputStream(sock.getInputStream());
                                if(BUFFERED_OUTPUT)
                                {
                                   tmpConn.output
                                      = new DataOutputStream(new BufferedOutputStream(
                                                                      sock.getOutputStream()));
                                }
                                else
                                {
                                   tmpConn.output
                                      = new DataOutputStream(sock.getOutputStream());
                                }
                    */

                    // check host version
                    int hostVersionUID = tmpConn.ReadInt();

                    if (hostVersionUID == NetAdapterConstants.versionUID)
                    {
                        // tell the server that the versionUID matched
                        tmpConn.WriteByte(NetAdapterConstants.RET_SUCCESS);
                        tmpConn.Flush();

                        // if the versionUID matches, we need to authenticate ourselves
                        // using the challenge from the server.
                        byte[] chlg = new byte[8];
                        tmpConn.Read(chlg, 0, 8);

                        // compute the crc of the secret and the challenge
                        uint crc = CRC16.Compute(netAdapterSecret, 0);
                        crc = CRC16.Compute(chlg, crc);
                        // and send it back to the server
                        tmpConn.WriteInt((int)crc);
                        tmpConn.Flush();

                        // check to see if it matched
                        CheckReturnValue(tmpConn);

                        bSuccess = true;
                    }
                    else
                    {
                        tmpConn.WriteByte(NetAdapterConstants.RET_FAILURE);
                        tmpConn.Flush();
                        tmpConn = null;
                    }
                }
                catch (System.Net.Sockets.SocketException)
                {
                    bSuccess = false;
                    tmpConn = null;
                }

                if (bSuccess)
                {
                    portNameForReconnect = tmpConn.PortNameForReconnect; // sock.getInetAddress().getHostName() +
                    //                                                           ":" + sock.getPort();
                    conn = sock.TransferControl(tmpConn);
                }
            }

            // invalid response or version number
            return bSuccess;
        }

        /**
         * Frees ownership of the selected port, if it is currently owned, back
         * to the system.  This should only be called if the recently
         * selected port does not have an adapter, or at the end of
         * your application's use of the port.
         *
         * @throws OneWireException If port does not exist
         */
        //private void FreePort ()
        //{

        //}

        /**
         * Retrieves the name of the selected port as a <code>String</code>.
         *
         * @return  <code>String</code> of selected port
         *
         * @throws OneWireException if valid port not yet selected
         */
        public override string PortName
        {
            get
            {
                lock (conn)
                {
                    if (!AdapterDetected)
                        return "Not Connected";
                    else if (useCustomSecret)
                        return conn.PortNameForReconnect + // conn.sock.getInetAddress().getHostName() +
                    //   ":" + conn.sock.getPort() +
                                              ":" + this.netAdapterSecret;
                    else
                        return conn.PortNameForReconnect; // conn.sock.getInetAddress().getHostName() +
                    // ":" + conn.sock.getPort();
                }
            }
        }

        /**
         * Returns whether adapter can physically support overdrive mode.
         *
         * @return  <code>true</code> if this port adapter can do OverDrive,
         * <code>false</code> otherwise.
         *
         * @throws OneWireIOException on a 1-Wire communication error with the adapter
         * @throws OneWireException on a setup error with the 1-Wire
         *         adapter
         */
        public override Boolean CanOverdrive
        {
            get
            {
                try
                {
                    lock (conn)
                    {
                        // send beginExclusive command
                        conn.WriteByte(NetAdapterConstants.CMD_CANOVERDRIVE);
                        conn.Flush();

                        // check return value for success
                        CheckReturnValue(conn);

                        // next parameter should be the return from beginExclusive
                        return conn.ReadBoolean();
                    }
                }
                catch (System.Net.Sockets.SocketException ioe)
                {
                    throw new OneWireException(COMM_FAILED + ioe.Message);
                }
            }
        }

        /**
         * Returns whether the adapter can physically support hyperdrive mode.
         *
         * @return  <code>true</code> if this port adapter can do HyperDrive,
         * <code>false</code> otherwise.
         *
         * @throws OneWireIOException on a 1-Wire communication error with the adapter
         * @throws OneWireException on a setup error with the 1-Wire
         *         adapter
         */
        public override Boolean CanHyperdrive
        {
            get
            {
                try
                {
                    lock (conn)
                    {
                        // send beginExclusive command
                        conn.WriteByte(NetAdapterConstants.CMD_CANHYPERDRIVE);
                        conn.Flush();

                        // check return value for success
                        CheckReturnValue(conn);

                        // next parameter should be the return from beginExclusive
                        return conn.ReadBoolean();
                    }
                }
                catch (System.Net.Sockets.SocketException ioe)
                {
                    throw new OneWireException(COMM_FAILED + ioe.Message);
                }
            }
        }

        /**
         * Returns whether the adapter can physically support flex speed mode.
         *
         * @return  <code>true</code> if this port adapter can do flex speed,
         * <code>false</code> otherwise.
         *
         * @throws OneWireIOException on a 1-Wire communication error with the adapter
         * @throws OneWireException on a setup error with the 1-Wire
         *         adapter
         */
        public override Boolean CanFlex
        {
            get
            {
                try
                {
                    lock (conn)
                    {
                        // send beginExclusive command
                        conn.WriteByte(NetAdapterConstants.CMD_CANFLEX);
                        conn.Flush();

                        // check return value for success
                        CheckReturnValue(conn);

                        // next parameter should be the return from beginExclusive
                        return conn.ReadBoolean();
                    }
                }
                catch (System.Net.Sockets.SocketException ioe)
                {
                    throw new OneWireException(COMM_FAILED + ioe.Message);
                }
            }
        }

        /**
         * Returns whether adapter can physically support 12 volt power mode.
         *
         * @return  <code>true</code> if this port adapter can do Program voltage,
         * <code>false</code> otherwise.
         *
         * @throws OneWireIOException on a 1-Wire communication error with the adapter
         * @throws OneWireException on a setup error with the 1-Wire
         *         adapter
         */
        public override Boolean CanProgram
        {
            get
            {
                try
                {
                    lock (conn)
                    {
                        // send beginExclusive command
                        conn.WriteByte(NetAdapterConstants.CMD_CANPROGRAM);
                        conn.Flush();

                        // check return value for success
                        CheckReturnValue(conn);

                        // next parameter should be the return from beginExclusive
                        return conn.ReadBoolean();
                    }
                }
                catch (System.Net.Sockets.SocketException ioe)
                {
                    throw new OneWireException(COMM_FAILED + ioe.Message);
                }
            }
        }

        /**
         * Returns whether the adapter can physically support strong 5 volt power
         * mode.
         *
         * @return  <code>true</code> if this port adapter can do strong 5 volt
         * mode, <code>false</code> otherwise.
         *
         * @throws OneWireIOException on a 1-Wire communication error with the adapter
         * @throws OneWireException on a setup error with the 1-Wire
         *         adapter
         */
        public override Boolean CanDeliverPower
        {
            get
            {
                try
                {
                    lock (conn)
                    {
                        // send beginExclusive command
                        conn.WriteByte(NetAdapterConstants.CMD_CANDELIVERPOWER);
                        conn.Flush();

                        // check return value for success
                        CheckReturnValue(conn);

                        // next parameter should be the return from beginExclusive
                        return conn.ReadBoolean();
                    }
                }
                catch (System.Net.Sockets.SocketException ioe)
                {
                    throw new OneWireException(COMM_FAILED + ioe.Message);
                }
            }
        }

        /**
         * Returns whether the adapter can physically support "smart" strong 5
         * volt power mode.  "smart" power delivery is the ability to deliver
         * power until it is no longer needed.  The current drop it detected
         * and power delivery is stopped.
         *
         * @return  <code>true</code> if this port adapter can do "smart" strong
         * 5 volt mode, <code>false</code> otherwise.
         *
         * @throws OneWireIOException on a 1-Wire communication error with the adapter
         * @throws OneWireException on a setup error with the 1-Wire
         *         adapter
         */
        public override Boolean CanDeliverSmartPower
        {
            get
            {
                try
                {
                    lock (conn)
                    {
                        // send beginExclusive command
                        conn.WriteByte(NetAdapterConstants.CMD_CANDELIVERSMARTPOWER);
                        conn.Flush();

                        // check return value for success
                        CheckReturnValue(conn);

                        // next parameter should be the return from beginExclusive
                        return conn.ReadBoolean();
                    }
                }
                catch (System.Net.Sockets.SocketException ioe)
                {
                    throw new OneWireException(COMM_FAILED + ioe.Message);
                }
            }
        }

        /**
         * Returns whether adapter can physically support 0 volt 'break' mode.
         *
         * @return  <code>true</code> if this port adapter can do break,
         * <code>false</code> otherwise.
         *
         * @throws OneWireIOException on a 1-Wire communication error with the adapter
         * @throws OneWireException on a setup error with the 1-Wire
         *         adapter
         */
        public override Boolean CanBreak
        {
            get
            {
                try
                {
                    lock (conn)
                    {
                        // send beginExclusive command
                        conn.WriteByte(NetAdapterConstants.CMD_CANBREAK);
                        conn.Flush();

                        // check return value for success
                        CheckReturnValue(conn);

                        // next parameter should be the return from beginExclusive
                        return conn.ReadBoolean();
                    }
                }
                catch (System.Net.Sockets.SocketException ioe)
                {
                    throw new OneWireException(COMM_FAILED + ioe.Message);
                }
            }
        }

        //--------
        //-------- Finding iButton/1-Wire device options
        //--------

        /**
         * Returns <code>true</code> if the first iButton or 1-Wire device
         * is found on the 1-Wire Network.
         * If no devices are found, then <code>false</code> will be returned.
         *
         * @return  <code>true</code> if an iButton or 1-Wire device is found.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override Boolean GetFirstDevice(byte[] address, int offset)
        {
            try
            {
                lock (conn)
                {
                    // send findFirstDevice command
                    conn.WriteByte(NetAdapterConstants.CMD_FINDFIRSTDEVICE);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // return boolean from findFirstDevice
                    Boolean ret = conn.ReadBoolean();

                    if (ret)
                        GetAddress(address);

                    return ret;




                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Returns <code>true</code> if the next iButton or 1-Wire device
         * is found. The previous 1-Wire device found is used
         * as a starting point in the search.  If no more devices are found
         * then <code>false</code> will be returned.
         *
         * @return  <code>true</code> if an iButton or 1-Wire device is found.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override Boolean GetNextDevice(byte[] address, int offset)
        {
            try
            {
                lock (conn)
                {
                    // send findNextDevice command
                    conn.WriteByte(NetAdapterConstants.CMD_FINDNEXTDEVICE);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // return boolean from findNextDevice
                    Boolean ret = conn.ReadBoolean();

                    if (ret)
                        GetAddress(address);

                    return ret;

                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }


        /**
         * Copies the 'current' 1-Wire device address being used by the adapter into
         * the array.  This address is the last iButton or 1-Wire device found
         * in a search (findNextDevice()...).
         * This method copies into a user generated array to allow the
         * reuse of the buffer.  When searching many iButtons on the one
         * wire network, this will reduce the memory burn rate.
         *
         * @param  address An array to be filled with the current iButton address.
         * @see   com.dalsemi.onewire.utils.Address
         */
        private void GetAddress(byte[] address)
        {
            try
            {
                lock (conn)
                {
                    // send getAddress command
                    conn.WriteByte(NetAdapterConstants.CMD_GETADDRESS);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // get the address
                    conn.Read(address, 0, 8);
                }
            }
            catch (Exception)
            { /* drain */ }
        }

        /**
         * Sets the 1-Wire Network search to find only iButtons and 1-Wire
         * devices that are in an 'Alarm' state that signals a need for
         * attention.  Not all iButton types
         * have this feature.  Some that do: DS1994, DS1920, DS2407.
         * This selective searching can be canceled with the
         * 'setSearchAllDevices()' method.
         *
         * @see #setNoResetSearch
         */
        public override void SetSearchOnlyAlarmingDevices()
        {
            try
            {
                lock (conn)
                {
                    // send setSearchOnlyAlarmingDevices command
                    conn.WriteByte(NetAdapterConstants.CMD_SETSEARCHONLYALARMINGDEVICES);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (Exception)
            { /* drain */ }
        }


        /**
         * Sets the 1-Wire Network search to not perform a 1-Wire
         * reset before a search.  This feature is chiefly used with
         * the DS2409 1-Wire coupler.
         * The normal reset before each search can be restored with the
         * 'setSearchAllDevices()' method.
         */
        public override void SetNoResetSearch()
        {
            try
            {
                lock (conn)
                {
                    // send setNoResetSearch command
                    conn.WriteByte(NetAdapterConstants.CMD_SETNORESETSEARCH);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (Exception)
            { /* drain */ }
        }


        /**
         * Sets the 1-Wire Network search to find all iButtons and 1-Wire
         * devices whether they are in an 'Alarm' state or not and
         * restores the default setting of providing a 1-Wire reset
         * command before each search. (see setNoResetSearch() method).
         *
         * @see #setNoResetSearch
         */
        public override void SetSearchAllDevices()
        {
            try
            {
                lock (conn)
                {
                    // send setSearchAllDevices command
                    conn.WriteByte(NetAdapterConstants.CMD_SETSEARCHALLDEVICES);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (Exception)
            { /* drain */ }
        }


        /**
         * Removes any selectivity during a search for iButtons or 1-Wire devices
         * by family type.  The unique address for each iButton and 1-Wire device
         * contains a family descriptor that indicates the capabilities of the
         * device.
         * @see    #targetFamily
         * @see    #targetFamily(byte[])
         * @see    #excludeFamily
         * @see    #excludeFamily(byte[])
         */
        public override void TargetAllFamilies()
        {
            try
            {
                lock (conn)
                {
                    // send targetAllFamilies command
                    conn.WriteByte(NetAdapterConstants.CMD_TARGETALLFAMILIES);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (Exception)
            { /* drain */ }
        }


        /**
         * Takes an integer to selectively search for this desired family type.
         * If this method is used, then no devices of other families will be
         * found by any of the search methods.
         *
         * @param  family   the code of the family type to target for searches
         * @see   com.dalsemi.onewire.utils.Address
         * @see    #targetAllFamilies
         */
        public override void TargetFamily(int family)
        {
            try
            {
                lock (conn)
                {
                    // send targetFamily command
                    conn.WriteByte(NetAdapterConstants.CMD_TARGETFAMILY);
                    conn.WriteInt(1);
                    conn.WriteByte((byte)family);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (Exception)
            { /* drain */ }
        }

        /**
         * Takes an array of bytes to use for selectively searching for acceptable
         * family codes.  If used, only devices with family codes in this array
         * will be found by any of the search methods.
         *
         * @param  family  array of the family types to target for searches
         * @see   com.dalsemi.onewire.utils.Address
         * @see    #targetAllFamilies
         */
        public override void TargetFamily(byte[] family)
        {
            try
            {
                lock (conn)
                {
                    // send targetFamily command
                    conn.WriteByte(NetAdapterConstants.CMD_TARGETFAMILY);
                    conn.WriteInt(family.Length);
                    conn.Write(family, 0, family.Length);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (Exception)
            { /* drain */ }
        }


        /**
         * Takes an integer family code to avoid when searching for iButtons.
         * or 1-Wire devices.
         * If this method is used, then no devices of this family will be
         * found by any of the search methods.
         *
         * @param  family   the code of the family type NOT to target in searches
         * @see   com.dalsemi.onewire.utils.Address
         * @see    #targetAllFamilies
         */
        public override void ExcludeFamily(int family)
        {
            try
            {
                lock (conn)
                {
                    // send excludeFamily command
                    conn.WriteByte(NetAdapterConstants.CMD_EXCLUDEFAMILY);
                    conn.WriteInt(1);
                    conn.WriteByte((byte)family);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (Exception)
            { /* drain */ }
        }


        /**
         * Takes an array of bytes containing family codes to avoid when finding
         * iButtons or 1-Wire devices.  If used, then no devices with family
         * codes in this array will be found by any of the search methods.
         *
         * @param  family  array of family cods NOT to target for searches
         * @see   com.dalsemi.onewire.utils.Address
         * @see    #targetAllFamilies
         */
        public override void ExcludeFamily(byte[] family)
        {
            try
            {
                lock (conn)
                {
                    // send excludeFamily command
                    conn.WriteByte(NetAdapterConstants.CMD_EXCLUDEFAMILY);
                    conn.WriteInt(family.Length);
                    conn.Write(family, 0, family.Length);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (Exception)
            { /* drain */ }
        }


        //--------
        //-------- 1-Wire Network Semaphore methods
        //--------

        /**
         * Gets exclusive use of the 1-Wire to communicate with an iButton or
         * 1-Wire Device.
         * This method should be used for critical sections of code where a
         * sequence of commands must not be interrupted by communication of
         * threads with other iButtons, and it is permissible to sustain
         * a delay in the special case that another thread has already been
         * granted exclusive access and this access has not yet been
         * relinquished. <p>
         *
         * It can be called through the OneWireContainer
         * class by the end application if they want to ensure exclusive
         * use.  If it is not called around several methods then it
         * will be called inside each method.
         *
         * @param blocking <code>true</code> if want to block waiting
         *                 for an excluse access to the adapter
         * @return <code>true</code> if blocking was false and a
         *         exclusive session with the adapter was aquired
         *
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override Boolean BeginExclusive(Boolean blocking)
        {
            Boolean bGotLocalBlock = false, bGotServerBlock = false;
            if (blocking)
            {
                while (!BeginExclusive())
                {
                    try { System.Threading.Thread.Sleep(50); }
                    catch (Exception) { }
                }

                bGotLocalBlock = true;
            }
            else
                bGotLocalBlock = BeginExclusive();

            try
            {
                lock (conn)
                {
                    // send beginExclusive command
                    conn.WriteByte(NetAdapterConstants.CMD_BEGINEXCLUSIVE);
                    conn.WriteBoolean(blocking);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // next parameter should be the return from beginExclusive
                    bGotServerBlock = conn.ReadBoolean();
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }

            // if blocking, I shouldn'thread get here unless both are true
            return bGotLocalBlock && bGotServerBlock;
        }

        /**
         * Gets exclusive use of the 1-Wire to communicate with an iButton or
         * 1-Wire Device.
         * This method should be used for critical sections of code where a
         * sequence of commands must not be interrupted by communication of
         * threads with other iButtons, and it is permissible to sustain
         * a delay in the special case that another thread has already been
         * granted exclusive access and this access has not yet been
         * relinquished. This is private and non blocking<p>
         *
         * @return <code>true</code> a exclusive session with the adapter was
         *         aquired
         *
         * @throws OneWireException
         */
        private Boolean BeginExclusive()
        {
            lock (currentThreadHashLock)
            {
                if (currentThreadHash == NOT_OWNED)
                {
                    // not owned so take
                    currentThreadHash = System.Threading.Thread.CurrentThread.GetHashCode();

#if DEBUG_NETADAPTER
                    // provided debug on (standard out)
                    Debug.DebugStr("beginExclusive, now owned by: "
                                      + System.Threading.Thread.CurrentThread.Name);
#endif

                    return true;
                }
                else if (currentThreadHash
                         == System.Threading.Thread.CurrentThread.GetHashCode())
                {
#if DEBUG_NETADAPTER
                    // provided debug on (standard out)
                    Debug.DebugStr("beginExclusive, already owned by: "
                                      + System.Threading.Thread.CurrentThread.Name);
#endif

                    // already own
                    return true;
                }
                else
                {
                    // want port but don'thread own
                    return false;
                }
            }
        }

        /**
         * Relinquishes exclusive control of the 1-Wire Network.
         * This command dynamically marks the end of a critical section and
         * should be used when exclusive control is no longer needed.
         */
        public override void EndExclusive()
        {
            lock (currentThreadHashLock)
            {
                // if own then release
                if (currentThreadHash != NOT_OWNED &&
                    currentThreadHash == System.Threading.Thread.CurrentThread.GetHashCode())
                {
#if DEBUG_NETADAPTER
                    Debug.DebugStr("endExclusive, was owned by: "
                                      + System.Threading.Thread.CurrentThread.Name);
#endif

                    currentThreadHash = NOT_OWNED;
                    try
                    {
                        lock (conn)
                        {
                            // send endExclusive command
                            conn.WriteByte(NetAdapterConstants.CMD_ENDEXCLUSIVE);
                            conn.Flush();

                            // check return value for success
                            CheckReturnValue(conn);
                        }
                    }
                    catch (Exception)
                    { /* drain */ }
                }
            }
        }

        //--------
        //-------- Primitive 1-Wire Network data methods
        //--------

        /**
         * Sends a Reset to the 1-Wire Network.
         *
         * @return  the result of the reset. Potential results are:
         * <ul>
         * <li> 0 (RESET_NOPRESENCE) no devices present on the 1-Wire Network.
         * <li> 1 (RESET_PRESENCE) normal presence pulse detected on the 1-Wire
         *        Network indicating there is a device present.
         * <li> 2 (RESET_ALARM) alarming presence pulse detected on the 1-Wire
         *        Network indicating there is a device present and it is in the
         *        alarm condition.  This is only provided by the DS1994/DS2404
         *        devices.
         * <li> 3 (RESET_SHORT) inticates 1-Wire appears shorted.  This can be
         *        transient conditions in a 1-Wire Network.  Not all adapter types
         *        can detect this condition.
         * </ul>
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override OWResetResult Reset()
        {
            try
            {
                lock (conn)
                {
                    // send reset command
                    conn.WriteByte(NetAdapterConstants.CMD_RESET);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // next parameter should be the return from reset
                    return (OWResetResult)conn.ReadInt();
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Sends a bit to the 1-Wire Network.
         *
         * @param  bitValue  the bit value to send to the 1-Wire Network.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override void PutBit(Boolean bitValue)
        {
            try
            {
                lock (conn)
                {
                    // send putBit command
                    conn.WriteByte(NetAdapterConstants.CMD_PUTBIT);
                    // followed by the bit
                    conn.WriteBoolean(bitValue);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Gets a bit from the 1-Wire Network.
         *
         * @return  the bit value recieved from the the 1-Wire Network.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override Boolean GetBit()
        {
            try
            {
                lock (conn)
                {
                    // send getBit command
                    conn.WriteByte(NetAdapterConstants.CMD_GETBIT);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // next parameter should be the return from getBit
                    return conn.ReadBoolean();
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Sends a byte to the 1-Wire Network.
         *
         * @param  byteValue  the byte value to send to the 1-Wire Network.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override void PutByte(int byteValue)
        {
            try
            {
                lock (conn)
                {
                    // send putByte command
                    conn.WriteByte(NetAdapterConstants.CMD_PUTBYTE);
                    // followed by the byte
                    conn.WriteByte((byte)byteValue);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Gets a byte from the 1-Wire Network.
         *
         * @return  the byte value received from the the 1-Wire Network.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override byte GetByte()
        {
            try
            {
                lock (conn)
                {
                    // send getByte command
                    conn.WriteByte(NetAdapterConstants.CMD_GETBYTE);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // next parameter should be the return from getByte
                    return conn.ReadByte();
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Gets a block of data from the 1-Wire Network.
         *
         * @param  len  length of data bytes to receive
         *
         * @return  the data received from the 1-Wire Network.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override byte[] GetBlock(int len)
        {
            byte[] buffer = new byte[len];
            GetBlock(buffer, 0, len);
            return buffer;
        }

        /**
         * Gets a block of data from the 1-Wire Network and write it into
         * the provided array.
         *
         * @param  arr     array in which to write the received bytes
         * @param  len     length of data bytes to receive
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override void GetBlock(byte[] arr, int len)
        {
            GetBlock(arr, 0, len);
        }

        /**
         * Gets a block of data from the 1-Wire Network and write it into
         * the provided array.
         *
         * @param  arr     array in which to write the received bytes
         * @param  off     offset into the array to start
         * @param  len     length of data bytes to receive
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override void GetBlock(byte[] arr, int off, int len)
        {
            try
            {
                lock (conn)
                {
                    // send getBlock command
                    conn.WriteByte(NetAdapterConstants.CMD_GETBLOCK);
                    // followed by the number of bytes to get
                    conn.WriteInt(len);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // next should be the bytes
                    conn.ReadFully(arr, off, len);
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Sends a block of data and returns the data received in the same array.
         * This method is used when sending a block that contains reads and writes.
         * The 'read' portions of the data block need to be pre-loaded with 0xFF's.
         * It starts sending data from the index at offset 'off' for length 'len'.
         *
         * @param  dataBlock array of data to transfer to and from the 1-Wire Network.
         * @param  off       offset into the array of data to start
         * @param  len       length of data to send / receive starting at 'off'
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override void DataBlock(byte[] dataBlock, int off, int len)
        {
#if DEBUG_NETADAPTER
            Debug.DebugStr("DataBlock called for " + len + " bytes");
#endif
            try
            {
                lock (conn)
                {
                    // send dataBlock command
                    conn.WriteByte(NetAdapterConstants.CMD_DATABLOCK);
                    // followed by the number of bytes to block
                    conn.WriteInt(len);
                    // followed by the bytes
                    conn.Write(dataBlock, off, len);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // next should be the bytes returned
                    conn.ReadFully(dataBlock, off, len);
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
#if DEBUG_NETADAPTER
            Debug.DebugStr("   Done DataBlocking");
#endif
        }

        //--------
        //-------- 1-Wire Network power methods
        //--------

        /**
         * Sets the duration to supply power to the 1-Wire Network.
         * This method takes a time parameter that indicates the program
         * pulse length when the method startPowerDelivery().<p>
         *
         * Note: to avoid getting an exception,
         * use the canDeliverPower() and canDeliverSmartPower()
         * method to check it's availability. <p>
         *
         * @param timeFactor
         * <ul>
         * <li>   0 (DELIVERY_HALF_SECOND) provide power for 1/2 second.
         * <li>   1 (DELIVERY_ONE_SECOND) provide power for 1 second.
         * <li>   2 (DELIVERY_TWO_SECONDS) provide power for 2 seconds.
         * <li>   3 (DELIVERY_FOUR_SECONDS) provide power for 4 seconds.
         * <li>   4 (DELIVERY_SMART_DONE) provide power until the
         *          the device is no longer drawing significant power.
         * <li>   5 (DELIVERY_INFINITE) provide power until the
         *          setPowerNormal() method is called.
         * </ul>
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override void SetPowerDuration(OWPowerTime timeFactor)
        {
            try
            {
                lock (conn)
                {
                    // send setPowerDuration command
                    conn.WriteByte(NetAdapterConstants.CMD_SETPOWERDURATION);
                    // followed by the timeFactor
                    conn.WriteInt((int)timeFactor);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Sets the 1-Wire Network voltage to supply power to a 1-Wire device.
         * This method takes a time parameter that indicates whether the
         * power delivery should be done immediately, or after certain
         * conditions have been met. <p>
         *
         * Note: to avoid getting an exception,
         * use the canDeliverPower() and canDeliverSmartPower()
         * method to check it's availability. <p>
         *
         * @param changeCondition
         * <ul>
         * <li>   0 (CONDITION_NOW) operation should occur immediately.
         * <li>   1 (CONDITION_AFTER_BIT) operation should be pending
         *           execution immediately after the next bit is sent.
         * <li>   2 (CONDITION_AFTER_BYTE) operation should be pending
         *           execution immediately after next byte is sent.
         * </ul>
         *
         * @return <code>true</code> if the voltage change was successful,
         * <code>false</code> otherwise.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override Boolean StartPowerDelivery(OWPowerStart changeCondition)
        {
            try
            {
                lock (conn)
                {
                    // send startPowerDelivery command
                    conn.WriteByte(NetAdapterConstants.CMD_STARTPOWERDELIVERY);
                    // followed by the changeCondition
                    conn.WriteInt((int)changeCondition);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // and get the return value from startPowerDelivery
                    return conn.ReadBoolean();
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Sets the duration for providing a program pulse on the
         * 1-Wire Network.
         * This method takes a time parameter that indicates the program
         * pulse length when the method startProgramPulse().<p>
         *
         * Note: to avoid getting an exception,
         * use the canDeliverPower() method to check it's
         * availability. <p>
         *
         * @param timeFactor
         * <ul>
         * <li>   7 (DELIVERY_EPROM) provide program pulse for 480 microseconds
         * <li>   5 (DELIVERY_INFINITE) provide power until the
         *          setPowerNormal() method is called.
         * </ul>
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         */
        public override void SetProgramPulseDuration(OWPowerTime timeFactor)
        {
            try
            {
                lock (conn)
                {
                    // send setProgramPulseDuration command
                    conn.WriteByte(NetAdapterConstants.CMD_SETPROGRAMPULSEDURATION);
                    // followed by the timeFactor
                    conn.WriteInt((int)timeFactor);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Sets the 1-Wire Network voltage to eprom programming level.
         * This method takes a time parameter that indicates whether the
         * power delivery should be done immediately, or after certain
         * conditions have been met. <p>
         *
         * Note: to avoid getting an exception,
         * use the canProgram() method to check it's
         * availability. <p>
         *
         * @param changeCondition
         * <ul>
         * <li>   0 (CONDITION_NOW) operation should occur immediately.
         * <li>   1 (CONDITION_AFTER_BIT) operation should be pending
         *           execution immediately after the next bit is sent.
         * <li>   2 (CONDITION_AFTER_BYTE) operation should be pending
         *           execution immediately after next byte is sent.
         * </ul>
         *
         * @return <code>true</code> if the voltage change was successful,
         * <code>false</code> otherwise.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         *         or the adapter does not support this operation
         */
        public override Boolean StartProgramPulse(OWPowerStart changeCondition)
        {
            try
            {
                lock (conn)
                {
                    // send startProgramPulse command
                    conn.WriteByte(NetAdapterConstants.CMD_STARTPROGRAMPULSE);
                    // followed by the changeCondition
                    conn.WriteInt((int)changeCondition);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);

                    // and get the return value from startPowerDelivery
                    return conn.ReadBoolean();
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        /**
         * Sets the 1-Wire Network voltage to 0 volts.  This method is used
         * rob all 1-Wire Network devices of parasite power delivery to force
         * them into a hard reset.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         *         or the adapter does not support this operation
         */
        public override void StartBreak()
        {
            try
            {
                lock (conn)
                {
                    // send startBreak command
                    conn.WriteByte(NetAdapterConstants.CMD_STARTBREAK);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                throw new OneWireException(COMM_FAILED);
            }
        }

        /**
         * Sets the 1-Wire Network voltage to normal level.  This method is used
         * to disable 1-Wire conditions created by startPowerDelivery and
         * startProgramPulse.  This method will automatically be called if
         * a communication method is called while an outstanding power
         * command is taking place.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         *         or the adapter does not support this operation
         */
        public override void SetPowerNormal()
        {
            try
            {
                lock (conn)
                {
                    // send startBreak command
                    conn.WriteByte(NetAdapterConstants.CMD_SETPOWERNORMAL);
                    conn.Flush();

                    // check return value for success
                    CheckReturnValue(conn);
                }
            }
            catch (System.Net.Sockets.SocketException ioe)
            {
                throw new OneWireException(COMM_FAILED + ioe.Message);
            }
        }

        //--------
        //-------- 1-Wire Network speed methods
        //--------

        /**
         * Sets the new speed of data
         * transfer on the 1-Wire Network. <p>
         *
         * @param speed
         * <ul>
         * <li>     0 (SPEED_REGULAR) set to normal communciation speed
         * <li>     1 (SPEED_FLEX) set to flexible communciation speed used
         *            for long lines
         * <li>     2 (SPEED_OVERDRIVE) set to normal communciation speed to
         *            overdrive
         * <li>     3 (SPEED_HYPERDRIVE) set to normal communciation speed to
         *            hyperdrive
         * <li>    >3 future speeds
         * </ul>
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         *         or the adapter does not support this operation
         */
        public override OWSpeed Speed
        {
            set
            {
                try
                {
                    lock (conn)
                    {
                        // send startBreak command
                        conn.WriteByte(NetAdapterConstants.CMD_SETSPEED);
                        // followed by the speed
                        conn.WriteInt((int)value);
                        conn.Flush();

                        // check return value for success
                        CheckReturnValue(conn);
                    }
                }
                catch (System.Net.Sockets.SocketException ioe)
                {
                    throw new OneWireException(COMM_FAILED + ioe.Message);
                }
            }
            get
            {
                try
                {
                    lock (conn)
                    {
                        // send startBreak command
                        conn.WriteByte(NetAdapterConstants.CMD_GETSPEED);
                        conn.Flush();

                        // check return value for success
                        CheckReturnValue(conn);

                        // and return the return value from getSpeed()
                        return (OWSpeed)conn.ReadInt();
                    }
                }
                catch (Exception)
                {
                    /* drain */
                }

                return OWSpeed.SPEED_REGULAR; // was: -1
            }
        }

        /**
         * Returns the current data transfer speed on the 1-Wire Network. <p>
         *
         * @return <code>int</code> representing the current 1-Wire speed
         * <ul>
         * <li>     0 (SPEED_REGULAR) set to normal communication speed
         * <li>     1 (SPEED_FLEX) set to flexible communication speed used
         *            for long lines
         * <li>     2 (SPEED_OVERDRIVE) set to normal communication speed to
         *            overdrive
         * <li>     3 (SPEED_HYPERDRIVE) set to normal communication speed to
         *            hyperdrive
         * <li>    >3 future speeds
         * </ul>
         */




        #region Ralph Maas / RM

        /// <summary> Verifies that the iButton or 1-Wire device specified is present on
        /// the 1-Wire Network. This does not affect the 'current' device
        /// state information used in searches (findNextDevice...).
        /// </summary>
        /// <param name="address"> device address to verify is present
        /// </param>
        /// <returns>  <code>true</code> if device is present else
        /// <code>false</code>.
        /// </returns>
        public override bool IsPresent(byte[] address, int offset)
        {
            Reset();
            PutByte(0xF0);   // Search ROM command

            byte[] addr = new byte[8];
            Array.Copy(address, offset, addr, 0, 8);
            return StrongAccess(addr);
        }



        // START: PROBABLY MOVE TO PORTADAPTER
        /**
         * Writes the bit state in a byte array.
         *
         * @param state new state of the bit 1, 0
         * @param index bit index into byte array
         * @param buf byte array to manipulate
         */
        private void ArrayWriteBit(int state, int index, byte[] buf)
        {
            int nbyt = (index >> 3);
            int nbit = index - (nbyt << 3);

            if (state == 1)
                buf[nbyt] |= (byte)(0x01 << nbit);
            else
            {
                // TODO: test
                byte b = (byte)(0x01 << nbit);
                buf[nbyt] &= (byte)(~b);
            }
        }

        /**
         * Reads a bit state in a byte array.
         *
         * @param index bit index into byte array
         * @param buf byte array to read from
         *
         * @return bit state 1 or 0
         */
        private int ArrayReadBit(int index, byte[] buf)
        {
            int nbyt = (index >> 3);
            int nbit = index - (nbyt << 3);

            return ((buf[nbyt] >> nbit) & 0x01);
        }

        /**
         * Performs a 'strongAccess' with the provided 1-Wire address.
         * 1-Wire Network has already been reset and the 'search'
         * command sent before this is called.
         *
         * @param  address  device address to do strongAccess on
         *
         * @return  true if device participated and was present
         *         in the strongAccess search
         */
        private Boolean StrongAccess(byte[] address)
        {
            byte[] send_packet = new byte[24];
            int i;

            // set all bits at first
            for (i = 0; i < 24; i++)
                send_packet[i] = (byte)0xFF;

            // now set or clear apropriate bits for search
            for (i = 0; i < 64; i++)
                ArrayWriteBit(ArrayReadBit(i, address), (i + 1) * 3 - 1,
                              send_packet);

            // send to 1-Wire Net
            DataBlock(send_packet, 0, 24);

            // check the results of last 8 triplets (should be no conflicts)
            int cnt = 56, goodbits = 0, tst, s;

            for (i = 168; i < 192; i += 3)
            {
                tst = (ArrayReadBit(i, send_packet) << 1)
                      | ArrayReadBit(i + 1, send_packet);
                s = ArrayReadBit(cnt++, address);

                if (tst == 0x03)   // no device on line
                {
                    goodbits = 0;   // number of good bits set to zero

                    break;          // quit
                }

                if (((s == 0x01) && (tst == 0x02)) || ((s == 0x00) && (tst == 0x01)))   // correct bit
                    goodbits++;   // count as a good bit
            }

            // check too see if there were enough good bits to be successful
            return (goodbits >= 8);
        }
        // END: PROBABLY MOVE TO PORTADAPTER


        /**
         * Verifies that the iButton or 1-Wire device specified is present
         * on the 1-Wire Network and in an alarm state. This does not
         * affect the 'current' device state information used in searches
         * (findNextDevice...).
         *
         * @param  address  device address to verify is present and alarming
         *
         * @return  <code>true</code> if device is present and alarming, else
         * <code>false</code>.
         *
         * @throws OneWireIOException on a 1-Wire communication error
         * @throws OneWireException on a setup error with the 1-Wire adapter
         *
         * @see   com.dalsemi.onewire.utils.Address
         */
        public override bool IsAlarming(byte[] address, int offset)
        {
            Reset();
            PutByte(0xEC);   // Conditional search commands

            byte[] addr = new byte[8];
            Array.Copy(address, offset, addr, 0, 8);
            return StrongAccess(addr);
        }

        #endregion

    }
}
