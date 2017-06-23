// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class DnsEndPointTest
    {
        [Fact]
        public static void Ctor_HostPort_Success()
        {
            DnsEndPoint ep = new DnsEndPoint("host", 500);
            Assert.Equal("host", ep.Host);
            Assert.Equal(500, ep.Port);
            Assert.Equal(AddressFamily.Unspecified, ep.AddressFamily);
        }

        [Fact]
        public static void Ctor_HostPort_AddressFamily_Success()
        {
            DnsEndPoint ep = new DnsEndPoint("host", 500, AddressFamily.InterNetwork);
            Assert.Equal("host", ep.Host);
            Assert.Equal(500, ep.Port);
            Assert.Equal(AddressFamily.InterNetwork, ep.AddressFamily);
        }

        [Fact]
        public static void Ctor_HostPortAddressFamily_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new DnsEndPoint(null, 500, AddressFamily.InterNetwork)); //Null host
            AssertExtensions.Throws<ArgumentException>(null, () => new DnsEndPoint("", 500, AddressFamily.InterNetwork)); //Empty host

            Assert.Throws<ArgumentOutOfRangeException>(() => new DnsEndPoint("host", IPEndPoint.MinPort - 1, AddressFamily.InterNetwork)); //Port < min port (0)
            Assert.Throws<ArgumentOutOfRangeException>(() => new DnsEndPoint("host", IPEndPoint.MaxPort + 1, AddressFamily.InterNetwork)); //Port > max port (65535)

            AssertExtensions.Throws<ArgumentException>("addressFamily", () => new DnsEndPoint("host", 500, AddressFamily.AppleTalk
                )); //Invalid address family
        }

        [Fact]
        public static void Equals_Compare_Success()
        {
            DnsEndPoint ep1 = new DnsEndPoint("name", 500, AddressFamily.InterNetwork);
            DnsEndPoint ep2 = new DnsEndPoint("name", 500, AddressFamily.InterNetwork);
            DnsEndPoint ep3 = new DnsEndPoint("name", 700, AddressFamily.InterNetwork);
            DnsEndPoint ep4 = new DnsEndPoint("name", 500, AddressFamily.InterNetworkV6);

            Assert.NotEqual(ep1, null);
            Assert.False(ep1.Equals("string"));

            Assert.Equal(ep1, ep2);
            Assert.Equal(ep2, ep1);
            Assert.Equal(ep1.GetHashCode(), ep2.GetHashCode());

            Assert.NotEqual(ep1, ep3);
            Assert.NotEqual(ep1.GetHashCode(), ep3.GetHashCode());

            Assert.NotEqual(ep1, ep4);
            Assert.NotEqual(ep1.GetHashCode(), ep4.GetHashCode());
        }
    }
}
