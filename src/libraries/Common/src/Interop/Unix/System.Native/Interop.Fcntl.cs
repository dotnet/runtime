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
            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetIsNonBlocking", SetLastError = true)]
            internal static extern int DangerousSetIsNonBlocking(IntPtr fd, int isNonBlocking);

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetIsNonBlocking", SetLastError=true)]
            internal static extern int SetIsNonBlocking(SafeHandle fd, int isNonBlocking);

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlGetIsNonBlocking", SetLastError = true)]
            internal static extern int GetIsNonBlocking(SafeHandle fd, out bool isNonBlocking);

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetFD", SetLastError=true)]
            internal static extern int SetFD(SafeHandle fd, int flags);

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlGetFD", SetLastError=true)]
            internal static extern int GetFD(SafeHandle fd);

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlGetFD", SetLastError=true)]
            internal static extern int GetFD(IntPtr fd);
        }
    }
}
