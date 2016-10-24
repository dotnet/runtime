// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Text;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets whether the process with the specified ID on the specified machine is currently running.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <returns>true if the process is running; otherwise, false.</returns>
        public static bool IsProcessRunning(int processId, string machineName)
        {
            ThrowIfRemoteMachine(machineName);
            return IsProcessRunning(processId);
        }

        /// <summary>Gets whether the process with the specified ID is currently running.</summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>true if the process is running; otherwise, false.</returns>
        public static bool IsProcessRunning(int processId)
        {
            // kill with signal==0 means to not actually send a signal.
            // If we get back 0, the process is still alive.
            return 0 == Interop.Sys.Kill(processId, Interop.Sys.Signals.None);
        }

        /// <summary>Gets the ProcessInfo for the specified process ID on the specified machine.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <returns>The ProcessInfo for the process if it could be found; otherwise, null.</returns>
        public static ProcessInfo GetProcessInfo(int processId, string machineName)
        {
            ThrowIfRemoteMachine(machineName);
            return CreateProcessInfo(processId);
        }

        /// <summary>Gets the IDs of all processes on the specified machine.</summary>
        /// <param name="machineName">The machine to examine.</param>
        /// <returns>An array of process IDs from the specified machine.</returns>
        public static int[] GetProcessIds(string machineName)
        {
            ThrowIfRemoteMachine(machineName);
            return GetProcessIds();
        }

        /// <summary>Gets the ID of a process from a handle to the process.</summary>
        /// <param name="processHandle">The handle.</param>
        /// <returns>The process ID.</returns>
        public static int GetProcessIdFromHandle(SafeProcessHandle processHandle)
        {
            return (int)processHandle.DangerousGetHandle(); // not actually dangerous; just wraps a process ID
        }

        /// <summary>Gets whether the named machine is remote or local.</summary>
        /// <param name="machineName">The machine name.</param>
        /// <returns>true if the machine is remote; false if it's local.</returns>
        public static bool IsRemoteMachine(string machineName)
        {
            return 
                machineName != "." && 
                machineName != Interop.Sys.GetHostName();
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        internal static void ThrowIfRemoteMachine(string machineName)
        {
            if (IsRemoteMachine(machineName))
            {
                throw new PlatformNotSupportedException(SR.RemoteMachinesNotSupported);
            }
        }
        public static IntPtr GetMainWindowHandle(int processId)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
