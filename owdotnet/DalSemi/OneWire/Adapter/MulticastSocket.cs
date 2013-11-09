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

    public class DatagramPacket
    {
        public byte[] buffer;
        public EndPoint RemoteEndPoint;

        public int Length
        {
            get
            {
                return buffer.Length;
            }
        }

        public byte[] GetData()
        {
            return buffer;
        }

        public IPEndPoint GetAddress()
        {
            return (IPEndPoint)RemoteEndPoint;
        }

    }

    public class MulticastSocket : IDisposable
    {

        internal class StateObject
        {
            public const int BufferSize = 1024;
            public Socket workSocket;
            public byte[] buffer = new byte[BufferSize];
            public MulticastSocket Multicaster = null;
        }

        public List<DatagramPacket> dgpl;

        //Socket creation, regular UDP socket 
        private Socket UDPSocket = new
                      Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private string Target_IP = "224.10.10.10";
        private int Target_Port = 31337;
        private Boolean ASync = false;

        //socket initialization 
        public MulticastSocket(int target_Port)
            : this(target_Port, false)
        {
        }

        public MulticastSocket(int target_Port, Boolean aSync)
        {
            Target_Port = target_Port;
            ASync = aSync;
            //nothing should go wrong in here 
            try
            {
                if (aSync)
                    dgpl = new List<DatagramPacket>();

                //recieve data from any source 
                IPEndPoint LocalHostIPEnd = new IPEndPoint(IPAddress.Any, Target_Port);

                //init Socket properties:
                UDPSocket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, 1);

                //allow for loopback testing 
                UDPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

                //extremly important to bind the Socket before joining multicast groups 
                UDPSocket.Bind(LocalHostIPEnd);

                //set multicast flags, sending flags - TimeToLive (TTL) 
                // 0 - LAN 
                // 1 - Single Router Hop 
                // 2 - Two Router Hops... 

                UDPSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive,
                 0); // TODO: how about this field ???

                //get in waiting mode for data - always (this doesn'thread halt code execution) 
                if (aSync)
                    StartRecieve();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " " + ex.StackTrace);
            }
        }

        ~MulticastSocket()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(Boolean disposing)
        {
            UDPSocket.Close();
            UDPSocket = null;
        }

        public void SetSoTimeout(int to)
        {
            UDPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout,
                to);
        }

        public void JoinGroup(string target_IP)
        {
            JoinGroup(IPAddress.Parse(target_IP));
        }

        public void JoinGroup(IPAddress target_IP)
        {
            Target_IP = target_IP.ToString();
            // join multicast group 
            UDPSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                new MulticastOption(target_IP));
        }

        public void LeaveGroup(IPAddress target_IP)
        {
            Target_IP = "";
            // leave multicast group 
            UDPSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership,
                new MulticastOption(target_IP));
        }

        //client send function 
        public void Send(byte[] bytesToSend)
        {
            //set the target IP 
            IPEndPoint RemoteIPEndPoint = new IPEndPoint(IPAddress.Parse(Target_IP), Target_Port);
            EndPoint RemoteEndPoint = (EndPoint)RemoteIPEndPoint;

            //do asynchronous send 
            if (ASync)
                UDPSocket.BeginSendTo(bytesToSend, 0, bytesToSend.Length, SocketFlags.None, RemoteEndPoint,
                                    new AsyncCallback(SendCallback), UDPSocket);
            else
                UDPSocket.SendTo(bytesToSend, 0, bytesToSend.Length, SocketFlags.None, RemoteEndPoint);
        }

        //executes the asynchronous send 
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object. 
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device. 
                int bytesSent = client.EndSendTo(ar);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        public DatagramPacket Receive()
        {
            if (ASync)
            {
                lock (dgpl)
                {
                    if (dgpl.Count < 1)
                        new SocketException(10060); // time-out
                    DatagramPacket curr = dgpl[0];
                    dgpl.RemoveAt(0);
                    return curr;
                }
            }
            else
            {
                IPEndPoint LocalIPEndPoint = new IPEndPoint(IPAddress.Any, Target_Port);
                EndPoint LocalEndPoint = (EndPoint)LocalIPEndPoint;

                // Create the state object. 
                byte[] buffer = new byte[StateObject.BufferSize];

                // Begin receiving the data from the remote device. 

                int bytesRead = UDPSocket.ReceiveFrom(buffer, 0, buffer.Length, 0, ref LocalEndPoint);

                if (bytesRead == 0)
                    throw new SocketException(10060); // time-out

                DatagramPacket dgp = new DatagramPacket();
                dgp.buffer = new byte[bytesRead];
                Array.Copy(buffer, dgp.buffer, bytesRead);
                dgp.RemoteEndPoint = LocalEndPoint;

                return dgp;
            }
        }

        //initial receive function - called only once 
        private void StartRecieve()
        {
            try
            {
                IPEndPoint LocalIPEndPoint = new IPEndPoint(IPAddress.Any, Target_Port);
                EndPoint LocalEndPoint = (EndPoint)LocalIPEndPoint;

                // Create the state object. 
                StateObject state = new StateObject();
                state.Multicaster = this;
                state.workSocket = UDPSocket;

                // Begin receiving the data from the remote device. 

                UDPSocket.BeginReceiveFrom(state.buffer, 0, StateObject.BufferSize, 0, ref LocalEndPoint, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        //executes the asynchronous receive - executed everytime data is received on the port 
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                IPEndPoint LocalIPEndPoint = new IPEndPoint(IPAddress.Any, Target_Port);
                EndPoint LocalEndPoint = (EndPoint)LocalIPEndPoint;

                // Retrieve the state object and the client socket 
                // from the async state object. 
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device. 
                int bytesRead = client.EndReceiveFrom(ar, ref LocalEndPoint);

                if (bytesRead != 0)
                {
                    DatagramPacket dgp = new DatagramPacket();
                    dgp.buffer = new byte[bytesRead];
                    Array.Copy(state.buffer, dgp.buffer, bytesRead);
                    dgp.RemoteEndPoint = LocalEndPoint;

                    lock (state.Multicaster.dgpl)
                    {
                        state.Multicaster.dgpl.Add(dgp);
                        //byte[] ba = new byte[bytesRead];
                        //Array.Copy(state.buffer, ba, bytesRead);
                    }
                }

                //keep listening 
                client.BeginReceiveFrom(state.buffer, 0, StateObject.BufferSize, 0, ref LocalEndPoint,
                                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }


}
