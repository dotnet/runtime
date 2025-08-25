// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class @procfs
    {
        internal const string RootPath = "/proc/";
        private const string psinfoFileName = "/psinfo";
        private const string lwpDirName = "/lwp";
        private const string lwpsinfoFileName = "/lwpsinfo";

        // Constants from sys/procfs.h
        private const int PRARGSZ = 80;

        // Output type for TryGetProcessInfoById()
        // Keep in sync with pal_io.h ProcessStatus
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
            // add more fields when needed.
        }

        // Output type for TryGetThreadInfoById()
        // Keep in sync with pal_io.h ThreadStatus
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
            // add more fields when needed.
        }

        internal static string GetInfoFilePathForProcess(int pid) =>
            $"{RootPath}{(uint)pid}{psinfoFileName}";

        internal static string GetLwpDirForProcess(int pid) =>
            $"{RootPath}{(uint)pid}{lwpDirName}";

        internal static string GetInfoFilePathForThread(int pid, int tid) =>
            $"{RootPath}{(uint)pid}{lwpDirName}/{(uint)tid}{lwpsinfoFileName}";

    }
}
