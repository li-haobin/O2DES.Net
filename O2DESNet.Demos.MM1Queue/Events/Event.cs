﻿using System;

namespace O2DESNet.Demos.MM1Queue
{
    [Serializable]
    internal abstract class Event : IEvent
    {
        protected Simulator _sim { get; private set; }
        protected Event(Simulator simulator) { _sim = simulator; }
        public abstract void Invoke();
    }
}
