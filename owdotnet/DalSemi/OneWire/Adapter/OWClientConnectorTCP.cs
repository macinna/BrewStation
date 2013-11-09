// This file is distributed as part of the open source OWdotNET project.
// Project pages: https://sourceforge.net/projects/owdotnet
// Web Site:      http://owdotnet.sourceforge.net/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace DalSemi.OneWire.Adapter
{

    public sealed class OWClientConnectorTCP : OWClientConnector
    {

        private Socket socClient = null;

        //-------
        //------- Multicast variables
        //-------

        static private Boolean asyncMulticast = false;

        /** The multicast group to use for NetAdapter Datagram packets */
        static private string multicastGroup = null;

        /** The port to use for NetAdapter Datagram packets */
        static private int datagramPort = -1;

        private OWClientConnectorTCP() // only used by Clone()
        {
        }

        public OWClientConnectorTCP(Socket socket)
        {
            if (!socket.Connected)
                throw new Exception("Socket has to be connected to remote endpoint");
            socClient = socket;
        }

        public OWClientConnectorTCP(string hostName, int port)
        {
            //create a new client socket ...
            socClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                IPHostEntry he = Dns.GetHostEntry(hostName);
                if (he.AddressList.Length < 1)
                    throw new SocketException(11001); // host not found
                IPAddress remoteIPAddress = IPAddress.Parse(he.AddressList[0].ToString());
                IPEndPoint remoteEndPoint = new IPEndPoint(remoteIPAddress, port);
                socClient.Connect(remoteEndPoint); // make sure connected
            }
            catch
            {
                socClient = null;
                throw;
            }
        }

        public override byte ReadByte()
        {
            byte[] ba = new byte[1];
            if (socClient.Receive(ba, 0, 1, SocketFlags.None) != 1)
                throw new Exception("Cannot recieve 1 byte");
            return ba[0];
        }

        public override string ReadUTF()
        {
            // we have to do some UTF decoding...
            StringBuilder sb = new StringBuilder();
            int length = (ReadByte() << 8) + ReadByte();
            byte[] ba = new byte[length];
            for (int i = 0; i < length; i++)
                ba[i] = ReadByte();
            return System.Text.UTF8Encoding.UTF8.GetString(ba);
        }

        public override int ReadInt()
        {
            // MSB !!!
            byte[] ba = new byte[4];
            if (Read(ba, 0, 4) != 4)
                throw new SocketException(10060); // Connection timed out
            return ba[3] + (ba[2] << 8) + (ba[1] << 16) + (ba[0] << 24);
            //return DalSemi.OneWire.Utils.Convert.ToInt(ba); // wrong way around!!!
        }

        public override Boolean ReadBoolean()
        {
            return (ReadByte() != 0);
        }

        public override int Read(byte[] b, int off, int len)
        {
            return socClient.Receive(b, off, len, SocketFlags.None);
        }

        public override void ReadFully(byte[] b, int off, int len)
        {
            if (socClient.Receive(b, off, len, SocketFlags.None) != len)
                throw new SocketException(10060); // Connection timed out
        }

        public override void WriteByte(byte b)
        {
            socClient.Send(new byte[] { b });
        }

        public override void WriteUTF(string s)
        {
            // we have to do some UTF encoding...
            byte[] ba = System.Text.UTF8Encoding.UTF8.GetBytes(s);
            int i = ba.Length;
            // write length first
            Write(new byte[] { (byte)((i & 0xFF00) >> 8), (byte)(i & 0xFF) }, 0, 2);
            // then encoded string
            Write(ba, 0, i);
        }

        public override void WriteInt(int v)
        {
            // MSB !!!
            byte[] ba = new byte[4];

            ba[0] = (byte)(v >> 24);
            ba[1] = (byte)((v >> 16) & 0xFF);
            ba[2] = (byte)((v >> 8) & 0xFF);
            ba[3] = (byte)(v & 0xFF);

            Write(ba, 0, 4);
        }

        public override void WriteBoolean(Boolean b)
        {
            WriteByte(b ? (byte)0xFF : (byte)0);
        }

        public override void Write(byte[] b, int off, int len)
        {
            socClient.Send(b, off, len, SocketFlags.None);
        }

        public override void Flush()
        {
            // not supported
            // how does java do this ???
        }

        private void Close()
        {
            socClient.Close(); // Shutdown / Disconnect(true);
            socClient = null;
        }

        public override OWClientConnector Clone()
        {
            OWClientConnectorTCP cc = new OWClientConnectorTCP();
            cc.socClient = this.socClient; // share the socket
            return cc;
        }

        public override string PortNameForReconnect
        {
            get
            {
                IPEndPoint ep = (IPEndPoint)socClient.RemoteEndPoint;
                return ep.Address.ToString() + ":" + ep.Port.ToString();
            }
        }

        public override void Dispose(Boolean disposing)
        {
            if (socClient != null)
                Close();
        }

        public override OWClientConnector TransferControl(OWClientConnector newConnector)
        {
            // I (this) will be discarded and the newConnector will do the work...
            OWClientConnectorTCP cc = newConnector as OWClientConnectorTCP;
            if (cc == null)
                throw new ArgumentException("New connector is not of type " + this.GetType().Name);
            if (cc.socClient == null)
                cc.socClient = socClient;
            else
                if (socClient != cc.socClient)
                    throw new ArgumentException("New connector does not share the socket");
            socClient = null; // transfered control of the socket to the newConnector
            Dispose();
            return newConnector;
        }

        public override void SetSoTimeout(int msTimeout)
        {
            socClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout,
                msTimeout);
        }

        static public void GetMulticastPortNames(List<string> sl)
        {

            // figure out what the datagram listen port is
            if (datagramPort == -1)
            {
                String strPort = null;
                try
                {
                    strPort = AccessProvider.GetProperty("NetAdapter.MulticastPort");
                }
                catch (Exception) { ;}
                if (strPort == null)
                    datagramPort = NetAdapterConstants.DEFAULT_MULTICAST_PORT;
                else
                    datagramPort = int.Parse(strPort);
            }

            // figure out what the multicast group is
            if (multicastGroup == null)
            {
                String group = null;
                try
                {
                    group = AccessProvider.GetProperty("NetAdapter.MulticastGroup");
                }
                catch (Exception) { ;}
                if (group == null)
                    multicastGroup = NetAdapterConstants.DEFAULT_MULTICAST_GROUP;
                else
                    multicastGroup = group;
            }

            //MulticastSocket socket = null;
            //                             InetAddress group = null;

            // create the multi-cast socket
            using (MulticastSocket socket = new MulticastSocket(datagramPort, asyncMulticast))
            {
                try
                {
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
#if DEBUG_OWCLIENTCONNECTORTCP
                    Debug.DebugStr("DEBUG: Opening multicast on port: " + datagramPort);
                    Debug.DebugStr("DEBUG: joining group: " + multicastGroup);
#endif
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

                    // create the multi-cast socket
                    //socket = new MulticastSocket(multicastGroup, datagramPort);
                    // create the group's InetAddress
                    //                                group = InetAddress.getByName(multicastGroup);
                    IPAddress multicastGroupIP = IPAddress.Parse(multicastGroup);
                    // join the group
                    //                                socket.joinGroup(group);
                    socket.JoinGroup(multicastGroupIP);
                    try
                    {

                        //                        socket.Target_IP = multicastGroup;

                        // convert the versionUID to a byte[]
                        byte[] versionBytes = DalSemi.OneWire.Utils.Convert.ToByteArray(NetAdapterConstants.versionUID);

                        // send a packet with the versionUID
                        //DatagramPacket outPacket
                        // = new DatagramPacket(versionBytes, 4, group, datagramPort);
                        socket.Send(versionBytes /* outPacket */);

                        // set a timeout of 1/2 second for the receive
                        if (asyncMulticast)
                            System.Threading.Thread.Sleep(500);  // TODO: time-out field
                        else
                            socket.SetSoTimeout(500);  // TODO: time-out field

                        //byte[] receiveBuffer = new byte[32];
                        for (; ; )
                        {
                            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
#if DEBUG_OWCLIENTCONNECTORTCP
                            Debug.DebugStr("DEBUG: waiting for multicast packet");
#endif
                            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                            //                                   DatagramPacket inPacket
                            //                                      = new DatagramPacket(receiveBuffer, receiveBuffer.length);
                            DatagramPacket inPacket = socket.Receive();/* inPacket */

                            int length = inPacket.Length;
                            byte[] data = inPacket.GetData();
                            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
#if DEBUG_OWCLIENTCONNECTORTCP
                            Debug.DebugStr("DEBUG: packet.length=" + length);
                            Debug.DebugStr("DEBUG: expecting=" + 5);
#endif
                            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                            if (length == 5 && data[4] == NetAdapterConstants.VERSION_UID_TERMINATOR)
                            {
                                int listenPort = DalSemi.OneWire.Utils.Convert.ToInt(data, 0, 4);
                                sl.Add(inPacket.GetAddress().Address.ToString() // inPacket.getAddress().getHostName()
                                      + ":" + listenPort.ToString());
                            }
                        }
                    }
                    finally
                    {
                        socket.LeaveGroup(multicastGroupIP);
                    }
                }
                catch (Exception)
                { ;} // drain
                //finally
                //{
                //    try
                //    {
                //        //                                   socket.leaveGroup(group);
                //        //                                   socket.close();
                //    }
                //    catch (Exception)
                //    { ;} // drain
                //}
            }

        }

    }

}
