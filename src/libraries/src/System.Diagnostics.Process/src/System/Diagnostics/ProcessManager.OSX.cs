// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            return Interop.libproc.proc_listallpids();
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private unsafe static ProcessInfo CreateProcessInfo(int pid)
        {
            // Negative PIDs aren't valid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException("pid");
            }

            ProcessInfo procInfo = new ProcessInfo();

            // Try to get the task info. This can fail if the user permissions don't permit
            // this user context to query the specified process
            Interop.libproc.proc_taskallinfo? info = Interop.libproc.GetProcessInfoById(pid);
            if (info.HasValue)
            {
                // We need to convert the byte pointer to an IntPtr 
                // that we can pass to the Marshal.PtrToStringAnsi call
                // but the nullable struct type makes it difficult to inline,
                // so make a temp variable to remove the nullable and get the pointer 
                Interop.libproc.proc_taskallinfo temp = info.Value;
                IntPtr ptrString = new IntPtr(temp.pbsd.pbi_comm);

                // Set the values we have; all the other values don't have meaning or don't exist on OSX
                procInfo.BasePriority = temp.ptinfo.pti_priority;
                procInfo.HandleCount = Interop.libproc.GetFileDescriptorCountForPid(pid);
                procInfo.ProcessId = pid;
                procInfo.ProcessName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptrString);
                procInfo.VirtualBytes = (long)temp.ptinfo.pti_virtual_size;
                procInfo.WorkingSet = (long)temp.ptinfo.pti_resident_size;
            }

            // Create a threadinfo for each thread in the process
            List<KeyValuePair<ulong, Interop.libproc.proc_threadinfo?>> lstThreads = Interop.libproc.GetAllThreadsInProcess(pid);
            foreach (KeyValuePair<ulong, Interop.libproc.proc_threadinfo?> t in lstThreads)
            {
                if (t.Value.HasValue)
                {
                    procInfo._threadInfoList.Add(new ThreadInfo()
                    {
                        _basePriority = 0,
                        _currentPriority = t.Value.Value.pth_curpri,
                        _processId = pid,
                        _startAddress = IntPtr.Zero, // We don't have this info
                        _threadId = Convert.ToInt32(t.Key),
                        _threadState = ConvertOsxThreadRunStateToThreadState((Interop.libproc.ThreadRunState)t.Value.Value.pth_run_state),
                        _threadWaitReason = ConvertOsxThreadFlagsToWaitReason((Interop.libproc.ThreadFlags)t.Value.Value.pth_flags)
                    });
                }
            }

            return procInfo;
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        internal static ModuleInfo[] GetModuleInfos(int processId)
        {
            // We currently don't provide support for modules on OS X.
            return Array.Empty<ModuleInfo>();
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        private static System.Diagnostics.ThreadState ConvertOsxThreadRunStateToThreadState(Interop.libproc.ThreadRunState state)
        {
            switch (state)
            {
                case Interop.libproc.ThreadRunState.TH_STATE_RUNNING:
                    return System.Diagnostics.ThreadState.Running;
                case Interop.libproc.ThreadRunState.TH_STATE_STOPPED:
                    return System.Diagnostics.ThreadState.Terminated;
                case Interop.libproc.ThreadRunState.TH_STATE_HALTED:
                    return System.Diagnostics.ThreadState.Wait;
                case Interop.libproc.ThreadRunState.TH_STATE_UNINTERRUPTIBLE:
                    return System.Diagnostics.ThreadState.Running;
                case Interop.libproc.ThreadRunState.TH_STATE_WAITING:
                    return System.Diagnostics.ThreadState.Standby;
                default:
                    throw new ArgumentOutOfRangeException("state");
            }
        }

        private static System.Diagnostics.ThreadWaitReason ConvertOsxThreadFlagsToWaitReason(Interop.libproc.ThreadFlags flags)
        {
            // Since ThreadWaitReason isn't a flag, we have to do a mapping and will lose some information.
            if ((flags & Interop.libproc.ThreadFlags.TH_FLAGS_SWAPPED) == Interop.libproc.ThreadFlags.TH_FLAGS_SWAPPED)
                return System.Diagnostics.ThreadWaitReason.PageOut;
            else
                return System.Diagnostics.ThreadWaitReason.Unknown; // There isn't a good mapping for anything else
        }
    }
}
