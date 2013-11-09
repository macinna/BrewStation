// ---------------------------------------------------------------------------
// Copyright (C) 2008 Per Malmberg <www.pmalmberg.com>, All Rights Reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY,  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL DALLAS SEMICONDUCTOR BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
//---------------------------------------------------------------------------

// This file is distributed as part of the open source OWdotNET project.
// Project pages: https://sourceforge.net/projects/owdotnet
// Web Site:      http://owdotnet.sourceforge.net/

using System;
using System.Collections.Generic;
using System.Text;
using DalSemi.OneWire.Container;
using DalSemi.OneWire.Adapter;

namespace OWN.CustomContainer
{
    /// <summary>
    /// The CustomOneWireContainer acts as an abstract base class for all custom OneWire devices, i.e. devices not manufactured my Dallas.
    /// An example on such a device is Louis Swart's LCD controller. This class exists only to allow the OW.Net library to distinguish between
    /// the standard devices and the custom ones included in the OW.Net library.
    /// 
    /// All custom containers are put on their own list for retrieval by the various methods in the PortAdapter class.
    /// Since custom devices doesn't follow the rule of a unique family code (generally 0xFF is used to mark a device as
    /// a custom type) for each type, they cannot be automatically created when found on the network. To come around this
    /// problem, the configuration (OneWire.properties) file is read for a list of ROM codes. If a matching ROM code is
    /// found in the file, then an instance of the class specified for that ROM code is created. If no match is found, a
    /// OneWireContainer will be used to encapsulate the device.
    /// This can also be used as a way to replace a standard container with a custom one for a specific device on the network.
    /// 
    /// The correct syntax in the OneWire.properties file is:
    /// CustomDevice.ROM=ClassName
    /// 
    /// Example:
    /// CustomDevice.C500010000039BFF=MyClassName
    /// </summary>
    public abstract class CustomOneWireContainer : OneWireContainer
    {
        public CustomOneWireContainer( PortAdapter portAdapter, byte[] address ) : base( portAdapter, address )
        {
        }
    }
}
