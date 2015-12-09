﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using O2DESNet.Warehouse.Statics;

namespace O2DESNet.Warehouse
{
    class WarehouseSim
    {
        public Simulator sim { get; private set; }
        public Scenario wh { get; private set; }

        public WarehouseSim(string scenarioName)
        {
            Initialize(scenarioName);
        }

        private void Initialize(string scenarioName)
        {
            wh = new Scenario(scenarioName);
            sim = new Simulator(wh, 0);

            // wh.ReadLayoutFiles();

            BasicBuilder();



            // wh.ReadSKUsFile("ZA_SKUs.csv");
            // wh.InitializeRouting(); // Need to find a way to generate this better (e.g. only generate for those CP with SKU. or no Rack. Or serialise...
        }

        private void BasicBuilder()
        {
            // Dimensions in metres

            string[] zone = { "A", "B", "C", "D" ,"E", "F", "Y", "Z"};
            int numRows = 5; //160
            int numShelves = 20; //20
            int numRacks = 6; //6
            double interRowSpace = 1.7;
            double shelfWidth = 1.5;
            double rackHeight = 0.35;
            double rowLength = numShelves * shelfWidth;
            double shelfHeight = numRacks * rackHeight;

            for (int z = 0; z < zone.Count() / 2; z++)
            {
                var mainAisle = wh.CreateAisle(zone[2 * z] + zone[2 * z + 1], interRowSpace * (numRows - 1));

                for (int i = 0; i < numRows; i++)
                {
                    var row1 = wh.CreateRow(zone[2 * z] + "-" + (i + 1).ToString(), rowLength, mainAisle, i * interRowSpace);
                    var row2 = wh.CreateRow(zone[2 * z + 1] + "-" + (i + 1).ToString(), rowLength, mainAisle, i * interRowSpace);

                    for (int j = 1; j <= numShelves; j++)
                    {
                        var shelf1 = wh.CreateShelf(row1.Row_ID + "-" + j.ToString(), shelfHeight, row1, j * shelfWidth);
                        var shelf2 = wh.CreateShelf(row2.Row_ID + "-" + j.ToString(), shelfHeight, row2, j * shelfWidth);

                        for (int k = 1; k <= numRacks; k++)
                        {
                            wh.CreateRack(shelf1.Shelf_ID + "-" + k.ToString(), shelf1, k * rackHeight);
                            wh.CreateRack(shelf2.Shelf_ID + "-" + k.ToString(), shelf2, k * rackHeight);
                        }
                    }
                }
            }
        }

        public void Run()
        {
            //while (true)
            //{
            //    sim.Run(10000);
            //    Console.Clear();
            //    foreach (var item in sim.Status.VehicleCounters)
            //        Console.WriteLine("CP{0}\t{1}", item.Key.Id, item.Value.TotalIncrementCount / (sim.ClockTime - DateTime.MinValue).TotalHours);
            //    Console.ReadKey();
            //}
        }
    }
}