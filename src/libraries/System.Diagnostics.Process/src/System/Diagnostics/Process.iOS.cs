// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public void Kill(bool entireProcessTree)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Creates an array of <see cref="Process"/> components that are associated with process resources on a
        /// remote computer. These process resources share the specified process name.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static Process[] GetProcessesByName(string? processName, string machineName)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan PrivilegedProcessorTime
        {
            get { throw new PlatformNotSupportedException(); }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static DateTime StartTimeCore
        {
            get { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent utilizing the CPU.
        /// It is the sum of the <see cref='System.Diagnostics.Process.UserProcessorTime'/> and
        /// <see cref='System.Diagnostics.Process.PrivilegedProcessorTime'/>.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan TotalProcessorTime
        {
            get { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent running code
        /// inside the application portion of the process (not the operating system core).
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan UserProcessorTime
        {
            get { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// Returns all immediate child processes.
        /// </summary>
        private static List<Process> GetChildProcesses(Process[]? processes = null)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Gets parent process ID</summary>
        private static int GetParentProcessId =>
            throw new PlatformNotSupportedException();

        /// <summary>
        /// Gets or sets which processors the threads in this process can be scheduled to run on.
        /// </summary>
        private static IntPtr ProcessorAffinityCore
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// Make sure we have obtained the min and max working set limits.
        /// </summary>
        private static void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private static void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Gets execution path</summary>
        private static string GetPathToOpenFile()
        {
            throw new PlatformNotSupportedException();
        }

#pragma warning disable CA1822
        private int ParentProcessId => throw new PlatformNotSupportedException();
#pragma warning restore CA1822

        private static bool IsProcessInvalidException(Exception e) =>
            // InvalidOperationException signifies conditions such as the process already being dead.
            // Win32Exception signifies issues such as insufficient permissions to get details on the process.
            // In either case, the predicate couldn't be applied so return the fallback result.
            e is InvalidOperationException || e is Win32Exception;
    }
}
