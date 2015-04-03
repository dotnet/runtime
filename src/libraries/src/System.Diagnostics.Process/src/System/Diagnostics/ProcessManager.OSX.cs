// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        private static ProcessInfo CreateProcessInfo(int pid)
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
            if (info.HasValue == false)
            {
                procInfo._basePriority = info.Value.ptinfo.pti_priority;
                procInfo._handleCount = Interop.libproc.GetFileDescriptorsForPid(pid).Count;
                procInfo._processId = pid;
                procInfo._processName = info.Value.pbsd.pbi_comm;
                procInfo._virtualBytes = (long)info.Value.ptinfo.pti_virtual_size;
                procInfo._workingSet = (long)info.Value.ptinfo.pti_resident_size;

                // These values don't have meaning or don't exist on OSX
                procInfo._pageFileBytes = -1;
                procInfo._poolNonpagedBytes = -1;
                procInfo._poolPagedBytes = -1;
                procInfo._privateBytes = -1;
                procInfo._virtualBytesPeak = -1;
                procInfo._sessionId = -1;
            }

            // Create a threadinfo for each thread in the process
            Interop.libproc.GetAllThreadsInProcess(pid).ForEach(t =>
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
                                _threadState = Interop.libproc.ConvertOsxThreadRunStateToThreadState((Interop.libproc.ThreadRunState)t.Value.Value.pth_run_state),
                                _threadWaitReason = Interop.libproc.ConvertOsxThreadFlagsToWaitReason((Interop.libproc.ThreadFlags)t.Value.Value.pth_flags)
                            });
                    }
                });
            
            return procInfo;
        }

        // ----------------------------------
        // ---- OSX PAL layer ends here ----
        // ----------------------------------
    }
}
