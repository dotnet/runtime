// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SchedSetAffinity", SetLastError = true)]
        internal static partial int SchedSetAffinity(int pid, ref IntPtr mask);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SchedGetAffinity", SetLastError = true)]
        internal static partial int SchedGetAffinity(int pid, out IntPtr mask);
    }
}
