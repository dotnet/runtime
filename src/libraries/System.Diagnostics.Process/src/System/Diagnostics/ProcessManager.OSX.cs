// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            return Interop.libproc.proc_listallpids();
        }

        private static string GetProcPath(int processId)
        {
            return Interop.libproc.proc_pidpath(processId);
        }

        internal static ProcessInfo? CreateProcessInfo(int pid, string? processNameFilter = null)
        {
            // Negative PIDs aren't valid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid));
            }

            ProcessInfo procInfo;

            // Try to get the task info. This can fail if the user permissions don't permit
            // this user context to query the specified process
            Interop.libproc.proc_taskallinfo? info = Interop.libproc.GetProcessInfoById(pid);
            if (info.HasValue)
            {
                // Set the values we have; all the other values don't have meaning or don't exist on OSX
                Interop.libproc.proc_taskallinfo temp = info.Value;
                string processName;
                unsafe { processName = Marshal.PtrToStringAnsi(new IntPtr(temp.pbsd.pbi_comm))!; }
                if (!string.IsNullOrEmpty(processNameFilter) && !string.Equals(processName, processNameFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                procInfo = new ProcessInfo()
                {
                    ProcessId = pid,
                    ProcessName = processName,
                    BasePriority = temp.pbsd.pbi_nice,
                    VirtualBytes = (long)temp.ptinfo.pti_virtual_size,
                    WorkingSet = (long)temp.ptinfo.pti_resident_size,
                };
            }
            else if (string.IsNullOrEmpty(processNameFilter))
            {
                procInfo = new ProcessInfo()
                {
                    ProcessId = pid,
                };
            }
            else
            {
                // We couldn't get process information but we only want to return a process that
                // matches the specified name, so consider this process not a match.
                return null;
            }

            // Get the sessionId for the given pid, getsid returns -1 on error
            int sessionId = Interop.Sys.GetSid(pid);
            if (sessionId != -1)
            {
                procInfo.SessionId = sessionId;
            }

            // Create a threadinfo for each thread in the process
            List<KeyValuePair<ulong, Interop.libproc.proc_threadinfo?>> lstThreads = Interop.libproc.GetAllThreadsInProcess(pid);
            foreach (KeyValuePair<ulong, Interop.libproc.proc_threadinfo?> t in lstThreads)
            {
                var ti = new ThreadInfo()
                {
                    _processId = pid,
                    _threadId = t.Key,
                    _basePriority = procInfo.BasePriority,
                    _startAddress = IntPtr.Zero
                };

                // Fill in additional info if we were able to retrieve such data about the thread
                if (t.Value.HasValue)
                {
                    ti._currentPriority = t.Value.Value.pth_curpri;
                    ti._threadState = ConvertOsxThreadRunStateToThreadState((Interop.libproc.ThreadRunState)t.Value.Value.pth_run_state);
                    ti._threadWaitReason = ConvertOsxThreadFlagsToWaitReason((Interop.libproc.ThreadFlags)t.Value.Value.pth_flags);
                }

                procInfo._threadInfoList.Add(ti);
            }

            return procInfo;
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        private static System.Diagnostics.ThreadState ConvertOsxThreadRunStateToThreadState(Interop.libproc.ThreadRunState state)
        {
            switch (state)
            {
                case Interop.libproc.ThreadRunState.TH_STATE_RUNNING:
                    return ThreadState.Running;
                case Interop.libproc.ThreadRunState.TH_STATE_STOPPED:
                    return ThreadState.Terminated;
                case Interop.libproc.ThreadRunState.TH_STATE_HALTED:
                    return ThreadState.Wait;
                case Interop.libproc.ThreadRunState.TH_STATE_UNINTERRUPTIBLE:
                    return ThreadState.Running;
                case Interop.libproc.ThreadRunState.TH_STATE_WAITING:
                    return ThreadState.Standby;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static System.Diagnostics.ThreadWaitReason ConvertOsxThreadFlagsToWaitReason(Interop.libproc.ThreadFlags flags)
        {
            // Since ThreadWaitReason isn't a flag, we have to do a mapping and will lose some information.
            if ((flags & Interop.libproc.ThreadFlags.TH_FLAGS_SWAPPED) == Interop.libproc.ThreadFlags.TH_FLAGS_SWAPPED)
                return ThreadWaitReason.PageOut;
            else
                return ThreadWaitReason.Unknown; // There isn't a good mapping for anything else
        }
    }
}
