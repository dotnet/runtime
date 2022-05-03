// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial IntPtr CreateThreadpoolTimer(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> pfnti, IntPtr pv, IntPtr pcbe);

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial IntPtr SetThreadpoolTimer(IntPtr pti, long* pftDueTime, uint msPeriod, uint msWindowLength);
    }
}
