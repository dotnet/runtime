// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
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

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private static ProcessInfo CreateProcessInfo(int pid)
        {
            // Read /proc/pid/stat to get information about the process, and churn that into a ProcessInfo
            ProcessInfo pi;
            try
            {
                Interop.procfs.ParsedStat procFsStat = Interop.procfs.ReadStatFile(pid);
                pi = new ProcessInfo
                {
                    ProcessId = pid,
                    ProcessName = procFsStat.comm,
                    BasePriority = (int)procFsStat.nice,
                    VirtualBytes = (long)procFsStat.vsize,
                    WorkingSet = procFsStat.rss,
                    SessionId = procFsStat.session,
                    HandleCount = 0, // not a Unix concept

                    // We don't currently fill in the following values.
                    // A few of these could probably be filled in from getrusage,
                    // but only for the current process or its children, not for
                    // arbitrary other processes.
                    PoolPagedBytes = 0,
                    PoolNonpagedBytes = 0,
                    VirtualBytesPeak = 0,
                    WorkingSetPeak = 0,
                    PageFileBytes = 0,
                    PageFileBytesPeak = 0,
                    PrivateBytes = 0,
                };
            }
            catch (FileNotFoundException)
            {
                // Between the time that we get an ID and the time that we try to read the associated stat
                // file(s), the process could be gone.
                return null;
            }

            // Then read through /proc/pid/task/ to find each thread in the process...
            try
            {
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
                            _basePriority = pi.BasePriority,
                            _currentPriority = (int)stat.nice,
                            _startAddress = (IntPtr)stat.startstack,
                            _threadState = ProcFsStateToThreadState(stat.state),
                            _threadWaitReason = ThreadWaitReason.Unknown
                        });
                    }
                }
            }
            catch (FileNotFoundException) { } // process and/or threads may go away by the time we try to read from them
            catch (DirectoryNotFoundException) { }

            // Finally return what we've built up
            return pi;
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

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
