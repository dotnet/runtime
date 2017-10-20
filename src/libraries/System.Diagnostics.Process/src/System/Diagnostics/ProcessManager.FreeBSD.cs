// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private static ProcessInfo CreateProcessInfo(int pid)
        {
            // Negative PIDs aren't valid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid));
            }

            ProcessInfo procInfo = new ProcessInfo()
            {
                ProcessId = pid
            };

            // Try to get the task info. This can fail if the user permissions don't permit
            // this user context to query the specified process
            ProcessInfo iinfo = Interop.Process.GetProcessInfoById(pid);

            procInfo.ProcessName = iinfo.ProcessName;
            procInfo.BasePriority = iinfo.BasePriority;
            procInfo.VirtualBytes = iinfo.VirtualBytes;
            procInfo.WorkingSet = iinfo.WorkingSet;
            procInfo.SessionId = iinfo.SessionId;
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
