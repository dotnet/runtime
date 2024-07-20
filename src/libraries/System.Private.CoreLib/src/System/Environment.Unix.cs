// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace System
{
    public static partial class Environment
    {
        public static string[] GetLogicalDrives() => Interop.Sys.GetAllMountPoints();

        public static string MachineName
        {
            get
            {
                string hostName = Interop.Sys.GetHostName();
                int dotPos = hostName.IndexOf('.');
                return dotPos < 0 ? hostName : hostName.Substring(0, dotPos);
            }
        }

        public static string UserName => Interop.Sys.GetUserNameFromPasswd(Interop.Sys.GetEUid());

        private static bool IsPrivilegedProcessCore() => Interop.Sys.GetEUid() == 0;

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Avoid inlining PInvoke frame into the hot path
        private static int GetProcessId() => Interop.Sys.GetPid();

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Avoid inlining PInvoke frame into the hot path
        private static string? GetProcessPath() => Interop.Sys.GetProcessPath();

        private static string[] GetCommandLineArgsNative()
        {
            // This is only used for delegate created from native host

            // Consider to use /proc/self/cmdline to get command line
            return Array.Empty<string>();
        }

#if !TARGET_IOS && !TARGET_TVOS
        /// <summary>
        /// Get the CPU usage, including the process time spent running the application code, the process time spent running the operating system code,
        /// and the total time spent running both the application and operating system code.
        /// </summary>
        [SupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static ProcessCpuUsage CpuUsage
        {
            get
            {
                Interop.Sys.ProcessCpuInformation cpuInfo = default;
                Interop.Sys.GetCpuUtilization(ref cpuInfo);

                ulong userTime100Nanoseconds = cpuInfo.lastRecordedUserTime / 100; // nanoseconds to 100-nanoseconds
                if (userTime100Nanoseconds > long.MaxValue)
                {
                    userTime100Nanoseconds = long.MaxValue;
                }

                ulong kernelTime100Nanoseconds = cpuInfo.lastRecordedKernelTime / 100; // nanoseconds to 100-nanoseconds
                if (kernelTime100Nanoseconds > long.MaxValue)
                {
                    kernelTime100Nanoseconds = long.MaxValue;
                }

                return new ProcessCpuUsage { UserTime = new TimeSpan((long)userTime100Nanoseconds), PrivilegedTime = new TimeSpan((long)kernelTime100Nanoseconds) };
            }
        }
#endif // !TARGET_OSX && !TARGET_IOS && !TARGET_TVOS && !TARGET_MACCATALYST
    }
}
