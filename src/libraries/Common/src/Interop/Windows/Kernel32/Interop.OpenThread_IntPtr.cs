// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        private const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        private const uint SYNCHRONIZE = 0x00100000;
        internal const uint THREAD_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x3FF;

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial IntPtr OpenThread(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inherited, int threadID);
    }
}
