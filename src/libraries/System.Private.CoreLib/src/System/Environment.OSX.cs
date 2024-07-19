// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System
{
    public static partial class Environment
    {
        public static long WorkingSet => (long)(Interop.libproc.GetProcessInfoById(ProcessId)?.ptinfo.pti_resident_size ?? 0);

        /// <summary>
        /// Get the CPU usage, including the process time spent running the application code, the process time spent running the operating system code,
        /// and the total time spent running both the application and operating system code.
        /// </summary>
        [SupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static ProcessCpuUsage CpuUsage
        {
            get
            {
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(ProcessId);
                return new ProcessCpuUsage { UserTime = MapTime(info.ri_user_time), PrivilegedTime = MapTime(info.ri_system_time) };
            }
        }

        private static volatile uint s_timeBase_numer, s_timeBase_denom;
        private static TimeSpan MapTime(ulong sysTime)
        {
            uint denom = s_timeBase_denom;
            if (denom == default)
            {
                Interop.libSystem.mach_timebase_info_data_t timeBase = GetTimeBase();
                s_timeBase_numer = timeBase.numer;
                s_timeBase_denom = denom = timeBase.denom;
            }
            uint numer = s_timeBase_numer;

            // By dividing by NanosecondsTo100NanosecondsFactor first, we lose some precision, but increase the range
            // where no overflow will happen.
            return new TimeSpan(Convert.ToInt64(sysTime / NanosecondsTo100NanosecondsFactor * numer / denom));
        }

        private static unsafe Interop.libSystem.mach_timebase_info_data_t GetTimeBase()
        {
            Interop.libSystem.mach_timebase_info_data_t timeBase = default;
            var returnCode = Interop.libSystem.mach_timebase_info(&timeBase);
            Debug.Assert(returnCode == 0, $"Non-zero exit code from mach_timebase_info: {returnCode}");
            if (returnCode != 0)
            {
                // Fallback: let's assume that the time values are in nanoseconds,
                // i.e. the time base is 1/1.
                timeBase.numer = timeBase.denom = 1;
            }
            return timeBase;
        }
    }
}
