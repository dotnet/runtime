// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Refresh
{
    public static class Program
    {
        public static bool UsesGcWithRegions 
        {
            get
            {
                // This is a way to guess if the runtime is using GC with regions, since we have no precise way to query that.
                // It works for this test since we don't set the GCRegionRange explicitly, and the default value is 0 
                // for non-GC with regions runtimes, and non-zero for GC with regions runtimes.
                if (GC.GetConfigurationVariables().TryGetValue("GCRegionRange", out object? value) &&
                    value is long range && range != 0)
                {
                    return true;
                }
                return false;
            }
        }

        [SkipOnCoreClr("This test is not compatible with GC stress.", RuntimeTestModes.AnyGCStress)]
        [ConditionalFact(typeof(Program), nameof(UsesGcWithRegions))]
        public static int TestEntryPoint()
        {
            long hundred_mb = 100 * 1024 * 1024;
            long two_hundred_mb = 2 * hundred_mb;
            AppContext.SetData("GCHeapHardLimit", (ulong)hundred_mb);
            GC.RefreshMemoryLimit();
            GCMemoryInfo gcMemoryInfo = GC.GetGCMemoryInfo();
            if (gcMemoryInfo.TotalAvailableMemoryBytes != hundred_mb)
            {
                Console.WriteLine("Fail");
                return 101;
            }
            AppContext.SetData("GCHeapHardLimit", (ulong)two_hundred_mb);
            GC.RefreshMemoryLimit();
            gcMemoryInfo = GC.GetGCMemoryInfo();
            if (gcMemoryInfo.TotalAvailableMemoryBytes != two_hundred_mb)
            {
                Console.WriteLine("Fail");
                return 101;
            }
            AppContext.SetData("GCHeapHardLimit", (ulong)hundred_mb);
            GC.RefreshMemoryLimit();
            gcMemoryInfo = GC.GetGCMemoryInfo();
            if (gcMemoryInfo.TotalAvailableMemoryBytes != hundred_mb)
            {
                Console.WriteLine("Fail");
                return 101;
            }
            Console.WriteLine("Pass");
            return 100;
        }
    }
}
