﻿using O2DESNet;
using O2DESNet.Distributions;
using O2DESNet.Standard;
using System;

namespace O2DESNet.Demos
{
    public class TandemQueue : Sandbox
    {
        #region Static Properties
        public double HourlyArrivalRate { get; }
        public double HourlyServiceRate1 { get; }
        public double HourlyServiceRate2 { get; }
        public int BufferQueueSize => (int)Queue2.Capacity;

        #endregion

        #region Dynamic Properties
        public double AvgNQueueing1 => Queue1.AvgNQueueing;
        public double AvgNQueueing2 => Queue2.AvgNQueueing;
        public double AvgNServing1 => Server1.AvgNServing;
        public double AvgNServing2 => Server2.AvgNServing;
        public double AvgHoursInSystem => HcInSystem.AverageDuration.TotalHours;

        private readonly IGenerator Generator;
        private readonly IQueue Queue1;
        private readonly IServer Server1;
        private readonly IQueue Queue2;
        private readonly IServer Server2;
        private readonly HourCounter HcInSystem;
        #endregion

        #region Events / Methods
        private void Arrive()
        {
            Log("Arrive");
            HcInSystem.ObserveChange(1, ClockTime);
        }

        private void Depart()
        {
            Log("Depart");
            HcInSystem.ObserveChange(-1, ClockTime);
        }
        #endregion

        /// <param name="arrRate">Hour arrival rate to the system</param>
        /// <param name="svcRate1">Hourly service rate of Server 1</param>
        /// <param name="svcRate2">Hourly service rate of Server 2</param>
        /// <param name="bufferQSize">Buffer queue (Queue 2) capacity</param>
        public TandemQueue(double arrRate, double svcRate1, double svcRate2, int bufferQSize, int seed = 0)
            : base(seed)
        {
            HourlyArrivalRate = arrRate;
            HourlyServiceRate1 = svcRate1;
            HourlyServiceRate2 = svcRate2;

            Generator = AddChild(new Generator(new Generator.Statics
            {
                InterArrivalTime = rs => Exponential.Sample(rs, TimeSpan.FromHours(1 / HourlyArrivalRate))
            }, DefaultRS.Next()));

            Queue1 = AddChild(new Queue(double.PositiveInfinity, DefaultRS.Next(), id: "Queue1"));

            Server1 = AddChild(new Server(new Server.Statics
            {
                Capacity = 1,
                ServiceTime = (rs, load) => Exponential.Sample(rs, TimeSpan.FromHours(1 / HourlyServiceRate1)),
            }, DefaultRS.Next(), id: "Server1"));

            Queue2 = AddChild(new Queue(bufferQSize, DefaultRS.Next(), id: "Queue2"));

            Server2 = AddChild(new Server(new Server.Statics
            {
                Capacity = 1,
                ServiceTime = (rs, load) => Exponential.Sample(rs, TimeSpan.FromHours(1 / HourlyServiceRate2)),
            }, DefaultRS.Next(), id: "Server2"));

            Generator.OnArrive += () => Queue1.RqstEnqueue(new Load());
            Generator.OnArrive += Arrive;

            Queue1.OnEnqueued += Server1.RqstStart;
            Server1.OnStarted += Queue1.Dequeue;

            Server1.OnReadyToDepart += Queue2.RqstEnqueue;
            Queue2.OnEnqueued += Server1.Depart;

            Queue2.OnEnqueued += Server2.RqstStart;
            Server2.OnStarted += Queue2.Dequeue;

            Server2.OnReadyToDepart += Server2.Depart;
            Server2.OnReadyToDepart += load => Depart();

            HcInSystem = AddHourCounter();

            /// Initial event
            Generator.Start();
        }

        public override void Dispose() { }
    }
}
