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
        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "OpenSemaphoreW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeWaitHandle OpenSemaphore(uint desiredAccess, bool inheritHandle, string name);
        
        [DllImport(Libraries.Kernel32, EntryPoint = "CreateSemaphoreExW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeWaitHandle CreateSemaphoreEx(IntPtr lpSecurityAttributes, int initialCount, int maximumCount, string? name, uint flags, uint desiredAccess);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern bool ReleaseSemaphore(SafeWaitHandle handle, int releaseCount, out int previousCount);
    }
}
