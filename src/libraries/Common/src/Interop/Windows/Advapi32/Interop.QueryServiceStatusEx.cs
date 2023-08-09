// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SERVICE_STATUS_PROCESS
        {
            public int dwServiceType;
            public int dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
            public int dwProcessId;
            public int dwServiceFlags;
        }

        private const int SC_STATUS_PROCESS_INFO = 0;

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool QueryServiceStatusEx(SafeServiceHandle serviceHandle, int InfoLevel, SERVICE_STATUS_PROCESS* pStatus, int cbBufSize, out int pcbBytesNeeded);

        internal static unsafe bool QueryServiceStatusEx(SafeServiceHandle serviceHandle, SERVICE_STATUS_PROCESS* pStatus) => QueryServiceStatusEx(serviceHandle, SC_STATUS_PROCESS_INFO, pStatus, sizeof(SERVICE_STATUS_PROCESS), out _);
    }
}
