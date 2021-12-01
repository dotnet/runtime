// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const uint CREATE_MUTEX_INITIAL_OWNER = 0x1;

        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "OpenMutexW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial SafeWaitHandle OpenMutex(uint desiredAccess, bool inheritHandle, string name);

        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "CreateMutexExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial SafeWaitHandle CreateMutexEx(IntPtr lpMutexAttributes, string? name, uint flags, uint desiredAccess);

        [GeneratedDllImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial bool ReleaseMutex(SafeWaitHandle handle);
    }
}
