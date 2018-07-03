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

        [ActiveIssue(30523, TestPlatforms.AnyUnix)]
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

            Assert.Equal("InterNetwork:64:{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}", sa1.ToString());
            Assert.Equal("InterNetworkV6:48:{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}", sa4.ToString());

            SocketAddress sa5 = new SocketAddress(AddressFamily.InterNetworkV6, 48);
            for (int i = 2; i < sa5.Size; i++)
            {
                sa5[i] = (byte)i;
            }
            Assert.EndsWith("2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47}", sa5.ToString());
        }
    }
}
