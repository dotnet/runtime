// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Net.Sockets;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        // Important: this API is called once by the System.Net.NameResolution contract implementation.
        // WSACleanup is not called and will be automatically performed at process shutdown.
        internal static unsafe SocketError WSAStartup()
        {
            WSAData d;
            return WSAStartup(0x0202 /* 2.2 */, &d);
        }

        [DllImport(Libraries.Ws2_32, SetLastError = true)]
        private static extern unsafe SocketError WSAStartup(short wVersionRequested, WSAData* lpWSAData);

        [StructLayout(LayoutKind.Sequential, Size = 408)]
        private unsafe struct WSAData
        {
            // WSADATA is defined as follows:
            //
            //     typedef struct WSAData {
            //             WORD                    wVersion;
            //             WORD                    wHighVersion;
            //     #ifdef _WIN64
            //             unsigned short          iMaxSockets;
            //             unsigned short          iMaxUdpDg;
            //             char FAR *              lpVendorInfo;
            //             char                    szDescription[WSADESCRIPTION_LEN+1];
            //             char                    szSystemStatus[WSASYS_STATUS_LEN+1];
            //     #else
            //             char                    szDescription[WSADESCRIPTION_LEN+1];
            //             char                    szSystemStatus[WSASYS_STATUS_LEN+1];
            //             unsigned short          iMaxSockets;
            //             unsigned short          iMaxUdpDg;
            //             char FAR *              lpVendorInfo;
            //     #endif
            //     } WSADATA, FAR * LPWSADATA;
            //
            // Important to notice is that its layout / order of fields differs between
            // 32-bit and 64-bit systems.  However, we don't actually need any of the
            // data it contains; it suffices to ensure that this struct is large enough
            // to hold either layout, which is 400 bytes on 32-bit and 408 bytes on 64-bit.
            // Thus, we don't declare any fields here, and simply make the size 408 bytes.
        }
    }
}
