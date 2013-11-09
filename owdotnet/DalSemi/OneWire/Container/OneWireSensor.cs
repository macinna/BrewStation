/*---------------------------------------------------------------------------
 * Copyright (C) 2002-2003 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace DalSemi.OneWire.Container
{
    /// <summary>
    /// 1-Wire sensor interface class for basic sensor operations.
    /// Typically the operations of 1-Wire Sensors are memory mapped so
    /// writing to a particular location causes the state to change.  To
    /// accommodate this type of architecture and reduce the number of 1-
    /// Wire operations that need to take place, a 'read-modify-write'
    /// technique is used.  Each Sensor interface is derived from this
    /// super-interface that contain just two methods: 
    /// ReadDevice()
    /// WriteDevice(byte[])
    /// The read returns a byte array and the write takes a byte array. The
    /// byte array is the state of the device.  The interfaces that
    /// extend this interface have 'Get' and 'Set' methods that
    /// manipulate the byte array. So a OneWireSensor operation is:
    /// 
    /// 
    /// state = ReadDevice()
    /// 'Get' and 'Set' methods on state
    /// WriteDevice(state)
    /// 
    /// Usage
    /// 
    /// Example 1
    /// Read the sensed level of a 
    /// Dalsemi.OneWire.Container.SwitchContainer instance 'sw': 
    ///
    /// byte[] state = sw.ReadDevice();
    /// if (sw.hasLevelSensing())
    /// {
    ///     for (int ch = 0; ch &lt; sw.getNumberChannels(state); ch++)
    ///     {
    ///         Console.WriteLine("Level for channel " + ch + " is : ");
    ///         if (sw.getLevel(ch, state))
    ///             Console.WriteLine("HIGH");
    ///         else
    ///             Console.WriteLine("HIGH");
    ///     }
    /// }
    /// else
    ///     Console.WriteLine("This SwitchContainer can not sense level");
    ///
    /// Example 2
    /// Set the clock of a Dalsemi.OneWire.Container.ClockContainer instance 'cl': 
    /// 
    /// byte[] state = cl.ReadDevice();
    /// cl.SetClock((new Date().getTime()), state);
    /// cl.SetClockRunEnable(true, state);
    /// cl.WriteDevice(state);
    /// </summary>
    /// <see cref="DalSemi.OneWire.Container.ADContainer"/>
    /// <see cref="DalSemi.OneWire.Container.ClockContainer"/>
    /// <see cref="DalSemi.OneWire.Container.PotentiometerContainer"/>
    /// <see cref="DalSemi.OneWire.Container.SwitchContainer"/>
    /// <see cref="DalSemi.OneWire.Container.TemperatureContainer"/>
    public interface IOneWireSensor
    {
        #region Sensor I/O methods


        /// <summary>
        ///  Retrieves the 1-Wire device sensor state.  This state is
        ///  returned as a byte array.  Pass this byte array to the 'Get'
        ///  and 'Set' methods.  If the device state needs to be changed then call
        ///  the 'EriteDevice' to finalize the changes.
        /// </summary>
        /// <returns>1-Wire device sensor state</returns>
        /// <exception cref="OneWireIOException">On a 1-Wire communication error such as 
        /// reading an incorrect CRC from a 1-Wire device.  This could be
        /// caused by a physical interruption in the 1-Wire Network due to
        /// shorts or a newly arriving 1-Wire device issuing a 'presence pulse'</exception>
        /// <exception cref="OneWireException">on a communication or setup error with the 1-Wire adapter</exception>
        byte[] ReadDevice();


        /// <summary>
        /// Writes the 1-Wire device sensor state that
        /// have been changed by 'set' methods. Only the state registers that
        /// changed are updated.  This is done by referencing a field information
        /// appended to the state data.
        /// </summary>
        /// <param name="state">1-Wire device sensor state</param>
        /// <exception cref="OneWireIOException">On a 1-Wire communication error such as 
        /// reading an incorrect CRC from a 1-Wire device.  This could be
        /// caused by a physical interruption in the 1-Wire Network due to
        /// shorts or a newly arriving 1-Wire device issuing a 'presence pulse'</exception>
        /// <exception cref="OneWireException">on a communication or setup error with the 1-Wire adapter</exception>
        void WriteDevice(byte[] state);


        #endregion // Sensor I/O methods


    }
}
