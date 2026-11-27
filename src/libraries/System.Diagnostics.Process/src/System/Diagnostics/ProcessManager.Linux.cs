// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        private static NullableBool _procMatchesPidNamespace;

        /// <summary>
        /// Creates a ProcessInfo from the data parsed from a /proc/pid/stat file and the associated tasks directory.
        /// </summary>
        internal static unsafe ProcessInfo CreateProcessInfo(Interop.procfs.ProcPid procPid, ref Interop.procfs.ParsedStat procFsStat, ref Interop.procfs.ParsedStatus procFsStatus, string? processName = null)
        {
            int pid = procFsStat.pid;

            var pi = new ProcessInfo()
            {
                ProcessId = pid,
                ProcessName = processName ?? Process.GetUntruncatedProcessName(procPid, ref procFsStat) ?? string.Empty,
                BasePriority = (int)procFsStat.nice,
                SessionId = procFsStat.session,
                PoolPagedBytes = (long)procFsStatus.VmSwap,
                VirtualBytes = (long)procFsStatus.VmSize,
                VirtualBytesPeak = (long)procFsStatus.VmPeak,
                WorkingSetPeak = (long)procFsStatus.VmHWM,
                WorkingSet = (long)procFsStatus.VmRSS,
                PageFileBytes = (long)procFsStatus.VmSwap,
                PrivateBytes = (long)procFsStatus.VmData,
                // We don't currently fill in the other values.
                // A few of these could probably be filled in from getrusage,
                // but only for the current process or its children, not for
                // arbitrary other processes.
            };

            // Then read through /proc/pid/task/ to find each thread in the process...
            string tasksDir = Interop.procfs.GetTaskDirectoryPathForProcess(procPid);
            try
            {
                foreach (string taskDir in Directory.EnumerateDirectories(tasksDir))
                {
                    // ...and read its associated /proc/pid/task/tid/stat file to create a ThreadInfo
                    string dirName = Path.GetFileName(taskDir);
                    int tid;
                    Interop.procfs.ParsedStat stat;
                    if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out tid) &&
                        Interop.procfs.TryReadStatFile(procPid, tid, out stat))
                    {
                        pi._threadInfoList.Add(new ThreadInfo()
                        {
                            _processId = pid,
                            _threadId = (ulong)tid,
                            _basePriority = pi.BasePriority,
                            _currentPriority = (int)stat.nice,
                            _startAddress = null,
                            _threadState = ProcFsStateToThreadState(stat.state),
                            _threadWaitReason = ThreadWaitReason.Unknown
                        });
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

        /// <summary>Gets a ThreadState to represent the value returned from the status field of /proc/pid/stat.</summary>
        /// <param name="c">The status field value.</param>
        /// <returns></returns>
        private static ThreadState ProcFsStateToThreadState(char c)
        {
            // Information on these in fs/proc/array.c
            // `man proc` does not document them all
            switch (c)
            {
                case 'R': // Running
                    return ThreadState.Running;

                case 'D': // Waiting on disk
                case 'P': // Parked
                case 'S': // Sleeping in a wait
                case 't': // Tracing/debugging
                case 'T': // Stopped on a signal
                    return ThreadState.Wait;

                case 'x': // dead
                case 'X': // Dead
                case 'Z': // Zombie
                    return ThreadState.Terminated;

                case 'W': // Paging or waking
                case 'K': // Wakekill
                    return ThreadState.Transition;

                case 'I': // Idle
                    return ThreadState.Ready;

                default:
                    Debug.Fail($"Unexpected status character: {c}");
                    return ThreadState.Unknown;
            }
        }

        internal static bool TryReadStatFile(int pid, out Interop.procfs.ParsedStat stat)
        {
            if (!TryGetProcPid(pid, out Interop.procfs.ProcPid procPid))
            {
                stat = default;
                return false;
            }
            return Interop.procfs.TryReadStatFile(procPid, out stat);
        }

        internal static bool TryReadStatusFile(int pid, out Interop.procfs.ParsedStatus status)
        {
            if (!TryGetProcPid(pid, out Interop.procfs.ProcPid procPid))
            {
                status = default;
                return false;
            }
            return Interop.procfs.TryReadStatusFile(procPid, out status);
        }

        internal static bool TryReadStatFile(int pid, int tid, out Interop.procfs.ParsedStat stat)
        {
            if (!TryGetProcPid(pid, out Interop.procfs.ProcPid procPid))
            {
                stat = default;
                return false;
            }
            return Interop.procfs.TryReadStatFile(procPid, tid, out stat);
        }

        internal static bool TryGetProcPid(int pid, out Interop.procfs.ProcPid procPid)
        {
            // Use '/proc/self' for the current process.
            if (pid == Environment.ProcessId)
            {
                procPid = Interop.procfs.ProcPid.Self;
                return true;
            }

            if (ProcMatchesPidNamespace)
            {
                procPid = (Interop.procfs.ProcPid)pid;
                return true;
            }

            // We can't map a process namespace pid to a procfs pid.
            procPid = Interop.procfs.ProcPid.Invalid;
            return false;
        }

        internal static bool ProcMatchesPidNamespace
        {
            get
            {
                if (_procMatchesPidNamespace == NullableBool.Undefined)
                {
                    // '/proc/self' is a symlink to the pid used by '/proc' for the current process.
                    // We compare it with the pid of the current process to see if the '/proc' and pid namespace match up.
                    int? procSelfPid = null;
                    if (Interop.Sys.ReadLink($"{Interop.procfs.RootPath}{Interop.procfs.Self}") is string target &&
                        int.TryParse(target, out int pid))
                    {
                        procSelfPid = pid;
                    }
                    Debug.Assert(procSelfPid.HasValue);

                    _procMatchesPidNamespace = !procSelfPid.HasValue || procSelfPid == Environment.ProcessId ? NullableBool.True : NullableBool.False;
                }
                return _procMatchesPidNamespace == NullableBool.True;
            }
        }
    }
}
