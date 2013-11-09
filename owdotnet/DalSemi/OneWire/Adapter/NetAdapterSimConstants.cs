// This file is distributed as part of the open source OWdotNET project.
// Project pages: https://sourceforge.net/projects/owdotnet
// Web Site:      http://owdotnet.sourceforge.net/

using System;
using System.Collections.Generic;
using System.Text;

namespace DalSemi.OneWire.Adapter
{
    public class NetAdapterSimConstants
    {

        // -----------------------------------------------------------------------
        // Simulation Methods
        //
        // -----------------------------------------------------------------------

        public const string OW_RESET_RESULT = "onewire reset at time";
        public const string OW_RESET_CMD = "task tb.xow_master.ow_reset";
        public const int OW_RESET_RUN_LENGTH = 1000000;

        public const string OW_WRITE_BYTE_ARG = "deposit tb.xow_master.ow_write_byte.data = 8'h"; // ie 8'hFF
        public const string OW_WRITE_BYTE_CMD = "task tb.xow_master.ow_write_byte";
        public const int OW_WRITE_BYTE_RUN_LENGTH = 520000;

        public const string OW_READ_RESULT = "(data=";
        public const string OW_READ_BYTE_CMD = "task tb.xow_master.ow_read_byte";
        public const int OW_READ_BYTE_RUN_LENGTH = 632009;

        public const string OW_READ_SLOT_CMD = "task tb.xow_master.ow_read_slot";
        public const int OW_READ_SLOT_RUN_LENGTH = 80000;

        public const string OW_WRITE_ZERO_CMD = "task tb.xow_master.ow_write0";
        public const int OW_WRITE_ZERO_RUN_LENGTH = 80000;

        public const string OW_WRITE_ONE_CMD = "task tb.xow_master.ow_write1";
        public const int OW_WRITE_ONE_RUN_LENGTH = 80000;

        public const string GENERIC_CMD_END = "Ran until";


        public const long PING_MS_RUN_LENGTH = 1000000L;

        public const string RUN = "run ";
        public const string LINE_DELIM = "\r\n";
        public const string PROMPT = "ncsim> ";

    }
}
