﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using O2DESNet.Warehouse.Dynamics;

namespace O2DESNet.Warehouse.Statics
{
    /// <summary>
    /// Static class for generating picklists based on specified rule.
    /// Requires the use of Layout, SKU and inventory snapshot.
    /// </summary>
    public static class PicklistGenerator
    {
        public enum Strategy { A, B, C, D };

        const string A_PickerID = "Strategy_A_Picker";

        const string B_PickerID_SingleItem = "Strategy_B_SingleItem";
        const string B_PickerID_SingleZone = "Strategy_B_SingleZone";
        const string B_PickerID_MultiZone = "Strategy_B_MultiZone";

        const string D_PickerID_SingleItem = "Strategy_D_SingleItem";
        const string D_PickerID_MultiItem = "Strategy_D_MultiItem";

        const string C_PickerID_SingleItem = "Strategy_C_SingleItem";
        const string C_PickerID_SingleZone = "Strategy_C_SingleZone";
        const string C_PickerID_MultiZone = "Strategy_C_MultiZone";

        public static Dictionary<string, Order> AllOrders { get; private set; }
        public static Dictionary<PickerType, List<List<PickJob>>> MasterPickList { get; private set; }

        // For debug
        public static List<string> MissingSKU { get; private set; } // SKU in order but missing in inventory
        public static List<string> InsufficientSKU { get; private set; } // Insufficient inventory

        #region Picklist generation
        /// <summary>
        /// Generate picklists to FILE, based on given strategy. Optional copy to scenario directly.
        /// </summary>
        /// <param name="strategy"></param>
        /// <param name="scenario"></param>
        public static void Generate(Strategy strategy, Scenario scenario, bool copyToScenario = false)
        {
            if (AllOrders.Count == 0) throw new Exception("Orders have not been read! Read orders first.");

            MasterPickList = new Dictionary<PickerType, List<List<PickJob>>>();

            if (strategy == Strategy.A) StrategyA(scenario);
            if (strategy == Strategy.B) StrategyB(scenario);
            if (strategy == Strategy.C) StrategyC(scenario);
            if (strategy == Strategy.D) StrategyD(scenario);

            SortByLocation();

            WriteToFiles(scenario);

            if (copyToScenario) CopyToScenario(scenario);

            // For debug
            using (StreamWriter sw = new StreamWriter(@"Picklist\" + scenario.Name + "_InsufficientSKUs.csv"))
            {
                foreach (var sku_id in InsufficientSKU)
                {
                    sw.WriteLine(sku_id);
                }
            }

        }
        /// <summary>
        /// Write MasterPickList to files
        /// </summary>
        /// <param name="scenario"></param>
        private static void WriteToFiles(Scenario scenario)
        {
            DeletePicklistFiles(scenario);

            int count = 1;
            string filename = @"Picklist\" + scenario.Name + "_Picklist_" + count.ToString() + ".csv";

            foreach (var pickerType in MasterPickList)
            {
                var type_ID = pickerType.Key.PickerType_ID;
                var typePicklists = pickerType.Value;

                foreach (var picklist in typePicklists)
                {
                    // One picklist one file
                    using (StreamWriter output = new StreamWriter(filename))
                    {
                        output.WriteLine(type_ID); // First line is PickerType_ID
                        foreach (var pickJob in picklist)
                        {
                            output.WriteLine("{0},{1},{2}", pickJob.item.SKU_ID, pickJob.rack.Rack_ID, pickJob.quantity);
                        }
                    }

                    count++;
                    filename = @"Picklist\" + scenario.Name + "_Picklist_" + count.ToString() + ".csv";
                }
            }
        }
        /// <summary>
        /// Copy MasterPickList to scenario.MasterPickList
        /// </summary>
        /// <param name="scenario"></param>
        private static void CopyToScenario(Scenario scenario)
        {
            var types = scenario.MasterPickList.Keys.ToList();

            for (int i = 0; i < types.Count; i++)
            {
                scenario.MasterPickList[types[i]].Clear();

                if (MasterPickList.ContainsKey(types[i]))
                {
                    scenario.MasterPickList[types[i]] = new List<List<PickJob>>(MasterPickList[types[i]]);
                }
            }
        }
        private static void DeletePicklistFiles(Scenario scenario)
        {
            int count = 1;
            string filename = @"Picklist\" + scenario.Name + "_Picklist_" + count.ToString() + ".csv";

            while (File.Exists(filename))
            {
                File.Delete(filename);
                count++;
                filename = @"Picklist\" + scenario.Name + "_Picklist_" + count.ToString() + ".csv";
            }

        }
        /// <summary>
        /// Sort each picklist by location (PickJob.CPRack.Rack_ID)
        /// </summary>
        private static void SortByLocation()
        {
            foreach (var type in MasterPickList.Keys)
            {
                var typePicklists = MasterPickList[type];

                for (int i = 0; i < typePicklists.Count; i++)
                {
                    typePicklists[i] = typePicklists[i].OrderBy(o => o.rack.Rack_ID).ToList();
                }
            }
        }
        #endregion

        #region Strategies

        /// <summary>
        /// Current strategy. Sequential assignment of orders. Only one PickerType.
        /// </summary>
        /// <param name="scenario"></param>
        private static void StrategyA(Scenario scenario)
        {
            List<Order> orders = AllOrders.Values.ToList();

            // Assume only one picker type A_Picker
            GeneratePicklistsFromOrders(scenario, orders, A_PickerID);
        }
        /// <summary>
        /// Hybrid Order Picking
        /// </summary>
        /// <param name="scenario"></param>
        private static void StrategyB(Scenario scenario)
        {
            List<Order> orders = AllOrders.Values.ToList();
            List<Order> singleItemOrders = ExtractSingleItemOrders(orders);

            GenerateSingleZoneOrders(scenario, orders, B_PickerID_SingleZone);

            // Remaining order in List orders are multi-zone orders
            GeneratePicklistsFromOrders(scenario, orders, B_PickerID_MultiZone);
            // Single-Item orders last
            GeneratePicklistsFromOrders(scenario, singleItemOrders, B_PickerID_SingleItem);
        }
        /// <summary>
        /// Hybrid Zone Picking
        /// </summary>
        /// <param name="scenario"></param>
        private static void StrategyC(Scenario scenario)
        {
            List<Order> orders = AllOrders.Values.ToList();

            List<Order> singleItemOrders = ExtractSingleItemOrders(orders);

            GenerateSingleZoneOrders(scenario, orders, C_PickerID_SingleZone);

            // Split remaining orders into zones
            GeneratePureZoneOrders(scenario, orders, C_PickerID_MultiZone);

            // Single-Item orders last
            GeneratePicklistsFromOrders(scenario, singleItemOrders, C_PickerID_SingleItem);
        }
        /// <summary>
        /// Pure Zone Picking
        /// </summary>
        /// <param name="scenario"></param>
        private static void StrategyD(Scenario scenario)
        {
            List<Order> orders = AllOrders.Values.ToList();

            List<Order> singleItemOrders = ExtractSingleItemOrders(orders);

            // Split remaining orders into zones
            GeneratePureZoneOrders(scenario, orders, D_PickerID_MultiItem);

            // Single-Item orders last
            GeneratePicklistsFromOrders(scenario, singleItemOrders, D_PickerID_SingleItem);
        }

        /// <summary>
        /// Generate picklists for pure zone orders. No orders should remain.
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="orders"></param>
        private static void GeneratePureZoneOrders(Scenario scenario, List<Order> orders, string pickerID)
        {
            HashSet<string> allZones = GetFulfilmentZones(orders);

            List<SKU> items = orders.SelectMany(order => order.Items).ToList(); // Flattening items from orders

            foreach (var zone in allZones)
            {
                var oneZone = items.ExtractAll(item => item.IsFulfiledZone(zone)); // Potentially fulfilled in this zone

                var unfulfilled = GeneratePicklistsFromItems(scenario, oneZone, pickerID, zone);

                items.AddRange(unfulfilled); // Append back unfulfilled items
            }
        }
        /// <summary>
        /// Generate picklists for single zone orders. Remaining orders in List are unfulfilled.
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="orders"></param>
        private static void GenerateSingleZoneOrders(Scenario scenario, List<Order> orders, string pickerID)
        {
            // Determine orders with items in a single zone

            HashSet<string> allZones = GetFulfilmentZones(orders);
            // Find single-zone orders
            foreach (var zone in allZones)
            {
                List<Order> zoneOrders = orders.ExtractAll(order => order.IsSingleZoneFulfil(zone)); // Potentially fulfiled in zone

                var unfilfilled = GeneratePicklistsFromOrders(scenario, zoneOrders, pickerID, zone); // Reservation done here

                orders.AddRange(unfilfilled); // Append back unfulfilled orders
            }
        }

        /// <summary>
        /// Determine the zones where the orders can be fulfilled
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        private static HashSet<string> GetFulfilmentZones(List<Order> orders)
        {
            // Init zones of interest
            HashSet<string> allZones = new HashSet<string>();
            foreach (var order in orders)
            {
                foreach (var item in order.Items)
                {
                    allZones.UnionWith(item.GetFulfilmentZones());
                }
            }

            return allZones;
        }
        /// <summary>
        /// Generate picklists for specified picker type from given set of orders. Optional only from single zone. Return unfulfilled items.
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="items"></param>
        /// <param name="pickerType_ID"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        private static List<SKU> GeneratePicklistsFromItems(Scenario scenario, List<SKU> items, string pickerType_ID, string zone = null)
        {
            List<SKU> unfulfilledItems = new List<SKU>();

            var type = scenario.GetPickerType[pickerType_ID];
            if (!MasterPickList.ContainsKey(type)) MasterPickList.Add(type, new List<List<PickJob>>());

            while (items.Count > 0)
            {
                if (zone != null && !items.First().IsFulfiledZone(zone))
                {
                    unfulfilledItems.Add(items.First());
                }
                else
                {
                    // If does not fit, create new picklist
                    if (MasterPickList[type].Count == 0 || MasterPickList[type].Last().Count >= type.Capacity)
                        MasterPickList[type].Add(new List<PickJob>());

                    // Process item
                    bool isReserved = ReserveItem(type, items.First(), zone);
                    if (!isReserved)
                    {
                        InsufficientSKU.Add(items.First().SKU_ID); // Add to insufficient
                                                                   // throw new Exception("No available quantity for reservation for SKU " + items.First().SKU_ID);
                    }
                }
                // Next item
                items.RemoveAt(0);
            }


            return unfulfilledItems;
        }
        /// <summary>
        /// Generate picklists for specified picker type from given set of orders. Optional only from single zone. Return unfulfilled orders.
        /// </summary>
        /// <param name="pickerType_ID"></param>
        /// <param name="orders"></param>
        /// <param name="scenario"></param>
        private static List<Order> GeneratePicklistsFromOrders(Scenario scenario, List<Order> orders, string pickerType_ID, string zone = null)
        {
            List<Order> unfulfilledOrders = new List<Order>();

            var type = scenario.GetPickerType[pickerType_ID];
            if (!MasterPickList.ContainsKey(type)) MasterPickList.Add(type, new List<List<PickJob>>());

            while (orders.Count > 0)
            {
                if (zone != null && !orders.First().IsSingleZoneFulfil(zone))
                {
                    unfulfilledOrders.Add(orders.First());
                }
                else
                {
                    // If does not fit, create new picklist
                    if (MasterPickList[type].Count == 0 || MasterPickList[type].Last().Count + orders.First().Items.Count > type.Capacity)
                        MasterPickList[type].Add(new List<PickJob>());

                    // Process items in current order
                    foreach (var item in orders.First().Items)
                    {
                        bool isReserved = ReserveItem(type, item, zone);

                        if (!isReserved)
                        {
                            InsufficientSKU.Add(item.SKU_ID); // Add to insufficient
                                                              // throw new Exception("No available quantity for reservation for SKU " + item.SKU_ID);
                        }
                    }

                }
                // Next order
                orders.RemoveAt(0);
            }

            return unfulfilledOrders;
        }

        /// <summary>
        /// Inventory reservation procedure
        /// </summary>
        /// <param name="type"></param>
        /// <param name="item"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        private static bool ReserveItem(PickerType type, SKU item, string zone = null)
        {
            var locations = item.QtyAtRack.Keys.ToList();
            bool reserved = false;
            // Inventory reservation procedure
            while (locations.Count > 0 && !reserved)
            {
                // Check item availability
                var rack = locations.First(); // ONLY FROM DESIRED ZONE
                if (zone != null && zone != rack.GetZone()) locations.RemoveAt(0);
                else
                {
                    if (item.GetQtyAvailable(rack) > 0)
                    {
                        // Reserve item at location
                        item.ReserveFromRack(rack);
                        // Add to curPicklist
                        MasterPickList[type].Last().Add(new PickJob(item, rack));
                        reserved = true;
                    }
                    else
                    {
                        locations.RemoveAt(0);
                    }
                }
            }

            return reserved;
        }
        private static List<Order> ExtractSingleItemOrders(List<Order> orders)
        {
            return orders.ExtractAll(order => order.Items.Count == 1);
        }
        /// <summary>
        /// List extension to extract elements defined by predicate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        private static List<T> ExtractAll<T>(this List<T> source, Predicate<T> match)
        {
            List<T> extract = source.FindAll(match);
            source.RemoveAll(match);
            return extract;
        }
        #endregion

        #region Read Orders    
        /// <summary>
        /// CSV file in Picklist folder
        /// </summary>
        /// <param name="filename"></param>
        public static void ReadOrders(Scenario scenario, string filename)
        {
            // For debug
            MissingSKU = new List<string>();
            InsufficientSKU = new List<string>();

            AllOrders = new Dictionary<string, Order>();

            filename = @"Picklist\" + filename;
            if (!File.Exists(filename))
                throw new Exception("Order file " + filename + " does not exist");

            using (StreamReader sr = new StreamReader(filename))
            {
                string line = sr.ReadLine(); // First line header: Order_ID, SKU

                while ((line = sr.ReadLine()) != null)
                {
                    var data = line.Split(',');
                    var id = data[0];
                    var sku = data[1];

                    AddOrCreateOrder(scenario, id, sku);
                }
            }

            // For debug, write Missing SKU into file
            using (StreamWriter sw = new StreamWriter(@"Picklist\" + scenario.Name + "_MissingSKUs.csv"))
            {
                foreach (var sku_id in MissingSKU)
                {
                    sw.WriteLine(sku_id);
                }
            }

        }
        private static void AddOrCreateOrder(Scenario scenario, string order_id, string sku_id)
        {
            if (!scenario.SKUs.ContainsKey(sku_id))
            {
                // Record missing SKU
                MissingSKU.Add(sku_id);
            }
            else
            {

                // Find SKU
                var sku = scenario.SKUs[sku_id];

                // New order
                if (!AllOrders.ContainsKey(order_id))
                {
                    AllOrders.Add(order_id, new Order(order_id));
                }

                // Add SKU to order
                AllOrders[order_id].Items.Add(sku);
            }
        }
        #endregion
    }
}
