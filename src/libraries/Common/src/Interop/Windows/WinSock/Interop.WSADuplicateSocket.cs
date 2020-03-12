// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct WSAPROTOCOLCHAIN
        {
            private const int MAX_PROTOCOL_CHAIN = 7;

            internal int ChainLen;
            internal fixed uint ChainEntries[MAX_PROTOCOL_CHAIN];
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal unsafe struct WSAPROTOCOL_INFOW
        {
            private const int WSAPROTOCOL_LEN = 255;

            internal uint dwServiceFlags1;
            internal uint dwServiceFlags2;
            internal uint dwServiceFlags3;
            internal uint dwServiceFlags4;
            internal uint dwProviderFlags;
            internal Guid ProviderId;
            internal uint dwCatalogEntryId;
            internal WSAPROTOCOLCHAIN ProtocolChain;
            internal int iVersion;
            internal AddressFamily iAddressFamily;
            internal int iMaxSockAddr;
            internal int iMinSockAddr;
            internal SocketType iSocketType;
            internal ProtocolType iProtocol;
            internal int iProtocolMaxOffset;
            internal int iNetworkByteOrder;
            internal int iSecurityScheme;
            internal uint dwMessageSize;
            internal uint dwProviderReserved;
            internal fixed char szProtocol[WSAPROTOCOL_LEN + 1];
        }

        [DllImport(Interop.Libraries.Ws2_32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe int WSADuplicateSocket(
            [In] SafeSocketHandle s,
            [In] uint dwProcessId,
            [In] WSAPROTOCOL_INFOW* lpProtocolInfo
        );
    }
}
