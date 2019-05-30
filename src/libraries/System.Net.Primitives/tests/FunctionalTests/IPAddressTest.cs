// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
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

        private const string IpV4AddressString1 = "192.168.0.9";
        private const string IpV4AddressString2 = "169.192.1.10";
        private const string IpV6AddressString = "fe80::200:f8ff:fe21:67cf";

        private static readonly byte[] IpV4AddressBytes = { 0x01, 0x02, 0x03, 0x04 };
        private static readonly byte[] IpV6AddressBytes1 = { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        private static readonly byte[] IpV6AddressBytes2 = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };

        private static IPAddress IPV4Address1()
        {
            return IPAddress.Parse(IpV4AddressString1);
        }

        private static IPAddress IPV4Address2()
        {
            return IPAddress.Parse(IpV4AddressString2);
        }

        private static IPAddress IPV6Address1()
        {
            return new IPAddress(IpV6AddressBytes1);
        }

        private static IPAddress IPV6Address2()
        {
            return new IPAddress(IpV6AddressBytes2);
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("newAddress", () => new IPAddress(address));
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
            new object[] { IpV6AddressBytes1, AddressFamily.InterNetworkV6 },
            new object[] { IpV6AddressBytes2, AddressFamily.InterNetworkV6 }
        };

        [Fact]
        public static void Ctor_Bytes_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("address", () => new IPAddress(null));
            AssertExtensions.Throws<ArgumentException>("address", () => new IPAddress(new byte[] { 0x01, 0x01, 0x02 }));
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
                    yield return new object[] { IpV6AddressBytes1, scopeId };
                    yield return new object[] { IpV6AddressBytes2, scopeId };
                }
            }
        }

        [Fact]
        public static void Ctor_BytesScopeId_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("address", () => new IPAddress(null, 500));

            AssertExtensions.Throws<ArgumentException>("address", () => new IPAddress(new byte[] { 0x01, 0x01, 0x02 }, 500));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("scopeid", () => new IPAddress(IpV6AddressBytes1, MinScopeId - 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("scopeid", () => new IPAddress(IpV6AddressBytes1, MaxScopeId + 1));
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
            IPAddress ip = IPV4Address1(); //IpV4
            Assert.ThrowsAny<Exception>(() => ip.ScopeId = 500);
            Assert.ThrowsAny<Exception>(() => ip.ScopeId);

            ip = IPV6Address1(); //IpV6
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => ip.ScopeId = MinScopeId - 1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => ip.ScopeId = MaxScopeId + 1);
        }

        [Fact]
        public static void HostToNetworkOrder_Compare_Equal()
        {
            long l1 = (long)0x1350;
            long l2 = (long)0x5013000000000000;
            long l3 = (long)0x0123456789ABCDEF;
            long l4 = unchecked((long)0xEFCDAB8967452301);

            int i1 = (int)0x1350;
            int i2 = (int)0x50130000;
            int i3 = (int)0x01234567;
            int i4 = (int)0x67452301;
            
            short s1 = (short)0x1350;
            short s2 = (short)0x5013;
            
            Assert.Equal(l2, IPAddress.HostToNetworkOrder(l1));
            Assert.Equal(l4, IPAddress.HostToNetworkOrder(l3));
            Assert.Equal(i2, IPAddress.HostToNetworkOrder(i1));
            Assert.Equal(i4, IPAddress.HostToNetworkOrder(i3));
            Assert.Equal(s2, IPAddress.HostToNetworkOrder(s1));

            Assert.Equal(l1, IPAddress.NetworkToHostOrder(l2));
            Assert.Equal(l3, IPAddress.NetworkToHostOrder(l4));
            Assert.Equal(i1, IPAddress.NetworkToHostOrder(i2));
            Assert.Equal(i3, IPAddress.NetworkToHostOrder(i4));
            Assert.Equal(s1, IPAddress.NetworkToHostOrder(s2));
        }

        [Fact]
        public static void IsLoopback_Get_Success()
        {
            IPAddress ip = IPV4Address1(); //IpV4
            Assert.False(IPAddress.IsLoopback(ip));

            ip = new IPAddress(IPAddress.Loopback.GetAddressBytes()); //IpV4 loopback
            Assert.True(IPAddress.IsLoopback(ip));

            ip = IPV6Address1(); //IpV6
            Assert.False(IPAddress.IsLoopback(ip));

            ip = new IPAddress(IPAddress.IPv6Loopback.GetAddressBytes()); //IpV6 loopback
            Assert.True(IPAddress.IsLoopback(ip));

            ip = new IPAddress(IPAddress.Loopback.MapToIPv6().GetAddressBytes()); // IPv4 loopback mapped to IPv6
            Assert.Equal(!PlatformDetection.IsFullFramework, IPAddress.IsLoopback(ip)); // https://github.com/dotnet/corefx/issues/35454
        }

        [Fact]
        public static void IsLooback_Get_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("address", () => IPAddress.IsLoopback(null));
        }

        [Fact]
        public static void IsIPV6Multicast_Get_Success()
        {
            Assert.True(IPAddress.Parse("ff02::1").IsIPv6Multicast);
            Assert.False(IPAddress.Parse("Fe08::1").IsIPv6Multicast);
            Assert.False(IPV4Address1().IsIPv6Multicast);
        }

        [Fact]
        public static void IsIPV6LinkLocal_Get_Success()
        {
            Assert.True(IPAddress.Parse("fe80::1").IsIPv6LinkLocal);
            Assert.False(IPAddress.Parse("Fe08::1").IsIPv6LinkLocal);
            Assert.False(IPV4Address1().IsIPv6LinkLocal);
        }

        [Fact]
        public static void IsIPV6SiteLocal_Get_Success()
        {
            Assert.True(IPAddress.Parse("FEC0::1").IsIPv6SiteLocal);
            Assert.False(IPAddress.Parse("Fe08::1").IsIPv6SiteLocal);
            Assert.False(IPV4Address1().IsIPv6SiteLocal);
        }

        [Fact]
        public static void IsIPV6Teredo_Get_Success()
        {
            Assert.True(IPAddress.Parse("2001::1").IsIPv6Teredo);
            Assert.False(IPAddress.Parse("Fe08::1").IsIPv6Teredo);
            Assert.False(IPV4Address1().IsIPv6Teredo);
        }

        [Fact]
        public static void Equals_Compare_Success()
        {
            IPAddress ip1 = IPAddress.Parse(IpV4AddressString1); //IpV4
            IPAddress ip2 = IPAddress.Parse(IpV4AddressString1); //IpV4
            IPAddress ip3 = IPAddress.Parse(IpV4AddressString2); //IpV4

            IPAddress ip4 = IPV6Address1(); //IpV6
            IPAddress ip5 = IPV6Address1(); //IpV6
            IPAddress ip6 = IPV6Address2(); //IpV6

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

        [Theory]
        [MemberData(nameof(GetValidIPAddresses))]
        [MemberData(nameof(GeneratedIPAddresses))]
        public static void GetHashCode_ValidIPAddresses_Success(IPAddress ip)
        {
            Assert.Equal(ip.GetHashCode(), ip.GetHashCode());

            var clonedIp = ip.AddressFamily == AddressFamily.InterNetworkV6 ?
                new IPAddress(ip.GetAddressBytes(), ip.ScopeId) :
                new IPAddress(ip.GetAddressBytes());

            Assert.Equal(ip.GetHashCode(), clonedIp.GetHashCode());
        }

        public static IEnumerable<object[]> GetValidIPAddresses()
        {
            return IPAddressParsing.ValidIpv4Addresses
                .Concat(IPAddressParsing.ValidIpv6Addresses)
                .Select(array => new object[] {IPAddress.Parse((string)array[0])});
        }

        public static readonly object[][] GeneratedIPAddresses =
        {
            new object[] {IPAddress.Parse(IpV4AddressString1)},
            new object[] {IPAddress.Parse(IpV6AddressString)},
            new object[] {new IPAddress(MinAddress)},
            new object[] {new IPAddress(MaxAddress)},
            new object[] {new IPAddress(IpV4AddressBytes)},
            new object[] {new IPAddress(IpV6AddressBytes1)},
            new object[] {new IPAddress(IpV6AddressBytes1, MinScopeId)},
        };

#pragma warning disable 618
        [Fact]
        public static void Address_Property_Failure()
        {
            IPAddress ip = IPAddress.Parse("fe80::200:f8ff:fe21:67cf");
            Assert.Throws<SocketException>(() => ip.Address);
        }

        [Fact]
        public static void Address_Property_Success()
        {
            IPAddress ip1 = IPV4Address1();
            IPAddress ip2 = IPV4Address2();
            ip1.Address = ip2.Address;

            Assert.Equal(ip1, ip2);
            Assert.Equal(ip1.GetHashCode(), ip2.GetHashCode());
        }

        [Fact]
        public static void Address_ReadOnlyStatics_Set_Failure()
        {
            Assert.Throws<SocketException>(() => IPAddress.Any.Address = MaxAddress - 1);
            Assert.Throws<SocketException>(() => IPAddress.Broadcast.Address = MaxAddress - 1);
            Assert.Throws<SocketException>(() => IPAddress.Loopback.Address = MaxAddress - 1);
            Assert.Throws<SocketException>(() => IPAddress.None.Address = MaxAddress - 1);
        }
#pragma warning restore 618
    }
}
