﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using O2DESNet.Warehouse.Dynamics;

namespace O2DESNet.Warehouse.Events
{
    [Serializable]
    internal class BeginSorting : Event
    {
        internal SortingStation sortingStation;

        internal BeginSorting(Simulator sim, SortingStation station) : base(sim)
        {
            sortingStation = station;
        }

        public override void Invoke()
        {
            // Record Statistics before clearing
            sortingStation.orderBatch.CompletionTime = _sim.ClockTime;
            _sim.Status.OrderBatchCompletionTime.Add(sortingStation.orderBatch, _sim.ClockTime - (DateTime)sortingStation.orderBatch.StartTime);

            double sortingTime = sortingStation.GetSortingTime(); // Calculates numItems as well
            int numItems = sortingStation.numItems;
            _sim.Status.NumItemsSorted += numItems;
            if (numItems > _sim.Status.MaxNumItemsSorted) _sim.Status.MaxNumItemsSorted = numItems;
            _sim.Status.OrderBatchWaitingTimeForSorting.Add(sortingStation.orderBatch, _sim.ClockTime - _sim.Status.OrderBatchStartWaitForSorting[sortingStation.orderBatch]);

            // Clear and continue to sort
            sortingStation.ClearSortingStation();
            _sim.Status.DecrementBatchWaiting();
            _sim.Status.DecrementToteWaiting(numItems);
            _sim.Scenario.Consolidator.NumSortersAvailable--;
            _sim.Status.IncrementActiveSorter();
            _sim.ScheduleEvent(new CompleteSorting(_sim, sortingStation), TimeSpan.FromMinutes(sortingTime));
        }

        public override void Backtrack()
        {
            throw new NotImplementedException();
        }
    }
}
