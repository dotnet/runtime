// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="processNameFilter">Optional process name to use as an inclusion filter.</param>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static ProcessInfo[] GetProcessInfos(string? processNameFilter, string machineName)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        internal static ProcessModuleCollection GetModules(int processId)
        {
            return new ProcessModuleCollection(0);
        }

        private static ProcessInfo CreateProcessInfo(int pid)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
