// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class @procfs
    {

        // See caller: ProcessManager.SunOS.cs

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadProcessLwpInfo", SetLastError = true)]
        internal static unsafe partial int ReadProcessLwpInfo(int pid, int tid, ThreadInfo* threadInfo);

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
            ThreadInfo info = default;
            if (ReadProcessLwpInfo(pid, tid, &info) < 0)
            {
                Interop.ErrorInfo errorInfo = Sys.GetLastErrorInfo();
                throw new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
            }
            threadInfo = info;

            return true;
        }

    }
}
