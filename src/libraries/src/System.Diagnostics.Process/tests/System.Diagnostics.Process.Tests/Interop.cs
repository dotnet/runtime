using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics.ProcessTests
{
    class Interop
    {
        [DllImport("api-ms-win-core-processthreads-l1-1-0.dll")]
        public static extern int GetProcessId(SafeProcessHandle nativeHandle);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, uint size);

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
    }
}
