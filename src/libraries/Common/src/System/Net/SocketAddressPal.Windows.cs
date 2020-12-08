// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Net.Sockets;

namespace System.Net
{
    internal static class SocketAddressPal
    {
        public const int IPv6AddressSize = 28;
        public const int IPv4AddressSize = 16;

        public static AddressFamily GetAddressFamily(ReadOnlySpan<byte> buffer)
        {
            return (AddressFamily)BitConverter.ToInt16(buffer);
        }

        public static void SetAddressFamily(byte[] buffer, AddressFamily family)
        {
            if ((int)(family) > ushort.MaxValue)
            {
                // For legacy values family maps directly to Winsock value.
                // Other values will need mapping if/when supported.
                // Currently, that is Netlink, Packet and ControllerAreaNetwork, neither of them supported on Windows.
                throw new PlatformNotSupportedException();
            }

#if BIGENDIAN
            BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)family);
#else
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)family);
#endif
        }

        public static ushort GetPort(ReadOnlySpan<byte> buffer)
            => BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2));

        public static void SetPort(byte[] buffer, ushort port)
            => BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), port);

        public static uint GetIPv4Address(ReadOnlySpan<byte> buffer)
            => BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(4));

        public static void GetIPv6Address(ReadOnlySpan<byte> buffer, Span<byte> address, out uint scope)
        {
            buffer.Slice(8, address.Length).CopyTo(address);

            scope = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(24));
        }

        public static void SetIPv4Address(byte[] buffer, uint address)
        {
            // IPv4 Address serialization
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(4), address);
        }

        public static void SetIPv6Address(byte[] buffer, Span<byte> address, uint scope)
        {
            // No handling for Flow Information
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(4), 0);

            // Scope serialization
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(24), scope);

            // Address serialization
            address.CopyTo(buffer.AsSpan(8));
        }
    }
}
