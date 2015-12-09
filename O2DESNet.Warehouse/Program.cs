﻿using O2DESNet.Warehouse.Statics;
using System;
using System.Linq;

namespace O2DESNet.Warehouse
{
    class Program
    {
        static void Main(string[] args)
        {
            WarehouseSim whsim = new WarehouseSim("ZA");
            //whsim.wh.ViewRacks();

            //DisplayRouteTable(whsim.wh);
            //DisplayPathingTable(whsim.wh);

            Console.WriteLine("Simulation End");
            Console.ReadKey();
        }

        static void DisplayRouteTable(Scenario pm)
        {
            foreach (var cp in pm.ControlPoints)
            {
                Console.WriteLine("Route Table at CP_{0}:", cp.Id);
                foreach (var item in cp.RoutingTable)
                    Console.WriteLine("{0}:{1}", item.Key.Id, item.Value.Id);
                Console.WriteLine();
            }
        }

        static void DisplayPathingTable(Scenario pm)
        {
            foreach (var cp in pm.ControlPoints)
            {
                Console.WriteLine("Pathing Table at CP_{0}:", cp.Id);
                foreach (var item in cp.PathingTable)
                    Console.WriteLine("{0}:{1}", item.Key.Id, item.Value.Id);
                Console.WriteLine();
            }
        }
    }
}