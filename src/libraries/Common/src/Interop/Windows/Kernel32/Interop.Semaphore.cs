// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "OpenSemaphoreW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial SafeWaitHandle OpenSemaphore(uint desiredAccess, bool inheritHandle, string name);

        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "CreateSemaphoreExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial SafeWaitHandle CreateSemaphoreEx(IntPtr lpSecurityAttributes, int initialCount, int maximumCount, string? name, uint flags, uint desiredAccess);

        [GeneratedDllImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial bool ReleaseSemaphore(SafeWaitHandle handle, int releaseCount, out int previousCount);
    }
}
