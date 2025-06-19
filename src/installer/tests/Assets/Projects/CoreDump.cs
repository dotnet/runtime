// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.InteropServices;

namespace Utilities
{
    public static partial class CoreDump
    {
        public static void Disable()
        {
            string? envValue = Environment.GetEnvironmentVariable("DOTNET_DbgEnableMiniDump");
            if (envValue is not null && envValue != "0")
                throw new InvalidOperationException("DOTNET_DbgEnableMiniDump is set and not 0. Ensure it is unset or set to 0 to disable dumps.");

            envValue = Environment.GetEnvironmentVariable("COMPlus_DbgEnableMiniDump");
            if (envValue is not null && envValue != "0")
                throw new InvalidOperationException("COMPlus_DbgEnableMiniDump is set and not 0. Ensure it is unset or set to 0 to disable dumps.");

            if (OperatingSystem.IsLinux())
            {
                if (prctl(PR_SET_DUMPABLE, 0) != 0)
                {
                    throw new InvalidOperationException($"Failed to disable core dump. Error: {Marshal.GetLastPInvokeError()}.");
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                RLimit rlimit = new() { rlim_cur = 0, rlim_max = 0 };
                if (setrlimit(RLIMIT_CORE, rlimit) != 0)
                {
                    throw new InvalidOperationException($"Failed to disable core dump. Error: {Marshal.GetLastPInvokeError()}.");
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RLimit
        {
            // These are rlim_t. All macOS platforms we use this on currently define it as unsigned 64-bit
            public ulong rlim_cur; // Soft limit
            public ulong rlim_max; // Hard limit
        }

        // Max core file size
        private const int RLIMIT_CORE = 4;

        [DllImport("libc", SetLastError = true)]
        private static extern int setrlimit(int resource, in RLimit rlim);

        // "dumpable" attribute of the calling process
        private const int PR_SET_DUMPABLE = 4;

        [DllImport("libc", SetLastError = true)]
        private static extern int prctl(int option, int arg2);
    }
}
