// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

internal static partial class Interop
{
    internal static partial class @procfs
    {

        /// <summary>
        /// Attempts to get status info for the specified process ID.
        /// </summary>
        /// <param name="pid">PID of the process to read status info for.</param>
        /// <param name="result">The pointer to ProcessInfo instance.</param>
        /// <returns>
        /// true if the process info was read; otherwise, false.
        /// </returns>

        // ProcessManager.SunOS.cs calls this
        internal static bool TryGetProcessInfoById(int pid, out ProcessInfo result)
        {
            result = default;

            try
            {
                string fileName = GetInfoFilePathForProcess(pid);
                using FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                psinfo pr;
                Unsafe.SkipInit(out pr);
                Span<byte> prspan = MemoryMarshal.AsBytes(new Span<psinfo>(ref pr));
                Debug.Assert(prspan.Length == PR_PSINFO_SIZE,
                    $"psinfo struct size {prspan.Length} bytes not {PR_PSINFO_SIZE}.");
                fs.ReadExactly(prspan);

                result.Pid = pr.pr_pid;
                result.ParentPid = pr.pr_ppid;
                result.SessionId = pr.pr_sid;
                result.VirtualSize = (nuint)pr.pr_size * 1024; // pr_size is in Kbytes
                result.ResidentSetSize = (nuint)pr.pr_rssize * 1024; // pr_rssize is in Kbytes
                result.StartTime.TvSec = pr.pr_start.tv_sec;
                result.StartTime.TvNsec = pr.pr_start.tv_nsec;
                result.CpuTotalTime.TvSec = pr.pr_time.tv_sec;
                result.CpuTotalTime.TvNsec = pr.pr_time.tv_nsec;

                // Get Args as a managed string, using accessor for pr_psargs[]
                ReadOnlySpan<byte> argspan = pr.PsArgsSpan;
                int argslen = argspan.IndexOf((byte)0);
                argslen = (argslen >= 0) ? argslen : argspan.Length;
                result.Args = Encoding.UTF8.GetString(argspan.Slice(0, argslen));

                // A couple things from pr_lwp
                result.Priority = pr.pr_lwp.pr_pri;
                result.NiceVal  = (int)pr.pr_lwp.pr_nice;

                return true;
            }
            catch (Exception e)
            {
                Debug.Fail($"Failed to read process info for PID {pid}: {e}");
            }

            return false;
        }

    }
}
