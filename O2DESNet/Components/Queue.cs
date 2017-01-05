﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace O2DESNet
{
    public class Queue<TLoad> : Component<Queue<TLoad>.Statics>
    {
        #region Static Properties
        public class Statics : Scenario
        {
            /// <summary>
            /// Maximum number of loads in the queue
            /// </summary>
            public int Capacity { get; set; } = int.MaxValue;
        }
        #endregion

        #region Dynamic Properties
        public List<TLoad> Waiting { get; private set; } = new List<TLoad>();
        public int Occupancy { get { return Waiting.Count; } }
        public int Vacancy { get { return Config.Capacity - Occupancy; } }
        public bool ToDequeue { get; private set; } = true;

        public HourCounter HourCounter { get; private set; } = new HourCounter(); // statistics    
        public double Utilization { get { return HourCounter.AverageCount / Config.Capacity; } }    
        #endregion

        #region Events
        /// <summary>
        /// Enqueue given load
        /// </summary>
        private class EnqueueEvent : Event
        {
            public Queue<TLoad> Queue { get; private set; }
            public TLoad Load { get; private set; }

            internal EnqueueEvent(Queue<TLoad> queue, TLoad load)
            {
                Queue = queue;
                Load = load;
            }
            public override void Invoke()
            {
                if (Queue.Vacancy == 0) throw new HasZeroVacancyException();
                Queue.Waiting.Add(Load);
                Queue.HourCounter.ObserveChange(1, ClockTime);
                if (Queue.ToDequeue) Execute(new DequeueEvent(Queue));                
                else Execute(new StateChangeEvent(Queue));
            }
            public override string ToString() { return string.Format("{0}_Enqueue", Queue); }
        }        
        private class UpdToDequeueEvent : Event
        {
            public Queue<TLoad> Queue { get; private set; }
            public bool ToDequeue { get; private set; }

            internal UpdToDequeueEvent(Queue<TLoad> queue, bool toDequeue)
            {
                Queue = queue;
                ToDequeue = toDequeue;
            }
            public override void Invoke()
            {
                Queue.ToDequeue = ToDequeue;
                if (Queue.ToDequeue && Queue.Waiting.Count > 0) Execute(new DequeueEvent(Queue));
            }
            public override string ToString() { return string.Format("{0}_UpdToDequeue", Queue); }
        }
        private class StateChangeEvent : Event
        {
            public Queue<TLoad> Queue { get; private set; }
            internal StateChangeEvent(Queue<TLoad> queue) { Queue = queue; }
            public override void Invoke()
            {
                foreach (var evnt in Queue.OnStateChange) Execute(evnt(Queue));
            }
            public override string ToString() { return string.Format("{0}_StateChange", Queue); }
        }
        /// <summary>
        /// Dequeue the first load
        /// </summary>
        private class DequeueEvent : Event
        {
            public Queue<TLoad> Queue { get; private set; }

            internal DequeueEvent(Queue<TLoad> queue)
            {
                Queue = queue;
            }
            public override void Invoke()
            {
                TLoad load = Queue.Waiting.FirstOrDefault();
                Queue.Waiting.RemoveAt(0);
                Queue.HourCounter.ObserveChange(-1, ClockTime);
                
                foreach (var evnt in Queue.OnDequeue) Execute(evnt(load));
                if (Queue.ToDequeue && Queue.Waiting.Count > 0) Execute(new DequeueEvent(Queue));
                else Execute(new StateChangeEvent(Queue));
            }
            public override string ToString() { return string.Format("{0}_Dequeue", Queue); }
        }
        #endregion

        #region Input Events - Getters
        public Event Enqueue(TLoad load) { return new EnqueueEvent(this, load); }
        public Event UpdToDequeue(bool toDequeue) { return new UpdToDequeueEvent(this, toDequeue); }
        #endregion

        #region Output Events - Reference to Getters
        public List<Func<TLoad, Event>> OnDequeue { get; private set; } = new List<Func<TLoad, Event>>();
        public List<Func<Queue<TLoad>, Event>> OnStateChange { get; private set; } = new List<Func<Queue<TLoad>, Event>>();
        #endregion

        #region Exeptions
        public class HasZeroVacancyException : Exception
        {
            public HasZeroVacancyException() : base("Make sure the vacancy of the queue is updated before execute Enqueue event.") { }
        }
        #endregion

        public Queue() : base(new Statics()) { Name = "Queue"; }
        public Queue(Statics config, string tag = null) : base(config, tag: tag) { Name = "Queue"; }

        public override void WarmedUp(DateTime clockTime)
        {
            HourCounter.WarmedUp(clockTime);
        }
        public override void WriteToConsole(DateTime? clockTime = null)
        {
            Console.Write("[{0}]: ", this);
            foreach (var load in Waiting) Console.Write("{0} ", load);
            Console.WriteLine();
        }
    }
}
