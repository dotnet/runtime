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
            return Interop.Process.ListAllPids();
        }

        internal static string GetProcPath(int processId)
        {
            return Interop.Process.GetProcPath(processId);
        }

        internal static ProcessInfo? CreateProcessInfo(int pid, string? processNameFilter = null)
        {
            // Negative PIDs aren't valid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid));
            }

            // Try to get the task info. This can fail if the user permissions don't permit
            // this user context to query the specified process
            ProcessInfo iinfo = Interop.Process.GetProcessInfoById(pid);
            if (!string.IsNullOrEmpty(processNameFilter) && !processNameFilter.Equals(iinfo.ProcessName))
            {
                return null;
            }

            ProcessInfo procInfo = new ProcessInfo()
            {
                ProcessId = pid,
                ProcessName = iinfo.ProcessName,
                BasePriority = iinfo.BasePriority,
                VirtualBytes = iinfo.VirtualBytes,
                WorkingSet = iinfo.WorkingSet,
                SessionId = iinfo.SessionId,
            };

            foreach (ThreadInfo ti in iinfo._threadInfoList)
            {
                procInfo._threadInfoList.Add(ti);
            }

            return procInfo;
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

    }
}
