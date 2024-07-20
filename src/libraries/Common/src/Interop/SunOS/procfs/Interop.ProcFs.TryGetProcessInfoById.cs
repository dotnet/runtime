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

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadProcessStatusInfo", SetLastError = true)]
        private static unsafe partial int ReadProcessStatusInfo(int pid, ProcessInfo* processInfo, byte* argBuf, int argBufSize);

        // Handy helpers for Environment.SunOS etc.

        /// <summary>
        /// Attempts to get status info for the specified process ID.
        /// </summary>
        /// <param name="pid">PID of the process to read status info for.</param>
        /// <param name="processInfo">The pointer to ProcessInfo instance.</param>
        /// <returns>
        /// true if the process status was read; otherwise, false.
        /// </returns>
        internal static unsafe bool TryGetProcessInfoById(int pid, out ProcessInfo processInfo)
        {
            ProcessInfo info = default;
            if (ReadProcessStatusInfo(pid, &info, null, 0) < 0)
            {
                Interop.ErrorInfo errorInfo = Sys.GetLastErrorInfo();
                throw new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
            }
            processInfo = info;

            return true;
        }

        // Variant that also gets the arg string.
        internal static unsafe bool TryGetProcessInfoById(int pid, out ProcessInfo processInfo, out  string argString)
        {
            ProcessInfo info = default;
            byte* argBuf = stackalloc byte[PRARGSZ];
            if (ReadProcessStatusInfo(pid, &info, argBuf, PRARGSZ) < 0)
            {
                Interop.ErrorInfo errorInfo = Sys.GetLastErrorInfo();
                throw new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
            }
            processInfo = info;
            argString = Marshal.PtrToStringUTF8((IntPtr)argBuf)!;

            return true;
        }


    }
}
