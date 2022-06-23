// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class @procfs
    {
        /// <summary>
        /// Attempts to get status info for the specified process ID.
        /// </summary>
        /// <param name="pid">PID of the process to read status info for.</param>
        /// <param name="processStatus">The pointer to processStatus instance.</param>
        /// <returns>
        /// true if the process status was read; otherwise, false.
        /// </returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadProcessStatusInfo", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool TryReadProcessStatusInfo(int pid, ProcessStatusInfo* processStatus);

        internal struct ProcessStatusInfo
        {
            internal nuint ResidentSetSize;
            // add more fields when needed.
        }

        internal static unsafe bool TryReadProcessStatusInfo(int pid, out ProcessStatusInfo statusInfo)
        {
            statusInfo = default;
            fixed (ProcessStatusInfo* pStatusInfo = &statusInfo)
            {
                return TryReadProcessStatusInfo(pid, pStatusInfo);
            }
        }
    }
}
