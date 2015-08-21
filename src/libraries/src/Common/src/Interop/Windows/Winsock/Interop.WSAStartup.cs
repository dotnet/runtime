// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct WSAData
        {
            internal short wVersion;
            internal short wHighVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
            internal string szDescription;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 129)]
            internal string szSystemStatus;
            internal short iMaxSockets;
            internal short iMaxUdpDg;
            internal IntPtr lpVendorInfo;
        }

        // Important: this API is called once by the System.Net.NameResolution contract implementation.
        // WSACleanup is not called and will be automatically performed at process shutdown.
        [DllImport(Interop.Libraries.Ws2_32, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        internal static extern SocketError WSAStartup(
                                           [In] short wVersionRequested,
                                           [Out] out WSAData lpWSAData
                                           );
    }
}
