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
            [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetIsNonBlocking", SetLastError = true)]
            internal static partial int DangerousSetIsNonBlocking(IntPtr fd, int isNonBlocking);

            [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetIsNonBlocking", SetLastError=true)]
            internal static partial int SetIsNonBlocking(SafeHandle fd, int isNonBlocking);

            [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlGetIsNonBlocking", SetLastError = true)]
            internal static partial int GetIsNonBlocking(SafeHandle fd, out bool isNonBlocking);

            [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetFD", SetLastError=true)]
            internal static partial int SetFD(SafeHandle fd, int flags);

            [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlGetFD", SetLastError=true)]
            internal static partial int GetFD(SafeHandle fd);

            [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlGetFD", SetLastError=true)]
            internal static partial int GetFD(IntPtr fd);
        }
    }
}
