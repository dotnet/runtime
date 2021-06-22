// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Runtime.InteropServices;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicAddressHelpers
    {
        internal static unsafe IPEndPoint INetToIPEndPoint(ref SOCKADDR_INET inetAddress)
        {
            if (inetAddress.si_family == (ushort)QUIC_ADDRESS_FAMILY.INET)
            {
                return new IPEndPoint(new IPAddress(MemoryMarshal.CreateReadOnlySpan<byte>(ref inetAddress.Ipv4.sin_addr[0], 4)), (ushort)IPAddress.NetworkToHostOrder((short)inetAddress.Ipv4.sin_port));
            }
            else
            {
                return new IPEndPoint(new IPAddress(MemoryMarshal.CreateReadOnlySpan<byte>(ref inetAddress.Ipv6.sin6_addr[0], 16)), (ushort)IPAddress.NetworkToHostOrder((short)inetAddress.Ipv6.sin6_port));
            }
        }

        internal static unsafe SOCKADDR_INET IPEndPointToINet(IPEndPoint endpoint)
        {
            SOCKADDR_INET socketAddress = default;
            if (endpoint.Address != IPAddress.Any && endpoint.Address != IPAddress.IPv6Any)
            {
                switch (endpoint.Address.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        endpoint.Address.TryWriteBytes(MemoryMarshal.CreateSpan<byte>(ref socketAddress.Ipv4.sin_addr[0], 4), out _);
                        socketAddress.Ipv4.sin_family = (ushort)QUIC_ADDRESS_FAMILY.INET;
                        break;
                    case AddressFamily.InterNetworkV6:
                        endpoint.Address.TryWriteBytes(MemoryMarshal.CreateSpan<byte>(ref socketAddress.Ipv6.sin6_addr[0], 16), out _);
                        socketAddress.Ipv6.sin6_family = (ushort)QUIC_ADDRESS_FAMILY.INET6;
                        break;
                    default:
                        throw new ArgumentException(SR.net_quic_addressfamily_notsupported);
                }
            }

            SetPort(endpoint.Address.AddressFamily, ref socketAddress, endpoint.Port);
            return socketAddress;
        }

        private static void SetPort(AddressFamily addressFamily, ref SOCKADDR_INET socketAddrInet, int originalPort)
        {
            ushort convertedPort = (ushort)IPAddress.HostToNetworkOrder((short)originalPort);
            socketAddrInet.Ipv4.sin_port = convertedPort;
        }
    }
}
