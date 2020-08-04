// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum Signals : int
        {
            None = 0,
            SIGKILL = 9,
#if TARGET_MIPS64
            //see kernel source code arch/mips/include/uapi/asm/signal.h
            SIGSTOP = 23
#else
            SIGSTOP = 19
#endif
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Kill", SetLastError = true)]
        internal static extern int Kill(int pid, Signals signal);
    }
}
