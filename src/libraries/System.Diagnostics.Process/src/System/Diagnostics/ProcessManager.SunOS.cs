// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            IEnumerable<int> pids = EnumerateProcessIds();
            return new List<int>(pids).ToArray();
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="processNameFilter">Optional process name to use as an inclusion filter.</param>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string? processNameFilter, string machineName)
        {
            ThrowIfRemoteMachine(machineName);

            // Iterate through all process IDs to load information about each process
            IEnumerable<int> pids = EnumerateProcessIds();
            ArrayBuilder<ProcessInfo> processes = default;
            foreach (int pid in pids)
            {
                ProcessInfo? pi = CreateProcessInfo(pid, processNameFilter);
                if (pi != null)
                {
                    processes.Add(pi);
                }
            }

            return processes.ToArray();
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        internal static ProcessModuleCollection GetModules(int processId)
        {

            // Negative PIDs aren't valid
            ArgumentOutOfRangeException.ThrowIfNegative(processId);

            // GetModules(x)[0].FileName is often used to find the path to the executable,
            // so at least get that.  That appears to be sufficient, at least for now.
            // If needed, the full list of loaded modules could be obtained using another
            // Interop function to read /proc/$pid/auxv similar to how the "pargs" and "pldd"
            // commands do their work.

            Interop.procfs.ProcessInfo iProcInfo;
            if (Interop.procfs.TryGetProcessInfoById(processId, out iProcInfo))
            {
                string fullName = Process.GetUntruncatedProcessName(ref iProcInfo);
                if (!string.IsNullOrEmpty(fullName))
                {
                    return new ProcessModuleCollection(1)
                    {
                        new ProcessModule(fullName, Path.GetFileName(fullName))
                    };
                }
            }
            return new ProcessModuleCollection(0);
        }

        /// <summary>
        /// Creates a ProcessInfo from the specified process ID.
        /// </summary>
        internal static ProcessInfo? CreateProcessInfo(int pid, string? processNameFilter = null)
        {
            // Negative PIDs aren't valid
            ArgumentOutOfRangeException.ThrowIfNegative(pid);

            Interop.procfs.ProcessInfo iProcInfo;
            if (!Interop.procfs.TryGetProcessInfoById(pid, out iProcInfo))
            {
                return null;
            }

            string processName = Process.GetUntruncatedProcessName(ref iProcInfo);
            if (!string.IsNullOrEmpty(processNameFilter) &&
                !string.Equals(processName, processNameFilter, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return CreateProcessInfo(ref iProcInfo);
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        /// <summary>Enumerates the IDs of all processes on the current machine.</summary>
        internal static IEnumerable<int> EnumerateProcessIds()
        {
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

        /// <summary>Enumerates the IDs of all threads in the specified process.</summary>
        internal static IEnumerable<int> EnumerateThreadIds(int pid)
        {
            // Parse /proc/$pid/lwp for any directory that's named with a number.
            // Each such directory represents a thread.
            string dir = Interop.procfs.GetLwpDirForProcess(pid);
            foreach (string lwpDir in Directory.EnumerateDirectories(dir))
            {
                string dirName = Path.GetFileName(lwpDir);
                int tid;
                if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out tid))
                {
                    Debug.Assert(tid >= 0);
                    yield return tid;
                }
            }
        }

        /// <summary>
        /// Creates a ProcessInfo from the data read from a /proc/pid/psinfo file and the associated lwp directory.
        /// </summary>
        internal static ProcessInfo CreateProcessInfo(ref Interop.procfs.ProcessInfo iProcInfo)
        {
            int pid = iProcInfo.Pid;

            string name = Process.GetUntruncatedProcessName(ref iProcInfo);
            var pi = new ProcessInfo()
            {
                ProcessId = pid,
                ProcessName = name,
                BasePriority = iProcInfo.Priority,
                SessionId = iProcInfo.SessionId,
                VirtualBytes = (long)iProcInfo.VirtualSize,
                WorkingSet = (long)iProcInfo.ResidentSetSize,
                // StartTime: See Process.StartTimeCore()
            };

            // Then read through /proc/pid/lwp/ to find each thread in the process...
            // Can we use a "get" method to avoid loading this for every process until it's asked for?
            try
            {

                // Iterate through all thread IDs to load information about each thread
                IEnumerable<int> tids = EnumerateThreadIds(pid);

                foreach (int tid in tids)
                {
                    Interop.procfs.ThreadInfo iThrInfo;
                    ThreadInfo? ti;

                    if (!Interop.procfs.TryGetThreadInfoById(pid, tid, out iThrInfo))
                    {
                        continue;
                    }

                    ti = CreateThreadInfo(ref iProcInfo, ref iThrInfo);
                    if (ti != null)
                    {
                        pi._threadInfoList.Add(ti);
                    }
                }
            }
            catch (IOException)
            {
                // Between the time that we get an ID and the time that we try to read the associated
                // directories and files in procfs, the process could be gone.
            }

            // Finally return what we've built up
            return pi;
        }

        /// <summary>
        /// Creates a ThreadInfo from the data read from a /proc/pid/lwp/lwpsinfo file.
        /// </summary>
        internal static ThreadInfo CreateThreadInfo(ref Interop.procfs.ProcessInfo iProcInfo,
                                                     ref Interop.procfs.ThreadInfo iThrInfo)
        {

            var ti = new ThreadInfo()
            {
                _processId = iProcInfo.Pid,
                _threadId = (ulong)iThrInfo.Tid,
                _basePriority = iThrInfo.Priority,
                _currentPriority = iThrInfo.Priority,
                _startAddress = null,
                _threadState = ProcFsStateToThreadState(iThrInfo.Status),
                _threadWaitReason = ThreadWaitReason.Unknown
            };

            return ti;
        }

        /// <summary>Gets a ThreadState to represent the value returned from the status field of /proc/pid/stat.</summary>
        /// <param name="c">The status field value.</param>
        /// <returns></returns>
        private static ThreadState ProcFsStateToThreadState(char c)
        {
            // Information on these in fs/proc/array.c
            // `man proc` does not document them all
            switch (c)
            {
                case 'O': // On-CPU
                case 'R': // Runnable
                    return ThreadState.Running;

                case 'S': // Sleeping in a wait
                case 'T': // Stopped on a signal
                    return ThreadState.Wait;

                case 'Z': // Zombie
                    return ThreadState.Terminated;

                case 'W': // Waiting for CPU
                    return ThreadState.Transition;

                case '\0': // new, not started yet
                    return ThreadState.Initialized;

                default:
                    Debug.Fail($"Unexpected status character: {(int)c}");
                    return ThreadState.Unknown;
            }
        }

    }
}
