// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class IPAddressMappingTest
    {
        [Fact]
        public void IPAddressMapping_IsIPv4MappedToIPv6_Success()
        {
            Assert.True(IPAddress.Parse("::FFFF:0:0").IsIPv4MappedToIPv6);
            Assert.True(IPAddress.Parse("0:0:0:0:0:FFFF::").IsIPv4MappedToIPv6);
            Assert.True(IPAddress.Parse("::FFFF:" + IPAddress.Loopback.ToString()).IsIPv4MappedToIPv6);
            Assert.True(IPAddress.Parse("::FFFF:192.168.1.1").IsIPv4MappedToIPv6);
            Assert.True(IPAddress.Parse("::ffff:192.168.1.1").IsIPv4MappedToIPv6);

            Assert.False(IPAddress.Parse("1::FFFF:0:0").IsIPv4MappedToIPv6);
            Assert.False(IPAddress.Loopback.IsIPv4MappedToIPv6);
            Assert.False(IPAddress.IPv6Loopback.IsIPv4MappedToIPv6);
        }

        [Fact]
        public void IPAddressMapping_MapIPv6ToIPv6_Success()
        {
            IPAddress result = IPAddress.IPv6Loopback.MapToIPv6();
            Assert.Same(result, IPAddress.IPv6Loopback);
            Assert.Equal(result, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void IPAddressMapping_MapIPv4ToIPv4_Success()
        {
            IPAddress result = IPAddress.Loopback.MapToIPv4();
            Assert.Same(result, IPAddress.Loopback);
            Assert.Equal(result, IPAddress.Loopback);
        }

        [Fact]
        public void IPAddressMapping_MapIPv4ToIPv6_Success()
        {
            IPAddress result = IPAddress.Loopback.MapToIPv6();
            Assert.Equal("::ffff:127.0.0.1", result.ToString());
            Assert.Equal(IPAddress.Parse("::ffff:127.0.0.1"), result);

            IPAddress roundTrip = result.MapToIPv4();
            Assert.Equal(IPAddress.Loopback, roundTrip);
        }

        [Fact]
        public void IPAddressMapping_MapIPv6ToIPv4_Success()
        {
            IPAddress result = IPAddress.Parse("::ffff:127.0.0.1").MapToIPv4();
            Assert.Equal(IPAddress.Loopback.ToString(), result.ToString());
            Assert.Equal(IPAddress.Loopback, result);

            IPAddress roundTrip = result.MapToIPv6();
            Assert.Equal(IPAddress.Parse("::ffff:127.0.0.1"), roundTrip);
            Assert.True(roundTrip.IsIPv4MappedToIPv6);

            IPAddress result2 = IPAddress.Parse("2001:4898:0:fff:0:5efe:10.57.74.64").MapToIPv4();
            Assert.Equal(IPAddress.Parse("10.57.74.64"), result2);
        }

        [Fact]
        public void IPAddressMapping_VerifyOriginalBugWhenLastByteofIPv4IsGreaterThan127_Success()
        {
            var originalAddress = "1.65.128.190";
            var initAddress = IPAddress.Parse(originalAddress);
            var ipv6Address = initAddress.MapToIPv6();
            Assert.True(ipv6Address.IsIPv4MappedToIPv6);

            var ipv4Address = ipv6Address.MapToIPv4();

            Assert.Equal(originalAddress, ipv4Address.ToString());
        }

        [Fact]
        public void IPAddressMapping_MapIPv4ToIPv6ToIPv4WhenFirstByteOfIPv4IsGreaterThan127_Success()
        {
            IPAddressMappingHighByteTestHelper("{0}.127.127.127");
        }

        [Fact]
        public void IPAddressMapping_MapIPv4ToIPv6ToIPv4WhenSecondByteOfIPv4IsGreaterThan127_Success()
        {
            IPAddressMappingHighByteTestHelper("127.{0}.127.127");
        }

        [Fact]
        public void IPAddressMapping_MapIPv4ToIPv6ToIPv4WhenThirdByteOfIPv4IsGreaterThan127_Success()
        {
            IPAddressMappingHighByteTestHelper("127.127.{0}.127");
        }

        [Fact]
        public void IPAddressMapping_MapIPv4ToIPv6ToIPv4WhenLastByteOfIPv4IsGreaterThan127_Success()
        {
            IPAddressMappingHighByteTestHelper("127.127.127.{0}");
        }

        private void IPAddressMappingHighByteTestHelper(string ipv4AddressFormat)
        {
            string address;
            IPAddress initialIPv4Address;
            IPAddress ipv6Address;
            IPAddress finalIPv4Address;

            for (var octet = 128; octet < 256; octet++)
            {
                address = string.Format(ipv4AddressFormat, octet);

                initialIPv4Address = IPAddress.Parse(address);
                ipv6Address = initialIPv4Address.MapToIPv6();
                finalIPv4Address = ipv6Address.MapToIPv4();

                Assert.Equal(address, finalIPv4Address.ToString());
            }
        }
    }
}
