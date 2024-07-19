// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        private static long s_ticksPerSecond;

        /// <summary>Convert a number of "jiffies", or ticks, to a TimeSpan.</summary>
        /// <param name="ticks">The number of ticks.</param>
        /// <returns>The equivalent TimeSpan.</returns>
        internal static TimeSpan TicksToTimeSpan(double ticks)
        {
            long ticksPerSecond = Volatile.Read(ref s_ticksPerSecond);
            if (ticksPerSecond == 0)
            {
                // Look up the number of ticks per second in the system's configuration,
                // then use that to convert to a TimeSpan
                ticksPerSecond = Interop.Sys.SysConf(Interop.Sys.SysConfName._SC_CLK_TCK);
                if (ticksPerSecond <= 0)
                {
                    throw new Win32Exception();
                }

                Volatile.Write(ref s_ticksPerSecond, ticksPerSecond);
            }

            return TimeSpan.FromSeconds(ticks / (double)ticksPerSecond);
        }
    }
}
