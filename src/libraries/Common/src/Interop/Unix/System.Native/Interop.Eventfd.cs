// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        //
        // Since eventfd is a Linux-only feature, there's no need to define our own
        // flag values: pass these values through as the Linux system call expects.
        //
        [Flags]
        internal enum EventFdFlags
        {
            EFD_SEMAPHORE = 0x1,
            EFD_CLOEXEC = 0x80000,
            EFD_NONBLOCK = 0x800,
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_EventFD", SetLastError = true)]
        internal static extern unsafe int EventFD(uint initialVal, EventFdFlags flags = 0);
    }
}
