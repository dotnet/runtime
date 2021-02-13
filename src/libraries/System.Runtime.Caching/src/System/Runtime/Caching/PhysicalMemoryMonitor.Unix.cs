// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace System.Runtime.Caching
{
    internal sealed partial class PhysicalMemoryMonitor : MemoryMonitor
    {
        private static Regex availableRegex = new Regex(@"MemAvailable:\s*([0-9]+)\s*kB", RegexOptions.Compiled);
        private static Regex totalRegex = new Regex(@"MemTotal:\s*([0-9]+)\s*kB", RegexOptions.Compiled);


#if NET5_0
        internal static bool IsSupported() => true;
        private long lastGCIndex;
#else
        internal static bool IsSupported() => ReadMemInfo(out ulong t, out ulong a);
#endif

        /*
         * For .Net 5 and later, use the GC-supported stats first. Then fallback on
         *  the method for netstandard 2.0 as follows:
         * Use /proc/meminfo to get this information. Specifically "MemAvailable" and
         *  "MemTotal." The former was a Linux addition a few years ago and seems to be
         *  widespread among Linux distros. This is potentially inaccurate in container
         *  scenarios, but then again, if the container is restricted in memory use
         *  then this "good citizen" memory monitor probably doesn't make much sense
         *  anyway since the current process is likely to be the _only_ citizen of
         *  imortance.
         * Note: BSD and OSX often don't have procfs installed or mounted and may not have
         *  "MemAvailable" as a field of meminfo. These OS's could fall back on
         *  using 'sysconf' with _SC_PAGE_SIZE, _SC_PHYS_PAGES, and _SC_AVPHYS_PAGES.
         *  But adding shims to sysconf is outside the scope of getting this working
         *  on Linux and other /proc/meminfo-supporting OS's. (Also note that
         *  _SC_AVPHYS_PAGES is not quite the same as "MemAvailable" - See
         *  https://github.com/dotnet/runtime/issues/13371
         */

        protected override int GetCurrentPressure()
        {
            int memoryLoad = 0;

#if NET5_0
            // First get stats from GC. Be sure they are fresher than the last time we checked.
            GCMemoryInfo memInfo = GC.GetGCMemoryInfo();
            if (memInfo.Index <= lastGCIndex)
            {
                GC.Collect(0);
                memInfo = GC.GetGCMemoryInfo();
            }
            lastGCIndex = memInfo.Index;

            if (memInfo.TotalAvailableMemoryBytes >= memInfo.MemoryLoadBytes)
            {
                // MemoryLoadBytes is current memory use. Not dependent on container limits.
                // TotalAvailableMemoryBytes does NOT account for container limits in 3.1.
                //      It does in 5.0 if there are any. (https://github.com/dotnet/coreclr/pull/25437)
                memoryLoad = (int)((float)memInfo.MemoryLoadBytes * 100.0 / (float)memInfo.TotalAvailableMemoryBytes);
                return Math.Max(1, memoryLoad);
            }
#endif

            // Fallback on reading /proc/meminfo
            if (ReadMemInfo(out ulong total, out ulong available))
            {
                memoryLoad = (int)(((float)(total - available) * 100) / (float)total);

                // Zero will be interpretted by callers as not being able to get a reading. Return
                // something less confusing since the reading is legit but resulted in 0 after
                // casting to int.
                return Math.Max(1, memoryLoad);
            }

            // Something didn't read correctly. Just use what GC told us - or '0' if on netstandard 2.0.
            return memoryLoad;
        }

        // Get memory stats from /proc/meminfo. According to this commit
        // (https://git.kernel.org/pub/scm/linux/kernel/git/torvalds/linux.git/commit/?id=34e431b0ae398fc54ea69ff85ec700722c9da773),
        // the best estimate of available RAM before paging is forced is the "MemAvailable" number introduced
        // by that commit. The goal of this monitor is to be a good citizen and avoid causing paging or
        // other perf degradations associated with low available RAM on the system. The doc for MEMORYSTATUSEX
        // says dwMemoryLoad represents "approximate percentage of physical memory that is in use".
        // Exact precision or perscribing to a particular definition of "physical memory that is in use"
        // is not of utmost importance here. Having roughly analagous behavior on Windows and Unix is the goal.
        private static bool ReadMemInfo(out ulong total, out ulong available)
        {
            bool totalValid = false;
            bool availableValid = false;
            total = available = 0;

            if (File.Exists("/proc/meminfo"))
            {
                try
                {
                    string s = File.ReadAllText("/proc/meminfo");

                    // MemTotal
                    Match tMatch = totalRegex.Match(s);
                    if (tMatch.Success)
                    {
                        totalValid = ulong.TryParse(tMatch.Groups[1].Value, out total);
                    }

                    // MemAvailable
                    Match aMatch = availableRegex.Match(s);
                    if (aMatch.Success)
                    {
                        availableValid = ulong.TryParse(aMatch.Groups[1].Value, out available);
                    }
                }
                catch
                {
                    return false;
                }
            }

            return (totalValid && availableValid && (total >= available));
        }
    }
}
