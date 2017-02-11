// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

using Xunit;

namespace System.Net.Primitives.PalTests
{
    public unsafe class IPAddressPalTests
    {
        [Fact]
        public void IPv4StringToAddress_Valid()
        {
            const string AddressString = "127.0.64.255";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv4AddressBytes];
            ushort port;
            uint err = IPAddressPal.Ipv4StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv4AddressBytes, out port);
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            Assert.Equal(127, bytes[0]);
            Assert.Equal(0, bytes[1]);
            Assert.Equal(64, bytes[2]);
            Assert.Equal(255, bytes[3]);
            Assert.Equal(0, port);
        }

        [Fact]
        public void IPv4StringToAddress_Valid_ClassB()
        {
            const string AddressString = "128.64.256";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv4AddressBytes];
            ushort port;
            uint err = IPAddressPal.Ipv4StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv4AddressBytes, out port);
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            Assert.Equal(128, bytes[0]);
            Assert.Equal(64, bytes[1]);
            Assert.Equal(1, bytes[2]);
            Assert.Equal(0, bytes[3]);
            Assert.Equal(0, port);
        }

        [Fact]
        public void IPv4StringToAddress_Valid_ClassC()
        {
            const string AddressString = "192.65536";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv4AddressBytes];
            ushort port;
            uint err = IPAddressPal.Ipv4StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv4AddressBytes, out port);
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            Assert.Equal(192, bytes[0]);
            Assert.Equal(1, bytes[1]);
            Assert.Equal(0, bytes[2]);
            Assert.Equal(0, bytes[3]);
            Assert.Equal(0, port);
        }

        [Fact]
        public void IPv4StringToAddress_Valid_ClassA()
        {
            const string AddressString = "2130706433";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv4AddressBytes];
            ushort port;
            uint err = IPAddressPal.Ipv4StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv4AddressBytes, out port);
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            Assert.Equal(127, bytes[0]);
            Assert.Equal(0, bytes[1]);
            Assert.Equal(0, bytes[2]);
            Assert.Equal(1, bytes[3]);
            Assert.Equal(0, port);
        }

        [Fact]
        public void IPv4StringToAddress_Invalid_Empty()
        {
            const string AddressString = "";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv4AddressBytes];
            ushort port;
            uint err = IPAddressPal.Ipv4StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv4AddressBytes, out port);
            Assert.NotEqual(err, IPAddressPal.SuccessErrorCode);
        }

        [Fact]
        public void IPv4StringToAddress_Invalid_NotAnAddress()
        {
            const string AddressString = "hello, world";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv4AddressBytes];
            ushort port;
            uint err = IPAddressPal.Ipv4StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv4AddressBytes, out port);
            Assert.NotEqual(err, IPAddressPal.SuccessErrorCode);
        }

        [Fact]
        public void IPv4StringToAddress_Invalid_Port()
        {
            const string AddressString = "127.0.64.255:80";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv4AddressBytes];
            ushort port;
            uint err = IPAddressPal.Ipv4StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv4AddressBytes, out port);
            Assert.True(err != IPAddressPal.SuccessErrorCode || port != 0);
        }

        public static object[][] ValidIPv4Addresses = new object[][] {
            new object[] { new byte[] { 0, 0, 0, 0 }, "0.0.0.0" },
            new object[] { new byte[] { 127, 0, 64, 255 }, "127.0.64.255" },
            new object[] { new byte[] { 128, 64, 1, 0 }, "128.64.1.0" },
            new object[] { new byte[] { 192, 0, 0, 1 }, "192.0.0.1" },
            new object[] { new byte[] { 255, 255, 255, 255 }, "255.255.255.255" }
        };

        [Theory, MemberData(nameof(ValidIPv4Addresses))]
        public void IPv4AddressToString_Valid(byte[] bytes, string addressString)
        {
            var buffer = new StringBuilder(IPAddressParser.INET_ADDRSTRLEN);
            uint err = IPAddressPal.Ipv4AddressToString(bytes, buffer);
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            Assert.Equal(addressString, buffer.ToString());
        }

        [Theory, MemberData(nameof(ValidIPv4Addresses))]
        public void IPv4AddressToString_RoundTrip(byte[] bytes, string addressString)
        {
            var buffer = new StringBuilder(IPAddressParser.INET_ADDRSTRLEN);
            uint err = IPAddressPal.Ipv4AddressToString(bytes, buffer);
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);

            var actualAddressString = buffer.ToString();
            Assert.Equal(addressString, actualAddressString);

            var roundTrippedBytes = stackalloc byte[IPAddressParserStatics.IPv4AddressBytes];
            ushort port;
            err = IPAddressPal.Ipv4StringToAddress(actualAddressString, roundTrippedBytes, IPAddressParserStatics.IPv4AddressBytes, out port);
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            Assert.Equal(bytes[0], roundTrippedBytes[0]);
            Assert.Equal(bytes[1], roundTrippedBytes[1]);
            Assert.Equal(bytes[2], roundTrippedBytes[2]);
            Assert.Equal(bytes[3], roundTrippedBytes[3]);
            Assert.Equal(0, port);
        }

        [Theory, MemberData(nameof(ValidIPv4Addresses))]
        public void IPv4StringToAddress_RoundTrip(byte[] bytes, string addressString)
        {
            var actualBytesArr = new byte[IPAddressParserStatics.IPv4AddressBytes];
            fixed (byte* actualBytes = actualBytesArr)
            {
                ushort port;
                uint err = IPAddressPal.Ipv4StringToAddress(addressString, actualBytes, IPAddressParserStatics.IPv4AddressBytes, out port);
                Assert.Equal(IPAddressPal.SuccessErrorCode, err);
                Assert.Equal(bytes[0], actualBytes[0]);
                Assert.Equal(bytes[1], actualBytes[1]);
                Assert.Equal(bytes[2], actualBytes[2]);
                Assert.Equal(bytes[3], actualBytes[3]);
                Assert.Equal(0, port);

                var buffer = new StringBuilder(IPAddressParser.INET_ADDRSTRLEN);
                err = IPAddressPal.Ipv4AddressToString(actualBytesArr, buffer);
                Assert.Equal(IPAddressPal.SuccessErrorCode, err);

                var roundTrippedAddressString = buffer.ToString();
                Assert.Equal(addressString, roundTrippedAddressString);
            }
        }

        [Fact]
        public void IPv6StringToAddress_Valid_Localhost()
        {
            const string AddressString = "::1";
            var expectedBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };

            var bytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
            Assert.Equal(bytes.Length, expectedBytes.Length);

            uint scope;
            uint err;
            fixed (byte* bytesPtr = bytes)
            {
                err = IPAddressPal.Ipv6StringToAddress(AddressString, bytesPtr, bytes.Length, out scope);
            }
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                Assert.Equal(expectedBytes[i], bytes[i]);
            }
            Assert.Equal(0, (int)scope);
        }

        [Fact]
        public void IPv6StringToAddress_Valid()
        {
            const string AddressString = "2001:db8:aaaa:bbbb:cccc:dddd:eeee:1";
            var expectedBytes = new byte[] { 0x20, 0x01, 0x0d, 0x0b8, 0xaa, 0xaa, 0xbb, 0xbb, 0xcc, 0xcc, 0xdd, 0xdd, 0xee, 0xee, 0x00, 0x01 };

            var bytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
            Assert.Equal(bytes.Length, expectedBytes.Length);

            uint scope;
            uint err;
            fixed (byte* bytesPtr = bytes)
            {
                err = IPAddressPal.Ipv6StringToAddress(AddressString, bytesPtr, bytes.Length, out scope);
            }
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                Assert.Equal(expectedBytes[i], bytes[i]);
            }
            Assert.Equal(0, (int)scope);
        }

        [Fact]
        public void IPv6StringToAddress_Valid_Bracketed()
        {
            const string AddressString = "[2001:db8:aaaa:bbbb:cccc:dddd:eeee:1]";
            var expectedBytes = new byte[] { 0x20, 0x01, 0x0d, 0x0b8, 0xaa, 0xaa, 0xbb, 0xbb, 0xcc, 0xcc, 0xdd, 0xdd, 0xee, 0xee, 0x00, 0x01 };

            var bytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
            Assert.Equal(bytes.Length, expectedBytes.Length);

            uint scope;
            uint err;
            fixed (byte* bytesPtr = bytes)
            {
                err = IPAddressPal.Ipv6StringToAddress(AddressString, bytesPtr, bytes.Length, out scope);
            }
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                Assert.Equal(expectedBytes[i], bytes[i]);
            }
            Assert.Equal(0, (int)scope);
        }

        [Fact]
        public void IPv6StringToAddress_Valid_Port()
        {
            const string AddressString = "[2001:db8:aaaa:bbbb:cccc:dddd:eeee:1]:80";
            var expectedBytes = new byte[] { 0x20, 0x01, 0x0d, 0x0b8, 0xaa, 0xaa, 0xbb, 0xbb, 0xcc, 0xcc, 0xdd, 0xdd, 0xee, 0xee, 0x00, 0x01 };

            var bytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
            Assert.Equal(bytes.Length, expectedBytes.Length);

            uint scope;
            uint err;
            fixed (byte* bytesPtr = bytes)
            {
                err = IPAddressPal.Ipv6StringToAddress(AddressString, bytesPtr, bytes.Length, out scope);
            }
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                Assert.Equal(expectedBytes[i], bytes[i]);
            }
            Assert.Equal(0, (int)scope);
        }

        [Fact]
        public void IPv6StringToAddress_Valid_Port_2()
        {
            const string AddressString = "[2001:db8:aaaa:bbbb:cccc:dddd:eeee:1]:";
            var expectedBytes = new byte[] { 0x20, 0x01, 0x0d, 0x0b8, 0xaa, 0xaa, 0xbb, 0xbb, 0xcc, 0xcc, 0xdd, 0xdd, 0xee, 0xee, 0x00, 0x01 };

            var bytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
            Assert.Equal(bytes.Length, expectedBytes.Length);

            uint scope;
            uint err;
            fixed (byte* bytesPtr = bytes)
            {
                err = IPAddressPal.Ipv6StringToAddress(AddressString, bytesPtr, bytes.Length, out scope);
            }
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                Assert.Equal(expectedBytes[i], bytes[i]);
            }
            Assert.Equal(0, (int)scope);
        }

        [Fact]
        public void IPv6StringToAddress_Valid_Compressed()
        {
            const string AddressString = "2001:db8::1";
            var expectedBytes = new byte[] { 0x20, 0x01, 0x0d, 0x0b8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

            var bytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
            Assert.Equal(bytes.Length, expectedBytes.Length);

            uint scope;
            uint err;
            fixed (byte* bytesPtr = bytes)
            {
                err = IPAddressPal.Ipv6StringToAddress(AddressString, bytesPtr, bytes.Length, out scope);
            }
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                Assert.Equal(expectedBytes[i], bytes[i]);
            }
            Assert.Equal(0, (int)scope);
        }

        [Fact]
        public void IPv6StringToAddress_Valid_IPv4Compatible()
        {
            const string AddressString = "::ffff:222.1.41.90";
            var expectedBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 222, 1, 41, 90 };

            var bytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
            Assert.Equal(bytes.Length, expectedBytes.Length);

            uint scope;
            uint err;
            fixed (byte* bytesPtr = bytes)
            {
                err = IPAddressPal.Ipv6StringToAddress(AddressString, bytesPtr, bytes.Length, out scope);
            }
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                Assert.Equal(expectedBytes[i], bytes[i]);
            }
            Assert.Equal(0, (int)scope);
        }

        [Fact]
        public void IPv6StringToAddress_Invalid_Empty()
        {
            const string AddressString = "";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
            uint scope;
            uint err = IPAddressPal.Ipv6StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv6AddressBytes, out scope);
            Assert.NotEqual(err, IPAddressPal.SuccessErrorCode);
        }

        [Fact]
        public void IPv6StringToAddress_Invalid_NotAnAddress()
        {
            const string AddressString = "hello, world";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
            uint scope;
            uint err = IPAddressPal.Ipv6StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv6AddressBytes, out scope);
            Assert.NotEqual(err, IPAddressPal.SuccessErrorCode);
        }

        [Fact]
        public void IPv6StringToAddress_Invalid_Port()
        {
            const string AddressString = "[2001:db8::1]:xx";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
            uint scope;
            uint err = IPAddressPal.Ipv6StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv6AddressBytes, out scope);
            Assert.NotEqual(err, IPAddressPal.SuccessErrorCode);
        }

        [Fact]
        public void IPv6StringToAddress_Invalid_Compression()
        {
            const string AddressString = "2001::db8::1";

            var bytes = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
            uint scope;
            uint err = IPAddressPal.Ipv6StringToAddress(AddressString, bytes, IPAddressParserStatics.IPv6AddressBytes, out scope);
            Assert.NotEqual(err, IPAddressPal.SuccessErrorCode);
        }

        public static object[][] ValidIPv6Addresses = new object[][] {
            new object[] {
                new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
                "::1"
            },
            new object[] {
                new byte[] { 0x20, 0x01, 0x0d, 0x0b8, 0xaa, 0xaa, 0xbb, 0xbb, 0xcc, 0xcc, 0xdd, 0xdd, 0xee, 0xee, 0x00, 0x01 },
                "2001:db8:aaaa:bbbb:cccc:dddd:eeee:1"
            },
            new object[] {
                new byte[] { 0x20, 0x01, 0x0d, 0x0b8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 },
                "2001:db8::1"
            },
            new object[] {
                new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 222, 1, 41, 90 },
                "::ffff:222.1.41.90"
            }
        };

        [Theory, MemberData(nameof(ValidIPv6Addresses))]
        public void IPv6AddressToString_Valid(byte[] bytes, string addressString)
        {
            var buffer = new StringBuilder(IPAddressParser.INET6_ADDRSTRLEN);
            uint err = IPAddressPal.Ipv6AddressToString(bytes, 0, buffer);
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            Assert.Equal(addressString, buffer.ToString());
        }

        [Theory, MemberData(nameof(ValidIPv6Addresses))]
        public void IPv6AddressToString_RoundTrip(byte[] bytes, string addressString)
        {
            var buffer = new StringBuilder(IPAddressParser.INET6_ADDRSTRLEN);
            uint err = IPAddressPal.Ipv6AddressToString(bytes, 0, buffer);
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);

            var actualAddressString = buffer.ToString();
            Assert.Equal(addressString, actualAddressString);

            var roundTrippedBytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
            uint scope;
            fixed (byte* roundTrippedBytesPtr = roundTrippedBytes)
            {
                err = IPAddressPal.Ipv6StringToAddress(actualAddressString, roundTrippedBytesPtr, IPAddressParserStatics.IPv6AddressBytes, out scope);
            }
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            for (int i = 0; i < bytes.Length; i++)
            {
                Assert.Equal(bytes[i], roundTrippedBytes[i]);
            }
            Assert.Equal(0, (int)scope);
        }

        [Theory, MemberData(nameof(ValidIPv6Addresses))]
        public void IPv6StringToAddress_RoundTrip(byte[] bytes, string addressString)
        {
            var actualBytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
            uint scope;
            uint err;
            fixed (byte* actualBytesPtr = actualBytes)
            {
                err = IPAddressPal.Ipv6StringToAddress(addressString, actualBytesPtr, actualBytes.Length, out scope);
            }
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);
            for (int i = 0; i < bytes.Length; i++)
            {
                Assert.Equal(bytes[i], actualBytes[i]);
            }
            Assert.Equal(0, (int)scope);

            var buffer = new StringBuilder(IPAddressParser.INET6_ADDRSTRLEN);
            err = IPAddressPal.Ipv6AddressToString(actualBytes, 0, buffer);
            Assert.Equal(IPAddressPal.SuccessErrorCode, err);

            var roundTrippedAddressString = buffer.ToString();
            Assert.Equal(addressString, roundTrippedAddressString);
        }
    }
}
