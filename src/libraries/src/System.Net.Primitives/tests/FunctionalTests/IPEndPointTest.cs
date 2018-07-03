// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class IPEndPointTest
    {
        private static long testIpAddress = 0x2414188f;
        private static IPAddress testIpV41 = new IPAddress(testIpAddress);
        private static IPAddress testIpV42 = IPAddress.Parse("192.169.0.9");

        private static IPAddress testIpV61 = IPAddress.Parse("0:0:0:0:0:0:0:1");

        [Fact]
        public static void Ctor_LongAddressPort_Success()
        {
            IPEndPoint ep = new IPEndPoint(testIpAddress, 500);
            Assert.Equal(testIpV41, ep.Address);
            Assert.Equal(500, ep.Port);
        }

        [Fact]
        public static void Ctor_LongAddressPort_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new IPEndPoint(null, 500));

            Assert.Throws<ArgumentOutOfRangeException>(() => new IPEndPoint(testIpAddress, IPEndPoint.MinPort - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IPEndPoint(testIpAddress, IPEndPoint.MaxPort + 1));
        }

        [Fact]
        public static void Ctor_IPAddressPort_Success()
        {
            IPEndPoint ep = new IPEndPoint(testIpV41, 700);
            Assert.Equal(testIpV41, ep.Address);
            Assert.Equal(700, ep.Port);

            Assert.Equal(testIpV41.AddressFamily, ep.AddressFamily);
        }

        [Fact]
        public static void Ctor_IPAddressPort_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IPEndPoint(testIpV41, IPEndPoint.MinPort - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IPEndPoint(testIpV41, IPEndPoint.MaxPort + 1));
        }

        [Fact]
        public static void Port_GetSet_Success()
        {
            IPEndPoint ep = new IPEndPoint(testIpAddress, 500);
            ep.Port = 700;
            Assert.Equal(700, ep.Port);
        }

        [Fact]
        public static void Port_Set_Invalid()
        {
            IPEndPoint ep = new IPEndPoint(testIpAddress, 500);
            Assert.Throws<ArgumentOutOfRangeException>(() => ep.Port = IPEndPoint.MinPort - 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => ep.Port = IPEndPoint.MaxPort + 1);
        }

        [Fact]
        public static void Address_GetSet_Success()
        {
            IPEndPoint ep = new IPEndPoint(testIpV41, 500);
            ep.Address = testIpV42;
            Assert.Equal(ep.Address, testIpV42);
        }
        
        [Fact]
        public static void ToString_Compare_Success()
        {
            IPEndPoint ep = new IPEndPoint(testIpV61, 500);
            Assert.Equal(ep.ToString(), string.Format("[{0}]:500", ep.Address.ToString()));

            ep = new IPEndPoint(testIpV42, 500);
            Assert.Equal(ep.ToString(), string.Format("{0}:500", ep.Address.ToString()));
        }

        [Fact]
        public static void SerializeCreate_Compare_Equal()
        { 
            //Serializing an IPEndPoint from a SocketAddress should produce the same output as creating one from a SocketAddress
            IPEndPoint ep = new IPEndPoint(testIpV41, 500);
            SocketAddress sa = ep.Serialize();
            
            EndPoint ep2 = ep.Create(sa);

            Assert.Equal(ep, ep2); 
        }

        [Fact]
        public static void Create_Set_Invalid()
        {
            IPEndPoint ep = new IPEndPoint(testIpV41, 500);
            AssertExtensions.Throws<ArgumentException>("socketAddress", () => ep.Create(new SocketAddress(Sockets.AddressFamily.InterNetworkV6))); //Different address families
            AssertExtensions.Throws<ArgumentException>("socketAddress", () => ep.Create(new SocketAddress(Sockets.AddressFamily.InterNetwork, 7))); //
        }

        [Fact]
        public static void Equals_Compare_Success()
        {
            IPEndPoint ep = new IPEndPoint(testIpV41, 500);
            IPEndPoint ep1 = new IPEndPoint(testIpV41, 500);
            IPEndPoint ep2 = new IPEndPoint(testIpV41, 700);
            IPEndPoint ep3 = new IPEndPoint(IPAddress.Parse("192.168.0.9"), 700);

            Assert.False(ep.Equals(null));

            Assert.True(ep.Equals(ep1));
            Assert.True(ep.GetHashCode().Equals(ep1.GetHashCode()));

            Assert.True(ep1.Equals(ep));

            Assert.False(ep.Equals(ep2));
            Assert.False(ep.GetHashCode().Equals(ep2.GetHashCode()));

            Assert.False(ep2.Equals(ep3));
            Assert.False(ep2.GetHashCode().Equals(ep3.GetHashCode()));
        }
    }
}
