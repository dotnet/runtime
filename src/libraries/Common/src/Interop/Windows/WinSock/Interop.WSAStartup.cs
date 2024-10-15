// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        private static int s_initialized;

        internal static void EnsureInitialized()
        {
            // No volatile needed here. Reading stale information is just going to cause a harmless extra startup.
            if (s_initialized == 0)
                Initialize();

            static unsafe void Initialize()
            {
                WSAData d;
                SocketError errorCode = WSAStartup(0x0202 /* 2.2 */, &d);

                if (errorCode != SocketError.Success)
                {
                    // WSAStartup does not set LastWin32Error
                    throw new SocketException((int)errorCode);
                }

                if (Interlocked.CompareExchange(ref s_initialized, 1, 0) != 0)
                {
                    // Keep the winsock initialization count balanced if other thread beats us to finish the initialization.
                    // This cleanup is just for good hygiene. A few extra startups would not matter.
                    errorCode = WSACleanup();
                    Debug.Assert(errorCode == SocketError.Success);
                }
            }
        }

        [LibraryImport(Libraries.Ws2_32)]
        private static unsafe partial SocketError WSAStartup(short wVersionRequested, WSAData* lpWSAData);

        [LibraryImport(Libraries.Ws2_32)]
        private static partial SocketError WSACleanup();

        [StructLayout(LayoutKind.Sequential, Size = 408)]
        private struct WSAData
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
