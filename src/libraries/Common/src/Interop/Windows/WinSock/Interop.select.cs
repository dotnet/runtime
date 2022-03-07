// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [GeneratedDllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static unsafe partial int select(
            int ignoredParameter,
            IntPtr* readfds,
            IntPtr* writefds,
            IntPtr* exceptfds,
            ref TimeValue timeout);

        [GeneratedDllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static unsafe partial int select(
            int ignoredParameter,
            IntPtr* readfds,
            IntPtr* writefds,
            IntPtr* exceptfds,
            IntPtr nullTimeout);
    }
}
