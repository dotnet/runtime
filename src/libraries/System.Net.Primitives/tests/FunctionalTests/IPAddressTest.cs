// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class IPAddressTest
    {
        private static byte[] ipV6AddressBytes1 = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        private static byte[] ipV6AddressBytes2 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };

        private static IPAddress IPV4Address()
        {
            return IPAddress.Parse("192.168.0.9");
        }

        private static IPAddress IPV6Address1()
        {
            return new IPAddress(ipV6AddressBytes1);
        }

        private static IPAddress IPV6Address2()
        {
            return new IPAddress(ipV6AddressBytes2);
        }

        [Fact]
        public static void Ctor_Long_Success()
        {
            IPAddress ip = new IPAddress(0x2414188f);
            Assert.Equal(BitConverter.GetBytes(0x2414188f), ip.GetAddressBytes());
        }

        [Fact]
        public static void Ctor_Long_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IPAddress((long)0x0 - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IPAddress((long)0x00000000FFFFFFFF + 1));
        }

        [Fact]
        public static void Ctor_Bytes_Success()
        {
            IPAddress ip = new IPAddress(ipV6AddressBytes1);
            Assert.Equal(ipV6AddressBytes1, ip.GetAddressBytes());
        }
        
        [Fact]
        public static void Ctor_Bytes_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new IPAddress(null));
            Assert.Throws<ArgumentException>(() => new IPAddress(new byte[] { 0x01, 0x01, 0x02 }));
        }

        [Fact]
        public static void Ctor_BytesScopeId_Success()
        {
            IPAddress ip = new IPAddress(ipV6AddressBytes1, 500);
            Assert.Equal(ipV6AddressBytes1, ip.GetAddressBytes());
            Assert.Equal(500, ip.ScopeId);
        }

        [Fact]
        public static void Ctor_BytesScopeId_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new IPAddress(null, 500));

            Assert.Throws<ArgumentException>(() => new IPAddress(new byte[] { 0x01, 0x01, 0x02 }, 500));
            
            Assert.Throws<ArgumentOutOfRangeException>(() => new IPAddress(ipV6AddressBytes1, (long)0x0 - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IPAddress(ipV6AddressBytes1, (long)0x00000000FFFFFFFF + 1));
        }

        [Theory]
        [InlineData("192.168.0.1", "192.168.0.1")] //IpV4

        [InlineData("Fe08::1", "fe08::1")] //IpV6...
        [InlineData("[Fe08::1]", "fe08::1")]
        [InlineData("[Fe08::1]:80", "fe08::1")] //Drop port
        [InlineData("[Fe08::1]:0xFA", "fe08::1")] //Drop hex port
        [InlineData("Fe08::1%13542", "fe08::1%13542")] //With scope id
        public static void Parse_String_Success(string ipString, string expected)
        {
            Assert.Equal(expected, IPAddress.Parse(ipString).ToString());
        }

        [Theory]
        [InlineData("")] //Empty string

        [InlineData("192.168.0.0/16")] //IpV4: Invalid format
        [InlineData("192.168.0.0:80")] //IpV4: Port included

        [InlineData("Fe08::1]")] //IpV6: No leading bracket
        [InlineData("[Fe08::1")] //IpV6: No trailing bracket
        [InlineData("[Fe08::1]:80Z")] //IpV6: Invalid port
        [InlineData("Fe08::/64")] //IpV6: Subnet fail
        public static void Parse_String_Invalid(string ipString)
        {
            Assert.Throws<FormatException>(() => { IPAddress.Parse(ipString); });
            IPAddress ip;
            Assert.False(IPAddress.TryParse(ipString, out ip));
        }

        [Fact]
        public static void Parse_String_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => { IPAddress.Parse(null); });
            IPAddress ip;
            Assert.False(IPAddress.TryParse(null, out ip));
        }
        
        [Fact]
        public static void ScopeId_GetSet_Success()
        {
            IPAddress ip = IPV6Address1();
            Assert.Equal(0, ip.ScopeId);

            ip.ScopeId = 700;
            Assert.Equal(ip.ScopeId, 700);

            ip.ScopeId = 700;
        }

        [Fact]
        public static void ScopeId_Set_Invalid()
        {
            IPAddress ip = IPV4Address(); //IpV4
            Assert.ThrowsAny<Exception>(() => ip.ScopeId = 500);
            Assert.ThrowsAny<Exception>(() => ip.ScopeId);

            ip = IPV6Address1(); //IpV6
            Assert.Throws<ArgumentOutOfRangeException>(() => ip.ScopeId = (long)0x0 - 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => ip.ScopeId = (long)0x00000000FFFFFFFF + 1);
        }

        [Fact]
        public static void HostToNetworkOrder_Compare_Equal()
        {
            long l1 = (long)0x1350;
            long l2 = (long)0x5013000000000000;

            int i1 = (int)0x1350;
            int i2 = (int)0x50130000;

            short s1 = (short)0x1350;
            short s2 = (short) 0x5013;

            Assert.Equal(l2, IPAddress.HostToNetworkOrder(l1));
            Assert.Equal(i2, IPAddress.HostToNetworkOrder(i1));
            Assert.Equal(s2, IPAddress.HostToNetworkOrder(s1));

            Assert.Equal(l1, IPAddress.NetworkToHostOrder(l2));
            Assert.Equal(i1, IPAddress.NetworkToHostOrder(i2));
            Assert.Equal(s1, IPAddress.NetworkToHostOrder(s2));
        }

        [Fact]
        public static void IsLoopback_Get_Success()
        {
            IPAddress ip = IPV4Address(); //IpV4
            Assert.False(IPAddress.IsLoopback(ip));

            ip = new IPAddress(IPAddress.Loopback.GetAddressBytes()); //IpV4 loopback
            Assert.True(IPAddress.IsLoopback(ip));

            ip = IPV6Address1(); //IpV6
            Assert.False(IPAddress.IsLoopback(ip));

            ip = new IPAddress(IPAddress.IPv6Loopback.GetAddressBytes()); //IpV6 loopback
            Assert.True(IPAddress.IsLoopback(ip));
        }

        [Fact]
        public static void IsLooback_Get_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => IPAddress.IsLoopback(null));
        }

        [Fact]
        public static void IsIPV6Multicast_Get_Success()
        {
            Assert.True(IPAddress.Parse("ff02::1").IsIPv6Multicast);
            Assert.False(IPAddress.Parse("Fe08::1").IsIPv6Multicast);
            Assert.False(IPV4Address().IsIPv6Multicast);
        }

        [Fact]
        public static void IsIPV6LinkLocal_Get_Success()
        {
            Assert.True(IPAddress.Parse("fe80::1").IsIPv6LinkLocal);
            Assert.False(IPAddress.Parse("Fe08::1").IsIPv6LinkLocal);
            Assert.False(IPV4Address().IsIPv6LinkLocal);
        }

        [Fact]
        public static void IsIPV6SiteLocal_Get_Success()
        {
            Assert.True(IPAddress.Parse("FEC0::1").IsIPv6SiteLocal);
            Assert.False(IPAddress.Parse("Fe08::1").IsIPv6SiteLocal);
            Assert.False(IPV4Address().IsIPv6SiteLocal);
        }

        [Fact]
        public static void IsIPV6Teredo_Get_Success()
        {
            Assert.True(IPAddress.Parse("2001::1").IsIPv6Teredo);
            Assert.False(IPAddress.Parse("Fe08::1").IsIPv6Teredo);
            Assert.False(IPV4Address().IsIPv6Teredo);
        }
        
        [Fact]
        public static void Equals_Compare_Success()
        {
            IPAddress ip1 = IPAddress.Parse("192.168.0.9"); //IpV4
            IPAddress ip2 = IPAddress.Parse("192.168.0.9"); //IpV4
            IPAddress ip3 = IPAddress.Parse("169.192.1.10"); //IpV4

            IPAddress ip4 = new IPAddress(ipV6AddressBytes1); //IpV6
            IPAddress ip5 = new IPAddress(ipV6AddressBytes1); //IpV6
            IPAddress ip6 = new IPAddress(ipV6AddressBytes2); //IpV6

            Assert.True(ip1.Equals(ip2));
            Assert.True(ip2.Equals(ip1));

            Assert.True(ip1.GetHashCode().Equals(ip2.GetHashCode()));
            Assert.False(ip1.GetHashCode().Equals(ip3.GetHashCode()));

            Assert.False(ip1.Equals(ip3));

            Assert.False(ip1.Equals(ip4)); //IpV4 /= IpV6
            Assert.False(ip1.Equals(null));
            Assert.False(ip1.Equals(""));

            Assert.True(ip4.Equals(ip5));
            Assert.False(ip4.Equals(ip6));

            Assert.True(ip4.GetHashCode().Equals(ip5.GetHashCode()));
            Assert.False(ip4.GetHashCode().Equals(ip6.GetHashCode()));
        }
    }
}
