// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;

namespace System.Net.Sockets
{
    internal static partial class IPEndPointExtensions
    {
        public static IPAddress GetIPAddress(ReadOnlySpan<byte> socketAddressBuffer)
        {
            AddressFamily family = SocketAddressPal.GetAddressFamily(socketAddressBuffer);

            if (family == AddressFamily.InterNetworkV6)
            {
                Span<byte> address = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                uint scope;
                SocketAddressPal.GetIPv6Address(socketAddressBuffer, address, out scope);

                // Clear scope if set for anything but Link Local address (always starts with fe80 first 10bits).
                return new IPAddress(address, (address[0] == 0xFE && (address[1] & 0xC0) == 0x80) ? (long)scope : 0);
            }
            else if (family == AddressFamily.InterNetwork)
            {
                return new IPAddress((long)SocketAddressPal.GetIPv4Address(socketAddressBuffer) & 0x0FFFFFFFF);
            }

            throw new SocketException((int)SocketError.AddressFamilyNotSupported);
        }

        public static void SetIPAddress(Span<byte> socketAddressBuffer, IPAddress address)
        {
            SocketAddressPal.SetAddressFamily(socketAddressBuffer, address.AddressFamily);
            SocketAddressPal.SetPort(socketAddressBuffer, 0);
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
#pragma warning disable CS0618
                SocketAddressPal.SetIPv4Address(socketAddressBuffer, (uint)address.Address);
#pragma warning restore CS0618
            }
            else
            {
                Span<byte> addressBuffer = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                address.TryWriteBytes(addressBuffer, out int written);
                Debug.Assert(written == IPAddressParserStatics.IPv6AddressBytes);
                SocketAddressPal.SetIPv6Address(socketAddressBuffer, addressBuffer, (uint)address.ScopeId);
            }
        }

        public static IPEndPoint CreateIPEndPoint(ReadOnlySpan<byte> socketAddressBuffer)
        {
            return new IPEndPoint(GetIPAddress(socketAddressBuffer), SocketAddressPal.GetPort(socketAddressBuffer));
        }

        // suggestion from https://github.com/dotnet/runtime/issues/78993
        public static void Serialize(this IPEndPoint endPoint, Span<byte> destination)
        {
            SocketAddressPal.SetAddressFamily(destination, endPoint.AddressFamily);
            SetIPAddress(destination, endPoint.Address);
            SocketAddressPal.SetPort(destination, (ushort)endPoint.Port);
        }

        public static bool Equals(this IPEndPoint endPoint, ReadOnlySpan<byte> socketAddressBuffer)
        {
            if (socketAddressBuffer.Length >= SocketAddress.GetMaximumAddressSize(endPoint.AddressFamily) &&
                endPoint.AddressFamily == SocketAddressPal.GetAddressFamily(socketAddressBuffer) &&
                endPoint.Port == (int)SocketAddressPal.GetPort(socketAddressBuffer))
            {
                if (endPoint.AddressFamily == AddressFamily.InterNetwork)
                {
#pragma warning disable CS0618
                    return endPoint.Address.Address == (long)SocketAddressPal.GetIPv4Address(socketAddressBuffer);
#pragma warning restore CS0618
                }
                else
                {
                    Span<byte> addressBuffer1 = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                    Span<byte> addressBuffer2 = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                    SocketAddressPal.GetIPv6Address(socketAddressBuffer, addressBuffer1, out uint scopeid);
                    if (endPoint.Address.ScopeId != (long)scopeid)
                    {
                        return false;
                    }
                    endPoint.Address.TryWriteBytes(addressBuffer2, out _);
                    return addressBuffer1.SequenceEqual(addressBuffer2);
                }
            }

            return false;
        }
    }
}
