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
        internal struct WSAProtocolChain
        {
            internal int ChainLen;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=7)]
            internal uint[] ChainEntries;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        internal struct WSAProtocolInfo
        {
            internal uint ServiceFlags1;
            internal uint ServiceFlags2;
            internal uint ServiceFlags3;
            internal uint ServiceFlags4;
            internal uint ProviderFlags;
            internal Guid ProviderId;
            internal uint CatalogEntryId;
            internal WSAProtocolChain ProtocolChain;
            internal int Version;
            internal AddressFamily AddressFamily;
            internal int MaxSockAddr;
            internal int MinSockAddr;
            internal SocketType SocketType;
            internal ProtocolType ProtocolType;
            internal int ProtocolMaxOffset;
            internal int NetworkByteOrder;
            internal int SecurityScheme;
            internal uint MessageSize;
            internal uint ProviderReserved;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            internal string ProtocolName;

            public static readonly int Size = Marshal.SizeOf(typeof(WSAProtocolInfo));
        }

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern unsafe int WSADuplicateSocket(
            [In] SafeSocketHandle socketHandle,
            [In] uint targetProcessId,
            [In] byte* lpProtocolInfo
        );
    }
}
