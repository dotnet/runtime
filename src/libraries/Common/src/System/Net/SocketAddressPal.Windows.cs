// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    internal static class SocketAddressPal
    {
        public const int IPv6AddressSize = 28;
        public const int IPv4AddressSize = 16;
        public const int DataOffset = 2;

        public static unsafe AddressFamily GetAddressFamily(byte[] buffer)
        {
            return (AddressFamily)BitConverter.ToInt16(buffer, 0);
        }

        public static unsafe void SetAddressFamily(byte[] buffer, AddressFamily family)
        {
#if BIGENDIAN
            buffer[0] = unchecked((byte)((int)family >> 8));
            buffer[1] = unchecked((byte)((int)family));
#else
            buffer[0] = unchecked((byte)((int)family));
            buffer[1] = unchecked((byte)((int)family >> 8));
#endif
        }

        public static unsafe ushort GetPort(byte[] buffer)
        {
            return buffer.NetworkBytesToHostUInt16(2);
        }

        public static unsafe void SetPort(byte[] buffer, ushort port)
        {
            port.HostToNetworkBytes(buffer, 2);
        }

        public static unsafe uint GetIPv4Address(byte[] buffer)
        {
            return (uint)((buffer[4] & 0x000000FF) |
                (buffer[5] << 8 & 0x0000FF00) |
                (buffer[6] << 16 & 0x00FF0000) |
                (buffer[7] << 24));
        }

        public static unsafe void GetIPv6Address(byte[] buffer, byte[] address, out uint scope)
        {
            for (int i = 0; i < address.Length; i++)
            {
                address[i] = buffer[8 + i];
            }

            scope = (uint)((buffer[27] << 24) +
                (buffer[26] << 16) +
                (buffer[25] << 8) +
                (buffer[24]));
        }

        public static unsafe void SetIPv4Address(byte[] buffer, uint address)
        {
            // IPv4 Address serialization
            buffer[4] = unchecked((byte)(address));
            buffer[5] = unchecked((byte)(address >> 8));
            buffer[6] = unchecked((byte)(address >> 16));
            buffer[7] = unchecked((byte)(address >> 24));
        }

        public static unsafe void SetIPv6Address(byte[] buffer, byte[] address, uint scope)
        {
            // No handling for Flow Information
            buffer[4] = (byte)0;
            buffer[5] = (byte)0;
            buffer[6] = (byte)0;
            buffer[7] = (byte)0;

            // Scope serialization
            buffer[24] = (byte)scope;
            buffer[25] = (byte)(scope >> 8);
            buffer[26] = (byte)(scope >> 16);
            buffer[27] = (byte)(scope >> 24);

            // Address serialization
            for (int i = 0; i < address.Length; i++)
            {
                buffer[8 + i] = address[i];
            }
        }
    }
}
