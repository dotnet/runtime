// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Returns the pid of a waitable child without reaping/consuming its notification.
        /// </summary>
        /// <returns>
        /// 1) returns the process id of a waitable child process; <paramref name="isExited"/> is set to
        ///    <see langword="true"/> if this represents an actual exit, or <see langword="false"/> if it is a
        ///    non-exit (stopped/continued) notification that some platforms report even though only exit
        ///    notifications were requested.
        /// 2) if no children are waitable, 0 is returned
        /// 3) on error, -1 is returned
        /// </returns>
        /// <remarks>
        /// This never consumes the observed notification. A non-exit notification should only be drained via
        /// <see cref="WaitIdDrainNonExited"/> once the managed caller has confirmed it owns the pid's reaping
        /// responsibility (e.g. it is a process started via Process.Start), since draining it could otherwise
        /// hide the notification from an unrelated WUNTRACED/WCONTINUED-based waiter.
        /// </remarks>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitIdAnyExitedNoHangNoWait", SetLastError = true)]
        internal static partial int WaitIdAnyExitedNoHangNoWait([MarshalAs(UnmanagedType.Bool)] out bool isExited);

        /// <summary>
        /// Consumes a pending stopped/continued (non-exit) notification for the specified pid so it is no
        /// longer reported by <see cref="WaitIdAnyExitedNoHangNoWait"/>.
        /// </summary>
        /// <remarks>
        /// Only call this for a pid the caller is certain it owns the reaping responsibility for.
        /// </remarks>
        /// <returns>0 on success (including when there was nothing to drain), -1 on error.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitIdDrainNonExited", SetLastError = true)]
        internal static partial int WaitIdDrainNonExited(int pid);
    }
}
