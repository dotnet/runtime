// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        // System priorities go from 1 to 100, where 60 and above are for "system" things
        // These mappingsare relatively arbitrary.  Normal user processes run at priority 59.
        // and the other values above and below are simply distributed somewhat evenly.
        private static System.Diagnostics.ThreadPriorityLevel GetThreadPriorityFromSysPri(int pri)
        {
            Debug.Assert((pri >= 0) && (pri <= 100));
            return
                (pri >= 90) ? ThreadPriorityLevel.TimeCritical :
                (pri >= 80) ? ThreadPriorityLevel.Highest :
                (pri >= 60)  ? ThreadPriorityLevel.AboveNormal :
                (pri == 59)  ? ThreadPriorityLevel.Normal :
                (pri >= 40)  ? ThreadPriorityLevel.BelowNormal :
                (pri >= 20) ? ThreadPriorityLevel.Lowest :
                ThreadPriorityLevel.Idle;
        }

        /// <summary>Reads the information for this thread from the procfs file system.</summary>
        private Interop.procfs.ThreadInfo GetThreadInfo()
        {
            Interop.procfs.ThreadInfo threadInfo;
            if (!Interop.procfs.GetThreadInfoById(_processId, tid: Id, out threadInfo))
            {
                throw new InvalidOperationException(SR.Format(SR.ThreadExited, Id));
            }
            return threadInfo;
        }
    }
}
