// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const uint CREATE_EVENT_INITIAL_SET = 0x2;
        internal const uint CREATE_EVENT_MANUAL_RESET = 0x1;

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern bool SetEvent(SafeWaitHandle handle);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern bool ResetEvent(SafeWaitHandle handle);

        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "CreateEventExW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeWaitHandle CreateEventEx(IntPtr lpSecurityAttributes, string? name, uint flags, uint desiredAccess);

        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "OpenEventW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeWaitHandle OpenEvent(uint desiredAccess, bool inheritHandle, string name);
    }
}
