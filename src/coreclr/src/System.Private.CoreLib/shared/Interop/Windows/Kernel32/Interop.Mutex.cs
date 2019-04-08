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
        internal const uint CREATE_MUTEX_INITIAL_OWNER = 0x1;

        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "OpenMutexW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeWaitHandle OpenMutex(uint desiredAccess, bool inheritHandle, string name);

        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "CreateMutexExW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeWaitHandle CreateMutexEx(IntPtr lpMutexAttributes, string? name, uint flags, uint desiredAccess);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern bool ReleaseMutex(SafeWaitHandle handle);
    }
}
