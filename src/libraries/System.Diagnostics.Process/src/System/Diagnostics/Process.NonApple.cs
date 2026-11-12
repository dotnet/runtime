// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        public partial TimeSpan PrivilegedProcessorTime
        {
            get
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
                {
                    return IsCurrentProcess ? Environment.CpuUsage.PrivilegedTime : TicksToTimeSpan(GetStat().stime);
                }

                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
                {
                    if (IsCurrentProcess)
                    {
                        return Environment.CpuUsage.PrivilegedTime;
                    }

                    EnsureState(State.HaveNonExitedId);
                    return TicksToTimeSpan(Interop.Process.GetThreadInfo(_processId, 0).systemTime);
                }

                if (ProcessManager.IsSunOS())
                {
                    // procfs psinfo only reports the total; it is all counted as user time.
                    EnsureState(State.HaveNonExitedId);
                    return TimeSpan.Zero;
                }

                throw new PlatformNotSupportedException();
            }
        }

        public partial TimeSpan TotalProcessorTime
        {
            get
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
                {
                    if (IsCurrentProcess)
                    {
                        return Environment.CpuUsage.TotalTime;
                    }

                    Interop.procfs.ParsedStat stat = GetStat();
                    return TicksToTimeSpan(stat.utime + stat.stime);
                }

                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
                {
                    if (IsCurrentProcess)
                    {
                        return Environment.CpuUsage.TotalTime;
                    }

                    EnsureState(State.HaveNonExitedId);
                    Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, 0);
                    return TicksToTimeSpan(stat.userTime + stat.systemTime);
                }

                if (ProcessManager.IsSunOS())
                {
                    return TotalProcessorTimeSunOS;
                }

                throw new PlatformNotSupportedException();
            }
        }

        public partial TimeSpan UserProcessorTime
        {
            get
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
                {
                    return IsCurrentProcess ? Environment.CpuUsage.UserTime : TicksToTimeSpan(GetStat().utime);
                }

                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
                {
                    if (IsCurrentProcess)
                    {
                        return Environment.CpuUsage.UserTime;
                    }

                    EnsureState(State.HaveNonExitedId);
                    return TicksToTimeSpan(Interop.Process.GetThreadInfo(_processId, 0).userTime);
                }

                if (ProcessManager.IsSunOS())
                {
                    // procfs psinfo only reports the total; it is all counted as user time.
                    return TotalProcessorTimeSunOS;
                }

                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        internal DateTime StartTimeCore
        {
            get
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
                {
                    return BootTimeToDateTime(TicksToTimeSpan(GetStat().starttime));
                }

                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
                {
                    EnsureState(State.HaveNonExitedId);
                    Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, 0);
                    return new DateTime(DateTime.UnixEpoch.Ticks + (stat.startTime * TimeSpan.TicksPerSecond)).ToLocalTime();
                }

                if (ProcessManager.IsSunOS())
                {
                    Interop.procfs.ProcessInfo processInfo = GetProcInfo();
                    DateTime startTime = DateTime.UnixEpoch +
                        TimeSpan.FromSeconds(processInfo.StartTime) +
                        TimeSpan.FromMicroseconds(processInfo.StartTimeNsec / 1000);

                    // The return value is expected to be in the local time zone.
                    return startTime.ToLocalTime();
                }

                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>Gets the parent process ID</summary>
        private int ParentProcessId
        {
            get
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
                {
                    return GetStat().ppid;
                }

                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
                {
                    EnsureState(State.HaveNonExitedId);
                    return Interop.Process.GetParentPid(_processId);
                }

                if (ProcessManager.IsSunOS())
                {
                    return GetProcInfo().ParentPid;
                }

                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>Gets execution path</summary>
        internal static string? GetPathToOpenFile()
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            {
                ReadOnlySpan<string> allowedProgramsToRun = ["xdg-open", "gnome-open", "kfmclient"];
                foreach (var program in allowedProgramsToRun)
                {
                    string? pathToProgram = ProcessUtils.FindProgramInPath(program);
                    if (!string.IsNullOrEmpty(pathToProgram))
                    {
                        return pathToProgram;
                    }
                }
                return null;
            }

            if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
            {
                return Interop.Sys.Stat("/usr/local/bin/open", out _) == 0 ? "/usr/local/bin/open" : null;
            }

            if (ProcessManager.IsSunOS())
            {
                return ProcessUtils.FindProgramInPath("xdg-open");
            }

            throw new PlatformNotSupportedException();
        }

        partial void EnsureHandleCountPopulated()
        {
            if (!(OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()))
            {
                return;
            }

            if (_processInfo!.HandleCount <= 0 && _haveProcessId)
            {
                // Don't get information for a PID that exited and has possibly been recycled.
                if (GetHasExited(refresh: false))
                {
                    return;
                }
                if (!ProcessManager.TryGetProcPid(_processId, out Interop.procfs.ProcPid procPid))
                {
                    return;
                }
                string path = Interop.procfs.GetFileDescriptorDirectoryPathForProcess(procPid);
                if (Directory.Exists(path))
                {
                    try
                    {
                        _processInfo.HandleCount = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Length;
                    }
                    catch (DirectoryNotFoundException) // Occurs when the process is deleted between the Exists check and the GetFiles call.
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets which processors the threads in this process can be scheduled to run on.
        /// </summary>
        private IntPtr ProcessorAffinityCore
        {
            get
            {
                if (!(OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()))
                {
                    throw new PlatformNotSupportedException(SR.ProcessorAffinityNotSupported);
                }

                EnsureState(State.HaveNonExitedId);

                IntPtr set;
                if (Interop.Sys.SchedGetAffinity(_processId, out set) != 0)
                {
                    throw new Win32Exception(); // match Windows exception
                }

                return set;
            }
            set
            {
                if (!(OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()))
                {
                    throw new PlatformNotSupportedException(SR.ProcessorAffinityNotSupported);
                }

                EnsureState(State.HaveNonExitedId);

                if (Interop.Sys.SchedSetAffinity(_processId, ref value) != 0)
                {
                    throw new Win32Exception(); // match Windows exception
                }
            }
        }

        /// <summary>
        /// Make sure we have obtained the min and max working set limits.
        /// </summary>
        private void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            {
                minWorkingSet = IntPtr.Zero; // no defined limit available

                // For max working set, try to respect container limits by reading
                // from cgroup, but if it's unavailable, fall back to reading from procfs.
                EnsureState(State.HaveNonExitedId);
                if (!Interop.cgroups.TryGetMemoryLimit(out ulong rsslim))
                {
                    rsslim = GetStat().rsslim;
                }

                // rsslim is a ulong, but maxWorkingSet is an IntPtr, so we need to cap rsslim
                // at the max size of IntPtr.  This often happens when there is no configured
                // rsslim other than ulong.MaxValue, which without these checks would show up
                // as a maxWorkingSet == -1.
                switch (IntPtr.Size)
                {
                    case 4:
                        if (rsslim > int.MaxValue)
                            rsslim = int.MaxValue;
                        break;
                    case 8:
                        if (rsslim > long.MaxValue)
                            rsslim = long.MaxValue;
                        break;
                }

                maxWorkingSet = (IntPtr)rsslim;
            }
            else if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD() || ProcessManager.IsSunOS())
            {
                GetWorkingSetLimitsBsd(out minWorkingSet, out maxWorkingSet);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            {
                EnsureState(State.HaveNonExitedId);
                // RLIMIT_RSS with setrlimit not supported on Linux > 2.4.30.
                throw new PlatformNotSupportedException(SR.MinimumWorkingSetNotSupported);
            }

            if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD() || ProcessManager.IsSunOS())
            {
                SetWorkingSetLimitsCoreBsd(newMin, newMax, out resultingMin, out resultingMax);
                return;
            }

            throw new PlatformNotSupportedException();
        }
    }
}
