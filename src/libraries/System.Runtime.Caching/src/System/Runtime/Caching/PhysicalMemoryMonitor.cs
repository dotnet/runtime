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

#if NETCOREAPP
        // Track heap size after trimming to measure effectiveness in GCThresholds mode
        private GCMemoryInfo? _lastTrimMemInfo;

        private static long GetGCThresholdsLimit(GCMemoryInfo memInfo) => Math.Min(memInfo.TotalAvailableMemoryBytes, memInfo.HighMemoryLoadThresholdBytes);
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

        internal PhysicalMemoryMonitor(int physicalMemoryLimitPercentage, PhysicalMemoryMode physicalMemoryMode)
        {
            SetLimit(physicalMemoryLimitPercentage, physicalMemoryMode);
            InitHistory();
        }

        internal void SetLimit(int physicalMemoryLimitPercentage, PhysicalMemoryMode physicalMemoryMode)
        {
#if NETCOREAPP
            _physicalMemoryMode = physicalMemoryMode;
#else
            // For non-netcoreapp, we only support Legacy mode because the GC.GetGCMemoryInfo API is not available.
            _physicalMemoryMode = PhysicalMemoryMode.Legacy;
            if (physicalMemoryMode != PhysicalMemoryMode.Legacy)
            {
                throw new PlatformNotSupportedException("PhysicalMemoryMonitor only supports Legacy mode on non-netcoreapp platforms.");
            }
#endif // NETCOREAPP

            if (physicalMemoryLimitPercentage == 0)
            {
                UseDefaultLimits();
            }
            else
            {
#if NETCOREAPP
                // The GC.GetGCMemoryInfo API is available only in .NET Core
                if (_physicalMemoryMode == PhysicalMemoryMode.GCThresholds)
                {
                    // GCThresholds mode always targets HighMemoryLoadThresholdBytes, but we can still adjust the low threshold.
                    _pressureHigh = 100;
                    _pressureLow = Math.Max(3, physicalMemoryLimitPercentage);
                }
                else
#endif
                {
                    // Legacy and Standard modes: Use specified percentage with appropriate monitoring
                    _pressureHigh = Math.Max(3, physicalMemoryLimitPercentage);
                    _pressureLow = Math.Max(1, _pressureHigh - 9);
                }
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

            long memory = TotalPhysical;
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

#if NETCOREAPP
            // The GC.GetGCMemoryInfo API is available only in .NET Core
            if (_physicalMemoryMode == PhysicalMemoryMode.GCThresholds)
            {
                // Set "high" to 100% because we want to use the GC's high memory load threshold exactly.
                // But set "low" based on the previous size-adjusted "high" to maintain a comfortable - but not too large - buffer below
                // the limit, allowing the cache to operate efficiently within a safe memory range.
                _pressureLow = 200 - (2 * _pressureHigh);
                _pressureHigh = 100;
            }
#endif
        }

        protected override int GetCurrentPressure()
        {
            int currentPressure = CalculateCurrentPressure();

#if NETCOREAPP
            if (currentPressure < PressureHigh) // Reset in any mode. No ill effects in Legacy/Standard modes.
            {
                // Not above high pressure - reset tracking
                _lastTrimMemInfo = null;
            }
#endif

            return currentPressure;
        }

#pragma warning disable CA1822 // Mark members as static
        private int CalculateCurrentPressure()
        {
#if NETCOREAPP
            // Modern GC-based monitoring for Standard and GCThresholds modes
            if (_physicalMemoryMode != PhysicalMemoryMode.Legacy)
            {
                // Get stats from GC without inducing collection
                GCMemoryInfo memInfo = GC.GetGCMemoryInfo();

                long limit = memInfo.TotalAvailableMemoryBytes;
                if (_physicalMemoryMode == PhysicalMemoryMode.GCThresholds)
                {
                    // GCThresholds mode: Use GC's high memory load threshold
                    limit = GetGCThresholdsLimit(memInfo);
                }

                if (limit > memInfo.MemoryLoadBytes)
                {
                    int memoryLoad = (int)((float)memInfo.MemoryLoadBytes * 100.0 / (float)limit);
                    return Math.Max(1, memoryLoad);
                }

                return (memInfo.MemoryLoadBytes > 0) ? 100 : 0;
            }
#endif // NETCOREAPP

            // Legacy mode: Platform-specific implementation
            return LegacyGetCurrentPressure();
        }
#pragma warning restore CA1822 // Mark members as static

#if NETCOREAPP
        // In GCThresholds mode, we want to try and account for heap reductions that may not yet
        // be reflected in `MemoryLoadBytes` when determining if we're above high pressure. (The GC
        // may use optimizations that delay uncommitting memory back to the OS even after a trim.)
        // It's ok to use a possibly lagging `MemoryLoadBytes` outside of this class - it's only used
        // to determine the monitor/timer interval... which probably should still speed up if the
        // official `MemoryLoadBytes` is high, even if we think we know better.
        internal bool IsReallyAboveHighPressure(GCMemoryInfo currentMemInfo)
        {
            var baseIsAboveHighPressure = base.IsAboveHighPressure();

            // Use base implementation for non-GCThresholds modes, or when
            // even the official metric says there's no pressure, or when
            // we haven't had a previous trim to compare `HeapSizeBytes` against.
            if ((_physicalMemoryMode != PhysicalMemoryMode.GCThresholds)
                || !baseIsAboveHighPressure
                || _lastTrimMemInfo is not GCMemoryInfo lastTrimInfo)
            {
                return baseIsAboveHighPressure;
            }

            // Trim has already happened since entering high pressure - check for adequate heap reduction
            long heapReduction = lastTrimInfo.HeapSizeBytes - currentMemInfo.HeapSizeBytes;

            // If heap hasn't reduced, we're still under pressure
            if (heapReduction <= 0)
            {
                return true;
            }

            // If heap hasn't reduced enough to drop below target, we're still under pressure
            long estimatedMemoryLoad = currentMemInfo.MemoryLoadBytes - heapReduction;
            long targetMemoryLoad = (long)(((float)_pressureLow / 100.0) * GetGCThresholdsLimit(currentMemInfo));

            // If the estimated load is below target, we're likely not under real pressure anymore
            return estimatedMemoryLoad >= targetMemoryLoad;
        }
#endif // NETCOREAPP

        internal override int GetPercentToTrim(DateTime lastTrimTime, int lastTrimPercent)
        {
            int percent = 0;
            long ticksSinceTrim = DateTime.UtcNow.Subtract(lastTrimTime).Ticks;

#if NETCOREAPP
            // For GCThresholds mode, track when we trim to help IsAboveHighPressure make better decisions
            if (_physicalMemoryMode == PhysicalMemoryMode.GCThresholds)
            {
                var memInfo = GC.GetGCMemoryInfo();

                if (IsReallyAboveHighPressure(memInfo))
                {
                    // Use the standard time-based calculation
                    percent = GetPercentToTrimInternal(ticksSinceTrim, lastTrimPercent);

                    // Record that we trimmed so IsAboveHighPressure can use heap size changes
                    _lastTrimMemInfo = memInfo;
                }
            }

            // For other modes, use original time-based calculation
            else
#endif // NETCOREAPP
            if (IsAboveHighPressure())
            {
                percent = GetPercentToTrimInternal(ticksSinceTrim, lastTrimPercent);
            }

#if PERF
            Debug.WriteLine($"PhysicalMemoryMonitor.GetPercentToTrim: percent={percent:N}, lastTrimPercent={lastTrimPercent:N}, secondsSinceTrim={ticksSinceTrim / TimeSpan.TicksPerSecond:N}{Environment.NewLine}");
#endif
            return percent;
        }

        private static int GetPercentToTrimInternal(long ticksSinceTrim, int lastTrimPercent)
        {
            int percent = 0;

            // Original time-based calculation
            if (ticksSinceTrim > 0)
            {
                percent = Math.Min(50, (int)((lastTrimPercent * TargetTotalMemoryTrimIntervalTicks) / ticksSinceTrim));
                percent = Math.Max(MinTotalMemoryTrimPercent, percent);
            }

            return percent;
        }
    }
}
