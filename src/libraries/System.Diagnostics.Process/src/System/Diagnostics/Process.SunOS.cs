// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        /// <summary>Gets the total processor time from the procfs psinfo, a.k.a. "user" + "system" time.</summary>
        private TimeSpan TotalProcessorTimeSunOS
        {
            get
            {
                Interop.procfs.ProcessInfo processInfo = GetProcInfo();
                return TimeSpan.FromSeconds(processInfo.CpuTotalTime) +
                    TimeSpan.FromMicroseconds(processInfo.CpuTotalTimeNsec / 1000);
            }
        }

        /// <summary>Gets the name that was used to start the process, or null if it could not be retrieved.</summary>
        internal static string? GetUntruncatedProcessName(ref Interop.procfs.ProcessInfo processInfo, ref string argString)
        {
            // This assumes the process name is the first part of the Args string
            // ending at the first space.  That seems to work well enough for now.
            // If someday this need to support a process name containing spaces,
            // this could call a new Interop function that reads /proc/$pid/auxv
            // (sys/auxv.h) and gets the AT_SUN_EXECNAME string from that file.
            if (processInfo.Pid != 0 && !string.IsNullOrEmpty(argString))
            {
                string[] argv = argString.Split(' ', 2);
                if (!string.IsNullOrEmpty(argv[0]))
                {
                    return Path.GetFileName(argv[0]);
                }
            }
            return null;
        }

        /// <summary>Reads the information for this process from the procfs file system.</summary>
        private Interop.procfs.ProcessInfo GetProcInfo()
        {
            EnsureState(State.HaveNonExitedId);
            Interop.procfs.ProcessInfo processInfo;
            if (!Interop.procfs.GetProcessInfoById(_processId, out processInfo))
            {
                throw new Win32Exception(SR.ProcessInformationUnavailable);
            }
            return processInfo;
        }
    }
}
