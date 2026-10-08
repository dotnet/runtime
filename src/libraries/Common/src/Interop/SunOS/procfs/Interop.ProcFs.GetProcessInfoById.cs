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
        // Constants from sys/procfs.h
        private const int PRARGSZ = 80;

        // Output type for GetProcessInfoById()
        // Keep in sync with pal_io.h ProcessInfo
        [StructLayout(LayoutKind.Sequential)]
        internal struct ProcessInfo
        {
            internal ulong VirtualSize;
            internal ulong ResidentSetSize;
            internal long StartTime;
            internal long StartTimeNsec;
            internal long CpuTotalTime;
            internal long CpuTotalTimeNsec;
            internal int Pid;
            internal int ParentPid;
            internal int SessionId;
            internal int Priority;
            internal int NiceVal;
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadProcessInfo", SetLastError = true)]
        private static unsafe partial int ReadProcessInfo(int pid, ProcessInfo* processInfo, byte* argBuf, int argBufSize);

        /// <summary>
        /// Attempts to get status info for the specified process ID.
        /// </summary>
        /// <param name="pid">PID of the process to read status info for.</param>
        /// <param name="processInfo">The pointer to ProcessInfo instance.</param>
        /// <returns>
        /// true if the process status was read; otherwise, false.
        /// </returns>
        internal static unsafe bool GetProcessInfoById(int pid, out ProcessInfo processInfo)
        {
            fixed (ProcessInfo* pProcessInfo = &processInfo)
            {
                if (ReadProcessInfo(pid, pProcessInfo, null, 0) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        // Variant that also gets the arg string.
        internal static unsafe bool GetProcessInfoById(int pid, out ProcessInfo processInfo, out string argString)
        {
            byte* argBuf = stackalloc byte[PRARGSZ];
            fixed (ProcessInfo* pProcessInfo = &processInfo)
            {
                if (ReadProcessInfo(pid, pProcessInfo, argBuf, PRARGSZ) < 0)
                {
                    argString = "";
                    return false;
                }
            }
            argString = Marshal.PtrToStringUTF8((IntPtr)argBuf)!;
            return true;
        }
    }
}
