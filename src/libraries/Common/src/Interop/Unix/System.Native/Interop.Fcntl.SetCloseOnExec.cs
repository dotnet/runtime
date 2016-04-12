// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static partial class Fcntl
        {
            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetCloseOnExec", SetLastError=true)]
            internal static extern int SetCloseOnExec(SafeHandle fd);
        }
    }
}
