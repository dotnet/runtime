// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

using Xunit;

namespace System.Net.Primitives.PalTests
{
    public class SocketAddressPalTests
    {
        public static object[][] AddressFamilyData = new object[][] {
            new object[] { AddressFamily.Unspecified },
            new object[] { AddressFamily.Unix },
            new object[] { AddressFamily.InterNetwork },
            new object[] { AddressFamily.InterNetworkV6 }
        };

        [Theory, MemberData(nameof(AddressFamilyData))]
        public void AddressFamily_Get_Set(AddressFamily family)
        {
            var buffer = new byte[16];
            SocketAddressPal.SetAddressFamily(buffer, family);
            Assert.Equal(family, SocketAddressPal.GetAddressFamily(buffer));
        }

        [Fact]
        public void AddressFamily_Get_Set_Throws()
        {
            var buffer = new byte[1];
            Assert.ThrowsAny<Exception>(() => SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetwork));
            Assert.ThrowsAny<Exception>(() => SocketAddressPal.GetAddressFamily(buffer));
        }

        public static object[][] PortData = new object[][] {
            new object[] { (ushort)0 },
            new object[] { (ushort)42 },
            new object[] { (ushort)1024 },
            new object[] { (ushort)65535 }
        };

        [Theory, MemberData(nameof(PortData))]
        public void Port_Get_Set_IPv4(ushort port)
        {
            var buffer = new byte[SocketAddressPal.IPv4AddressSize];
            SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetwork);
            SocketAddressPal.SetPort(buffer, port);
            Assert.Equal(port, SocketAddressPal.GetPort(buffer));
        }

        [Theory, MemberData(nameof(PortData))]
        public void Port_Get_Set_IPv6(ushort port)
        {
            var buffer = new byte[SocketAddressPal.IPv6AddressSize];
            SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetworkV6);
            SocketAddressPal.SetPort(buffer, port);
            Assert.Equal(port, SocketAddressPal.GetPort(buffer));
        }

        [Fact]
        public void Port_Get_Set_Throws_IPv4()
        {
            var buffer = new byte[2];
            SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetwork);
            Assert.ThrowsAny<Exception>(() => SocketAddressPal.SetPort(buffer, 0));
            Assert.ThrowsAny<Exception>(() => SocketAddressPal.GetPort(buffer));
        }

        [Fact]
        public void Port_Get_Set_Throws_IPv6()
        {
            var buffer = new byte[2];
            SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetworkV6);
            Assert.ThrowsAny<Exception>(() => SocketAddressPal.SetPort(buffer, 0));
            Assert.ThrowsAny<Exception>(() => SocketAddressPal.GetPort(buffer));
        }

        public static object[][] IPv4AddressData = new object[][] {
            new object[] { 0x00000000 },
            new object[] { 0x04030201 },
            new object[] { 0x0100007F },
            new object[] { 0xFFFFFFFF }
        };

        [Theory, MemberData(nameof(IPv4AddressData))]
        public void IPv4Address_Get_Set(uint address)
        {
            var buffer = new byte[SocketAddressPal.IPv4AddressSize];
            SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetwork);
            SocketAddressPal.SetIPv4Address(buffer, address);
            Assert.Equal(address, SocketAddressPal.GetIPv4Address(buffer));
        }

        [Fact]
        public void IPv4Address_Get_Set_Throws()
        {
            var buffer = new byte[4];
            SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetwork);
            Assert.ThrowsAny<Exception>(() => SocketAddressPal.SetIPv4Address(buffer, 0));
            Assert.ThrowsAny<Exception>(() => SocketAddressPal.GetIPv4Address(buffer));
        }

        public static object[][] IPv6AddressData = new object[][] {
            new object[] {
                new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
                (uint)0
            },
            new object[] {
                new byte[] { 0x20, 0x01, 0x0d, 0x0b8, 0xaa, 0xaa, 0xbb, 0xbb, 0xcc, 0xcc, 0xdd, 0xdd, 0xee, 0xee, 0x00, 0x01 },
                (uint)0xffeeddcc
            },
            new object[] {
                new byte[] { 0x20, 0x01, 0x0d, 0x0b8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 },
                (uint)0xccddeeff
            },
            new object[] {
                new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 222, 1, 41, 90 },
                (uint)0
            }
        };

        [Theory, MemberData(nameof(IPv6AddressData))]
        public void IPv6Address_Get_Set(byte[] address, uint scope)
        {
            var buffer = new byte[SocketAddressPal.IPv6AddressSize];
            SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetworkV6);
            SocketAddressPal.SetIPv6Address(buffer, address, scope);

            var actualAddress = new byte[address.Length];
            uint actualScope;
            SocketAddressPal.GetIPv6Address(buffer, actualAddress, out actualScope);

            for (var i = 0; i < actualAddress.Length; i++)
            {
                Assert.Equal(address[i], actualAddress[i]);
            }

            Assert.Equal(scope, actualScope);
        }

        [Fact]
        public void IPv6Address_Get_Set_Throws()
        {
            uint unused;
            var buffer = new byte[4];
            SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetworkV6);
            Assert.ThrowsAny<Exception>(() => SocketAddressPal.SetIPv6Address(buffer, new byte[16], 0));
            Assert.ThrowsAny<Exception>(() => SocketAddressPal.GetIPv6Address(buffer, new byte[16], out unused));
        }

        public static IEnumerable<object[]> IPv4AddressAndPortData = IPv4AddressData.SelectMany(o => PortData.Select(p => o.Concat(p))).Select(o => o.ToArray());

        [Theory, MemberData(nameof(IPv4AddressAndPortData))]
        public void IPv4AddressAndPort_Get_Set(uint address, ushort port)
        {
            var buffer = new byte[SocketAddressPal.IPv4AddressSize];
            SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetwork);
            SocketAddressPal.SetIPv4Address(buffer, address);
            SocketAddressPal.SetPort(buffer, port);

            Assert.Equal(address, SocketAddressPal.GetIPv4Address(buffer));
            Assert.Equal(port, SocketAddressPal.GetPort(buffer));
        }

        public static IEnumerable<object[]> IPv6AddressAndPortData = IPv6AddressData.SelectMany(o => PortData.Select(p => o.Concat(p))).Select(o => o.ToArray());

        [Theory, MemberData(nameof(IPv6AddressAndPortData))]
        public void IPv6AddressAndPort_Get_Set(byte[] address, uint scope, ushort port)
        {
            var buffer = new byte[SocketAddressPal.IPv6AddressSize];
            SocketAddressPal.SetAddressFamily(buffer, AddressFamily.InterNetworkV6);
            SocketAddressPal.SetIPv6Address(buffer, address, scope);
            SocketAddressPal.SetPort(buffer, port);

            var actualAddress = new byte[address.Length];
            uint actualScope;
            SocketAddressPal.GetIPv6Address(buffer, actualAddress, out actualScope);

            for (var i = 0; i < actualAddress.Length; i++)
            {
                Assert.Equal(address[i], actualAddress[i]);
            }

            Assert.Equal(scope, actualScope);
            Assert.Equal(port, SocketAddressPal.GetPort(buffer));
        }
    }
}
