// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string machineName)
        {
            if (IsRemoteMachine(machineName))
            {
                throw new PlatformNotSupportedException();
            }

            // Iterate through all process IDs to load information about each process
            int[] procIds = GetProcessIds(machineName);
            var processes = new List<ProcessInfo>(procIds.Length);
            foreach (int pid in procIds)
            {
                // Read /proc/pid/stat to get information about the process, and churn that into a ProcessInfo
                Interop.procfs.ParsedStat procFsStat = Interop.procfs.ReadStatFile(pid);
                var pi = new ProcessInfo 
                {
                    _processId = pid,
                    _processName = procFsStat.comm,
                    _basePriority = (int)procFsStat.nice,
                    _virtualBytes = (long)procFsStat.vsize,
                    _workingSet = procFsStat.rss,
                    _sessionId = procFsStat.session,
                    _handleCount = 0, // not a Unix concept

                    // We don't currently fill in the following values.
                    // A few of these could probably be filled in from getrusage,
                    // but only for the current process or its children, not for
                    // arbitrary other processes.
                    _poolPagedBytes = 0,
                    _poolNonpagedBytes = 0,
                    _virtualBytesPeak = 0,
                    _workingSetPeak = 0,
                    _pageFileBytes = 0,
                    _pageFileBytesPeak = 0,
                    _privateBytes = 0,
                };

                // Then read through /proc/pid/task/ to find each thread in the process...
                string tasksDir = Interop.procfs.GetTaskDirectoryPathForProcess(pid);
                foreach (string taskDir in Directory.EnumerateDirectories(tasksDir))
                {
                    string dirName = Path.GetFileName(taskDir);
                    int tid;
                    if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out tid))
                    {
                        // ...and read its associated /proc/pid/task/tid/stat file to create a ThreadInfo
                        Interop.procfs.ParsedStat stat = Interop.procfs.ReadStatFile(pid, tid);
                        pi._threadInfoList.Add(new ThreadInfo
                        {
                            _processId = pid,
                            _threadId = tid,
                            _basePriority = pi._basePriority,
                            _currentPriority = (int)stat.nice,
                            _startAddress = (IntPtr)stat.startstack,
                            _threadState = ProcFsStateToThreadState(stat.state),
                            _threadWaitReason = ThreadWaitReason.Unknown
                        });
                    }
                }

                processes.Add(pi);
            }
            return processes.ToArray();
        }

        /// <summary>Gets the IDs of all processes on the specified machine.</summary>
        /// <param name="machineName">The machine to examine.</param>
        /// <returns>An array of process IDs from the specified machine.</returns>
        public static int[] GetProcessIds(string machineName)
        {
            if (IsRemoteMachine(machineName))
            {
                throw new PlatformNotSupportedException();
            }
            return GetProcessIds();
        }

        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            // Parse /proc for any directory that's named with a number.  Each such
            // directory represents a process.
            var pids = new List<int>();
            foreach (string procDir in Directory.EnumerateDirectories(Interop.procfs.RootPath))
            {
                string dirName = Path.GetFileName(procDir);
                int pid;
                if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out pid))
                {
                    Debug.Assert(pid >= 0);
                    pids.Add(pid);
                }
            }
            return pids.ToArray();
        }

        /// <summary>Gets the ID of a process from a handle to the process.</summary>
        /// <param name="processHandle">The handle.</param>
        /// <returns>The process ID.</returns>
        public static int GetProcessIdFromHandle(SafeProcessHandle processHandle)
        {
            return (int)processHandle.DangerousGetHandle(); // not actually dangerous; just wraps a process ID
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        public static ModuleInfo[] GetModuleInfos(int processId)
        {
            // Not currently supported, but we can simply return an empty array rather than throwing.
            // Could potentially be done via /proc/pid/maps and some heuristics to determine
            // which entries correspond to modules.
            return Array.Empty<ModuleInfo>();
        }

        /// <summary>Gets whether the named machine is remote or local.</summary>
        /// <param name="machineName">The machine name.</param>
        /// <returns>true if the machine is remote; false if it's local.</returns>
        public static bool IsRemoteMachine(string machineName)
        {
            return 
                machineName != "." && 
                machineName != Interop.libc.gethostname();
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Gets a ThreadState to represent the value returned from the status field of /proc/pid/stat.</summary>
        /// <param name="c">The status field value.</param>
        /// <returns></returns>
        private static ThreadState ProcFsStateToThreadState(char c)
        {
            switch (c)
            {
                case 'R':
                    return ThreadState.Running;
                case 'S':
                case 'D':
                case 'T':
                    return ThreadState.Wait;
                case 'Z':
                    return ThreadState.Terminated;
                case 'W':
                    return ThreadState.Transition;
                default:
                    Debug.Fail("Unexpected status character");
                    return ThreadState.Unknown;
            }
        }

    }
}
