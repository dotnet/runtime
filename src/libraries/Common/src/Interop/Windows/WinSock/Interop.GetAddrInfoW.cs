// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, ExactSpelling = true, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        internal static extern int GetAddrInfoW(
            [In] string nodename,
            [In] string servicename,
            [In] ref AddressInfo hints,
            [Out] out SafeFreeAddrInfo handle
            );
    }
}
