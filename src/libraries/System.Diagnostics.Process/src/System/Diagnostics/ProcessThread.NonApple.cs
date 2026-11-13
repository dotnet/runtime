// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        /// <summary>
        /// Returns or sets the priority level of the associated thread.  The priority level is
        /// not an absolute level, but instead contributes to the actual thread priority by
        /// considering the priority class of the process.
        /// </summary>
        private ThreadPriorityLevel PriorityLevelCore
        {
            get
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
                {
                    // This mapping is relatively arbitrary.  0 is normal based on the man page,
                    // and the other values above and below are simply distributed evenly.
                    return Interop.Sys.GetThreadPriorityFromNiceValue((int)GetStat().nice);
                }

                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
                {
                    return Interop.Sys.GetThreadPriorityFromNiceValue(Interop.Process.GetThreadInfo(_processId, Id).nice);
                }

                if (ProcessManager.IsSunOS())
                {
                    return GetThreadPriorityFromSysPri(GetThreadInfo().Priority);
                }

                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException(); // We can find no API to set this
            }
        }

        /// <summary>Gets the time this thread was started.</summary>
        private DateTime GetStartTime()
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            {
                return Process.BootTimeToDateTime(Process.TicksToTimeSpan(GetStat().starttime));
            }

            if (ProcessManager.IsSunOS())
            {
                Interop.procfs.ThreadInfo threadInfo = GetThreadInfo();

                DateTime startTime = DateTime.UnixEpoch +
                    TimeSpan.FromSeconds(threadInfo.StartTime) +
                    TimeSpan.FromMicroseconds(threadInfo.StartTimeNsec / 1000);

                // The return value is expected to be in the local time zone.
                return startTime.ToLocalTime();
            }

            // On FreeBSD and OpenBSD, kinfo_proc has one entry per thread but the start time is
            // the same for all threads e.g. reflects process start.
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Returns the amount of time the thread has spent running code inside the operating
        /// system core.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
                {
                    return Process.TicksToTimeSpan(GetStat().stime);
                }

                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
                {
                    return Process.TicksToTimeSpan(Interop.Process.GetThreadInfo(_processId, Id).systemTime);
                }

                if (ProcessManager.IsSunOS())
                {
                    // procfs lwpsinfo only reports the total; it is all counted as user time.
                    return TimeSpan.Zero;
                }

                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent utilizing the CPU.
        /// It is the sum of the System.Diagnostics.ProcessThread.UserProcessorTime and
        /// System.Diagnostics.ProcessThread.PrivilegedProcessorTime.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan TotalProcessorTime
        {
            get
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
                {
                    Interop.procfs.ParsedStat stat = GetStat();
                    return Process.TicksToTimeSpan(stat.utime + stat.stime);
                }

                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
                {
                    Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, Id);
                    return Process.TicksToTimeSpan(stat.userTime + stat.systemTime);
                }

                if (ProcessManager.IsSunOS())
                {
                    Interop.procfs.ThreadInfo threadInfo = GetThreadInfo();
                    return TimeSpan.FromSeconds(threadInfo.CpuTotalTime) +
                        TimeSpan.FromMicroseconds(threadInfo.CpuTotalTimeNsec / 1000);
                }

                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent running code
        /// inside the application (not the operating system core).
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan UserProcessorTime
        {
            get
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
                {
                    return Process.TicksToTimeSpan(GetStat().utime);
                }

                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
                {
                    return Process.TicksToTimeSpan(Interop.Process.GetThreadInfo(_processId, Id).userTime);
                }

                if (ProcessManager.IsSunOS())
                {
                    // procfs lwpsinfo only reports the total; it is all counted as user time.
                    return TotalProcessorTime;
                }

                throw new PlatformNotSupportedException();
            }
        }
    }
}
