// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct ControlData
        {
            internal UIntPtr length;
            internal uint level;
            internal uint type;
            internal uint address;
            internal uint index;
        }

        internal const int IPv6AddressLength = 16;

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct ControlDataIPv6
        {
            internal UIntPtr length;
            internal uint level;
            internal uint type;
            internal fixed byte address[IPv6AddressLength];
            internal uint index;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WSAPROTOCOLCHAIN
        {
            // The length of the chain.
            internal int ChainLen;

            // A list of catalog entry IDs.
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
            internal uint[] ChainEntries;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WSAPROTOCOL_INFO
        {
            internal uint dwServiceFlags1;
            internal uint dwServiceFlags2;
            internal uint dwServiceFlags3;
            internal uint dwServiceFlags4;
            internal uint dwProviderFlags;
            private Guid _providerId;
            internal uint dwCatalogEntryId;
            private WSAPROTOCOLCHAIN _protocolChain;
            internal int iVersion;
            internal AddressFamily iAddressFamily;
            internal int iMaxSockAddr;
            internal int iMinSockAddr;
            internal int iSocketType;
            internal int iProtocol;
            internal int iProtocolMaxOffset;
            internal int iNetworkByteOrder;
            internal int iSecurityScheme;
            internal uint dwMessageSize;
            internal uint dwProviderReserved;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            internal string szProtocol;
        }

        // Flags equivalent to winsock TRANSMIT_PACKETS_ELEMENT flags
        //    #define TP_ELEMENT_MEMORY   1
        //    #define TP_ELEMENT_FILE     2
        //    #define TP_ELEMENT_EOP      4
        [Flags]
        internal enum TransmitPacketsElementFlags : uint
        {
            None = 0x00,
            Memory = 0x01,
            File = 0x02,
            EndOfPacket = 0x04
        }

        // Structure equivalent to TRANSMIT_PACKETS_ELEMENT
        //
        // typedef struct _TRANSMIT_PACKETS_ELEMENT {
        //     ULONG dwElFlags;  
        //     ULONG cLength;  
        //     union {    
        //         struct {      
        //             LARGE_INTEGER nFileOffset;      
        //             HANDLE hFile;
        //         };    
        //         PVOID pBuffer;  
        //     }
        //  };
        // } TRANSMIT_PACKETS_ELEMENT;
        [StructLayout(LayoutKind.Explicit)]
        internal struct TransmitPacketsElement
        {
            [System.Runtime.InteropServices.FieldOffset(0)]
            internal TransmitPacketsElementFlags flags;
            [System.Runtime.InteropServices.FieldOffset(4)]
            internal uint length;
            [System.Runtime.InteropServices.FieldOffset(8)]
            internal Int64 fileOffset;
            [System.Runtime.InteropServices.FieldOffset(8)]
            internal IntPtr buffer;
            [System.Runtime.InteropServices.FieldOffset(16)]
            internal IntPtr fileHandle;
        }

        // WinSock 2 extension -- bit values and indices for FD_XXX network events
        [Flags]
        internal enum AsyncEventBits
        {
            FdNone = 0,
            FdRead = 1 << 0,
            FdWrite = 1 << 1,
            FdOob = 1 << 2,
            FdAccept = 1 << 3,
            FdConnect = 1 << 4,
            FdClose = 1 << 5,
            FdQos = 1 << 6,
            FdGroupQos = 1 << 7,
            FdRoutingInterfaceChange = 1 << 8,
            FdAddressListChange = 1 << 9,
            FdAllEvents = (1 << 10) - 1,
        }

        // Array position in NetworkEvents (WSAEnumNetworkEvents).
        internal enum AsyncEventBitsPos
        {
            FdReadBit = 0,
            FdWriteBit = 1,
            FdOobBit = 2,
            FdAcceptBit = 3,
            FdConnectBit = 4,
            FdCloseBit = 5,
            FdQosBit = 6,
            FdGroupQosBit = 7,
            FdRoutingInterfaceChangeBit = 8,
            FdAddressListChangeBit = 9,
            FdMaxEvents = 10,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NetworkEvents
        {
            // Indicates which of the FD_XXX network events have occurred.
            public AsyncEventBits Events;

            // An array that contains any associated error codes,
            // with an array index that corresponds to the position of event bits in lNetworkEvents.
            // The identifiers FD_READ_BIT, FD_WRITE_BIT and other can be used to index the iErrorCode array.
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AsyncEventBitsPos.FdMaxEvents)]
            public int[] ErrorCodes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WSAMsg
        {
            internal IntPtr socketAddress;
            internal uint addressLength;
            internal IntPtr buffers;
            internal uint count;
            internal WSABuffer controlBuffer;
            internal SocketFlags flags;
        }
    }
}
