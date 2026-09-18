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
        ///    notifications were requested. When <paramref name="isExited"/> is <see langword="false"/>,
        ///    <paramref name="isPtraceStop"/> is set to <see langword="true"/> if the notification is
        ///    specifically a ptrace stop rather than a plain job-control stop/continue.
        /// 2) if no children are waitable, 0 is returned
        /// 3) on error, -1 is returned
        /// </returns>
        /// <remarks>
        /// This never consumes the observed notification (it always uses WNOWAIT), so it is always safe to
        /// call regardless of which pid it turns out to be.
        /// </remarks>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitIdAnyExitedNoHangNoWait", SetLastError = true)]
        internal static partial int WaitIdAnyExitedNoHangNoWait([MarshalAs(UnmanagedType.Bool)] out bool isExited, [MarshalAs(UnmanagedType.Bool)] out bool isPtraceStop);

        /// <summary>
        /// Consumes a pending stop/continue notification for a specific pid, if any, without ever
        /// consuming an exit notification for it.
        /// </summary>
        /// <returns>0 on success (whether or not anything was pending to drain), or -1 on error.</returns>
        /// <remarks>
        /// This is a targeted counterpart to <see cref="WaitIdAnyExitedNoHangNoWait"/>'s WNOWAIT peek,
        /// intended to drain the stale notification it can observe for a plain job-control stop/continue
        /// (notably on macOS, which reports these even though only WEXITED was requested there).
        /// </remarks>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitIdDrainNonExited", SetLastError = true)]
        internal static partial int WaitIdDrainNonExited(int pid);
    }
}
