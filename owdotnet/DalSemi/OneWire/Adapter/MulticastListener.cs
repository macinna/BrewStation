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

namespace DalSemi.OneWire.Adapter
{

    /**
     * Generic Mulitcast broadcast listener.  Listens for a specific message and,
     * in response, gives the specified reply.  Used by NetAdapterHost for
     * automatic discovery of host components for the network-based DSPortAdapter.
     *
     * @author SH
     * @version 1.00
     */

    public class MulticastListener //  implements Runnable
    {

        /** boolean flag to turn on debug messages */
        //private const Boolean DEBUG = true; // TODO: remove

        /** timeout for socket receive */
        private const int timeoutInSeconds = 3;

        /** multicast socket to receive datagram packets on */
        private MulticastSocket socket = null;
        /** the message we're expecting to receive on the multicast socket */
        private byte[] expectedMessage;
        /** the message we should reply with when we get the expected message */
        private byte[] returnMessage;

        /** boolean to stop the thread from listening for messages */
        private volatile Boolean listenerStopped = false;
        /** boolean to check if the thread is still running */
        private volatile Boolean listenerRunning = false;

        /**
         * Creates a multicast listener on the specified multicast port,
         * bound to the specified multicast group.  Whenever the byte[]
         * pattern specified by "expectedMessage" is received, the byte[]
         * pattern specifed by "returnMessage" is sent to the sender of
         * the "expected message".
         *
         * @param multicastPort Port to bind this listener to.
         * @param multicastGroup Group to bind this listener to.
         * @param expectedMessage the message to look for
         * @param returnMessage the message to reply with
         */
        public MulticastListener(int multicastPort, String multicastGroup,
                                byte[] expectedMessage, byte[] returnMessage)
        {
            this.expectedMessage = expectedMessage;
            this.returnMessage = returnMessage;

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_MULTICASTLISTENER
            Console.Out.WriteLine("DEBUG: Creating Multicast Listener");
            Console.Out.WriteLine("DEBUG:    Multicast port: " + multicastPort);
            Console.Out.WriteLine("DEBUG:    Multicast group: " + multicastGroup);
#endif
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

            // create multicast socket
            socket = new MulticastSocket(multicastPort);
            // set timeout at 3 seconds
            socket.SetSoTimeout(timeoutInSeconds * 1000);
            //join the multicast group
            socket.JoinGroup(IPAddress.Parse(multicastGroup));//InetAddress group = InetAddress.getByName(multicastGroup);
        }

        /**
         * Run method waits for Multicast packets with the specified contents
         * and replies with the specified message.
         */
        public void Run()
        {
            byte[] receiveBuffer; //  = new byte[expectedMessage.Length];

            listenerRunning = true;
            while (!listenerStopped)
            {
                try
                {
                    // packet for receiving messages
                    //DatagramPacket inPacket = new DatagramPacket(receiveBuffer,
                    //                                       receiveBuffer.Length);
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_MULTICASTLISTENER
                    Console.Out.WriteLine("DEBUG: waiting for multicast packet");
#endif
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
                    // blocks for message until timeout occurs
                    DatagramPacket inPacket = socket.Receive(/* inPacket */);

                    // check to see if the received data matches the expected message
                    int length = inPacket.Length;
                    receiveBuffer = inPacket.GetData();

                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_MULTICASTLISTENER
                    Console.Out.WriteLine("DEBUG: packet.length=" + length);
                    Console.Out.WriteLine("DEBUG: expecting=" + expectedMessage.Length);
#endif
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

                    if (length == expectedMessage.Length)
                    {
                        Boolean dataMatch = true;
                        for (int i = 0; dataMatch && i < length; i++)
                        {
                            dataMatch = (expectedMessage[i] == receiveBuffer[i]);
                        }
                        // check to see if we received the expected message
                        if (dataMatch)
                        {
                            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
#if DEBUG_MULTICASTLISTENER
                            Console.Out.WriteLine("DEBUG: packet match, replying");
#endif
                            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
                            // packet for sending messages
                            //DatagramPacket outPacket
                            // = new DatagramPacket(returnMessage, returnMessage.length,
                            //        inPacket.getAddress(), inPacket.getPort());
                            // send return message
                            socket.Send(returnMessage/* outPacket */); // TODO: check this one. is it going to the right address???
                        }
                    }
                }
                catch (Exception /* IOException */ 
#if DEBUG_MULTICASTLISTENER
                    ioe
#endif
                    )
                {/* drain */
#if DEBUG_MULTICASTLISTENER
                    Console.Out.WriteLine("       ERROR: " + ioe.Message);
#endif
                }
            }
            listenerRunning = false;
        }

        /**
         * Waits for datagram listener to finish, with a timeout.
         */
        public void StopListener()
        {
            listenerStopped = true;
            int i = 0;
            int timeout = timeoutInSeconds * 100;
            while (listenerRunning && i++ < timeout)
                try { System.Threading.Thread.Sleep(10); }
                catch (Exception) { ;}
        }

    }
}
