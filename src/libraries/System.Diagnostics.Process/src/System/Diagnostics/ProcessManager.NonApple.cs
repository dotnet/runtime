// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        internal static bool IsSunOS() =>
            OperatingSystem.IsOSPlatform("illumos") || OperatingSystem.IsOSPlatform("solaris");

        /// <summary>Gets process infos for each process on the local machine.</summary>
        /// <param name="builder">The builder to add found process infos to.</param>
        /// <param name="processNameFilter">Optional process name to use as an inclusion filter.</param>
        public static void GetProcessInfos(ref ArrayBuilder<ProcessInfo> builder, string? processNameFilter)
        {
            if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
            {
                GetProcessInfosBsd(ref builder, processNameFilter);
                return;
            }

            // Iterate through all process IDs to load information about each process
            foreach (int pid in EnumerateProcessIds())
            {
                ProcessInfo? pi = CreateProcessInfo(pid, processNameFilter);
                if (pi != null)
                {
                    builder.Add(pi);
                }
            }
        }

        /// <summary>Enumerates the IDs of all processes on the current machine from procfs.</summary>
        private static IEnumerable<int> EnumerateProcessIds()
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            {
                if (!ProcMatchesPidNamespace)
                {
                    // Limit to our own process. For other processes, the pids from /proc don't match with those in the process namespace.
                    yield return Environment.ProcessId;
                    yield break;
                }
            }
            else if (!IsSunOS())
            {
                throw new PlatformNotSupportedException();
            }

            // Parse /proc for any directory that's named with a number.  Each such
            // directory represents a process.
            foreach (string procDir in Directory.EnumerateDirectories(Interop.procfs.RootPath))
            {
                string dirName = Path.GetFileName(procDir);
                int pid;
                if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out pid))
                {
                    Debug.Assert(pid >= 0);
                    yield return pid;
                }
            }
        }

        internal static string? GetProcessName(int processId, string _ /* machineName */, bool __ /* isRemoteMachine */, ref ProcessInfo? processInfo)
        {
            if (processInfo is not null)
            {
                return processInfo.ProcessName;
            }

            if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            {
                if (TryGetProcPid(processId, out Interop.procfs.ProcPid procPid) &&
                    Interop.procfs.TryReadStatFile(procPid, out Interop.procfs.ParsedStat stat))
                {
                    return Process.GetUntruncatedProcessName(procPid, ref stat);
                }

                return null;
            }

            processInfo = CreateProcessInfo(processId);
            return processInfo?.ProcessName;
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        internal static ProcessModuleCollection GetModules(int processId)
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            {
                ProcessModuleCollection? modules = null;
                if (TryGetProcPid(processId, out Interop.procfs.ProcPid procPid))
                {
                    modules = Interop.procfs.ParseMapsModules(procPid);

                    // Move the main executable module to be the first in the list if it's not already
                    if (modules is not null && Process.GetExePath(procPid) is string exePath)
                    {
                        for (int i = 0; i < modules.Count; i++)
                        {
                            ProcessModule module = modules[i];
                            if (module.FileName == exePath)
                            {
                                if (i > 0)
                                {
                                    modules.RemoveAt(i);
                                    modules.Insert(0, module);
                                }
                                break;
                            }
                        }
                    }
                }

                return modules ?? new(capacity: 0);
            }

            if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
            {
                return GetModulesBsd(processId);
            }

            if (IsSunOS())
            {
                return GetModulesSunOS(processId);
            }

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Creates a ProcessInfo from the specified process ID.
        /// </summary>
        internal static ProcessInfo? CreateProcessInfo(int pid, string? processNameFilter = null)
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            {
                if (!TryGetProcPid(pid, out Interop.procfs.ProcPid procPid) ||
                    !Interop.procfs.TryReadStatFile(procPid, out Interop.procfs.ParsedStat stat))
                {
                    return null;
                }

                string? processName = null;
                if (processNameFilter != null)
                {
                    processName = Process.GetUntruncatedProcessName(procPid, ref stat);
                    if (!processNameFilter.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }

                Interop.procfs.TryReadStatusFile(procPid, out Interop.procfs.ParsedStatus status);
                return CreateProcessInfo(procPid, ref stat, ref status, processName: processName);
            }

            if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
            {
                // Negative PIDs aren't valid
                ArgumentOutOfRangeException.ThrowIfNegative(pid);

                // Try to get the task info. This can fail if the user permissions don't permit
                // this user context to query the specified process, or if the process has exited.
                ProcessInfo? iinfo = Interop.Process.GetProcessInfoById(pid);
                if (iinfo is null)
                {
                    return null;
                }

                if (processNameFilter != null && !processNameFilter.Equals(iinfo.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                ProcessInfo procInfo = new ProcessInfo()
                {
                    ProcessId = pid,
                    ProcessName = iinfo.ProcessName,
                    BasePriority = iinfo.BasePriority,
                    VirtualBytes = iinfo.VirtualBytes,
                    VirtualBytesPeak = iinfo.VirtualBytesPeak,
                    WorkingSet = iinfo.WorkingSet,
                    WorkingSetPeak = iinfo.WorkingSetPeak,
                    PrivateBytes = iinfo.PrivateBytes,
                    SessionId = iinfo.SessionId,
                };

                foreach (ThreadInfo ti in iinfo._threadInfoList)
                {
                    procInfo._threadInfoList.Add(ti);
                }

                return procInfo;
            }

            if (IsSunOS())
            {
                return CreateProcessInfoSunOS(pid, processNameFilter);
            }

            throw new PlatformNotSupportedException();
        }

        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds() => Interop.Process.ListAllPids();

        private static string GetProcPath(int processId) => Interop.Process.GetProcPath(processId);
    }
}
