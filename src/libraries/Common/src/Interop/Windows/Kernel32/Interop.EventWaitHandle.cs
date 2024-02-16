// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const uint CREATE_EVENT_INITIAL_SET = 0x2;
        internal const uint CREATE_EVENT_MANUAL_RESET = 0x1;

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetEvent(SafeWaitHandle handle);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ResetEvent(SafeWaitHandle handle);

        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateEventExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafeWaitHandle CreateEventEx(IntPtr lpSecurityAttributes, string? name, uint flags, uint desiredAccess);

        [LibraryImport(Libraries.Kernel32, EntryPoint = "OpenEventW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafeWaitHandle OpenEvent(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, string name);
    }
}
