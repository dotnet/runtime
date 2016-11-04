// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Sockets;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class IPAddressTest
    {
        private const long MinAddress = 0;
        private const long MaxAddress = 0xFFFFFFFF;

        private const long MinScopeId = 0;
        private const long MaxScopeId = 0xFFFFFFFF;

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

        [Theory]
        [InlineData(MinAddress, new byte[] { 0, 0, 0, 0 })]
        [InlineData(MaxAddress, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
        [InlineData(0x2414188f, new byte[] { 0x8f, 0x18, 0x14, 0x24 })]
        [InlineData(0xFF, new byte[] { 0xFF, 0, 0, 0 })]
        [InlineData(0xFF00FF, new byte[] { 0xFF, 0, 0xFF, 0 })]
        [InlineData(0xFF00FF00, new byte[] { 0, 0xFF, 0, 0xFF })]
        public static void Ctor_Long_Success(long address, byte[] expectedBytes)
        {
            IPAddress ip = new IPAddress(address);
            Assert.Equal(expectedBytes, ip.GetAddressBytes());
            Assert.Equal(AddressFamily.InterNetwork, ip.AddressFamily);
        }

        [Theory]
        [InlineData(MinAddress - 1)]
        [InlineData(MaxAddress + 1)]
        public static void Ctor_Long_Invalid(long address)
        {
            Assert.Throws<ArgumentOutOfRangeException>("newAddress", () => new IPAddress(address));
        }

        [Theory]
        [MemberData(nameof(AddressBytesAndFamilies))]
        public static void Ctor_Bytes_Success(byte[] address, AddressFamily expectedFamily)
        {
            IPAddress ip = new IPAddress(address);
            Assert.Equal(address, ip.GetAddressBytes());
            Assert.Equal(expectedFamily, ip.AddressFamily);
        }

        public static object[][] AddressBytesAndFamilies =
        {
            new object[] { new byte[] { 0x8f, 0x18, 0x14, 0x24 }, AddressFamily.InterNetwork },
            new object[] { ipV6AddressBytes1, AddressFamily.InterNetworkV6 },
            new object[] { ipV6AddressBytes2, AddressFamily.InterNetworkV6 }
        };

        [Fact]
        public static void Ctor_Bytes_Invalid()
        {
            Assert.Throws<ArgumentNullException>("address", () => new IPAddress(null));
            Assert.Throws<ArgumentException>("address", () => new IPAddress(new byte[] { 0x01, 0x01, 0x02 }));
        }

        [Theory]
        [MemberData(nameof(IPv6AddressBytesAndScopeIds))]
        public static void Ctor_BytesScopeId_Success(byte[] address, long scopeId)
        {
            IPAddress ip = new IPAddress(address, scopeId);
            Assert.Equal(address, ip.GetAddressBytes());
            Assert.Equal(scopeId, ip.ScopeId);
            Assert.Equal(AddressFamily.InterNetworkV6, ip.AddressFamily);
        }

        public static IEnumerable<object[]> IPv6AddressBytesAndScopeIds
        {
            get
            {
                foreach (long scopeId in new long[] { MinScopeId, MaxScopeId, 500 })
                {
                    yield return new object[] { ipV6AddressBytes1, scopeId };
                    yield return new object[] { ipV6AddressBytes2, scopeId };
                }
            }
        }

        [Fact]
        public static void Ctor_BytesScopeId_Invalid()
        {
            Assert.Throws<ArgumentNullException>("address", () => new IPAddress(null, 500));

            Assert.Throws<ArgumentException>("address", () => new IPAddress(new byte[] { 0x01, 0x01, 0x02 }, 500));

            Assert.Throws<ArgumentOutOfRangeException>("scopeid", () => new IPAddress(ipV6AddressBytes1, MinScopeId - 1));
            Assert.Throws<ArgumentOutOfRangeException>("scopeid", () => new IPAddress(ipV6AddressBytes1, MaxScopeId + 1));
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
            Assert.Throws<ArgumentNullException>("ipString", () => { IPAddress.Parse(null); });
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
            Assert.Throws<ArgumentOutOfRangeException>("value", () => ip.ScopeId = MinScopeId - 1);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => ip.ScopeId = MaxScopeId + 1);
        }

        [Fact]
        public static void HostToNetworkOrder_Compare_Equal()
        {
            long l1 = (long)0x1350;
            long l2 = (long)0x5013000000000000;

            int i1 = (int)0x1350;
            int i2 = (int)0x50130000;

            short s1 = (short)0x1350;
            short s2 = (short)0x5013;

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
            Assert.Throws<ArgumentNullException>("address", () => IPAddress.IsLoopback(null));
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

#if NetStandard17
#pragma warning disable 618
         [Fact]
         public static void Address_Property_Failure()
         {
             IPAddress ip1 = IPAddress.Parse("fe80::200:f8ff:fe21:67cf");
             Assert.Throws<SocketException>(() => ip1.Address);
         }
 
         [Fact]
         public static void Address_Property_Success()
         {
             IPAddress ip1 = IPAddress.Parse("192.168.0.9");
             //192.168.0.10
             long newIp4Address = 192 << 24 | 168 << 16 | 0 << 8 | 10;
             ip1.Address = newIp4Address;
             Assert.Equal("10.0.168.192" , ip1.ToString());
         }
#pragma warning restore 618
#endif //NetStandard17
    }
}
