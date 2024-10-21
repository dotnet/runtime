// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static partial class Fcntl
        {
            [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetIsNonBlocking", SetLastError = true)]
            internal static partial int DangerousSetIsNonBlocking(IntPtr fd, int isNonBlocking);

            [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetIsNonBlocking", SetLastError = true)]
            internal static partial int SetIsNonBlocking(SafeHandle fd, int isNonBlocking);

            [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlGetIsNonBlocking", SetLastError = true)]
            internal static partial int GetIsNonBlocking(SafeHandle fd, [MarshalAs(UnmanagedType.Bool)] out bool isNonBlocking);

            [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetFD", SetLastError = true)]
            internal static partial int SetFD(SafeHandle fd, int flags);

            [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlGetFD", SetLastError = true)]
            internal static partial int GetFD(SafeHandle fd);

            [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlGetFD", SetLastError = true)]
            internal static partial int GetFD(IntPtr fd);
        }
    }
}
