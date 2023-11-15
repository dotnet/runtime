// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        public const int SO_PROTOCOL_INFOW = 0x2005;

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

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct WSAPROTOCOLCHAIN
        {
            private const int MAX_PROTOCOL_CHAIN = 7;

            internal int ChainLen;
            internal fixed uint ChainEntries[MAX_PROTOCOL_CHAIN];
        }
    }
}
