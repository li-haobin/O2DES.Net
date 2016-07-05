﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace O2DESNet.PathMover
{
    public class Move<TScenario,TStatus> : Event<TScenario, TStatus>
        where TScenario : Scenario
        where TStatus : Status<TScenario>
    {
        public PMDynamics Dynamics { get; set; }
        public Vehicle Vehicle { get; set; }

        protected override void Invoke()
        {
            if (!Vehicle.Current.Equals(Vehicle.Targets.First()))
            {
                Vehicle.Move(Vehicle.Current.RoutingTable[Vehicle.Targets.First()], ClockTime);
                var path = Vehicle.Current.PathingTable[Vehicle.Next];
                foreach (var v in Dynamics.VehiclesOnPath[path]) 
                    // moving in new vehicle may update the speeds for existing vehicles
                    Schedule(new Reach<TScenario, TStatus> { Dynamics = Dynamics, Vehicle = v }, v.TimeToReach.Value);
                Dynamics.PathUtils[path].ObserveChange(1, ClockTime);
            }
            else Execute(new Reach<TScenario, TStatus> { Dynamics = Dynamics, Vehicle = Vehicle });
            
            Status.Log("{0}\tMove: {1}", ClockTime.ToLongTimeString(), Vehicle.GetStr_Status());
            //Status.Log(Dynamics.GetStr_VehiclesOnPath());
        }
    }
}
