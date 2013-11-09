// This file is distributed as part of the open source OWdotNET project.
// Project pages: https://sourceforge.net/projects/owdotnet
// Web Site:      http://owdotnet.sourceforge.net/

using System;
using System.Collections.Generic;
using System.Text;

namespace DalSemi.OneWire.Container
{
    /// <summary>
    /// Interface for 1-Wire devices that contains one or more counters
    /// </summary>
    public interface ICounterContainer
    {
        /// <summary>
        /// Returns an array of the valid counter pages available in the device.
        /// </summary>
        /// <returns></returns>
        uint[] GetCounterPages();

        /// <summary>
        /// Gets the count for the specified counter.
        /// </summary>
        /// <param name="counterPage">The counter page.</param>
        /// <returns></returns>
        /// <exception cref="OneWireIOException">Thrown if a IO error occures</exception>
        /// <exception cref="OneWireException">Thrown on a communication or setup error with the 1-Wire adapter</exception>
        uint ReadCounter( uint counterPage );
    }
}

