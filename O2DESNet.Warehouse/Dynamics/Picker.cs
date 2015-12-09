﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using O2DESNet.Warehouse.Statics;

namespace O2DESNet.Warehouse.Dynamics
{
    public class Picker
    {
        public ControlPoint CurLocation;
        public PickerType Type;
        public List<Pick> PickList;
        public Dictionary<SKU, int> Items;

        public Picker(PickerType type)
        {
            CurLocation = null;
            Type = type;
            PickList = new List<Pick>();
            Items = new Dictionary<SKU, int>();
        }

        // All time in seconds
        public double GetNextTravelTime(ControlPoint destination)
        {
            return Type.GetTravellingTime(CurLocation, destination);
        }
        public TimeSpan GetNextPickingTime()
        {
            return Type.GetPickingTime();
        }
        public void PickNextItem()
        {
            var pick = PickList.First();
            if (CurLocation != pick.location) throw new Exception("Wrong location, unable to pick");

            if (pick.item.Racks[pick.location] >= pick.quantity)
                pick.item.Racks[pick.location] -= pick.quantity;
            else
                throw new Exception("Shortage of item at location");

            if (!Items.ContainsKey(pick.item)) Items.Add(pick.item, 1);
            else Items[pick.item] += pick.quantity;

            PickList.RemoveAt(0);
        }
    }
}