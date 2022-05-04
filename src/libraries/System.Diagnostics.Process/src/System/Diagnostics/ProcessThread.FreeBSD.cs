// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        private ThreadPriorityLevel PriorityLevelCore
        {
            get
            {
                Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, Id);
                return Interop.Sys.GetThreadPriorityFromNiceValue((int)stat.nice);
            }
            set
            {
                throw new PlatformNotSupportedException(); // We can find no API to set this
            }
        }

        // kinfo_proc has one  entry per thread but ki_start seems to be same for
        // all threads e.g. reflects process start. This may be re-visited later.
        private static DateTime GetStartTime() => throw new PlatformNotSupportedException();

        private TimeSpan GetTotalProcessorTime()
        {
            Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, Id);
            return Process.TicksToTimeSpan(stat.userTime + stat.systemTime);
        }

        private TimeSpan GetUserProcessorTime()
            => Process.TicksToTimeSpan(Interop.Process.GetThreadInfo(_processId, Id).userTime);

        private TimeSpan GetPrivilegedProcessorTime()
            => Process.TicksToTimeSpan(Interop.Process.GetThreadInfo(_processId, Id).systemTime);
    }
}
