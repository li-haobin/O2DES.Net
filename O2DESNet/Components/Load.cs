﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace O2DESNet
{
    public class Load : Component
    {
        #region Statics
        #endregion

        #region Dynamics
        public List<Tuple<DateTime, Event>> TimeStamps { get; private set; }
        public TimeSpan TotalTimeSpan { get { return TimeStamps.Max(t => t.Item1) - TimeStamps.Min(t => t.Item1); } }
        public DateTime? GetFirstTimeStamp(Func<Event, bool> check = null)
        {
            for (int i = 0; i < TimeStamps.Count; i++)
                if (check == null || check(TimeStamps[i].Item2)) return TimeStamps[i].Item1;
            return null;
        }
        public DateTime? GetLastTimeStamp(Func<Event,bool> check)
        {
            for (int i = TimeStamps.Count; i > 0; i--)
                if (check == null || check(TimeStamps[i - 1].Item2)) return TimeStamps[i - 1].Item1;
            return null;
        }

        public virtual void Log(Event evnt)
        {
            TimeStamps.Add(new Tuple<DateTime, Event>(evnt.ClockTime, evnt));
            evnt.Log(this, evnt);
        }
        #endregion

        public Load(int seed = 0, string tag = null) : base(new Scenario(), seed: seed, tag: tag)
        {
            Name = "Load";
            TimeStamps = new List<Tuple<DateTime, Event>>();
        }       

        public override void WarmedUp(DateTime clockTime) { }

        public override void WriteToConsole() { Console.WriteLine(this); }
    }
    
}
