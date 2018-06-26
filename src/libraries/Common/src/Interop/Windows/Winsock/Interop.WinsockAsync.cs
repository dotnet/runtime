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
            internal long fileOffset;
            [System.Runtime.InteropServices.FieldOffset(8)]
            internal IntPtr buffer;
            [System.Runtime.InteropServices.FieldOffset(16)]
            internal IntPtr fileHandle;
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
