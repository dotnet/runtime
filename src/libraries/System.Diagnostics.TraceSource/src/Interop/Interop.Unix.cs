// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using pid_t = System.Int32;

internal static partial class Interop
{
    private const string LIBC = "libc";

    // NOTE: libc's getpid caches the pid, which can have an effect on fork'd children.
    // (see the man page for more info).  If this becomes an issue, we may want to use
    // syscall directly to bypass the libc caching.
    [DllImport(LIBC)]
    internal static extern pid_t getpid();
}
