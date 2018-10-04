﻿using O2DESNet.Demos.GGnQueue;
using O2DESNet.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace O2DESNet.Database
{
    class Program
    {
        static string HourlyArrivalRate = "hourly arrival rate";
        static string HourlyServiceRate = "hourly service rate";
        static string ServerCapacity = "server capacity";

        static GGnQueue InputFunc(int seed, Dictionary<string, double> inputValues)
        {
            var config = new GGnQueue.Statics
            {
                InterArrivalTime = rs => TimeSpan.FromHours(Exponential.Sample(rs, 1 / inputValues[HourlyArrivalRate])),
                ServiceTime = (l, rs) => TimeSpan.FromHours(Exponential.Sample(rs, 1 / inputValues[HourlyServiceRate])),
                ServerCapacity = (int)inputValues[ServerCapacity],
            };
            return new GGnQueue(config, seed);
        }
        static Dictionary<string, double> OutputFunc(GGnQueue state)
        {
            return new Dictionary<string, double>
            {
                { "average queue length", state.Queue.HourCounter.AverageCount },
                { "server utilization", state.Server.Utilization },
                { "number of processed", state.Processed.Count }
            };
        }

        static void Main(string[] args)
        {
            //db.SaveChanges();

            var expr = new Experimenter<GGnQueue>(
                dbContext: new DbContext(),
                projectName: "GGnQ_Experiment", versionNumber: "1.0.0.0",
                inputFunc: InputFunc, outputFunc: OutputFunc,
                runInterval: TimeSpan.FromHours(1),
                warmUpPeriod: TimeSpan.FromHours(2), 
                runLength: TimeSpan.FromDays(1),
                operatr: "Haobin"
                );

            expr.SetExperiment(new Dictionary<string, double> {
                { HourlyArrivalRate, 3 },
                { HourlyServiceRate, 4 },
                { ServerCapacity, 2 },
            }, 2);

            expr.SetExperiment(new Dictionary<string, double> {
                { HourlyArrivalRate, 3 },
                { HourlyServiceRate, 4 },
                { ServerCapacity, 2 },
            }, 3);

            expr.SetExperiment(new Dictionary<string, double> {
                { HourlyArrivalRate, 4 },
                { HourlyServiceRate, 4 },
                { ServerCapacity, 2 },
            }, 3);

            while (expr.RunExperiment()) ;


            //while (true)
            //{
            //    var db = new DbContext();

            //    var s = db.GetScenario("TuasFinger3", "1.0.0.3",
            //        new Dictionary<string, double> { { "b", 1 }, { "c", 4 } });

            //    s.AddSnapshot(db, 2, new DateTime(2, 1, 1, 0, 1, 0), new Dictionary<string, double> { { "f", 0.01 }, { "g", 400 } }, Environment.MachineName);
            //    //var res = s.RemoveReplication(db, 2);


            //    db.SaveChanges();         
            //}
                       
        }
    }
}
