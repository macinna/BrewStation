/*---------------------------------------------------------------------------
 * Copyright (C) 1999,2000 Dallas Semiconductor Corporation, All Rights Reserved.
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

using DalSemi.OneWire.Container; // OneWireContainer

namespace DalSemi.OneWire.Adapter
{

    /**
     * <p>This <code>DSPortAdapter</code> class was designed to be used for
     * the iB-IDE's emulator.  The <code>DumbAdapter</code> allows
     * programmers to add and remove <code>OneWireContainer</code>
     * objects that will be found in its search.  The Java iButton
     * emulator works by creating a class that subclasses all of
     * <code>OneWireContainer16</code>'s relevant methods and redirecting them
     * to the emulation code.  That object is then added to this class's
     * list of <code>OneWireContainer</code>s.</p>
     *
     * <p>Note that methods such as <code>selectPort</code> and
     * <code>beginExclusive</code> by default do nothing.  This class is
     * mainly meant for debugging using an emulated iButton.  It will do
     * a poor job of debugging any multi-threading, port-sharing issues.
     *
     * @see com.dalsemi.onewire.adapter.DSPortAdapter
     * @see com.dalsemi.onewire.container.OneWireContainer
     *
     * @version    0.00, 16 Mar 2001
     * @author     K
     */

    public class DumbAdapter : PortAdapter
    {

        //--------
        //-------- Variables
        //--------


        int containers_index = 0;

        private DeviceContainerList containers = new DeviceContainerList();

        /**
         * Adds a <code>OneWireContainer</code> to the list of containers that
         * this adapter object will find.
         *
         * @param c represents a 1-Wire device that this adapter will report from a search
         */
        public void AddContainer(OneWireContainer c)
        {
            lock (containers)
            {
                containers.Add(c);
            }
        }

        /**
         * Removes a <code>OneWireContainer</code> from the list of containers that
         * this adapter object will find.
         *
         * @param c represents a 1-Wire device that this adapter should no longer
         *        report as found by a search
         */
        public void RemoveContainer(OneWireContainer c)
        {
            lock (containers)
            {
                containers.Remove(c);
            }
        }


        /**
         * Hashtable to contain the user replaced OneWireContainers
         */
        //private Hashtable registeredOneWireContainerClasses = new Hashtable(5);

        /**
         * Byte array of families to include in search
         */
        //private byte[] include;

        /**
         * Byte array of families to exclude from search
         */
        //private byte[] exclude;

        //--------
        //-------- Methods
        //--------

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
                return "DumbAdapter";
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
                return "Virtual Emulated Port";
            }
        }

        /**
         * Retrieves a version string for this class.
         *
         * @return  version string
         */
        //   public override string GetClassVersion ()
        // {
        //        return "0.90";
        // }

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
                List<string> portNames = new List<string>();
                portNames.Add("NULL0");
                return portNames;
            }
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         */
        //   public void RegisterOneWireContainerClass (int family,  Class OneWireContainerClass)
        // {
        //}

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  portName  name of the target port, retrieved from
         * getPortNames()
         *
         * @return always returns <code>true</code>
         */
        public override Boolean OpenPort(string portName)
        {
            //be lazy, allow anything
            return true;
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         */
        //   public override void FreePort ()
        // {
        //       //airball
        //   }

        /**
         * Retrieves the name of the selected port as a <code>String</code>.
         *
         * @return  always returns the <code>String</code> "NULL0"
         */
        public override string PortName
        {
            get
            {
                return "NULL0";
            }
        }

        //--------
        //-------- Adapter detection
        //--------

        /**
         * Detects adapter presence on the selected port.  In <code>DumbAdapter</code>,
         * the adapter is always detected.
         *
         * @return  <code>true</code>
         */
        public override Boolean AdapterDetected
        {
            get
            {
                return true;
            }
        }

        //--------
        //-------- Adapter features
        //--------

        /* The following interogative methods are provided so that client code
         * can react selectively to underlying states without generating an
         * exception.
         */

        /**
         * Applications might check this method and not attempt operation unless this method
         * returns <code>true</code>. To make sure that a wide variety of applications can use this class,
         * this method always returns <code>true</code>.
         *
         * @return  <code>true</code>
         *
         */
        public override Boolean CanOverdrive
        {
            get
            {
                //don'thread want someone to bail because of this
                return true;
            }
        }

        /**
         * Applications might check this method and not attempt operation unless this method
         * returns <code>true</code>. To make sure that a wide variety of applications can use this class,
         * this method always returns <code>true</code>.
         *
         * @return  <code>true</code>
         */
        public override Boolean CanHyperdrive
        {
            get
            {
                //don'thread want someone to bail because of this, although it doesn'thread exist yet
                return true;
            }
        }

        /**
         * Applications might check this method and not attempt operation unless this method
         * returns <code>true</code>. To make sure that a wide variety of applications can use this class,
         * this method always returns <code>true</code>.
         *
         * @return  <code>true</code>
         */
        public override Boolean CanFlex
        {
            get
            {
                //don'thread want someone to bail because of this
                return true;
            }
        }

        /**
         * Applications might check this method and not attempt operation unless this method
         * returns <code>true</code>. To make sure that a wide variety of applications can use this class,
         * this method always returns <code>true</code>.
         *
         * @return  <code>true</code>
         */
        public override Boolean CanProgram
        {
            get
            {
                //don'thread want someone to bail because of this
                return true;
            }
        }

        /**
         * Applications might check this method and not attempt operation unless this method
         * returns <code>true</code>. To make sure that a wide variety of applications can use this class,
         * this method always returns <code>true</code>.
         *
         * @return  <code>true</code>
         */
        public override Boolean CanDeliverPower
        {
            get
            {
                //don'thread want someone to bail because of this
                return true;
            }
        }

        /**
         * Applications might check this method and not attempt operation unless this method
         * returns <code>true</code>. To make sure that a wide variety of applications can use this class,
         * this method always returns <code>true</code>.
         *
         * @return  <code>true</code>
         */
        public override Boolean CanDeliverSmartPower
        {
            get
            {
                //don'thread want someone to bail because of this
                return true;
            }
        }

        /**
         * Applications might check this method and not attempt operation unless this method
         * returns <code>true</code>. To make sure that a wide variety of applications can use this class,
         * this method always returns <code>true</code>.
         *
         * @return  <code>true</code>
         */
        public override Boolean CanBreak
        {
            get
            {
                //don'thread want someone to bail because of this
                return true;
            }
        }

        //--------
        //-------- Finding iButtons and 1-Wire devices
        //--------

        /**
         * Returns an enumeration of <code>OneWireContainer</code> objects corresponding
         * to all of the iButtons or 1-Wire devices found on the 1-Wire Network.  In the case of
         * the <code>DumbAdapter</code>, this method returns a simple copy of the internal
         * <code>java.util.Vector</code> that stores all the 1-Wire devices this class finds
         * in a search.
         *
         * @return  <code>Enumeration</code> of <code>OneWireContainer</code> objects
         * found on the 1-Wire Network.
         */
        //public override DeviceContainerList GetDeviceContainers()
        //{
        //    DeviceContainerList dcl = new DeviceContainerList();
        //    lock (containers)
        //    {
        //        for (int i = 0; i < containers.Count; i++)
        //        {
        //            dcl.Add(containers[i]);
        //        }
        //    }
        //    return dcl;
        //}

        /**
         * Returns a <code>OneWireContainer</code> object corresponding to the first iButton
         * or 1-Wire device found on the 1-Wire Network. If no devices are found,
         * then a <code>null</code> reference will be returned. In most cases, all further
         * communication with the device is done through the <code>OneWireContainer</code>.
         *
         * @return  The first <code>OneWireContainer</code> object found on the
         * 1-Wire Network, or <code>null</code> if no devices found.
         */
        //public OneWireContainer GetFirstDeviceContainer ()
        //{
        //   lock(containers)
        //   {
        //     if (containers.size() > 0)
        //     {
        //         containers_index = 1;
        //         return (OneWireContainer) containers.elementAt(0);
        //     }
        //     else
        //         return null;
        //   }
        //}

        /**
         * Returns a <code>OneWireContainer</code> object corresponding to the next iButton
         * or 1-Wire device found. The previous 1-Wire device found is used
         * as a starting point in the search.  If no devices are found,
         * then a <code>null</code> reference will be returned. In most cases, all further
         * communication with the device is done through the <code>OneWireContainer</code>.
         *
         * @return  The next <code>OneWireContainer</code> object found on the
         * 1-Wire Network, or <code>null</code> if no iButtons found.
         */
        //public OneWireContainer GetNextDeviceContainer ()
        //{
        //   lock(containers)
        //   {
        //     if (containers.size() > containers_index)
        //     {
        //         containers_index++;
        //         return (OneWireContainer) containers.elementAt(containers_index - 1);
        //     }
        //     else
        //         return null;
        //   }
        //}

        /**
         * Returns <code>true</code> if the first iButton or 1-Wire device
         * is found on the 1-Wire Network.
         * If no devices are found, then <code>false</code> will be returned.
         *
         * @return  <code>true</code> if an iButton or 1-Wire device is found.
         */
        public override Boolean GetFirstDevice(byte[] address, int offset)
        {
            lock (containers)
            {
                if (containers.Count > 0)
                {
                    containers_index = 0;
                    Array.Copy(containers[containers_index].Address, 0, address, offset, 8);
                    return true;
                }
                else
                    return false;
            }
        }

        /**
         * Returns <code>true</code> if the next iButton or 1-Wire device
         * is found. The previous 1-Wire device found is used
         * as a starting point in the search.  If no more devices are found
         * then <code>false</code> will be returned.
         *
         * @return  <code>true</code> if an iButton or 1-Wire device is found.
         */
        public override Boolean GetNextDevice(byte[] address, int offset)
        {
            lock (containers)
            {
                if (containers.Count > (containers_index + 1))
                {
                    containers_index++;
                    Array.Copy(containers[containers_index].Address, 0, address, offset, 8);
                    return true;
                }
                else
                    return false;
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
        //   public override void Address (byte[] address)
        //   {
        //        OneWireContainer temp = (OneWireContainer) containers.elementAt(containers_index - 1);
        //        if (temp != null)
        //        {
        //            System.arraycopy(temp.getAddress(), 0, address, 0, 8);
        //        }
        //   }

        /**
         * Gets the 'current' 1-Wire device address being used by the adapter as a long.
         * This address is the last iButton or 1-Wire device found
         * in a search (findNextDevice()...).
         *
         * @return <code>long</code> representation of the iButton address
         * @see   com.dalsemi.onewire.utils.Address
         */
        //public override long GetAddressAsLong ()
        //{
        //   byte[] address = new byte [8];

        //   getAddress(address);

        //   return Address.toLong(address);
        //}

        /**
         * Gets the 'current' 1-Wire device address being used by the adapter as a String.
         * This address is the last iButton or 1-Wire device found
         * in a search (findNextDevice()...).
         *
         * @return <code>String</code> representation of the iButton address
         * @see   com.dalsemi.onewire.utils.Address
         */
        //public override string GetAddressAsString ()
        //{
        //   byte[] address = new byte [8];

        //   getAddress(address);

        //   return Address.toString(address);
        //}

        /**
         * Verifies that the iButton or 1-Wire device specified is present on
         * the 1-Wire Network. This does not affect the 'current' device
         * state information used in searches (findNextDevice...).
         *
         * @param  address  device address to verify is present
         *
         * @return  <code>true</code> if device is present, else
         *         <code>false</code>.
         *
         * @see   com.dalsemi.onewire.utils.Address
         */
        public override Boolean IsPresent(byte[] address, int offset)
        {
            byte[] addr = new byte[8];
            Array.Copy(address, offset, addr, 0, 8);
            string a = DalSemi.OneWire.Utils.Address.ToString(addr);
            lock (containers)
            {
                for (int i = 0; i < containers.Count; i++)
                {
                    OneWireContainer temp = (OneWireContainer)containers[i];
                    if (a == DalSemi.OneWire.Utils.Address.ToString(temp.Address))
                        return true;
                }
            }
            return false;
        }

        /**
         * Verifies that the iButton or 1-Wire device specified is present
         * on the 1-Wire Network and in an alarm state. This method is currently
         * not implemented in <code>DumbAdapter</code>.
         *
         * @param  address  device address to verify is present and alarming
         *
         * @return  <code>false</code>
         *
         * @see   com.dalsemi.onewire.utils.Address
         */
        public override Boolean IsAlarming(byte[] address, int offset)
        {
            return false;
        }

        /**
         * Selects the specified iButton or 1-Wire device by broadcasting its
         * address.  With a <code>DumbAdapter</code>, this method simply
         * returns true.
         *
         * Warning, this does not verify that the device is currently present
         * on the 1-Wire Network (See isPresent).
         *
         * @param  address    address of iButton or 1-Wire device to select
         *
         * @return  <code>true</code> if device address was sent, <code>false</code>
         * otherwise.
         *
         * @see #isPresent(byte[])
         * @see   com.dalsemi.onewire.utils.Address
         */
        public override Boolean SelectDevice(byte[] address, int off)
        {
            return IsPresent(address, off);
        }

        //--------
        //-------- Finding iButton/1-Wire device options
        //--------

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @see #setNoResetSearch
         */
        public override void SetSearchOnlyAlarmingDevices()
        {
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         */
        public override void SetNoResetSearch()
        {
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @see #setNoResetSearch
         */
        public override void SetSearchAllDevices()
        {
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @see    #targetFamily
         * @see    #targetFamily(byte[])
         * @see    #excludeFamily
         * @see    #excludeFamily(byte[])
         */
        //public override void TargetAllFamilies ()
        //{
        //   include = null;
        //   exclude = null;
        //}

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  family   the code of the family type to target for searches
         * @see   com.dalsemi.onewire.utils.Address
         * @see    #targetAllFamilies
         */
        //public override void TargetFamily (int family)
        //{
        //   if ((include == null) || (include.Length != 1))
        //      include = new byte [1];

        //   include [0] = ( byte ) family;
        //}

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  family  array of the family types to target for searches
         * @see   com.dalsemi.onewire.utils.Address
         * @see    #targetAllFamilies
         */
        //public override void TargetFamily (byte[] family)
        //{
        //   if ((include == null) || (include.Length != family.Length))
        //      include = new byte [family.Length];

        //   System.arraycopy(family, 0, include, 0, family.length);
        //}

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  family   the code of the family type NOT to target in searches
         * @see   com.dalsemi.onewire.utils.Address
         * @see    #targetAllFamilies
         */
        //public override void ExcludeFamily (int family)
        //{
        //   if ((exclude == null) || (exclude.length != 1))
        //      exclude = new byte [1];

        //   exclude [0] = ( byte ) family;
        //}

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  family  array of family cods NOT to target for searches
         * @see   com.dalsemi.onewire.utils.Address
         * @see    #targetAllFamilies
         */
        //public override void ExcludeFamily (byte[] family)
        //{
        //   if ((exclude == null) || (exclude.length != family.length))
        //      exclude = new byte [family.length];

        //   System.arraycopy(family, 0, exclude, 0, family.length);
        //}

        //--------
        //-------- 1-Wire Network Semaphore methods
        //--------

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param blocking <code>true</code> if want to block waiting
         *                 for an excluse access to the adapter
         * @return <code>true</code>
         */
        public override Boolean BeginExclusive(Boolean blocking)
        {
            //DEBUG!!! RIGHT NOW THIS IS NOT IMPLEMENTED!!!
            return true;
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         */
        public override void EndExclusive()
        {
            //DEBUG!!! RIGHT NOW THIS IS NOT IMPLEMENTED!!!
        }

        //--------
        //-------- Primitive 1-Wire Network data methods
        //--------

        /**
         * Sends a bit to the 1-Wire Network.
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  bitValue  the bit value to send to the 1-Wire Network.
         */
        public override void PutBit(Boolean bitValue)
        {
            //this will not be implemented
        }

        /**
         * Gets a bit from the 1-Wire Network.
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @return  <code>true</code>
         */
        public override Boolean GetBit()
        {
            //this will not be implemented
            return true;
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  byteValue  the byte value to send to the 1-Wire Network.
         */
        public override void PutByte(int byteValue)
        {
            //this will not be implemented
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @return the value 0x0ff
         */
        public override byte GetByte()
        {
            //this will not be implemented
            return 0x0ff;
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  len  length of data bytes to receive
         *
         * @return a new byte array of length <code>len</code>
         */
        public override byte[] GetBlock(int len)
        {
            //this will not be implemented
            return new byte[len];
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  arr     array in which to write the received bytes
         * @param  len     length of data bytes to receive
         */
        public override void GetBlock(byte[] arr, int len)
        {
            //this will not be implemented
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  arr     array in which to write the received bytes
         * @param  off     offset into the array to start
         * @param  len     length of data bytes to receive
         */
        public override void GetBlock(byte[] arr, int off, int len)
        {
            //this will not be implemented
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param  dataBlock  array of data to transfer to and from the 1-Wire Network.
         * @param  off        offset into the array of data to start
         * @param  len        length of data to send / receive starting at 'off'
         */
        public override void DataBlock(byte[] dataBlock, int off, int len)
        {
            //this will not be implemented
        }

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
         * Note that in <code>DumbAdapter</code>, the only possible results are 0 and 1.
         */
        public override OWResetResult Reset()
        {
            //this will not be implemented
            if (containers.Count > 0)
                return OWResetResult.RESET_PRESENCE;
            return OWResetResult.RESET_NOPRESENCE;
        }

        //--------
        //-------- 1-Wire Network power methods
        //--------

        /**
         * This method does nothing in <code>DumbAdapter</code>.
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
         */
        public override void SetPowerDuration(OWPowerTime powerDur)
        {
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
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
         * @return <code>true</code>
         */
        public override Boolean StartPowerDelivery(OWPowerStart changeCondition)
        {
            return true;
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         * @param timeFactor
         * <ul>
         * <li>   7 (DELIVERY_EPROM) provide program pulse for 480 microseconds
         * <li>   5 (DELIVERY_INFINITE) provide power until the
         *          setPowerNormal() method is called.
         * </ul>
         */
        public override void SetProgramPulseDuration(OWPowerTime pulseDur)
        {
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
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
         * @return <code>true</code>
         */
        public override Boolean StartProgramPulse(OWPowerStart changeCondition)
        {
            return true;
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         */
        public override void StartBreak()
        {
        }

        /**
         * This method does nothing in <code>DumbAdapter</code>.
         *
         */
        public override void SetPowerNormal()
        {
            return;
        }

        //--------
        //-------- 1-Wire Network speed methods
        //--------

        /**
         * This method does nothing in <code>DumbAdapter</code>.
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
         */
        public override OWSpeed Speed
        {
            get
            {
                /**
                 * This method does nothing in <code>DumbAdapter</code>.
                 *
                 * @return <the last value passed to the <code>setSpeed(int)</code>
                 * method, or 0
                 */

                return sp;
            }
            set
            {
                sp = value;
            }
        }

        private OWSpeed sp = OWSpeed.SPEED_REGULAR;

        //--------
        //-------- Misc
        //--------

        /**
         * Gets the container from this adapter whose address matches the address of a container
         * in the <code>DumbAdapter</code>'s internal <code>java.util.Vector</code>.
         *
         * @param  address  device address with which to find a container
         *
         * @return  The <code>OneWireContainer</code> object, or <code>null</code> if no match could be found.
         * @see   com.dalsemi.onewire.utils.Address
         */
        //public override OneWireContainer GetDeviceContainer (byte[] address)
        //{
        //     long addr = Address.toLong(address);
        //     lock(containers)
        //     {
        //         for (int i=0;i<containers.size();i++)
        //         {
        //             if (((OneWireContainer)containers.elementAt(i)).getAddressAsLong() == addr)
        //                 return (OneWireContainer)containers.elementAt(i);
        //         }
        //     }
        //     return null;

        //}

        /**
         * Checks to see if the family found is in the desired
         * include group.
         *
         * @return  <code>true</code> if in include group
         */
        //protected override Boolean IsValidFamily (byte[] address)
        //{
        //   byte familyCode = address [0];

        //   if (exclude != null)
        //   {
        //      for (int i = 0; i < exclude.length; i++)
        //      {
        //         if (familyCode == exclude [i])
        //         {
        //            return false;
        //         }
        //      }
        //   }

        //   if (include != null)
        //   {
        //      for (int i = 0; i < include.length; i++)
        //      {
        //         if (familyCode == include [i])
        //         {
        //            return true;
        //         }
        //      }

        //      return false;
        //   }

        //   return true;
        //}


        protected override void Dispose(bool Disposing)
        {
            // no unmanaged resources used
        }

    }
}
