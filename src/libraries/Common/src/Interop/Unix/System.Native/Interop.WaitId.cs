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
        /// Returns the pid of a terminated child without reaping it.
        /// </summary>
        /// <returns>
        /// 1) returns the process id of a terminated child process
        /// 2) if no children are terminated, 0 is returned
        /// 3) on error, -1 is returned
        /// </returns>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitIdAnyExitedNoHangNoWait", SetLastError = true)]
        internal static extern int WaitIdAnyExitedNoHangNoWait();
    }
}
