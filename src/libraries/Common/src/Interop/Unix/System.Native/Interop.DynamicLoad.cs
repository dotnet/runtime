// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_LoadLibrary", CharSet = CharSet.Ansi)]
        internal static partial IntPtr LoadLibrary(string filename);

        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetProcAddress")]
        internal static partial IntPtr GetProcAddress(IntPtr handle, byte* symbol);

        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetProcAddress", CharSet = CharSet.Ansi)]
        internal static partial IntPtr GetProcAddress(IntPtr handle, string symbol);

        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_FreeLibrary")]
        internal static partial void FreeLibrary(IntPtr handle);
    }
}
