// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;

internal static partial class Interop
{
    internal static partial class @procfs
    {

        /// <summary>
        /// Attempts to get status info for the specified thread ID.
        /// </summary>
        /// <param name="pid">PID of the process to read status info for.</param>
        /// <param name="tid">TID of the thread to read status info for.</param>
        /// <param name="result">The pointer to ThreadInfo instance.</param>
        /// <returns>
        /// true if the thread info was read; otherwise, false.
        /// </returns>

        // ProcessManager.SunOS.cs calls this
        internal static bool TryGetThreadInfoById(int pid, int tid, out ThreadInfo result)
        {
            result = default;

            try
            {
                string fileName = GetInfoFilePathForThread(pid, tid);
                using FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                lwpsinfo pr;
                Unsafe.SkipInit(out pr);
                Span<byte> prspan = MemoryMarshal.AsBytes(new Span<lwpsinfo>(ref pr));
                Debug.Assert(prspan.Length == PR_LWPSINFO_SIZE,
                    $"psinfo struct size {prspan.Length} bytes not {PR_LWPSINFO_SIZE}.");
                fs.ReadExactly(prspan);

                result.Tid = pr.pr_lwpid;
                result.Priority = pr.pr_pri;
                result.NiceVal = (int)pr.pr_nice;
                result.Status = (char)pr.pr_sname;
                result.StartTime.TvSec = pr.pr_start.tv_sec;
                result.StartTime.TvNsec = pr.pr_start.tv_nsec;
                result.CpuTotalTime.TvSec = pr.pr_time.tv_sec;
                result.CpuTotalTime.TvNsec = pr.pr_time.tv_nsec;

                return true;
            }
            catch (Exception e)
            {
                Debug.Fail($"Failed to read thread info for PID {pid} TID {tid}: {e}");
            }

            return false;
        }

    }
}
