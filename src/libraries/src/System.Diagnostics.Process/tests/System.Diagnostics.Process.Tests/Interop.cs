// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace System.Diagnostics.ProcessTests
{
    internal class Interop
    {
        [StructLayout(LayoutKind.Sequential, Size = 40)]
        public struct PROCESS_MEMORY_COUNTERS
        {
            public uint cb;
            public uint PageFaultCount;
            public uint PeakWorkingSetSize;
            public uint WorkingSetSize;
            public uint QuotaPeakPagedPoolUsage;
            public uint QuotaPagedPoolUsage;
            public uint QuotaPeakNonPagedPoolUsage;
            public uint QuotaNonPagedPoolUsage;
            public uint PagefileUsage;
            public uint PeakPagefileUsage;
        }

        [DllImport("api-ms-win-core-memory-l1-1-1.dll")]
        public static extern bool GetProcessWorkingSetSizeEx(SafeProcessHandle hProcess, out IntPtr lpMinimumWorkingSetSize, out IntPtr lpMaximumWorkingSetSize, out uint flags);

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern int GetPriorityClass(SafeProcessHandle handle);

        [DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true)]
        public static extern SafeProcessHandle GetCurrentProcess();

        [DllImport("api-ms-win-core-processthreads-l1-1-0.dll")]
        internal static extern int GetCurrentProcessId();

        [DllImport("libc")]
        internal static extern int getpid();

        [DllImport("libc")]
        internal static extern int getsid(int pid);

        [DllImport("api-ms-win-core-processthreads-l1-1-2.dll")]
        internal static extern bool ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);

        [DllImport("api-ms-win-core-processthreads-l1-1-0.dll")]
        public static extern int GetProcessId(SafeProcessHandle nativeHandle);

        [DllImport("api-ms-win-core-console-l1-1-0.dll")]
        internal extern static int GetConsoleCP();

        [DllImport("api-ms-win-core-console-l1-1-0.dll")]
        internal extern static int GetConsoleOutputCP();

        [DllImport("api-ms-win-core-console-l1-1-0.dll")]
        internal extern static int SetConsoleCP(int codePage);

        [DllImport("api-ms-win-core-console-l1-1-0.dll")]
        internal extern static int SetConsoleOutputCP(int codePage);
    }
}
