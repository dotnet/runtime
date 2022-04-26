// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateThread")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CreateThread(IntPtr stackSize, delegate* unmanaged<IntPtr, IntPtr> startAddress, IntPtr parameter);
    }
}
