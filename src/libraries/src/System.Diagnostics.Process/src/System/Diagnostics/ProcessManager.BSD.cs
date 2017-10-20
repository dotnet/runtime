// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string machineName)
        {
            ThrowIfRemoteMachine(machineName);
            int[] procIds = GetProcessIds(machineName);

            // Iterate through all process IDs to load information about each process
            var processes = new List<ProcessInfo>(procIds.Length);
            foreach (int pid in procIds)
            {
                ProcessInfo pi = CreateProcessInfo(pid);
                if (pi != null)
                {
                    processes.Add(pi);
                }
            }

            return processes.ToArray();
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        internal static ProcessModuleCollection GetModules(int processId)
        {
            // We don't have a good way of getting all of the modules of the particular process,
            // but we can at least get the path to the executable file for the process, and
            // other than for debugging tools, that's the main reason consumers of Modules care about it,
            // and why MainModule exists.
            try
            {
                string exePath = GetProcPath(processId);
                if (!string.IsNullOrEmpty(exePath))
                {
                    return new ProcessModuleCollection(1)
                    {
                        new ProcessModule()
                        {
                            FileName = exePath,
                            ModuleName = Path.GetFileName(exePath)
                        }
                    };
                }
            }
            catch { } // eat all errors

            return new ProcessModuleCollection(0);
        }
    }
}
