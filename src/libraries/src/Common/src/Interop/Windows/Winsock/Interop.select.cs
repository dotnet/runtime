// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern int select(
            [In] int ignoredParameter,
            [In, Out] IntPtr[] readfds,
            [In, Out] IntPtr[] writefds,
            [In, Out] IntPtr[] exceptfds,
            [In] ref TimeValue timeout);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern int select(
            [In] int ignoredParameter,
            [In, Out] IntPtr[] readfds,
            [In, Out] IntPtr[] writefds,
            [In, Out] IntPtr[] exceptfds,
            [In] IntPtr nullTimeout);
    }
}
