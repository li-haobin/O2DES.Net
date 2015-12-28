﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using O2DESNet.Warehouse.Dynamics;

namespace O2DESNet.Warehouse.Statics
{
    public class Scenario
    {
        public string Name { get; private set; }

        #region Parent Classes for Dijkstra
        public List<Path> Paths { get; private set; }
        public List<ControlPoint> ControlPoints { get; private set; } // Excludes CPRack
        #endregion

        #region Pickers
        /// <summary>
        /// PickerType from picker type id string
        /// </summary>
        public Dictionary<string, PickerType> GetPickerType { get; private set; }
        /// <summary>
        /// Numbers of pickers of each type
        /// </summary>
        public Dictionary<PickerType, int> NumPickers { get; private set; }
        public List<Picker> AllPickers { get; private set; }
        #endregion

        #region Layout Lookup - from ID to object
        public Dictionary<string, PathAisle> Aisles { get; private set; }
        public Dictionary<string, PathRow> Rows { get; private set; }
        public Dictionary<string, PathShelf> Shelves { get; private set; }
        public Dictionary<string, CPRack> Racks { get; private set; }
        public Dictionary<string, SKU> SKUs { get; private set; }
        public ControlPoint StartCP { get; set; }
        #endregion

        #region Picklists
        public Dictionary<PickerType, List<List<PickJob>>> MasterPickList { get; set; }
        public Dictionary<PickerType, List<List<PickJob>>> CompletedPickLists { get; set; }
        #endregion

        public Scenario(string name)
        {
            Name = name;
            Paths = new List<Path>();
            ControlPoints = new List<ControlPoint>();

            GetPickerType = new Dictionary<string, PickerType>();
            NumPickers = new Dictionary<PickerType, int>();

            Aisles = new Dictionary<string, PathAisle>();
            Rows = new Dictionary<string, PathRow>();
            Shelves = new Dictionary<string, PathShelf>();
            Racks = new Dictionary<string, CPRack>();
            SKUs = new Dictionary<string, SKU>();


            MasterPickList = new Dictionary<PickerType, List<List<PickJob>>>();
            CompletedPickLists = new Dictionary<PickerType, List<List<PickJob>>>();

            // Starting location
            StartCP = null;
            //ControlPoints.Add(StartCP);


        }

        #region Build from CSV file
        public void ReadLayoutFiles()
        {
            string aisles_file = @"Layout\" + Name + "_Aisles.csv";
            string rows_file = @"Layout\" + Name + "_Rows.csv";
            string shelves_file = @"Layout\" + Name + "_Shelves.csv";
            string racks_file = @"Layout\" + Name + "_Racks.csv";
            string SKUs_file = @"Layout\" + Name + "_SKUs.csv";

            ReadAislesFile(aisles_file);
            ReadRowsFile(rows_file);
            ReadShelvesFile(shelves_file);
            ReadRacksFile(racks_file);
            ReadSKUsFile(SKUs_file);
        }
        private void ReadAislesFile(string filename)
        {
            var aisles = CSVToList(filename);
            foreach (var data in aisles)
            {
                CreateAisle(data[0], Convert.ToDouble(data[1]));
            }
        }
        private void ReadRowsFile(string filename)
        {
            // Assuming there are two aisles connected to the row.
            var rows = CSVToList(filename);
            foreach (var data in rows)
            {
                CreateRow(data[0], Convert.ToDouble(data[1]),
                    Aisles[data[2]], Convert.ToDouble(data[3]),
                    Aisles[data[4]], Convert.ToDouble(data[5]));
            }
        }
        private void ReadShelvesFile(string filename)
        {
            var shelves = CSVToList(filename);
            foreach (var data in shelves)
            {
                CreateShelf(data[0], Convert.ToDouble(data[1]),
                    Rows[data[2]], Convert.ToDouble(data[3]));
            }
        }
        private void ReadRacksFile(string filename)
        {
            var racks = CSVToList(filename);
            foreach (var data in racks)
            {
                CreateRack(data[0],
                    Shelves[data[1]], Convert.ToDouble(data[2]));
            }
        }
        private void ReadSKUsFile(string filename)
        {
            var SKUs = CSVToList(filename);
            foreach (var data in SKUs)
            {
                var sku = new SKU(data[0], data[1]);
                for (int i = 2; i < data.Length; i++)
                    AddToRack(sku, Racks[data[i]]);
            }
        }

        public void ReadSKUsFile()
        {
            ReadSKUsFile(@"Layout\" + Name + "_SKUs.csv");
        }
        /// <summary>
        /// Converts csv file with header into list (row) of string array (column)
        /// </summary>
        /// <param name="csvfile"></param>
        /// <returns></returns>
        private List<string[]> CSVToList(string csvfile)
        {
            List<string[]> output = new List<string[]>();
            string line;

            using (StreamReader sr = new StreamReader(csvfile))
            {
                sr.ReadLine(); // Skip header
                while ((line = sr.ReadLine()) != null)
                {
                    output.Add(line.Split(','));
                }
            }

            return output;
        }
        #endregion

        #region View Layout
        public void ViewAll()
        {
            ViewAisles();
            ViewRows();
            ViewShelves();
            ViewRacks();
            ViewSKUs();
        }
        public void ViewAisles()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("                Aisles                ");
            Console.WriteLine("--------------------------------------");

            foreach (var a in Aisles.Values.ToList())
            {
                Console.WriteLine("Aisle {0}\tLength {1}", a.Aisle_ID, a.Length);
                Console.WriteLine("Row_ID\tLength");
                foreach (var r in a.Rows)
                {
                    Console.WriteLine("{0}\t{1}", r.Row_ID, r.Length);
                }

            }
        }
        public void ViewRows()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("                 Rows                 ");
            Console.WriteLine("--------------------------------------");

            foreach (var r in Rows.Values.ToList())
            {
                Console.WriteLine("Row {0}\tLength {1}", r.Row_ID, r.Length);
                Console.WriteLine("In {0}\tOut {1}", r.AisleIn.Aisle_ID, ((r.AisleOut != null) ? r.AisleOut.Aisle_ID : "NIL"));
                Console.WriteLine("Shelf_ID\tHeight");
                foreach (var s in r.Shelves)
                {
                    Console.WriteLine("{0}\t{1}", s.Shelf_ID, s.Length);
                }

            }
        }
        public void ViewShelves()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("                Shelves               ");
            Console.WriteLine("--------------------------------------");

            foreach (var s in Shelves.Values.ToList())
            {
                Console.WriteLine("Shelf {0}\tHeight {1}\tOnRow {2}", s.Shelf_ID, s.Length, s.Row.Row_ID);
                foreach (var r in s.Racks)
                {
                    Console.WriteLine("{0}", r.Rack_ID);
                }

            }
        }
        public void ViewRacks()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("                 Racks                ");
            Console.WriteLine("--------------------------------------");

            foreach (var r in Racks.Values.ToList())
            {
                Console.WriteLine("Rack {0}", r.Rack_ID);
                foreach (var s in r.SKUs)
                {
                    Console.WriteLine("{0}", s.SKU_ID);
                }

            }
        }
        public void ViewSKUs()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("                 SKUs                 ");
            Console.WriteLine("--------------------------------------");

            Console.WriteLine("SKU_ID\tDescription\tRacks");
            foreach (var s in SKUs.Values.ToList())
            {
                Console.Write("{0}\t{1}\t", s.SKU_ID, s.Description);
                foreach (var r in s.QtyAtRack)
                    Console.Write("{0} ", r.Key.Rack_ID);
                Console.WriteLine("");
            }

        }
        #endregion

        #region Layout Builder
        /// <summary>
        /// Create and return a new aisle
        /// </summary>
        public PathAisle CreateAisle(string aisle_ID, double length, double maxSpeed = double.PositiveInfinity, Direction direction = Direction.TwoWay)
        {
            var aisle = new PathAisle(aisle_ID, length, maxSpeed, direction);
            Paths.Add(aisle);
            Aisles.Add(aisle_ID, aisle);
            return aisle;
        }
        /// <summary>
        /// Create and return a new row, connected to aisle(s)
        /// </summary>
        public PathRow CreateRow(string row_ID, double length, PathAisle aisleIn, double inPos, PathAisle aisleOut = null, double outPos = double.NegativeInfinity,
            double maxSpeed = double.PositiveInfinity, Direction direction = Direction.TwoWay)
        {
            var row = new PathRow(row_ID, length, aisleIn, aisleOut, maxSpeed, direction);
            Paths.Add(row);
            Rows.Add(row_ID, row);
            Connect(row, aisleIn, 0, inPos);
            if (aisleOut != null)
                if (!double.IsNegativeInfinity(outPos))
                    Connect(row, aisleOut, row.Length, outPos);
                else
                    throw new Exception("Specify aisleOut position");

            return row;
        }
        /// <summary>
        /// Create and return a new shelf, connected to a row
        /// </summary>
        public PathShelf CreateShelf(string shelf_ID, double height, PathRow row, double pos,
            double maxSpeed = double.PositiveInfinity, Direction direction = Direction.TwoWay)
        {
            var shelf = new PathShelf(shelf_ID, height, row, maxSpeed, direction);
            //Paths.Add(shelf); //Exclude Shelf from Dijkstra : for performance
            Shelves.Add(shelf_ID, shelf);
            Connect(shelf, row, 0, pos);
            shelf.BaseCP = shelf.ControlPoints[0];
            return shelf;
        }
        /// <summary>
        /// Create and return a new rack, on a shelf. Optional SKUs on rack.
        /// </summary>
        public CPRack CreateRack(string rack_ID, PathShelf shelf, double position, List<SKU> SKUs = null)
        {
            var rack = new CPRack(rack_ID, shelf);
            shelf.Add(rack, position);
            //ControlPoints.Add(rack); // Exclude CPRack from Dijkstra : for performance
            Racks.Add(rack_ID, rack);

            // For quick rack-SKU creation. Should not be used with AddToRack (duplicate).
            if (SKUs != null)
                foreach (var s in SKUs)
                {
                    AddToRack(s, rack);
                }

            return rack;
        }
        /// <summary>
        /// Add SKU into a Rack
        /// </summary>
        public void AddToRack(SKU sku, CPRack rack)
        {
            if (!SKUs.ContainsKey(sku.SKU_ID)) SKUs.Add(sku.SKU_ID, sku);

            sku.AddToRack(rack);
        }
        public void AddPickers(PickerType pickerType, int quantity)
        {
            if (!NumPickers.ContainsKey(pickerType)) NumPickers.Add(pickerType, 0);
            NumPickers[pickerType] += quantity;
        }

        // Consider making private the 3 methods below:
        /// <summary>
        /// Create and return a new control point
        /// </summary>
        public ControlPoint CreateControlPoint(Path path, double position)
        {
            var controlPoint = new ControlPoint();
            path.Add(controlPoint, position);
            ControlPoints.Add(controlPoint);
            return controlPoint;
        }
        /// <summary>
        /// Connect two paths at specified positions
        /// </summary>
        public void Connect(Path path_0, Path path_1, double position_0, double position_1)
        {
            var controlPoint = CreateControlPoint(path_0, position_0);
            path_1.Add(controlPoint, position_1);
        }
        /// <summary>
        /// Connect the end of path_0 to the start of path_1
        /// </summary>
        public void Connect(Path path_0, Path path_1) { Connect(path_0, path_1, path_0.Length, 0); }
        #endregion

        #region PickList Reader     
        public void ReadMasterPickList()
        {
            int count = 1;
            string filename = @"Picklist\" + Name + "_Picklist_" + count.ToString() + ".csv";

            while (File.Exists(filename))
            {
                ReadPickList(filename);

                count++;
                filename = @"Picklist\" + Name + "_Picklist_" + count.ToString() + ".csv";
            }
        }

        private void ReadPickList(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                List<PickJob> picklist = new List<PickJob>();
                string line = sr.ReadLine();

                string type_id = line.Split(',').First(); // First line is picker type id

                while ((line = sr.ReadLine()) != null)
                {
                    var data = line.Split(',');
                    var sku = SKUs[data[0]];
                    var rack = Racks[data[1]];
                    var qty = int.Parse(data[2]);

                    picklist.Add(new PickJob(sku, rack, qty));
                }

                MasterPickList[GetPickerType[type_id]].Add(picklist);
            }

        }
        #endregion

        #region Picker Reader
        public void ReadPickers()
        {
            string filename = @"Picklist\" + Name + "_Pickers.csv";

            var pickerTypes = CSVToList(filename); // ID, move speed, pick time, numPickers

            foreach (var data in pickerTypes)
            {
                var id = data[0];
                var moveSpd = double.Parse(data[1]); // From metres per sec
                var pickingTime = TimeSpan.FromSeconds(double.Parse(data[2])); // Seconds per item
                var numPickers = int.Parse(data[3]);

                int capacity;
                PickerType type;

                if (int.TryParse(data[4], out capacity))
                    type = new PickerType(id, moveSpd, pickingTime, capacity);
                else
                    type = new PickerType(id, moveSpd, pickingTime);

                MasterPickList.Add(type, new List<List<PickJob>>());
                CompletedPickLists.Add(type, new List<List<PickJob>>());

                GetPickerType.Add(id, type);
                NumPickers.Add(type, numPickers);
            }

            // Init pickers
            AllPickers = NumPickers.SelectMany(item => Enumerable.Range(0, item.Value).Select(i => new Picker(item.Key))).ToList();
        }
        #endregion

        #region For Static Routing (Distance-Based), Using Dijkstra
        public void InitializeRouting()
        {
            ConstructRoutingTables();
            ConstructPathingTables();
        }
        private void ConstructRoutingTables()
        {
            foreach (var cp in ControlPoints) cp.RoutingTable = new Dictionary<ControlPoint, ControlPoint>();
            var incompleteSet = ControlPoints.ToList();
            var edges = Paths.SelectMany(path => GetEdges(path)).ToArray();
            while (incompleteSet.Count > 0)
            {
                ConstructRoutingTables(incompleteSet.First().Id, edges);
                incompleteSet.RemoveAll(cp => cp.RoutingTable.Count == ControlPoints.Count - 1);
            }
        }
        private void ConstructRoutingTables(int sourceIndex, Dijkstra.Edge[] edges)
        {
            var edgeList = edges.ToList();
            edgeList.Add(new Dijkstra.Edge(0, sourceIndex, 0)); // set the source
            var dijkstra = new Dijkstra(edgeList.ToArray());
            var parents = dijkstra.Parents;
            for (int target = 1; target < parents.Length; target++)
            {
                var current = target;
                while (current != sourceIndex)
                {
                    var parent = parents[current];
                    if (!ControlPoints[parent - 1].RoutingTable.ContainsKey(ControlPoints[target - 1]))
                        ControlPoints[parent - 1].RoutingTable.Add(ControlPoints[target - 1], ControlPoints[current - 1]);
                    current = parent;
                }
            }
        }
        private void ConstructPathingTables()
        {
            foreach (var cp in ControlPoints) cp.PathingTable = new Dictionary<ControlPoint, Path>();
            foreach (var path in Paths)
            {
                // assume same pair of control points are connected only by one path
                if (path.Direction != Direction.Backward)
                    for (int i = 0; i < path.ControlPoints.Count - 1; i++)
                        path.ControlPoints[i].PathingTable.Add(path.ControlPoints[i + 1], path);
                if (path.Direction != Direction.Forward)
                    for (int i = path.ControlPoints.Count - 1; i > 0; i--)
                        path.ControlPoints[i].PathingTable.Add(path.ControlPoints[i - 1], path);
            }
        }
        private List<Dijkstra.Edge> GetEdges(Path path)
        {
            var edges = new List<Dijkstra.Edge>();
            for (int i = 0; i < path.ControlPoints.Count - 1; i++)
            {
                var length = path.ControlPoints[i + 1].Positions[path] - path.ControlPoints[i].Positions[path];
                var from = path.ControlPoints[i].Id;
                var to = path.ControlPoints[i + 1].Id;
                if (path.Direction != Direction.Backward) edges.Add(new Dijkstra.Edge(from, to, length));
                if (path.Direction != Direction.Forward) edges.Add(new Dijkstra.Edge(to, from, length));
            }
            return edges;
        }
        #endregion
    }
}