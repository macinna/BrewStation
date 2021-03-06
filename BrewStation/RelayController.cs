﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrewStation
{
    public abstract class RelayController
    {
        public abstract bool OpenRelay(Relays relay);

        public abstract bool CloseRelay(Relays relay);

        public abstract bool OpenAllRelays();

        public abstract bool Initialize();

    }
}
