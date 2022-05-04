// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text;

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        private ThreadPriorityLevel PriorityLevelCore
        {
            // This mapping is relatively arbitrary.  0 is normal based on the man page,
            // and the other values above and below are simply distributed evenly.
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return Interop.Sys.GetThreadPriorityFromNiceValue((int)stat.nice);
            }
            set
            {
                throw new PlatformNotSupportedException(); // We can find no API to set this on Linux
            }
        }

        private TimeSpan GetPrivilegedProcessorTime() => Process.TicksToTimeSpan(GetStat().stime);

        private DateTime GetStartTime() => Process.BootTimeToDateTime(Process.TicksToTimeSpan(GetStat().starttime));

        private TimeSpan GetTotalProcessorTime()
        {
            Interop.procfs.ParsedStat stat = GetStat();
            return Process.TicksToTimeSpan(stat.utime + stat.stime);
        }

        private TimeSpan GetUserProcessorTime() => Process.TicksToTimeSpan(GetStat().utime);

        private Interop.procfs.ParsedStat GetStat()
        {
            Interop.procfs.ParsedStat stat;
            if (!Interop.procfs.TryReadStatFile(pid: _processId, tid: Id, result: out stat))
            {
                throw new InvalidOperationException(SR.Format(SR.ThreadExited, Id));
            }
            return stat;
        }
    }
}
