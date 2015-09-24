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
        public const int IPv6AddressSize = Interop.libc.sockaddr_in6.Size;
        public const int IPv4AddressSize = Interop.libc.sockaddr_in.Size;
        public const int DataOffset = 0;

        public static unsafe AddressFamily GetAddressFamily(byte[] buffer)
        {
            fixed (byte* rawAddress = buffer)
            {
                var sockaddr = (Interop.libc.sockaddr*)rawAddress;
                switch (Interop.CheckedRead((byte*)sockaddr, buffer.Length, &sockaddr->sa_family))
                {
                    case Interop.libc.AF_UNSPEC:
                        return AddressFamily.Unspecified;

                    case Interop.libc.AF_UNIX:
                        return AddressFamily.Unix;

                    case Interop.libc.AF_INET:
                        return AddressFamily.InterNetwork;

                    case Interop.libc.AF_INET6:
                        return AddressFamily.InterNetworkV6;

                    default:
                        throw new PlatformNotSupportedException();
                }
            }
        }

        public static unsafe void SetAddressFamily(byte[] buffer, AddressFamily family)
        {
            fixed (byte* rawAddress = buffer)
            {
                var sockaddr = (Interop.libc.sockaddr*)rawAddress;
                Interop.CheckBounds((byte*)sockaddr, buffer.Length, &sockaddr->sa_family);

                switch (family)
                {
                    case AddressFamily.Unspecified:
                        sockaddr->sa_family = Interop.libc.AF_UNSPEC;
                        break;

                    case AddressFamily.Unix:
                        sockaddr->sa_family = Interop.libc.AF_UNIX;
                        break;

                    case AddressFamily.InterNetwork:
                        sockaddr->sa_family = Interop.libc.AF_INET;
                        break;

                    case AddressFamily.InterNetworkV6:
                        sockaddr->sa_family = Interop.libc.AF_INET6;
                        break;

                    default:
                        Debug.Fail("Unsupported addres family");
                        throw new PlatformNotSupportedException();
                }
            }
        }

        public static unsafe ushort GetPort(byte[] buffer)
        {
            ushort port;

            fixed (byte* rawAddress = buffer)
            {
                var sockaddr = (Interop.libc.sockaddr*)rawAddress;
                switch (sockaddr->sa_family)
                {
                    case Interop.libc.AF_INET:
                        port = Interop.CheckedRead((byte*)sockaddr, buffer.Length, &((Interop.libc.sockaddr_in*)sockaddr)->sin_port);
                        break;

                    case Interop.libc.AF_INET6:
                        port = Interop.CheckedRead((byte*)sockaddr, buffer.Length, &((Interop.libc.sockaddr_in6*)sockaddr)->sin6_port);
                        break;

                    default:
                        Debug.Fail("Unsupported address family");
                        throw new PlatformNotSupportedException();
                }
            }

            return port.NetworkToHost();
        }

        public static unsafe void SetPort(byte[] buffer, ushort port)
        {
            port = port.HostToNetwork();

            fixed (byte* rawAddress = buffer)
            {
                var sockaddr = (Interop.libc.sockaddr*)rawAddress;
                switch (sockaddr->sa_family)
                {
                    case Interop.libc.AF_INET:
                        Interop.CheckedWrite((byte*)sockaddr, buffer.Length, &((Interop.libc.sockaddr_in*)sockaddr)->sin_port, port);
                        break;

                    case Interop.libc.AF_INET6:
                        Interop.CheckedWrite((byte*)sockaddr, buffer.Length, &((Interop.libc.sockaddr_in6*)sockaddr)->sin6_port, port);
                        break;

                    default:
                        Debug.Fail("Unsupported address family");
                        throw new PlatformNotSupportedException();
                }
            }
        }

        public static unsafe uint GetIPv4Address(byte[] buffer)
        {
            fixed (byte* rawAddress = buffer)
            {
                var sockaddr = (Interop.libc.sockaddr_in*)rawAddress;
                Debug.Assert(sockaddr->sin_family == Interop.libc.AF_INET);

                return Interop.CheckedRead((byte*)sockaddr, buffer.Length, &sockaddr->sin_addr.s_addr);
            }
        }

        public static unsafe void GetIPv6Address(byte[] buffer, byte[] address, out uint scope)
        {
            Debug.Assert(address.Length == sizeof(Interop.libc.in6_addr));

            fixed (byte* rawAddress = buffer)
            {
                var sockaddr = (Interop.libc.sockaddr_in6*)rawAddress;
                Debug.Assert(sockaddr->sin6_family == Interop.libc.AF_INET6);

                Interop.CheckBounds((byte*)sockaddr, buffer.Length, (byte*)&sockaddr->sin6_addr.s6_addr[0], sizeof(Interop.libc.in6_addr));
                for (int i = 0; i < sizeof(Interop.libc.in6_addr); i++)
                {
                    address[i] = sockaddr->sin6_addr.s6_addr[i];
                }

                scope = Interop.CheckedRead((byte*)sockaddr, buffer.Length, &sockaddr->sin6_scope_id);
            }
        }

        public static unsafe void SetIPv4Address(byte[] buffer, uint address)
        {
            fixed (byte* rawAddress = buffer)
            {
                var sockaddr = (Interop.libc.sockaddr_in*)rawAddress;
                Debug.Assert(sockaddr->sin_family == Interop.libc.AF_INET);

                Interop.CheckedWrite((byte*)sockaddr, buffer.Length, &sockaddr->sin_addr.s_addr, address);
            }
        }

        public static unsafe void SetIPv6Address(byte[] buffer, byte[] address, uint scope)
        {
            Debug.Assert(address.Length == sizeof(Interop.libc.in6_addr));

            fixed (byte* rawAddress = buffer)
            {
                var sockaddr = (Interop.libc.sockaddr_in6*)rawAddress;
                Debug.Assert(sockaddr->sin6_family == Interop.libc.AF_INET6);

                Interop.CheckedWrite((byte*)sockaddr, buffer.Length, &sockaddr->sin6_flowinfo, 0);

                Interop.CheckBounds((byte*)sockaddr, buffer.Length, (byte*)&sockaddr->sin6_addr.s6_addr[0], sizeof(Interop.libc.in6_addr));
                for (int i = 0; i < sizeof(Interop.libc.in6_addr); i++)
                {
                    sockaddr->sin6_addr.s6_addr[i] = address[i];
                }

                Interop.CheckedWrite((byte*)sockaddr, buffer.Length, &sockaddr->sin6_scope_id, scope);
            }
        }
    }
}
