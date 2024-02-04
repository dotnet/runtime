// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "OpenSemaphoreW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafeWaitHandle OpenSemaphore(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, string name);

        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateSemaphoreExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafeWaitHandle CreateSemaphoreEx(IntPtr lpSecurityAttributes, int initialCount, int maximumCount, string? name, uint flags, uint desiredAccess);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ReleaseSemaphore(SafeWaitHandle handle, int releaseCount, out int previousCount);
    }
}
