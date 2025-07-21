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

        // This value has specific meaning:
        //    -1: means physical memory pressure should be calculated based on the detected size of physical memory.
        //    0:  means physical memory pressure should be based on GC thresholds for determining memory pressure.
        //    >0: means physical memory pressure should be calculated based on the value given.
        // Default target pressure precentages differe between these cases as well, though explicit percentage values take precedence.
        private long _physicalMemoryBytesAvailable;

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
                // If using GC thresholds, the GC threshold _is_ the limit.
                if (_physicalMemoryBytesAvailable == 0)
                {
                    _pressureHigh = 100;
                    _pressureLow = 90;
                }
                else
                {
                    // Otherwise (-1, or any positive value), calculate defaults.
                    UseDefaultLimits();
                }
                Dbg.Trace("MemoryCacheStats", $"PhysicalMemoryMonitor.SetLimit: _pressureHigh={_pressureHigh}, _pressureLow={_pressureLow}");
                return;
            }

            _pressureHigh = Math.Max(3, physicalMemoryLimitPercentage);
            _pressureLow = Math.Max(1, _pressureHigh - 9);
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
#if NETCORAPP
            // Do things the new way
            if (_physicalMemoryBytesAvailable >= 0)
            {
                // Get stats from GC.
                GCMemoryInfo memInfo = GC.GetGCMemoryInfo();
                var limit = (_physicalMemoryBytesAvailable == 0) ? memInfo.HighMemoryLoadThresholdBytes : _physicalMemoryBytesAvailable;

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
