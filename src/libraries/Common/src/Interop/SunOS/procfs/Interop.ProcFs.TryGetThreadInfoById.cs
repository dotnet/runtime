// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// See callers:
// ProcessManager.SunOS.cs
// ProcessThread.SunOS etc.

internal static partial class Interop
{
    internal static partial class @procfs
    {

        // Output type for TryGetThreadInfoById()
        // Keep in sync with pal_io.h ThreadInfo
        [StructLayout(LayoutKind.Sequential)]
        internal struct ThreadInfo
        {
            internal long StartTime;
            internal long StartTimeNsec;
            internal long CpuTotalTime; // user+sys
            internal long CpuTotalTimeNsec;
            internal int Tid;
            internal int Priority;
            internal int NiceVal;
            internal char StatusCode;
        }

        // See caller: ProcessManager.SunOS.cs

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadThreadInfo", SetLastError = true)]
        private static unsafe partial int ReadThreadInfo(int pid, int tid, ThreadInfo* threadInfo);

        /// <summary>
        /// Attempts to get status info for the specified thread ID.
        /// </summary>
        /// <param name="pid">PID of the process to read status info for.</param>
        /// <param name="tid">TID of the thread to read status info for.</param>
        /// <param name="threadInfo">The pointer to ThreadInfo instance.</param>
        /// <returns>
        /// true if the process status was read; otherwise, false.
        /// </returns>
        internal static unsafe bool TryGetThreadInfoById(int pid, int tid, out ThreadInfo threadInfo)
        {
            fixed (ThreadInfo* pThreadInfo = &threadInfo)
            {
                if (ReadThreadInfo(pid, tid, pThreadInfo) < 0)
                {
                    Interop.ErrorInfo errorInfo = Sys.GetLastErrorInfo();
                    throw new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
                }
            }
            return true;
        }
    }
}
