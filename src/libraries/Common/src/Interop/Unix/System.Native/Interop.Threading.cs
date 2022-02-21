// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateThread")]
        internal static unsafe partial bool CreateThread(IntPtr stackSize, delegate* unmanaged<IntPtr, IntPtr> startAddress, IntPtr parameter);
    }
}
