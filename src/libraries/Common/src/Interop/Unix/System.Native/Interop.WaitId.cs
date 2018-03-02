// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {        
        /// <summary>
        /// Waits for terminated child processes.
        /// </summary>
        /// <param name="pid">The PID of a child process. -1 for any child.</param>
        /// <param name="status">The output exit status of the process</param>
        /// <param name="keepWaitable">Tells the OS to leave the child waitable or reap it.</param>
        /// <returns>
        /// 1) returns the process id of a terminated child process
        /// 2) if no children are waiting, 0 is returned
        /// 3) on error, -1 is returned
        /// </returns>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitIdExitedNoHang", SetLastError = true)]
        internal static extern int WaitIdExitedNoHang(int pid, out int exitCode, bool keepWaitable);
    }
}
