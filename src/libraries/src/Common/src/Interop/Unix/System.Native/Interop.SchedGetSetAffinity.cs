// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SchedSetAffinity", SetLastError = true)]
        internal static extern int SchedSetAffinity(int pid, ref IntPtr mask);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SchedGetAffinity", SetLastError = true)]
        internal static extern int SchedGetAffinity(int pid, out IntPtr mask);
    }
}
