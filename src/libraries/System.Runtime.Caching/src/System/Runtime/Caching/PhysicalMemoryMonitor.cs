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
        // Track heap size before trimming to measure effectiveness
        private GCMemoryInfo? _initialMemInfo;
        private int _cumulativeTrimPercent;
        private float _targetBelowLimit = 0.95f;

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
                // Legacy and Standard modes: Use specified percentage with appropriate monitoring
                _pressureHigh = Math.Max(3, physicalMemoryLimitPercentage);
                _pressureLow = Math.Max(1, _pressureHigh - 9);
            }

#if NETCOREAPP
            // This is only used in GCThresholds mode, but setting it here is harmless for other modes.
            // We want to target being below the GC's high memory load threshold after trimming, but how
            // far below depends on total physical memory - large memory machines can handle being closer
            // to the threshold, while smaller machines need to be further away.
            // For RAM greater than 48GB, we target .99f
            // For RAM less than 12GB, we target .95f
            // In between, we scale linearly.
            // This is just a made up empirical heuristic to help avoid excessive trimming on large memory machines.
            long memory = TotalPhysical;
            float scale = (float)(memory - 0x300000000) / (float)(0xC00000000 - 0x300000000);
            _targetBelowLimit = Math.Max(0.95f, Math.Min(0.99f, scale * (0.99f - 0.95f)));
#endif

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
        }

        protected override int GetCurrentPressure()
        {
            int currentPressure = CalculateCurrentPressure();

#if NETCOREAPP
            if (currentPressure < PressureHigh) // Reset in any mode. No ill effects in Legacy/Standard modes.
            {
                // Not above high pressure - reset tracking
                _initialMemInfo = null;
                _cumulativeTrimPercent = 0;
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

        internal override int GetPercentToTrim(DateTime lastTrimTime, int lastTrimPercent)
        {
            int percent = 0;
            long ticksSinceTrim = DateTime.UtcNow.Subtract(lastTrimTime).Ticks;

            if (IsAboveHighPressure())
            {
#if NETCOREAPP
                // For GCThresholds mode, use heap size tracking to help determine trim effectiveness
                if (_physicalMemoryMode == PhysicalMemoryMode.GCThresholds)
                {
                    GCMemoryInfo currentMemInfo = GC.GetGCMemoryInfo();

                    if (_initialMemInfo is GCMemoryInfo initialInfo)
                    {
                        // Update cumulative trim percent: if this is the first trim, use lastTrimPercent directly.
                        // Otherwise, apply the new trim percent to the remaining untrimmed portion (100 - cumulative).
                        // This prevents over-counting when multiple trims happen in succession without a Gen2 GC.
                        _cumulativeTrimPercent += (_cumulativeTrimPercent == 0) ? lastTrimPercent : (int)((100 - _cumulativeTrimPercent) * (lastTrimPercent/100.0));
                        _cumulativeTrimPercent = Math.Min(100, _cumulativeTrimPercent);

                        long heapReduction = initialInfo.HeapSizeBytes - currentMemInfo.HeapSizeBytes;
                        long targetMemoryLoad = (long)(_targetBelowLimit * GetGCThresholdsLimit(initialInfo));

                        // Our previous trims might not be reflected in MemoryLoadBytes yet, so let's estimate the effect
                        // of our previous trims using HeapSizeBytes instead
                        long additionalReduction = initialInfo.MemoryLoadBytes - targetMemoryLoad - heapReduction;
                        if (additionalReduction <= 0)
                        {
                            // It looks like we've probably dropped below our target load after previous trims,
                            // but let's return a token 1% until we finally do see MemoryLoadBytes drop below target.
                            percent = 1;
                        }

                        // We haven't seen any reduction in HeapSizeBytes after trimming - throw our hands up and ajust
                        // trimming the old fashioned way
                        else if (heapReduction <= 0)
                        {
                            percent = GetPercentToTrimStandard(ticksSinceTrim, lastTrimPercent);
                        }

                        // We haven't seen enough reduction in HeapSizeBytes to be confident we've dropped below target load yet
                        else
                        {
                            // Estimate the percent reduction needed to reach target load
                            float estimatedPercent = ((float)additionalReduction / (float)heapReduction) * _cumulativeTrimPercent;
                            percent = Math.Min(50, (int)Math.Ceiling(estimatedPercent));
                            percent = Math.Max(MinTotalMemoryTrimPercent, percent);
                        }
                    }
                    else
                    {
                        // This is our first trim under high pressure - use standard calculation, but track initial state
                        percent = GetPercentToTrimStandard(ticksSinceTrim, lastTrimPercent);
                        _initialMemInfo = currentMemInfo;
                    }
                }

                // For other modes, use original time-based calculation
                else
#endif // NETCOREAPP
                {
                    percent = GetPercentToTrimStandard(ticksSinceTrim, lastTrimPercent);
                }
            }

#if PERF
            Debug.WriteLine($"PhysicalMemoryMonitor.GetPercentToTrim: percent={percent:N}, lastTrimPercent={lastTrimPercent:N}, secondsSinceTrim={ticksSinceTrim / TimeSpan.TicksPerSecond:N}{Environment.NewLine}");
#endif
            return percent;
        }

        private static int GetPercentToTrimStandard(long ticksSinceTrim, int lastTrimPercent)
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
