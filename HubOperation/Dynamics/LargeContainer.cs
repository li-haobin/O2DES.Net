﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HubOperation.Dynamics
{
    class LargeContainer : Container
    {
        public LargeContainer(DateTime ready, List<Package> list)
        {
            ReadyTime = ready;
            PackagesList = new List<Package>(list);
            isEmpty = false;
            isUnloading = false;
            FinishUnloadingTime = new DateTime();
            Type = "L";
        }

        public LargeContainer(Dynamics.Container container)
        {
            ReadyTime = container.ReadyTime;
            PackagesList = new List<Package>(container.PackagesList);
            isEmpty = container.isEmpty;
            isUnloading = container.isUnloading;
            FinishUnloadingTime = container.FinishUnloadingTime;
            Type = "L";
        }

        public LargeContainer()
        {
            ReadyTime = new DateTime();
            PackagesList = new List<Package>();
            isEmpty = false;
            isUnloading = false;
            FinishUnloadingTime = new DateTime();
            Type = "L";
        }
    }
}
