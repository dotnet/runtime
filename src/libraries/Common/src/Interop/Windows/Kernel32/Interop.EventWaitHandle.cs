// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const uint CREATE_EVENT_INITIAL_SET = 0x2;
        internal const uint CREATE_EVENT_MANUAL_RESET = 0x1;

        [GeneratedDllImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial bool SetEvent(SafeWaitHandle handle);

        [GeneratedDllImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial bool ResetEvent(SafeWaitHandle handle);

        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "CreateEventExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial SafeWaitHandle CreateEventEx(IntPtr lpSecurityAttributes, string? name, uint flags, uint desiredAccess);

        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "OpenEventW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial SafeWaitHandle OpenEvent(uint desiredAccess, bool inheritHandle, string name);
    }
}
