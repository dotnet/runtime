// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class SocketAddressTest
    {
        [Fact]
        public static void Ctor_AddressFamily_Success()
        {
            SocketAddress sa = new SocketAddress(AddressFamily.InterNetwork);
            Assert.Equal(AddressFamily.InterNetwork, sa.Family);
            Assert.Equal(32, sa.Size);
        }

        [Fact]
        public static void Ctor_AddressFamilySize_Success()
        {
            SocketAddress sa = new SocketAddress(AddressFamily.InterNetwork, 64);
            Assert.Equal(AddressFamily.InterNetwork, sa.Family);
            Assert.Equal(64, sa.Size);
        }

        [Fact]
        public static void Ctor_AddressFamilySize_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SocketAddress(AddressFamily.InterNetwork, 1)); //Size < MinSize (32)
        }

        [Fact]
        public static void Equals_Compare_Success()
        {
            SocketAddress sa1 = new SocketAddress(AddressFamily.InterNetwork, 64);
            SocketAddress sa2 = new SocketAddress(AddressFamily.InterNetwork, 64);
            SocketAddress sa3 = new SocketAddress(AddressFamily.InterNetworkV6, 64);
            SocketAddress sa4 = new SocketAddress(AddressFamily.InterNetwork, 60000);

            Assert.False(sa1.Equals(null));
            Assert.False(sa1.Equals(""));

            Assert.Equal(sa1, sa2);
            Assert.Equal(sa2, sa1);
            Assert.Equal(sa1.GetHashCode(), sa2.GetHashCode());

            Assert.NotEqual(sa1, sa3);
            Assert.NotEqual(sa1.GetHashCode(), sa3.GetHashCode());

            Assert.NotEqual(sa1, sa4);
        }

        [Fact]
        public static void ToString_Compare_Success()
        {
            SocketAddress sa1 = new SocketAddress(AddressFamily.InterNetwork, 64);
            SocketAddress sa2 = new SocketAddress(AddressFamily.InterNetwork, 64);
            SocketAddress sa3 = new SocketAddress(AddressFamily.InterNetwork, 48);

            SocketAddress sa4 = new SocketAddress(AddressFamily.InterNetworkV6, 48);

            Assert.Equal(sa1.ToString(), sa2.ToString());
            Assert.NotEqual(sa1.ToString(), sa3.ToString());
            Assert.NotEqual(sa1.ToString(), sa4.ToString());
        }
    }
}
