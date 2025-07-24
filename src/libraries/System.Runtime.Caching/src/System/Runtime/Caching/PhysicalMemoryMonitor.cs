// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.Caching.Configuration;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Caching
{
    // PhysicalMemoryMonitor monitors the amound of physical memory used on the machine
    // and helps us determine when to drop entries to avoid paging and GC thrashing.
    // The limit is configurable (see ConfigUtil.cs).
    internal sealed partial class PhysicalMemoryMonitor : MemoryMonitor
    {
        private const int MinTotalMemoryTrimPercent = 10;
        private const long TargetTotalMemoryTrimIntervalTicks = 5 * TimeSpan.TicksPerMinute;

        // It can be difficult to determine an accurate total for physical memory on non-bare-metal-windows systems.
        // This value has some special meanings:
        //  -1: indicates physical memory pressure should be determined in the classical way; platform-specific code & GC induced stats on non-windows.
        //   0: indicates physical memory pressure should always be based on latest GCMemroyInfo stats, without induced GC.
        //  >0: same as 0, except the total physical memory is given by this value rather than GCMemoryInfo.TotalAvailableMemoryBytes.
        private long _physicalMemoryBytesAvailable;

#if NETCOREAPP
        // This value indicates that "high" pressure should follow the GC's definition of high pressure. This is only applicable on .Net Core.
        // This can be set to true by specifying a physicalMemoryLimitPercentage of 1 in the configuration.
        private bool _followGCThresholds;
#endif

        // Returns the percentage of physical machine memory that can be consumed by an
        // application before the cache starts forcibly removing items.
        internal long MemoryLimit
        {
            get { return _pressureHigh; }
        }

        private PhysicalMemoryMonitor()
        {
            // hide default ctor
        }

        internal PhysicalMemoryMonitor(int physicalMemoryLimitPercentage, long physicalMemoryBytesAvailable)
        {
            SetLimit(physicalMemoryLimitPercentage, physicalMemoryBytesAvailable);
            InitHistory();
        }

        internal override int GetPercentToTrim(DateTime lastTrimTime, int lastTrimPercent)
        {
            int percent = 0;
            if (IsAboveHighPressure())
            {
                // choose percent such that we don't repeat this for ~5 (TARGET_TOTAL_MEMORY_TRIM_INTERVAL) minutes,
                // but keep the percentage between 10 and 50.
                DateTime utcNow = DateTime.UtcNow;
                long ticksSinceTrim = utcNow.Subtract(lastTrimTime).Ticks;
                if (ticksSinceTrim > 0)
                {
                    percent = Math.Min(50, (int)((lastTrimPercent * TargetTotalMemoryTrimIntervalTicks) / ticksSinceTrim));
                    percent = Math.Max(MinTotalMemoryTrimPercent, percent);
                }

#if PERF
                Debug.WriteLine($"PhysicalMemoryMonitor.GetPercentToTrim: percent={percent:N}, lastTrimPercent={lastTrimPercent:N}, secondsSinceTrim={ticksSinceTrim/TimeSpan.TicksPerSecond:N}{Environment.NewLine}");
#endif
            }

            return percent;
        }

        internal void SetLimit(int physicalMemoryLimitPercentage, long physicalMemoryBytesAvailable)
        {
            _physicalMemoryBytesAvailable = physicalMemoryBytesAvailable;

            if (physicalMemoryLimitPercentage == 0)
            {
                UseDefaultLimits();
            }
#if NETCOREAPP
            // Using GC thresholds only applies on .Net Core, and not in legacy mode.
            // Technically, the non-windows legacy code could compare against GC thresholds still, but allowing
            // that runs the risk of concept confusion for these already overloaded settings. So just let the
            // legacy mode stay legacy without the new features.
            else if (physicalMemoryLimitPercentage == 1 && physicalMemoryBytesAvailable >= 0)
            {
                _followGCThresholds = true;
                _pressureHigh = 100; // 100%, because we will check against the GC's high memory load threshold instead of total physical memory.
                _pressureLow = _pressureHigh - 9;
            }
#endif
            else
            {
                _pressureHigh = Math.Max(3, physicalMemoryLimitPercentage);
                _pressureLow = Math.Max(1, _pressureHigh - 9);
            }

            Dbg.Trace("MemoryCacheStats", $"PhysicalMemoryMonitor.SetLimit: _pressureHigh={_pressureHigh}, _pressureLow={_pressureLow}");
        }

        private void UseDefaultLimits()
        {
            /*
              The chart below shows physical memory in megabytes, and the 1, 3, and 10% values.
              When we reach "middle" pressure, we begin trimming the cache.

              RAM     1%      3%      10%
              -----------------------------
              128     1.28    3.84    12.8
              256     2.56    7.68    25.6
              512     5.12    15.36   51.2
              1024    10.24   30.72   102.4
              2048    20.48   61.44   204.8
              4096    40.96   122.88  409.6
              8192    81.92   245.76  819.2

            */

            long memory = (_physicalMemoryBytesAvailable > 0) ? _physicalMemoryBytesAvailable : TotalPhysical;
            if (memory >= 0x100000000)
            {
                _pressureHigh = 99;
            }
            else if (memory >= 0x80000000)
            {
                _pressureHigh = 98;
            }
            else if (memory >= 0x40000000)
            {
                _pressureHigh = 97;
            }
            else if (memory >= 0x30000000)
            {
                _pressureHigh = 96;
            }
            else
            {
                _pressureHigh = 95;
            }

            _pressureLow = _pressureHigh - 9;
        }

        protected override int GetCurrentPressure()
        {
#if NETCOREAPP
            // Do things the new way
            if (_physicalMemoryBytesAvailable >= 0)
            {
                // Get stats from GC.
                GCMemoryInfo memInfo = GC.GetGCMemoryInfo();

                var limit = (_physicalMemoryBytesAvailable == 0) ? memInfo.TotalAvailableMemoryBytes : _physicalMemoryBytesAvailable;

                // If we are following GC thresholds, then we're checking against the GC's "high" threshold, not total physical mem.
                if (_followGCThresholds)
                {
                    limit = memInfo.HighMemoryLoadThresholdBytes;
                }

                if (limit > memInfo.MemoryLoadBytes)
                {
                    int memoryLoad = (int)((float)memInfo.MemoryLoadBytes * 100.0 / (float)limit);
                    return Math.Max(1, memoryLoad);
                }

                return (memInfo.MemoryLoadBytes > 0) ? 100 : 0;
            }
#endif
            return LegacyGetCurrentPressure();
        }
    }
}
