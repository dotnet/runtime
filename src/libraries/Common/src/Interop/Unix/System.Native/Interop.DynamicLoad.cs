// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_LoadLibrary", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr LoadLibrary(string filename);

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetLoadLibraryError")]
        internal static partial IntPtr GetLoadLibraryError();

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetProcAddress")]
        internal static partial IntPtr GetProcAddress(IntPtr handle, byte* symbol);

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetProcAddress", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr GetProcAddress(IntPtr handle, string symbol);

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_FreeLibrary")]
        internal static partial void FreeLibrary(IntPtr handle);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetDefaultSearchOrderPseudoHandle", SetLastError = true)]
        internal static partial IntPtr GetDefaultSearchOrderPseudoHandle();
    }
}
