// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    [StructLayout(LayoutKind.Explicit)]
    internal partial struct SockAddr
    {
        internal const int IPv4AddressSize = 4;
        internal const int IPv6AddressSize = 16;

        private static ReadOnlySpan<byte> V4MappedPrefix=> new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xff, 0xff};

        [FieldOffset(0)]
        public Interop.Sys.sockaddr_in Ipv4;
        [FieldOffset(0)]
        public Interop.Sys.sockaddr_in6 Ipv6;

        public int Family => Ipv4.sin_family;

        public unsafe SockAddr(IPEndPoint endPoint) : this(endPoint, endPoint.Address.AddressFamily)
        {
        }

        public unsafe SockAddr(IPEndPoint endPoint, AddressFamily addressFamily)
        {
            if (addressFamily == endPoint.Address.AddressFamily)
            {
                if (endPoint.AddressFamily == AddressFamily.InterNetwork)
                {
                    Ipv4.sin_family = Interop.Sys.AF_INET;
                    Ipv4.sin_port = (ushort)IPAddress.HostToNetworkOrder((short)endPoint.Port);
                    Span<byte> address = MemoryMarshal.CreateSpan(ref Ipv4.sin_addr[0], IPv4AddressSize);
                    if (!endPoint.Address.TryWriteBytes(address, out int bytesWritten) || bytesWritten != IPv4AddressSize)
                    {
                        throw new SocketException((int)SocketError.InvalidArgument);
                    }
                }
                else if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    Ipv6.sin6_family = Interop.Sys.AF_INET6;
                    Ipv6.sin6_port = (ushort)IPAddress.HostToNetworkOrder((short)endPoint.Port);
                    Span<byte> address = MemoryMarshal.CreateSpan(ref Ipv6.sin6_addr[0], IPv6AddressSize);
                    if (!endPoint.Address.TryWriteBytes(address, out int bytesWritten) || bytesWritten != IPv6AddressSize)
                    {
                        throw new SocketException((int)SocketError.InvalidArgument);
                    }
                }
                else
                {
                    // Next IP version???
                    throw new SocketException((int)SocketError.AddressFamilyNotSupported);
                }
            }
            else if (endPoint.Address.AddressFamily == AddressFamily.InterNetwork && addressFamily == AddressFamily.InterNetworkV6)
            {
                // Map IPv4 to IPv4. This is currently only supported cases when AddressFamily does not match.
                Ipv6.sin6_family = Interop.Sys.AF_INET6;
                Ipv6.sin6_port = (ushort)IPAddress.HostToNetworkOrder((short)endPoint.Port);
                Span<byte> address = MemoryMarshal.CreateSpan(ref Ipv6.sin6_addr[0], IPv6AddressSize);

                V4MappedPrefix.CopyTo(address);
                if (!endPoint.Address.TryWriteBytes(address.Slice(V4MappedPrefix.Length), out int bytesWritten) || bytesWritten != IPv4AddressSize)
                {
                    throw new SocketException((int)SocketError.InvalidArgument);
                }
            }
            else
            {
                throw new SocketException((int)SocketError.AddressFamilyNotSupported);
            }
        }

        public AddressFamily AddressFamily
        {
            get
            {
                switch (Family)
                {
                    case Interop.Sys.AF_INET: return AddressFamily.InterNetwork;
                    case Interop.Sys.AF_INET6: return AddressFamily.InterNetworkV6;
                    default:
                        throw new SocketException((int)SocketError.AddressFamilyNotSupported);
                }
            }
        }

        public unsafe int Length
        {
            get
            {
                switch (Family)
                {
                    case Interop.Sys.AF_INET: return sizeof(Interop.Sys.sockaddr_in);
                    case Interop.Sys.AF_INET6: return sizeof(Interop.Sys.sockaddr_in6);
                    default:
                        throw new SocketException((int)SocketError.AddressFamilyNotSupported);
                }
            }
        }

        public unsafe ReadOnlySpan<byte> AsBytes()
        {
            return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1)).Slice(0, Length);
        }

        public ushort GetPort()
        {
            if (AddressFamily == AddressFamily.InterNetworkV6)
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)Ipv6.sin6_port);
            }
            else if (AddressFamily == AddressFamily.InterNetwork)
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)Ipv4.sin_port);
            }
            else
            {
                throw new SocketException((int)SocketError.AddressFamilyNotSupported);
            }
        }

        public unsafe IPAddress GetIPAddress()
        {
            if (AddressFamily == AddressFamily.InterNetworkV6)
            {
                return new IPAddress(MemoryMarshal.CreateSpan(ref Ipv6.sin6_addr[0], IPv6AddressSize), Ipv6.sin6_scope_id);
            }
            else if (AddressFamily == AddressFamily.InterNetwork)
            {
                return new IPAddress(MemoryMarshal.CreateSpan(ref Ipv4.sin_addr[0], IPv4AddressSize));
            }
            else
            {
                throw new SocketException((int)SocketError.AddressFamilyNotSupported);
            }
        }

        public IPEndPoint GetIPEndPoint()
        {
            return new IPEndPoint(GetIPAddress(), GetPort());
        }

        public void SetAddressFamily(AddressFamily family) => Ipv4.SetAddressFamily((int)family);
    }
}
