// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets whether the process with the specified ID on the specified machine is currently running.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <param name="isRemoteMachine">Unused on Unix since remote machines are not supported.</param>
        /// <returns>true if the process is running; otherwise, false.</returns>
#pragma warning disable IDE0060
        public static bool IsProcessRunning(int processId, string machineName, bool isRemoteMachine) =>
            IsProcessRunning(processId);
#pragma warning restore IDE0060

        /// <summary>Gets whether the process with the specified ID is currently running.</summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>true if the process is running; otherwise, false.</returns>
        public static bool IsProcessRunning(int processId)
        {
            // kill with signal==0 means to not actually send a signal.
            // If we get back 0, the process is still alive.
            int output = Interop.Sys.Kill(processId, 0);
            // If kill set errno=EPERM, assume querying process is alive.
            return 0 == output || (-1 == output && Interop.Error.EPERM == Interop.Sys.GetLastError());
        }

        /// <summary>Gets the ProcessInfo for the specified process ID on the specified machine.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">Unused on Unix since remote machines are not supported.</param>
        /// <param name="isRemoteMachine">Unused on Unix since remote machines are not supported.</param>
        /// <returns>The ProcessInfo for the process if it could be found; otherwise, null.</returns>
#pragma warning disable IDE0060
        public static ProcessInfo? GetProcessInfo(int processId, string machineName, bool isRemoteMachine) =>
            CreateProcessInfo(processId);
#pragma warning restore IDE0060

        private static bool IsRemoteMachine(string machineName)
        {
            return
                machineName != "." &&
                machineName != Interop.Sys.GetHostName();
        }

        internal static bool HandleRemoteMachineSupport(string machineName)
        {
            ArgumentException.ThrowIfNullOrEmpty(machineName);
            if (IsRemoteMachine(machineName))
            {
                throw new PlatformNotSupportedException(SR.RemoteMachinesNotSupported);
            }
            return false;
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <remarks>On Unix, <paramref name="isRemoteMachine"/> and <paramref name="machineName"/> are unused since remote machines are not supported.</remarks>
        /// <param name="builder">The builder to add found process infos to.</param>
        /// <param name="processNameFilter">Optional process name to use as an inclusion filter.</param>
        /// <param name="machineName">Unused on Unix.</param>
        /// <param name="isRemoteMachine">Unused on Unix.</param>
#pragma warning disable IDE0060
        public static void GetProcessInfos(ref ArrayBuilder<ProcessInfo> builder, string? processNameFilter, string machineName, bool isRemoteMachine) =>
            GetProcessInfos(ref builder, processNameFilter);
#pragma warning restore IDE0060

    }
}
