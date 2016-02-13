// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static class Fcntl
        {
            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetIsNonBlocking", SetLastError = true)]
            internal static extern int DangerousSetIsNonBlocking(IntPtr fd, int isNonBlocking);

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetIsNonBlocking", SetLastError=true)]
            internal static extern int SetIsNonBlocking(SafeHandle fd, int isNonBlocking);
        }
    }
}
