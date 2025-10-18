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

        // Controls memory monitoring behavior based on the PhysicalMemoryMode enum
        private PhysicalMemoryMode _physicalMemoryMode;
        private long? _physicalMemoryBytes;

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

        internal PhysicalMemoryMonitor(int physicalMemoryLimitPercentage, PhysicalMemoryMode physicalMemoryMode, long? physicalMemoryBytes)
        {
            SetLimit(physicalMemoryLimitPercentage, physicalMemoryMode, physicalMemoryBytes);
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

        internal void SetLimit(int physicalMemoryLimitPercentage, PhysicalMemoryMode physicalMemoryMode, long? physicalMemoryBytes)
        {
#if NETCOREAPP
            _physicalMemoryMode = physicalMemoryMode;
            _physicalMemoryBytes = (physicalMemoryMode == PhysicalMemoryMode.Default) ? physicalMemoryBytes : null;
#else
            // For non-netcoreapp, we only support Legacy mode because the GC.GetGCMemoryInfo API is not available.
            _physicalMemoryMode = PhysicalMemoryMode.Legacy;
            _physicalMemoryBytes = null;
            if (physicalMemoryMode != PhysicalMemoryMode.Legacy)
            {
                throw new PlatformNotSupportedException("PhysicalMemoryMonitor only supports Legacy mode on non-netcoreapp platforms.");
            }
#endif

            if (physicalMemoryLimitPercentage == 0)
            {
                UseDefaultLimits();
            }
            else
            {
                // Legacy and Default modes: Use specified percentage with appropriate monitoring
                _pressureHigh = Math.Max(3, physicalMemoryLimitPercentage);
                _pressureLow = Math.Max(1, _pressureHigh - 9);
            }

            Dbg.Trace("MemoryCacheStats", $"PhysicalMemoryMonitor.SetLimit: _pressureHigh={_pressureHigh}, _pressureLow={_pressureLow}, mode={_physicalMemoryMode}");
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


#if NETCOREAPP
            // The GC.GetGCMemoryInfo API is available only in .NET Core
            if (_physicalMemoryMode == PhysicalMemoryMode.GCThresholds)
            {
                _pressureHigh = 100; // 100%, because we will check against the GC's high memory load threshold instead of total physical memory
                _pressureLow = _pressureHigh - 9;
                return;
            }
#endif

            // _physicalMemoryBytes should always be null in non-Core apps and in Legacy mode.
            long memory = _physicalMemoryBytes ?? TotalPhysical;
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
            // Modern GC-based monitoring for Default and GCThresholds modes
            if (_physicalMemoryMode != PhysicalMemoryMode.Legacy)
            {
                // Get stats from GC without inducing collection
                GCMemoryInfo memInfo = GC.GetGCMemoryInfo();

                long limit = _physicalMemoryBytes ?? memInfo.TotalAvailableMemoryBytes;
                if (_physicalMemoryMode == PhysicalMemoryMode.GCThresholds)
                {
                    // GCThresholds mode: Use GC's high memory load threshold
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
            // Legacy mode: Platform-specific implementation
            return LegacyGetCurrentPressure();
        }
    }
}
