// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [Flags]
        internal enum WaitPidOptions : int
        {
            None        = 0,    /* no options */
            WNOHANG     = 1,    /* don't block waiting */
            WUNTRACED   = 2,    /* report status of stopped children */
        }
        
        /// <summary>
        /// Waits for child process(s) or gathers resource utilization information about child processes
        /// </summary>
        /// <param name="pid">The PID of a child process</param>
        /// <param name="status">The output wait status of the process</param>
        /// <param name="options">Tells the OS how to act on the WaitPid call (to block or not to block)</param>
        /// <returns>
        /// The return value from WaitPid can very greatly. 
        /// 1) returns the process id of a terminating or stopped child process
        /// 2) if no children are waiting, -1 is returned and errno is set to ECHILD
        /// 3) if WNOHANG is specified and there are no stopped or exited children, 0 is returned
        /// 4) on error, -1 is returned
        /// </returns>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitPid", SetLastError = true)]
        internal static extern int WaitPid(int pid, out int status, WaitPidOptions options);

        // The following 4 functions are wrappers around macros of the same name
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_WExitStatus")]
        internal static extern int WExitStatus(int status);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_WIfExited")]
        internal static extern bool WIfExited(int status);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_WIfSignaled")]
        internal static extern bool WIfSignaled(int status);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_WTermSig")]
        internal static extern int WTermSig(int status);
    }
}
