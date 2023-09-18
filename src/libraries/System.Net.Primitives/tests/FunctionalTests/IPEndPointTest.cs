// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Sockets;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class IPEndPointTest
    {
        [Theory]
        [InlineData(0, IPEndPoint.MinPort)]
        [InlineData(1, 10)]
        [InlineData(0x00000000FFFFFFFF, IPEndPoint.MaxPort)]
        public static void Ctor_Long_Int(long address, int port)
        {
            var endPoint = new IPEndPoint(address, port);
            Assert.Equal(new IPAddress(address), endPoint.Address);
            Assert.Equal(AddressFamily.InterNetwork, endPoint.AddressFamily);
            Assert.Equal(port, endPoint.Port);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0x100000000)]
        public static void Ctor_InvalidAddress_ThrowsArgumentOutOfRangeException(long address)
        {
            Assert.Throws<ArgumentOutOfRangeException>("newAddress", () => new IPEndPoint(address, 500));
        }

        public static IEnumerable<object[]> Ctor_IPAddress_Int_TestData()
        {
            yield return new object[] { new IPAddress(1), IPEndPoint.MinPort };
            yield return new object[] { IPAddress.Parse("192.169.0.9"), 10 };
            yield return new object[] { IPAddress.Parse("0:0:0:0:0:0:0:1"), IPEndPoint.MaxPort };
        }

        [Theory]
        [MemberData(nameof(Ctor_IPAddress_Int_TestData))]
        public static void Ctor_IPAddress_Int(IPAddress address, int port)
        {
            var endPoint = new IPEndPoint(address, port);
            Assert.Same(address, endPoint.Address);
            Assert.Equal(address.AddressFamily, endPoint.AddressFamily);
            Assert.Equal(port, endPoint.Port);
        }

        [Fact]
        public static void Ctor_NullAddress_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("address", () => new IPEndPoint(null, 500));
        }

        [Theory]
        [InlineData(IPEndPoint.MinPort - 1)]
        [InlineData(IPEndPoint.MaxPort + 1)]
        public static void Ctor_InvalidPort_ThrowsArgumentOutOfRangeException(int port)
        {
            Assert.Throws<ArgumentOutOfRangeException>("port", () => new IPEndPoint(1, port));
            Assert.Throws<ArgumentOutOfRangeException>("port", () => new IPEndPoint(new IPAddress(1), port));
        }

        [Fact]
        public static void MinPort_Get_ReturnsExpected()
        {
            Assert.Equal(0, IPEndPoint.MinPort);
        }

        [Fact]
        public static void MaxPort_Get_ReturnsExpected()
        {
            Assert.Equal(65535, IPEndPoint.MaxPort);
        }

        [Theory]
        [InlineData(IPEndPoint.MinPort)]
        [InlineData(10)]
        [InlineData(IPEndPoint.MaxPort)]
        public static void Port_Set_GetReturnsExpected(int value)
        {
            var endPoint = new IPEndPoint(1, 500)
            {
                Port = value
            };
            Assert.Equal(value, endPoint.Port);

            // Set same.
            endPoint.Port = value;
            Assert.Equal(value, endPoint.Port);
        }

        [Theory]
        [InlineData(IPEndPoint.MinPort - 1)]
        [InlineData(IPEndPoint.MaxPort + 1)]
        public static void Port_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
        {
            var endPoint = new IPEndPoint(1, 500);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => endPoint.Port = value);
        }

        public static IEnumerable<object[]> Address_Set_TestData()
        {
            yield return new object[] { new IPAddress(2) };
            yield return new object[] { IPAddress.Parse("192.169.0.9") };
            yield return new object[] { IPAddress.Parse("0:0:0:0:0:0:0:1") };
        }

        [Theory]
        [MemberData(nameof(Address_Set_TestData))]
        public static void Address_Set_GetReturnsExpected(IPAddress value)
        {
            var endPoint = new IPEndPoint(1, 500)
            {
                Address = value
            };
            Assert.Same(value, endPoint.Address);
            Assert.Equal(value.AddressFamily, endPoint.AddressFamily);

            // Set same.
            Assert.Same(value, endPoint.Address);
            Assert.Equal(value.AddressFamily, endPoint.AddressFamily);
        }

        [Fact]
        public static void Address_SetNull_ThrowsArgumentNullException()
        {
            var endPoint = new IPEndPoint(1, 500);
            Assert.Throws<ArgumentNullException>("value", () => endPoint.Address = null);
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new IPEndPoint(IPAddress.HostToNetworkOrder(0x02000000), 500), "2.0.0.0:500" };
            yield return new object[] { new IPEndPoint(IPAddress.Parse("192.169.0.9"), 500), "192.169.0.9:500" };
            yield return new object[] { new IPEndPoint(IPAddress.Parse("0:0:0:0:0:0:0:1"), 500), "[::1]:500" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString_Invoke_ReturnsExpected(IPEndPoint endPoint, string expected)
        {
            Assert.Equal(expected, endPoint.ToString());
        }

        [Fact]
        public static void Create_DifferentAF_Success()
        {
            SocketAddress sa = new SocketAddress(AddressFamily.InterNetwork, SocketAddress.GetMaximumAddressSize(AddressFamily.InterNetworkV6));
            var ep = new IPEndPoint(IPAddress.IPv6Any, 0);
            Assert.NotNull(ep.Create(sa));

            sa = new SocketAddress(AddressFamily.InterNetworkV6);
            ep = new IPEndPoint(IPAddress.Any, 0);

            Assert.NotNull(ep.Create(sa));
        }

        public static IEnumerable<object[]> Serialize_TestData()
        {
            yield return new object[] { new IPAddress(2), 16 };
            yield return new object[] { IPAddress.Parse("192.169.0.9"), 16 };
            yield return new object[] { IPAddress.Parse("0:0:0:0:0:0:0:1"), 28 };
        }

        [Theory]
        [MemberData(nameof(Serialize_TestData))]
        public static void Serialize_Create_ReturnsEqual(IPAddress address, int expectedSize)
        {
            var endPoint = new IPEndPoint(address, 500);
            SocketAddress serializedAddress = endPoint.Serialize();
            Assert.Equal(address.AddressFamily, serializedAddress.Family);
            Assert.Equal(expectedSize, serializedAddress.Size);

            IPEndPoint createdEndPoint = Assert.IsType<IPEndPoint>(endPoint.Create(serializedAddress));
            Assert.NotSame(endPoint, createdEndPoint);
            Assert.Equal(endPoint, createdEndPoint);
        }

        public static IEnumerable<object[]> Create_DefaultAddress_TestData()
        {
            yield return new object[] { IPAddress.Parse("192.169.0.9"), 16, IPAddress.Any };
            yield return new object[] { IPAddress.Parse("192.169.0.9"), 32, IPAddress.Any };
            yield return new object[] { IPAddress.Parse("0:0:0:0:0:0:0:1"), 28, IPAddress.IPv6Any };
            yield return new object[] { IPAddress.Parse("0:0:0:0:0:0:0:1"), 32, IPAddress.IPv6Any };
        }

        [Theory]
        [MemberData(nameof(Create_DefaultAddress_TestData))]
        public static void Create_DefaultAddress_Success(IPAddress address, int size, IPAddress expectedAddress)
        {
            var endPoint = new IPEndPoint(address, 500);
            var socketAddress = new SocketAddress(address.AddressFamily, size);

            IPEndPoint createdEndPoint = Assert.IsType<IPEndPoint>(endPoint.Create(socketAddress));
            Assert.NotSame(endPoint, createdEndPoint);
            Assert.Equal(expectedAddress, createdEndPoint.Address);
            Assert.Equal(expectedAddress.AddressFamily, createdEndPoint.AddressFamily);
            Assert.Equal(0, createdEndPoint.Port);
        }

        [Fact]
        public static void Create_NullSocketAddress_ThrowsArgumentNullException()
        {
            var endPoint = new IPEndPoint(1, 500);
            Assert.Throws<ArgumentNullException>("socketAddress", () => endPoint.Create(null));
        }

        public static IEnumerable<object[]> Create_InvalidAddressFamily_TestData()
        {
            yield return new object[] { new IPEndPoint(2, 500), new SocketAddress(Sockets.AddressFamily.Unknown) };
            yield return new object[] { new IPEndPoint(IPAddress.Parse("0:0:0:0:0:0:0:1"), 500), new SocketAddress(Sockets.AddressFamily.InterNetwork) };
        }

        [Theory]
        [MemberData(nameof(Create_InvalidAddressFamily_TestData))]
        public static void Create_InvalidAddressFamily_ThrowsArgumentException(IPEndPoint endPoint, SocketAddress socketAddress)
        {
            AssertExtensions.Throws<ArgumentException>("socketAddress", () => endPoint.Create(socketAddress));
        }

        public static IEnumerable<object[]> Create_InvalidSize_TestData()
        {
            yield return new object[] { IPAddress.Parse("192.169.0.9"), 15 };
            yield return new object[] { IPAddress.Parse("192.169.0.9"), 7 };
            yield return new object[] { IPAddress.Parse("192.169.0.9"), 2 };
            yield return new object[] { IPAddress.Parse("0:0:0:0:0:0:0:1"), 27 };
            yield return new object[] { IPAddress.Parse("0:0:0:0:0:0:0:1"), 7 };
            yield return new object[] { IPAddress.Parse("0:0:0:0:0:0:0:1"), 2 };
        }

        [Theory]
        [MemberData(nameof(Create_InvalidSize_TestData))]
        public static void Create_InvalidSize_ThrowsArgumentException(IPAddress address, int size)
        {
            var endPoint = new IPEndPoint(address, 500);
            var socketAddress = new SocketAddress(Sockets.AddressFamily.InterNetwork, size);
            AssertExtensions.Throws<ArgumentException>("socketAddress", () => endPoint.Create(socketAddress));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var endPoint = new IPEndPoint(1, 500);
            yield return new object[] { endPoint, endPoint, true };
            yield return new object[] { endPoint, new IPEndPoint(1, 500), true };
            yield return new object[] { endPoint, new IPEndPoint(2, 500), false };
            yield return new object[] { endPoint, new IPEndPoint(1, 5001), false };

            yield return new object[] { endPoint, new object(), false };
            yield return new object[] { endPoint, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals_Invoke_ReturnsExpected(IPEndPoint endPoint, object obj, bool expected)
        {
            Assert.Equal(expected, endPoint.Equals(obj));
            if (obj is IPEndPoint)
            {
                Assert.Equal(expected, endPoint.GetHashCode().Equals(obj.GetHashCode()));
            }
        }
    }
}
