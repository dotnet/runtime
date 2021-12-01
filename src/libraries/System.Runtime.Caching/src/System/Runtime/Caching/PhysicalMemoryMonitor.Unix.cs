// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace System.Runtime.Caching
{
    internal sealed partial class PhysicalMemoryMonitor : MemoryMonitor
    {
        /* There are sysconf and /proc/meminfo ways to get this information before .Net Core 3,
         * but it is very complicated to do it correctly, especially when accounting for
         * container scenarios. The GC does this for us in .Net Core 3.
         *
         * Note: This is still a little bit off in some restrited memory scenarios, as
         * 'TotalAvailableMemoryBytes' does not exactly report all of the available bytes.
         * But if running in a memory-restricted Linux container, this "good citizen"
         * memory monitor feels a little redundant anyway since memory is already capped
         * via the system.
         * But this comment (https://github.com/dotnet/coreclr/pull/25437#discussion_r299810957)
         * highlights how our behavior might be slightly different from windows in these
         * cases since this was a monitor that cared about actual physical memory.
         */
#if NETCOREAPP
        private int lastGCCount;

        protected override int GetCurrentPressure()
        {
            // Try to refresh GC stats if they haven't been updated since our last check.
            int ccount = GC.CollectionCount(0);
            if (ccount == lastGCCount)
            {
                GC.Collect(0, GCCollectionMode.Optimized); // A quick, ephemeral Gen 0 collection
                ccount = GC.CollectionCount(0);
            }
            lastGCCount = ccount;

            // Get stats from GC.
            GCMemoryInfo memInfo = GC.GetGCMemoryInfo();

            if (memInfo.TotalAvailableMemoryBytes >= memInfo.MemoryLoadBytes)
            {
                int memoryLoad = (int)((float)memInfo.MemoryLoadBytes * 100.0 / (float)memInfo.TotalAvailableMemoryBytes);
                return Math.Max(1, memoryLoad);
            }

            // It's possible the load was legitimately higher than "available". In that case, return 100.
            // Otherwise, return 0 to minimize impact because something was unexpected.
            return (memInfo.MemoryLoadBytes > 0) ? 100 : 0;
        }
#else
        protected override int GetCurrentPressure() => 0;
#endif
    }
}
