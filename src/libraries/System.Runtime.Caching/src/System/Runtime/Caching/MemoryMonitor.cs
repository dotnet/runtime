// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.Security;
using System.Runtime.InteropServices;


namespace System.Runtime.Caching
{
    // MemoryMonitor is the base class for memory monitors.  The MemoryCache has two
    // types of monitors:  PhysicalMemoryMonitor and CacheMemoryMonitor.  The first monitors
    // the amount of physical memory used on the machine, and helps determine when we should
    // drop cache entries to avoid paging.  The second monitors the amount of memory used by
    // the cache itself, and helps determine when we should drop cache entries to avoid 
    // exceeding the cache's memory limit.  Both are configurable (see ConfigUtil.cs).
    internal abstract class MemoryMonitor
    {
        protected const int TERABYTE_SHIFT = 40;
        protected const long TERABYTE = 1L << TERABYTE_SHIFT;

        protected const int GIGABYTE_SHIFT = 30;
        protected const long GIGABYTE = 1L << GIGABYTE_SHIFT;

        protected const int MEGABYTE_SHIFT = 20;
        protected const long MEGABYTE = 1L << MEGABYTE_SHIFT; // 1048576

        protected const int KILOBYTE_SHIFT = 10;
        protected const long KILOBYTE = 1L << KILOBYTE_SHIFT; // 1024

        protected const int HISTORY_COUNT = 6;

        protected int _pressureHigh;      // high pressure level
        protected int _pressureLow;       // low pressure level - slow growth here

        protected int _i0;
        protected int[] _pressureHist;
        protected int _pressureTotal;

        private static long s_totalPhysical;
        private static long s_totalVirtual;

        static MemoryMonitor()
        {
            Interop.Kernel32.MEMORYSTATUSEX memoryStatusEx;
            memoryStatusEx.dwLength = (uint)Marshal.SizeOf(typeof(Interop.Kernel32.MEMORYSTATUSEX));
            if (Interop.Kernel32.GlobalMemoryStatusEx(out memoryStatusEx) != 0)
            {
                s_totalPhysical = (long)memoryStatusEx.ullTotalPhys;
                s_totalVirtual = (long)memoryStatusEx.ullTotalVirtual;
            }
        }

        internal static long TotalPhysical { get { return s_totalPhysical; } }
        internal static long TotalVirtual { get { return s_totalVirtual; } }

        internal int PressureLast { get { return _pressureHist[_i0]; } }
        internal int PressureHigh { get { return _pressureHigh; } }
        internal int PressureLow { get { return _pressureLow; } }

        internal bool IsAboveHighPressure()
        {
            return PressureLast >= PressureHigh;
        }

        protected abstract int GetCurrentPressure();

        internal abstract int GetPercentToTrim(DateTime lastTrimTime, int lastTrimPercent);

        protected void InitHistory()
        {
            Dbg.Assert(_pressureHigh > 0, "_pressureHigh > 0");
            Dbg.Assert(_pressureLow > 0, "_pressureLow > 0");
            Dbg.Assert(_pressureLow <= _pressureHigh, "_pressureLow <= _pressureHigh");

            int pressure = GetCurrentPressure();

            _pressureHist = new int[HISTORY_COUNT];
            for (int i = 0; i < HISTORY_COUNT; i++)
            {
                _pressureHist[i] = pressure;
                _pressureTotal += pressure;
            }
        }

        // Get current pressure and update history
        internal void Update()
        {
            int pressure = GetCurrentPressure();

            _i0 = (_i0 + 1) % HISTORY_COUNT;
            _pressureTotal -= _pressureHist[_i0];
            _pressureTotal += pressure;
            _pressureHist[_i0] = pressure;

#if DEBUG
            Dbg.Trace("MemoryCacheStats", this.GetType().Name + ".Update: last=" + pressure
                        + ",high=" + PressureHigh
                        + ",low=" + PressureLow
                        + " " + Dbg.FormatLocalDate(DateTime.Now));
#endif
        }
    }
}
